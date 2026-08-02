#if UNITY_EDITOR
namespace Cwcbb.Tools
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// CwcRemoteController 的自定义编辑器，在 Inspector 中提供运行期的实例生命周期控制和信号发送调试面板。
    /// </summary>
    [CustomEditor(typeof(CwcRemoteController))]
    public class CwcRemoteControllerEditor : Editor
    {
        #region 生命周期方法
        /// <summary>
        /// 绘制 Inspector GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            // 1. 绘制组件默认的属性字段
            DrawDefaultInspector();

            EditorGUILayout.Space(15);

            CwcRemoteController controller = (CwcRemoteController)target;

            // 2. 绘制快速调试控制台标题
            EditorGUILayout.LabelField("控制器快速调试面板", EditorStyles.boldLabel);

            // 检查当前是否在运行模式 (Play Mode)
            bool isPlaying = Application.isPlaying;

            // 提示在非运行模式下的限制，编辑器状态下不运行以防意外修改物体永久状态
            if (!isPlaying)
            {
                EditorGUILayout.HelpBox("提示: 调试功能仅在运行模式 (Play Mode) 下可用。", MessageType.Info);
            }

            // 3. 开始绘制调试控制区域
            EditorGUI.BeginDisabledGroup(!isPlaying);
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    // 显示当前实例的加载状态
                    bool hasInstance = controller.ObjectInstance != null;
                    GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
                    statusStyle.normal.textColor = hasInstance ? Color.green : Color.yellow;
                    
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("受控对象实例状态:", EditorStyles.boldLabel, GUILayout.Width(130));
                        EditorGUILayout.LabelField(hasInstance ? "已加载 (Active)" : "未加载 (Null)", statusStyle);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(5);

                    // 4. 实例生命周期控制按钮
                    EditorGUILayout.LabelField("生命周期控制:", EditorStyles.miniBoldLabel);
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("请求加载实例", GUILayout.ExpandWidth(true)))
                        {
                            controller.RequestObject();
                        }

                        if (GUILayout.Button("显示实例", GUILayout.ExpandWidth(true)))
                        {
                            controller.SetVisible(true);
                        }

                        if (GUILayout.Button("隐藏实例", GUILayout.ExpandWidth(true)))
                        {
                            controller.SetVisible(false);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(10);

                    // 5. 信号发送测试
                    EditorGUILayout.LabelField("信号测试发送:", EditorStyles.miniBoldLabel);

                    // 获取绑定的 Config
                    SerializedProperty configProp = serializedObject.FindProperty("_objectConfig");
                    CwcRemoteControlObjectConfig config = configProp != null ? configProp.objectReferenceValue as CwcRemoteControlObjectConfig : null;

                    SerializedProperty mappingsProperty = null;
                    SerializedObject targetSerializedObj = null;

                    // 优先从已加载的实例中读取映射列表，否则从预制件中读取
                    if (hasInstance)
                    {
                        targetSerializedObj = new SerializedObject(controller.ObjectInstance);
                    }
                    else if (config != null && config.Prefab != null)
                    {
                        CwcRemoteControlObject prefabComponent = config.Prefab.GetComponent<CwcRemoteControlObject>();
                        if (prefabComponent != null)
                        {
                            targetSerializedObj = new SerializedObject(prefabComponent);
                        }
                    }

                    if (targetSerializedObj != null)
                    {
                        mappingsProperty = targetSerializedObj.FindProperty("_signalMappings");
                    }

                    // 遍历绘制信号快捷按钮
                    if (mappingsProperty != null && mappingsProperty.isArray && mappingsProperty.arraySize > 0)
                    {
                        for (int i = 0; i < mappingsProperty.arraySize; i++)
                        {
                            SerializedProperty element = mappingsProperty.GetArrayElementAtIndex(i);
                            SerializedProperty idProp = element.FindPropertyRelative("_signalId");
                            
                            if (idProp != null)
                            {
                                int signalId = idProp.intValue;

                                EditorGUILayout.BeginHorizontal();
                                {
                                    EditorGUILayout.LabelField($"信号 ID: {signalId}", GUILayout.Width(120));

                                    if (GUILayout.Button($"发送信号 {signalId}", GUILayout.ExpandWidth(true)))
                                    {
                                        controller.SendSignal(signalId);
                                        Debug.Log($"[{nameof(CwcRemoteControllerEditor)}] 调试控制台触发发送信号 ID: {signalId}", controller);
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                    else
                    {
                        if (config == null)
                        {
                            EditorGUILayout.HelpBox("未绑定 [ObjectConfig]，请先指定受控对象配置资产以读取可用信号列表。", MessageType.Warning);
                        }
                        else if (config.Prefab == null)
                        {
                            EditorGUILayout.HelpBox("绑定的配置资产中未指定 [Prefab] 预制件，无法读取可用信号列表。", MessageType.Warning);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("配置的预制件上未检测到信号映射配置，请在预制件的 CwcRemoteControlObject 组件上进行配置。", MessageType.None);
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUI.EndDisabledGroup();
        }
        #endregion
    }
}
#endif
