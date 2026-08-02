using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// UI 框架核心管理器，负责 Canvas 层级管理、面板实例化、缓存管理以及生命周期流转。
    /// <para>【架构设计哲学说明】</para>
    /// <para>本框架采用【同步实例化骨架 + UI 内部自异步加载大资源】的设计模式，故意不提供框架层面的全局异步打开界面接口（如 OpenAsync）。</para>
    /// <para>1. 避免时序灾难：全局异步打开会在点击与显示之间产生可变时间差，极易引发重复点击、并发加载、中途取消等复杂的生命周期时序 Bug。</para>
    /// <para>2. 提升交互响应：UI 预制件（Prefab）应保持极简，只携带 UI 骨架和基础组件，调用同步 Open() 实现毫秒级瞬间秒开。</para>
    /// <para>3. 资源按需异步：重度资源（如立绘、背景大图、3D模型、大型音效等）应由具体 UI 面板内部利用 Addressables/AssetBundle 自异步加载，并伴随局部 Loading 动画，避免界面加载卡顿。</para>
    /// </summary>
    public class CwcUIFrame : MonoBehaviour
    {
        #region 嵌套类型声明

        /// <summary>
        /// 用于在 Inspector 中配置层级与相机的对应关系
        /// </summary>
        [Serializable]
        public class LayerCameraConfig
        {
            [SerializeField] private CwcUILayerSO layer;
            [SerializeField] private Camera targetCamera;

            public CwcUILayerSO Layer => layer;
            public Camera TargetCamera => targetCamera;
        }

        #endregion

        #region 序列化属性与字段

        [Header("全局 UI 配置文件")]
        [SerializeField] private CwcUISettings uiSettings;

        [Header("层级相机绑定配置")]
        [SerializeField] private List<LayerCameraConfig> layerCameraConfigs = new List<LayerCameraConfig>();

        #endregion

        #region 非序列化私有字段

        /// <summary>
        /// 缓存所有已实例化的 UI 面板，Key 为 ScreenId
        /// </summary>
        private readonly Dictionary<string, CwcUIElement> _instantiatedScreens = new Dictionary<string, CwcUIElement>();

        /// <summary>
        /// 缓存已创建或绑定的 Canvas 层级节点，Key 为层级的名称
        /// </summary>
        private readonly Dictionary<string, Canvas> _layers = new Dictionary<string, Canvas>();

        /// <summary>
        /// 缓存已创建或绑定的层级 Transform 节点，Key 为层级的名称
        /// </summary>
        private readonly Dictionary<string, Transform> _layerNodes = new Dictionary<string, Transform>();

        /// <summary>
        /// 运行时相机映射，Key 为层级的名称
        /// </summary>
        private readonly Dictionary<string, Camera> _layerCameras = new Dictionary<string, Camera>();

        #endregion

        #region 公共属性与事件

        /// <summary>
        /// 统一转发 UI 面板发出的关闭请求事件
        /// </summary>
        public event Action<CwcUIElement> ScreenCloseRequested;

        #endregion

        #region 生命周期方法

        private void Awake()
        {
            // 初始化全局 UI 配置
            if (uiSettings != null)
            {
                uiSettings.Initialize();
            }
            else
            {
                Debug.LogError("[CwcUIFramework] 未在 CwcUIFrame 上配置 CwcUISettings！");
            }

            // 初始化相机映射
            foreach (var config in layerCameraConfigs)
            {
                if (config != null && config.Layer != null && config.TargetCamera != null)
                {
                    _layerCameras[config.Layer.name] = config.TargetCamera;
                }
            }
        }

        private void Start()
        {
            PreloadUIEntries();
        }

        private void OnDestroy()
        {
            // 清理缓存并解除事件监听
            foreach (var screen in _instantiatedScreens.Values)
            {
                if (screen != null)
                {
                    screen.ScreenDestroyed -= OnScreenDestroyed;
                    screen.CloseRequest -= OnScreenCloseRequested;
                }
            }

            _instantiatedScreens.Clear();
            _layers.Clear();
            _layerNodes.Clear();
            _layerCameras.Clear();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 运行时动态绑定层级的渲染相机
        /// </summary>
        /// <param name="layer">目标 UI 层配置</param>
        /// <param name="camera">目标相机</param>
        public virtual void RegisterLayerCamera(CwcUILayerSO layer, Camera camera)
        {
            if (layer == null || camera == null) return;

            string layerName = layer.name;
            _layerCameras[layerName] = camera;

            // 如果该层级已经创建，则立即更新 Canvas 渲染模式与相机
            if (_layers.TryGetValue(layerName, out var canvas))
            {
                ApplyCameraToCanvas(canvas, camera);
            }
        }

        /// <summary>
        /// 打开无参数的 UI 面板
        /// </summary>
        /// <param name="entry">UI 配置条目</param>
        /// <returns>实例化后的 UI 面板基类</returns>
        public virtual CwcUIElement Open(UIEntrySO entry)
        {
            return OpenInternal<CwcUIElement>(entry, null);
        }

        /// <summary>
        /// 打开无参数 of UI 面板并自动转换类型
        /// </summary>
        public virtual T Open<T>(UIEntrySO entry) where T : CwcUIElement
        {
            return OpenInternal<T>(entry, null);
        }

        /// <summary>
        /// 打开携带特定参数的 UI 面板
        /// </summary>
        /// <typeparam name="TData">数据类型</typeparam>
        /// <param name="entry">UI 配置条目</param>
        /// <param name="data">传递的参数数据</param>
        /// <returns>实例化后的 UI 面板基类</returns>
        public virtual CwcUIElement Open<TData>(UIEntrySO entry, TData data)
        {
            return OpenInternal<CwcUIElement>(entry, data);
        }

        /// <summary>
        /// 打开携带特定参数的 UI 面板并自动转换类型
        /// </summary>
        public virtual T Open<T, TData>(UIEntrySO entry, TData data) where T : CwcUIElement
        {
            return OpenInternal<T>(entry, data);
        }

        /// <summary>
        /// 根据 UI 注册条目关闭对应的 UI 面板
        /// </summary>
        public virtual void Close(UIEntrySO entry)
        {
            if (entry == null) return;
            Close(entry.ScreenId);
        }

        /// <summary>
        /// 根据 ScreenId 关闭对应的 UI 面板
        /// </summary>
        public virtual void Close(string screenId)
        {
            if (string.IsNullOrEmpty(screenId)) return;

            if (_instantiatedScreens.TryGetValue(screenId, out var screen))
            {
                screen.OnClose();
            }
            else
            {
                Debug.LogWarning($"[CwcUIFramework] 尝试关闭未打开或未实例化的 UI: '{screenId}'");
            }
        }

        /// <summary>
        /// 根据 ScreenId 获取已实例化的 UI 面板（若未实例化则返回 null）
        /// </summary>
        public virtual CwcUIElement GetUI(string screenId)
        {
            if (string.IsNullOrEmpty(screenId)) return null;

            _instantiatedScreens.TryGetValue(screenId, out var screen);
            return screen;
        }

        /// <summary>
        /// 查询指定的 UI 面板当前是否处于打开状态
        /// </summary>
        public virtual bool IsUIOpen(string screenId)
        {
            var ui = GetUI(screenId);
            return ui != null && ui.IsVisible;
        }

        /// <summary>
        /// 关闭所有当前已实例化的 UI 面板
        /// </summary>
        public virtual void CloseAll()
        {
            var keys = new List<string>(_instantiatedScreens.Keys);
            foreach (var screenId in keys)
            {
                Close(screenId);
            }
        }

        /// <summary>
        /// 关闭指定层级下的所有 UI 面板
        /// </summary>
        /// <param name="layer">目标 UI 层级</param>
        public virtual void CloseAllInLayer(CwcUILayerSO layer)
        {
            if (layer == null) return;
            var keys = new List<string>(_instantiatedScreens.Keys);
            foreach (var screenId in keys)
            {
                if (_instantiatedScreens.TryGetValue(screenId, out var screen) && screen != null)
                {
                    if (screen.TargetLayer == layer)
                    {
                        Close(screenId);
                    }
                }
            }
        }

        /// <summary>
        /// 根据层级名称关闭其下的所有 UI 面板
        /// </summary>
        /// <param name="layerName">目标层级名称</param>
        public virtual void CloseAllInLayer(string layerName)
        {
            if (string.IsNullOrEmpty(layerName)) return;
            var keys = new List<string>(_instantiatedScreens.Keys);
            foreach (var screenId in keys)
            {
                if (_instantiatedScreens.TryGetValue(screenId, out var screen) && screen != null)
                {
                    if (screen.TargetLayer != null && screen.TargetLayer.name == layerName)
                    {
                        Close(screenId);
                    }
                }
            }
        }

        /// <summary>
        /// 同步打开无参数的 UI 面板（通过唯一 ID）
        /// </summary>
        public virtual CwcUIElement Open(string screenId)
        {
            var entry = uiSettings != null ? uiSettings.GetEntry(screenId) : null;
            return entry != null ? Open(entry) : null;
        }

        /// <summary>
        /// 打开无参数的 UI 面板并自动转换类型（通过唯一 ID）
        /// </summary>
        public virtual T Open<T>(string screenId) where T : CwcUIElement
        {
            var entry = uiSettings != null ? uiSettings.GetEntry(screenId) : null;
            return entry != null ? Open<T>(entry) : null;
        }

        /// <summary>
        /// 打开携带特定参数的 UI 面板（通过唯一 ID）
        /// </summary>
        public virtual CwcUIElement Open<TData>(string screenId, TData data)
        {
            var entry = uiSettings != null ? uiSettings.GetEntry(screenId) : null;
            return entry != null ? Open(entry, data) : null;
        }

        /// <summary>
        /// 打开携带特定参数的 UI 面板并自动转换类型（通过唯一 ID）
        /// </summary>
        public virtual T Open<T, TData>(string screenId, TData data) where T : CwcUIElement
        {
            var entry = uiSettings != null ? uiSettings.GetEntry(screenId) : null;
            return entry != null ? Open<T, TData>(entry, data) : null;
        }

        #endregion

        #region 保护与私有方法

        /// <summary>
        /// 核心开启面板内部同步方法
        /// </summary>
        protected virtual T OpenInternal<T>(UIEntrySO entry, object data) where T : CwcUIElement
        {
            if (entry == null)
            {
                Debug.LogError("[CwcUIFramework] 传入的 UIEntrySO 为 Null！");
                return null;
            }

            string screenId = entry.ScreenId;
            CwcUIElement screenInstance;

            // 1. 检查缓存中是否已经实例化
            if (!_instantiatedScreens.TryGetValue(screenId, out screenInstance))
            {
                // 获取预制件（可能触发同步加载）
                CwcUIElement prefab = entry.GetPrefab();
                if (prefab == null)
                {
                    Debug.LogError($"[CwcUIFramework] UI 配置 '{screenId}' 的 Prefab 为 Null，无法实例化！");
                    return null;
                }

                // 获取所属层级
                CwcUILayerSO layerSO = entry.TargetLayer;
                Transform parentTransform = GetOrCreateLayerNode(layerSO);

                screenInstance = Instantiate(prefab, parentTransform);
                if (!screenInstance.gameObject.activeSelf)
                {
                    screenInstance.gameObject.SetActive(true);
                }
                screenInstance.Entry = entry; // 动态反向注入配置条目
                screenInstance.UIFrame = this; // 动态注入管理器引用
                screenInstance.gameObject.name = screenId;

                // 监听销毁与关闭请求事件
                screenInstance.ScreenDestroyed += OnScreenDestroyed;
                screenInstance.CloseRequest += OnScreenCloseRequested;

                // 执行初始化
                screenInstance.OnInit();

                // 加入缓存
                _instantiatedScreens.Add(screenId, screenInstance);
            }

            // 3. 分发参数或直接开启面板
            TriggerScreenOpen(screenInstance, data);

            return screenInstance as T;
        }

        /// <summary>
        /// 触发面板打开逻辑与数据路由分发
        /// </summary>
        protected virtual void TriggerScreenOpen(CwcUIElement screenInstance, object data)
        {
            if (screenInstance == null) return;

            if (data != null)
            {
                screenInstance.OpenWithData(data);
            }
            else
            {
                screenInstance.OnOpen();
            }
        }

        /// <summary>
        /// 获取或动态创建层级节点，支持 Canvas 附加以及 CanvasScaler 适配覆写
        /// </summary>
        protected virtual Transform GetOrCreateLayerNode(CwcUILayerSO layerSO)
        {
            string layerName = layerSO != null ? layerSO.name : "Default";

            // 1. 从 Transform 运行期缓存中寻找
            if (_layerNodes.TryGetValue(layerName, out var cachedNode) && cachedNode != null)
            {
                return cachedNode;
            }

            // 2. 检查场景中是否已有同名冲突子物体（警告提示）
            if (transform.Find(layerName) != null)
            {
                Debug.LogWarning($"[CwcUIFramework] 检测到场景中已存在名为 '{layerName}' 的子物体，这可能与即将动态创建的 UI 层级节点冲突，请检查层级结构！");
            }

            // 3. 动态创建全新层级 Transform 容器节点
            GameObject layerObj = new GameObject(layerName);
            layerObj.transform.SetParent(transform, false);

            bool needCanvas = layerSO != null && layerSO.CreateRootCanvas;

            if (needCanvas)
            {
                // 挂载 Canvas 组件
                Canvas newCanvas = layerObj.AddComponent<Canvas>();
                newCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
                // 始终使用伽马色彩空间，防止在线性色彩空间下 UI 顶点颜色变灰变暗
                newCanvas.vertexColorAlwaysGammaSpace = true;

                // 挂载 CanvasScaler 并应用适配参数
                CanvasScaler scaler = layerObj.AddComponent<CanvasScaler>();

                // 判断使用全局 Scaler 配置还是层级独立覆盖配置
                CanvasScalerConfig scalerConfig = uiSettings != null ? uiSettings.DefaultScalerConfig : null;
                if (layerSO != null && layerSO.OverrideScalerConfig)
                {
                    scalerConfig = layerSO.CustomScalerConfig;
                }

                if (scalerConfig != null)
                {
                    scalerConfig.ApplyTo(scaler);
                }
                else
                {
                    // 兜底默认设置
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0.5f;
                }

                // 挂载 GraphicRaycaster 用于处理 UI 点击交互
                layerObj.AddComponent<GraphicRaycaster>();

                // 设置 SortingOrder
                int order = layerSO != null ? layerSO.SortingOrder : 0;
                newCanvas.sortingOrder = order;

                // 绑定相机
                if (_layerCameras.TryGetValue(layerName, out var camera))
                {
                    ApplyCameraToCanvas(newCanvas, camera);
                }
                else
                {
                    // 默认使用 Overlay 模式
                    newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }

                _layers[layerName] = newCanvas;
            }

            _layerNodes[layerName] = layerObj.transform;
            return layerObj.transform;
        }

        /// <summary>
        /// 将指定层级的渲染配置（渲染相机、排序次序、适配缩放等）应用到目标 Canvas 以及 CanvasScaler 上
        /// </summary>
        /// <param name="canvas">目标 Canvas 组件</param>
        /// <param name="layerSO">目标层级配置</param>
        /// <param name="scaler">可选的目标 CanvasScaler 组件</param>
        public virtual void ApplyLayerSettings(Canvas canvas, CwcUILayerSO layerSO, CanvasScaler scaler = null)
        {
            if (canvas == null || layerSO == null) return;

            // 1. 设置渲染相机与模式
            if (_layerCameras.TryGetValue(layerSO.name, out var camera))
            {
                ApplyCameraToCanvas(canvas, camera);
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }

            // 2. 设置排序次序
            canvas.sortingOrder = layerSO.SortingOrder;

            // 3. 设置 CanvasScaler
            if (scaler != null)
            {
                CanvasScalerConfig scalerConfig = uiSettings != null ? uiSettings.DefaultScalerConfig : null;
                if (layerSO.OverrideScalerConfig)
                {
                    scalerConfig = layerSO.CustomScalerConfig;
                }

                if (scalerConfig != null)
                {
                    scalerConfig.ApplyTo(scaler);
                }
            }
        }

        /// <summary>
        /// 应用相机配置到 Canvas
        /// </summary>
        protected virtual void ApplyCameraToCanvas(Canvas canvas, Camera camera)
        {
            if (canvas == null) return;

            if (camera != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }
        }

        /// <summary>
        /// 响应 UI 面板发起的关闭请求
        /// </summary>
        protected virtual void OnScreenCloseRequested(CwcUIElement screen)
        {
            if (screen == null) return;

            ScreenCloseRequested?.Invoke(screen);
            Close(screen.ScreenId);
        }

        /// <summary>
        /// 监听 UI 实例的销毁事件，防止内存泄漏
        /// </summary>
        protected virtual void OnScreenDestroyed(CwcUIElement screen)
        {
            if (screen == null) return;

            screen.ScreenDestroyed -= OnScreenDestroyed;
            screen.CloseRequest -= OnScreenCloseRequested;

            if (_instantiatedScreens.TryGetValue(screen.ScreenId, out var cached) && cached == screen)
            {
                _instantiatedScreens.Remove(screen.ScreenId);
                Debug.LogWarning($"[CwcUIFramework] UI 面板 '{screen.ScreenId}' 被外部销毁，已自动清理缓存。");
            }
        }

        /// <summary>
        /// 扫描配置，执行标有 Preload 属性界面的预实例化逻辑
        /// </summary>
        private void PreloadUIEntries()
        {
            if (uiSettings == null || uiSettings.UIEntries == null) return;

            var entries = uiSettings.UIEntries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null && entry.Preload)
                {
                    PreloadInternal(entry);
                }
            }
        }

        /// <summary>
        /// 预实例化并静默隐藏 UI 界面
        /// </summary>
        protected virtual void PreloadInternal(UIEntrySO entry)
        {
            if (entry == null) return;
            string screenId = entry.ScreenId;
            if (_instantiatedScreens.ContainsKey(screenId)) return;

            CwcUIElement prefab = entry.GetPrefab();
            if (prefab == null)
            {
                Debug.LogError($"[CwcUIFramework] 预加载 UI 配置 '{screenId}' 的 Prefab 为 Null，无法预实例化！");
                return;
            }

            // 获取所属层级
            CwcUILayerSO layerSO = entry.TargetLayer;
            Transform parentTransform = GetOrCreateLayerNode(layerSO);

            CwcUIElement screenInstance = Instantiate(prefab, parentTransform);
            screenInstance.Entry = entry;
            screenInstance.UIFrame = this;
            screenInstance.gameObject.name = screenId;

            // 监听销毁与关闭请求事件
            screenInstance.ScreenDestroyed += OnScreenDestroyed;
            screenInstance.CloseRequest += OnScreenCloseRequested;

            // 执行初始化
            screenInstance.OnInit();

            // 静默对齐至完全隐藏状态（物理 Canvas 关闭，不产生渲染开销与动画）
            screenInstance.AlignCloseStateSilently();

            if (!screenInstance.gameObject.activeSelf)
            {
                screenInstance.gameObject.SetActive(true);
            }

            // 加入缓存
            _instantiatedScreens.Add(screenId, screenInstance);
        }

        #endregion
    }
}
