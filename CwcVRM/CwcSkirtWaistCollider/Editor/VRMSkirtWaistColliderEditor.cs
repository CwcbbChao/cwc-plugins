using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UniVRM10;

namespace CwcVRM.CwcSkirtWaistCollider.Editor
{
    /// <summary>
    /// VRM 1.0 角色裙子腰部限位碰撞体一键生成工具
    /// 用于在角色 Hips 下生成一圈环形碰撞体，并绑定到裙子弹簧链上，以防止抬腿或坐下时裙子过度翻折翘起。
    /// </summary>
    public class VRMSkirtWaistColliderEditor : EditorWindow
    {
        #region 1. 常量与静态字段

        private const string MenuPath = "Tools/CwcPlugins/VRM 裙子腰部碰撞体一键生成器";

        #endregion

        #region 3. 非序列化私有字段

        private Vrm10Instance _vrmInstance;
        private Transform _hipsTransform;

        // 碰撞体环物理分布参数
        private int _colliderCount = 8;
        private float _ringRadius = 0.13f;
        private float _widthScale = 1.1f;
        private float _depthScale = 1.0f;
        private float _heightOffset = -0.08f;
        private float _forwardOffset = -0.01f;
        private float _colliderRadius = 0.05f;

        // UI 状态与数据缓存
        private List<SpringSelectionInfo> _springInfos = new List<SpringSelectionInfo>();
        private Vector2 _scrollPosition;
        private bool _showPreview = true;

        #endregion

        #region 5. 生命周期方法

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<VRMSkirtWaistColliderEditor>("裙子腰部碰撞体生成器");
            window.minSize = new Vector2(420, 520);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            DetectSelectedVRM();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSelectionChange()
        {
            DetectSelectedVRM();
            Repaint();
        }

        private void OnGUI()
        {
            DrawHeaderGUI();

            if (_vrmInstance == null)
            {
                DrawNullInstanceWarningGUI();
                return;
            }

            DrawConfigPanelGUI();
            DrawSpringListGUI();
            DrawActionButtonsGUI();
        }

        #endregion

        #region 7. 私有方法

        /// <summary>
        /// 自动检测并加载当前选中的 VRM 1.0 角色实例
        /// </summary>
        private void DetectSelectedVRM()
        {
            var activeGo = Selection.activeGameObject;
            if (activeGo == null)
            {
                ResetCache();
                return;
            }

            var instance = activeGo.GetComponentInParent<Vrm10Instance>();
            if (instance == null)
            {
                ResetCache();
                return;
            }

            // 避免重复加载相同实例
            if (_vrmInstance == instance)
            {
                return;
            }

            _vrmInstance = instance;
            
            // 获取 Hips 骨骼
            if (!_vrmInstance.TryGetBoneTransform(HumanBodyBones.Hips, out _hipsTransform))
            {
                // 如果标准 Humanoid 获取不到，则尝试直接寻找名字包含 hips 的子节点
                var animator = _vrmInstance.GetComponent<Animator>();
                if (animator != null)
                {
                    _hipsTransform = animator.GetBoneTransform(HumanBodyBones.Hips);
                }
                
                if (_hipsTransform == null)
                {
                    _hipsTransform = FindDeepChild(_vrmInstance.transform, "hips");
                }
            }

            // 重新初始化 Spring 链列表
            InitializeSpringInfos();
        }

        /// <summary>
        /// 重置数据缓存
        /// </summary>
        private void ResetCache()
        {
            _vrmInstance = null;
            _hipsTransform = null;
            _springInfos.Clear();
        }

        /// <summary>
        /// 初始化弹簧链列表，并基于命名规则自动勾选裙子相关的弹簧链
        /// </summary>
        private void InitializeSpringInfos()
        {
            _springInfos.Clear();
            if (_vrmInstance == null || _vrmInstance.SpringBone == null)
            {
                return;
            }

            foreach (var spring in _vrmInstance.SpringBone.Springs)
            {
                if (spring == null) continue;

                bool shouldRecommend = IsSkirtSpring(spring);
                _springInfos.Add(new SpringSelectionInfo
                {
                    IsSelected = shouldRecommend,
                    Spring = spring,
                    Name = string.IsNullOrEmpty(spring.Name) ? "未命名弹簧链" : spring.Name
                });
            }
        }

        /// <summary>
        /// 判断弹簧链是否为裙子弹簧链（根据骨骼名或链名）
        /// </summary>
        private bool IsSkirtSpring(Vrm10InstanceSpringBone.Spring spring)
        {
            var keywords = new[] { "skirt", "裙", "suso", "dress", "下摆" };
            
            // 检查 Spring 链自身的名称
            if (!string.IsNullOrEmpty(spring.Name))
            {
                var lowerName = spring.Name.ToLower();
                if (keywords.Any(k => lowerName.Contains(k)))
                {
                    return true;
                }
            }

            // 检查 Spring 链下的骨骼节点名称
            if (spring.Joints != null && spring.Joints.Count > 0)
            {
                foreach (var joint in spring.Joints)
                {
                    if (joint != null && joint.transform != null)
                    {
                        var lowerBoneName = joint.transform.name.ToLower();
                        if (keywords.Any(k => lowerBoneName.Contains(k)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 在场景视图中绘制限位碰撞体环的实时预览
        /// </summary>
        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_showPreview || _vrmInstance == null || _hipsTransform == null)
            {
                return;
            }

            // 1. 绘制水平椭圆基准环
            Vector3 centerLocal = new Vector3(0, _heightOffset, _forwardOffset);
            Vector3 centerWorld = _hipsTransform.TransformPoint(centerLocal);

            Matrix4x4 ringMatrix = Matrix4x4.TRS(
                centerWorld,
                _hipsTransform.rotation,
                new Vector3(_widthScale * _ringRadius, 1f, _depthScale * _ringRadius)
            );

            Matrix4x4 originalMatrix = Handles.matrix;
            Handles.matrix = ringMatrix;
            Handles.color = new Color(1f, 0.4f, 0.6f, 0.9f); // 亮粉色线框
            Handles.DrawWireDisc(Vector3.zero, Vector3.up, 1.0f);
            Handles.matrix = originalMatrix;

            // 2. 绘制各个拟生成的球形限位碰撞体
            for (int i = 0; i < _colliderCount; i++)
            {
                float angle = (2f * Mathf.PI * i) / _colliderCount;
                float lx = Mathf.Cos(angle) * _ringRadius * _widthScale;
                float lz = Mathf.Sin(angle) * _ringRadius * _depthScale + _forwardOffset;
                float ly = _heightOffset;

                Vector3 localPos = new Vector3(lx, ly, lz);
                Vector3 worldPos = _hipsTransform.TransformPoint(localPos);

                // 绘制半透明球体填充
                Handles.color = new Color(1f, 0.2f, 0.5f, 0.25f);
                Handles.SphereHandleCap(0, worldPos, Quaternion.identity, _colliderRadius * 2f, EventType.Repaint);

                // 绘制实心线框外轮廓（使用三个正交圆圈拟合球体）
                Handles.color = new Color(1f, 0.2f, 0.5f, 0.85f);
                Handles.DrawWireDisc(worldPos, Vector3.up, _colliderRadius);
                Handles.DrawWireDisc(worldPos, Vector3.forward, _colliderRadius);
                Handles.DrawWireDisc(worldPos, Vector3.left, _colliderRadius);
            }

            // 强制刷新 Scene 视图，确保参数拖拽时画面平滑不卡顿
            sceneView.Repaint();
        }

        #region Draw GUI Elements

        private void DrawHeaderGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("VRM 1.0 裙子防翘起腰部碰撞体一键生成器", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.35f, 0.5f) }
            });
            GUILayout.Space(10);
        }

        private void DrawNullInstanceWarningGUI()
        {
            EditorGUILayout.HelpBox("请在场景中选择一个带有 VRM 1.0 组件 (Vrm10Instance) 的角色物体，或者将角色拖入下方的对象槽中。", MessageType.Warning);
            GUILayout.Space(10);

            EditorGUI.BeginChangeCheck();
            var dragInstance = (Vrm10Instance)EditorGUILayout.ObjectField("目标 VRM 角色", _vrmInstance, typeof(Vrm10Instance), true);
            if (EditorGUI.EndChangeCheck() && dragInstance != null)
            {
                Selection.activeGameObject = dragInstance.gameObject;
                DetectSelectedVRM();
            }
        }

        private void DrawConfigPanelGUI()
        {
            // 角色基本信息展示
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"已识别角色: {(_vrmInstance != null ? _vrmInstance.name : "无")}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"已定位骨骼: {(_hipsTransform != null ? _hipsTransform.name : "未找到 Hips")}");
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // 碰撞体环核心物理参数配置
            GUILayout.Label("碰撞环几何参数设置", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _colliderCount = EditorGUILayout.IntSlider("碰撞球总数", _colliderCount, 4, 16);
            _ringRadius = EditorGUILayout.Slider("腰围圆环半径", _ringRadius, 0.05f, 0.5f);
            _widthScale = EditorGUILayout.Slider("圆环左右展宽 (X)", _widthScale, 0.5f, 2.0f);
            _depthScale = EditorGUILayout.Slider("圆环前后深度 (Z)", _depthScale, 0.5f, 2.0f);
            _heightOffset = EditorGUILayout.Slider("垂直高度偏移 (Y)", _heightOffset, -0.3f, 0.3f);
            _forwardOffset = EditorGUILayout.Slider("前后位置偏移 (Z)", _forwardOffset, -0.2f, 0.2f);
            _colliderRadius = EditorGUILayout.Slider("碰撞球体半径", _colliderRadius, 0.01f, 0.15f);

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
        }

        private void DrawSpringListGUI()
        {
            GUILayout.Label("应用此碰撞体的物理弹簧链 (Springs)", EditorStyles.boldLabel);

            // 快捷控制按钮组
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", GUILayout.Height(20)))
            {
                foreach (var info in _springInfos) info.IsSelected = true;
            }
            if (GUILayout.Button("全不选", GUILayout.Height(20)))
            {
                foreach (var info in _springInfos) info.IsSelected = false;
            }
            if (GUILayout.Button("智能推荐", GUILayout.Height(20)))
            {
                foreach (var info in _springInfos) info.IsSelected = IsSkirtSpring(info.Spring);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // 弹簧链滚动选择框
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, EditorStyles.helpBox, GUILayout.Height(150));
            if (_springInfos.Count == 0)
            {
                EditorGUILayout.LabelField("未在该模型上找到任何 SpringBone 弹簧链数据。");
            }
            else
            {
                for (int i = 0; i < _springInfos.Count; i++)
                {
                    var info = _springInfos[i];
                    EditorGUILayout.BeginHorizontal();
                    info.IsSelected = EditorGUILayout.ToggleLeft($"[{i:D2}] {info.Name} (关节数: {info.Spring.Joints.Count})", info.IsSelected);
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);
        }

        private void DrawActionButtonsGUI()
        {
            _showPreview = EditorGUILayout.ToggleLeft("在场景 (Scene) 中显示碰撞球实时预览", _showPreview);
            
            GUILayout.Space(10);

            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.6f);
            if (GUILayout.Button("一键生成并绑定碰撞体", GUILayout.Height(40)))
            {
                GenerateColliders();
            }
            GUI.backgroundColor = Color.white;
        }

        #endregion

        /// <summary>
        /// 一键生成碰撞体及其组，并更新 VRM 骨骼系统的物理引用绑定（支持撤销）
        /// </summary>
        private void GenerateColliders()
        {
            if (_vrmInstance == null || _hipsTransform == null)
            {
                EditorUtility.DisplayDialog("提示", "未选中有效的 VRM 角色或找不到 Hips 髋部骨骼！", "确定");
                return;
            }

            // 开启一个新的可撤销事务组
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("生成VRM裙子腰部限位碰撞体");
            int undoGroupId = Undo.GetCurrentGroup();

            // 1. 查找并销毁旧的腰部碰撞体节点
            var oldContainer = _hipsTransform.Find("SkirtWaistColliders");
            if (oldContainer != null)
            {
                Undo.DestroyObjectImmediate(oldContainer.gameObject);
            }

            // 2. 清理并过滤 VRM 实例上现存的已失效 (Null) 的碰撞组引用，以防止数据残留
            Undo.RecordObject(_vrmInstance, "清理失效的碰撞体组引用");
            _vrmInstance.SpringBone.ColliderGroups.RemoveAll(cg => cg == null);
            foreach (var spring in _vrmInstance.SpringBone.Springs)
            {
                if (spring != null)
                {
                    spring.ColliderGroups.RemoveAll(cg => cg == null);
                }
            }

            // 3. 创建腰部碰撞体主挂载节点
            GameObject container = new GameObject("SkirtWaistColliders");
            Undo.RegisterCreatedObjectUndo(container, "创建腰部碰撞体挂载节点");
            Undo.SetTransformParent(container.transform, _hipsTransform, "挂载到 Hips");

            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;
            container.transform.localScale = Vector3.one;

            // 4. 添加 VRM 1.0 的 ColliderGroup 组件
            var colliderGroup = Undo.AddComponent<VRM10SpringBoneColliderGroup>(container);
            colliderGroup.Name = "SkirtWaistCollidersGroup";
            colliderGroup.Colliders = new List<VRM10SpringBoneCollider>();

            // 5. 按照椭圆环数学公式分布并生成各个球形限位碰撞体
            for (int i = 0; i < _colliderCount; i++)
            {
                float angle = (2f * Mathf.PI * i) / _colliderCount;
                float lx = Mathf.Cos(angle) * _ringRadius * _widthScale;
                float lz = Mathf.Sin(angle) * _ringRadius * _depthScale + _forwardOffset;
                float ly = _heightOffset;

                GameObject colGo = new GameObject($"WaistCollider_{i:D2}");
                Undo.RegisterCreatedObjectUndo(colGo, $"创建子碰撞体_{i}");
                Undo.SetTransformParent(colGo.transform, container.transform, "连接到容器");

                colGo.transform.localPosition = new Vector3(lx, ly, lz);
                colGo.transform.localRotation = Quaternion.identity;
                colGo.transform.localScale = Vector3.one;

                var collider = Undo.AddComponent<VRM10SpringBoneCollider>(colGo);
                collider.ColliderType = VRM10SpringBoneColliderTypes.Sphere;
                collider.Offset = Vector3.zero;
                collider.Radius = _colliderRadius;

                colliderGroup.Colliders.Add(collider);
            }

            // 6. 将新生成的碰撞组添加至 VRM 角色的全局碰撞池中
            if (!_vrmInstance.SpringBone.ColliderGroups.Contains(colliderGroup))
            {
                _vrmInstance.SpringBone.ColliderGroups.Add(colliderGroup);
            }

            // 7. 将碰撞组绑定到用户在 UI 面板中勾选的所有弹簧链 (Springs) 上
            int boundCount = 0;
            foreach (var info in _springInfos)
            {
                if (info.IsSelected && info.Spring != null)
                {
                    if (!info.Spring.ColliderGroups.Contains(colliderGroup))
                    {
                        info.Spring.ColliderGroups.Add(colliderGroup);
                        boundCount++;
                    }
                }
            }

            // 8. 标记对象为 Dirt 状态并请求 Unity 保存，以便场景及预制体数据正常序列化
            EditorUtility.SetDirty(_vrmInstance);
            
            // 适配 Prefab 编辑模式
            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }
            else if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_vrmInstance.gameObject.scene);
            }

            // 收束 Undo 历史记录，使其在编辑器中呈现为单个历史记录项
            Undo.CollapseUndoOperations(undoGroupId);

            // 刷新弹簧链显示状态并提示生成成功
            InitializeSpringInfos();
            EditorUtility.DisplayDialog("生成成功", $"已成功在 Hips 骨骼下创建了 {_colliderCount} 个限位碰撞体，并绑定至 {boundCount} 条裙子弹簧链上！\n您可以通过按 Ctrl+Z 来一键撤销本次生成操作。", "好的");
        }

        /// <summary>
        /// 深度递归寻找匹配特定名称的子物体节点（忽略大小写）
        /// </summary>
        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
                var result = FindDeepChild(child, name);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        #endregion

        #region Helper Class

        private class SpringSelectionInfo
        {
            public bool IsSelected;
            public Vrm10InstanceSpringBone.Spring Spring;
            public string Name;
        }

        #endregion
    }
}
