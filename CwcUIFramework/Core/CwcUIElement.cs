using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// UI 面板的统一基类，提供生命周期管理、物理 Canvas 渲染开关以及动画过渡支持
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class CwcUIElement : MonoBehaviour
    {
        #region 序列化属性与字段

        [Header("开启过渡组件")]
        [SerializeField] private CwcUITransitionComponent openTransition;

        [Header("关闭过渡组件")]
        [SerializeField] private CwcUITransitionComponent closeTransition;

        [Header("扩展组件节点（默认仅收集同层级组件，若组件在子物体上可在此配置节点）")]
        [SerializeField] private List<GameObject> extraComponentNodes = new List<GameObject>();

        #endregion

        #region 非序列化私有字段

        /// <summary>
        /// 缓存所有挂载在同层级或配置节点上的 UI 扩展组件
        /// </summary>
        private readonly List<ICwcUIComponent> _uiComponents = new List<ICwcUIComponent>();

        /// <summary>
        /// 缓存子代中属于当前面板管辖的所有层级 Canvas 绑定器组件
        /// </summary>
        private CwcUILayerCanvasBinder[] _subCanvasBinders;

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取管理当前面板的 UI 框架核心管理器引用
        /// </summary>
        public CwcUIFrame UIFrame { get; internal set; }

        /// <summary>
        /// 获取运行期动态注入的 UI 注册配置条目
        /// </summary>
        public UIEntrySO Entry { get; internal set; }

        /// <summary>
        /// 获取当前面板的唯一屏幕标识符
        /// </summary>
        public string ScreenId => Entry != null ? Entry.ScreenId : string.Empty;

        /// <summary>
        /// 获取当前挂载的 Canvas 组件
        /// </summary>
        public Canvas Canvas { get; private set; }

        /// <summary>
        /// 获取当前面板是否可见（状态由面板生命周期权威维护）
        /// </summary>
        public bool IsVisible { get; private set; } = false;

        /// <summary>
        /// 获取当前面板的运行期归属层级
        /// </summary>
        public CwcUILayerSO TargetLayer => Entry != null ? Entry.TargetLayer : null;

        /// <summary>
        /// 获取当前挂载的 CanvasGroup 组件
        /// </summary>
        public CanvasGroup CanvasGroup { get; private set; }


        /// <summary>
        /// UI 实例销毁时的事件回调，供管理器清理缓存
        /// </summary>
        public Action<CwcUIElement> ScreenDestroyed;

        /// <summary>
        /// 界面准备打开（过渡动画即将开始）时的事件回调
        /// </summary>
        public event Action<CwcUIElement> OpenStarted;

        /// <summary>
        /// 动效播完且界面完全打开时的事件回调
        /// </summary>
        public event Action<CwcUIElement> OpenFinished;

        /// <summary>
        /// 界面准备关闭（过渡动画即将开始）时的事件回调
        /// </summary>
        public event Action<CwcUIElement> CloseStarted;

        /// <summary>
        /// 动效播完且界面完全关闭时的事件回调
        /// </summary>
        public event Action<CwcUIElement> CloseFinished;

        /// <summary>
        /// 请求关闭该面板的事件回调
        /// </summary>
        public event Action<CwcUIElement> CloseRequest;

        #endregion

        #region 生命周期方法

        protected virtual void Awake()
        {
            Canvas = GetComponent<Canvas>();
            CanvasGroup = GetComponent<CanvasGroup>();
            if (CanvasGroup == null)
            {
                Debug.LogError($"[CwcUIFramework] GameObject '{gameObject.name}' 上未找到 CanvasGroup 组件！");
            }

            // 收集属于当前 UIElement 且没有被子 UIElement 阻断的 Canvas 绑定器
            var allBinders = GetComponentsInChildren<CwcUILayerCanvasBinder>(true);
            var filteredBinders = new List<CwcUILayerCanvasBinder>();
            for (int i = 0; i < allBinders.Length; i++)
            {
                if (allBinders[i] != null && allBinders[i].GetComponentInParent<CwcUIElement>() == this)
                {
                    filteredBinders.Add(allBinders[i]);
                }
            }
            _subCanvasBinders = filteredBinders.ToArray();

            // 1. 收集自身同层级的扩展组件
            var selfComponents = GetComponents<ICwcUIComponent>();
            if (selfComponents != null)
            {
                _uiComponents.AddRange(selfComponents);
            }

            // 2. 收集配置的额外节点上的扩展组件
            if (extraComponentNodes != null)
            {
                for (int i = 0; i < extraComponentNodes.Count; i++)
                {
                    var node = extraComponentNodes[i];
                    if (node != null)
                    {
                        var extraComponents = node.GetComponents<ICwcUIComponent>();
                        if (extraComponents != null)
                        {
                            _uiComponents.AddRange(extraComponents);
                        }
                    }
                }
            }

            // 3. 统一初始化所有收集到的扩展组件
            for (int i = 0; i < _uiComponents.Count; i++)
            {
                _uiComponents[i].Initialize(this);
            }
        }

        protected virtual void OnEnable()
        {
            // 在启用时执行全局事件订阅，防止在禁用状态下继续接收事件
            AddListeners();
        }

        protected virtual void OnDisable()
        {
            // 在禁用时注销事件订阅，防止内存泄漏和禁用状态下响应事件
            RemoveListeners();
        }

        protected virtual void OnDestroy()
        {
            ScreenDestroyed?.Invoke(this);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取挂载在当前面板上的指定类型扩展组件
        /// </summary>
        /// <typeparam name="T">扩展组件类型</typeparam>
        /// <returns>目标扩展组件，若未找到则返回 null</returns>
        public T GetUIComponent<T>() where T : class, ICwcUIComponent
        {
            for (int i = 0; i < _uiComponents.Count; i++)
            {
                if (_uiComponents[i] is T target)
                {
                    return target;
                }
            }
            return null;
        }

        /// <summary>
        /// 面板被实例化后的统一初始化接口
        /// </summary>
        public virtual void OnInit() { }

        /// <summary>
        /// 注册 UI 事件监听（在 OnEnable 头部自动触发，用于挂载全局事件）
        /// </summary>
        protected virtual void AddListeners() { }

        /// <summary>
        /// 注销 UI 事件监听（在 OnDisable 头部自动触发，防止内存泄漏）
        /// </summary>
        protected virtual void RemoveListeners() { }

        /// <summary>
        /// 界面准备开始打开时触发（在 OnOpen 头部、AddListeners 前自动触发，适合在动画播放前刷新界面数据）
        /// </summary>
        protected virtual void OnOpenStarted() { }

        /// <summary>
        /// 界面准备开始关闭时触发（在 OnClose 头部、RemoveListeners 前自动触发，适合在关闭动画播放前做清理工作）
        /// </summary>
        protected virtual void OnCloseStarted() { }

        /// <summary>
        /// 面板完全显示并播完打开过渡动画时触发
        /// </summary>
        protected virtual void OnOpenFinished() { }

        /// <summary>
        /// 面板完全隐藏并播完关闭过渡动画时触发
        /// </summary>
        protected virtual void OnCloseFinished() { }

        /// <summary>
        /// 关闭自身面板的快捷入口
        /// </summary>
        public virtual void Hide()
        {
            if (CloseRequest != null)
            {
                CloseRequest.Invoke(this);
            }
            else
            {
                OnClose();
            }
        }

        /// <summary>
        /// 管理器调用的数据分发统一入口。若无数据，则默认直接开启面板。
        /// </summary>
        /// <param name="data">传递的外部数据对象</param>
        public virtual void OpenWithData(object data)
        {
            OnOpen();
        }

        /// <summary>
        /// 开启面板的逻辑与状态流转过程
        /// </summary>
        public virtual void OnOpen()
        {
            // 如果界面已经是打开状态，则无视过渡动画，仅触发组件事件以刷新界面显示
            if (IsVisible)
            {
                // 即使已打开，也进行一次安全状态同步，确保物理 Canvas 状态正确
                SetAllCanvasEnabled(true);
                OnOpenStarted();
                for (int i = 0; i < _uiComponents.Count; i++)
                {
                    _uiComponents[i].OnOpen();
                }
                return;
            }

            if (closeTransition != null)
            {
                closeTransition.Stop();
            }

            // 0. 更新权威显示状态为开启
            IsVisible = true;

            // 1. 开启物理 Canvas 渲染
            SetAllCanvasEnabled(true);

            // 2. 预先对齐为隐藏状态（透明度为0，且不拦截射线）
            PreAlignCloseState();

            // 3. 触发开始打开钩子（适合在过渡动画播放前刷新数据）
            OnOpenStarted();

            // 遍历通知组件 OnOpen
            for (int i = 0; i < _uiComponents.Count; i++)
            {
                _uiComponents[i].OnOpen();
            }

            // 触发准备开启的事件通知
            OpenStarted?.Invoke(this);

            // 4. 播放过渡动画
            if (openTransition != null)
            {
                openTransition.Play(AlignOpenState);
            }
            else
            {
                AlignOpenState();
            }
        }

        /// <summary>
        /// 关闭面板的逻辑与状态流转过程，GameObject 保持激活以支持数据静默更新
        /// </summary>
        public virtual void OnClose()
        {
            if (!IsVisible)
            {
                return;
            }

            if (openTransition != null)
            {
                openTransition.Stop();
            }

            // 0. 更新权威显示状态为关闭
            IsVisible = false;

            // 1. 触发开始关闭钩子（适合在关闭动画播放前做清理）
            OnCloseStarted();

            // 遍历通知组件 OnClose
            for (int i = 0; i < _uiComponents.Count; i++)
            {
                _uiComponents[i].OnClose();
            }

            // 触发准备关闭的事件通知
            CloseStarted?.Invoke(this);

            // 2. 预先对齐为显示状态
            PreAlignOpenState();

            // 3. 播放过渡动画并安全隐藏
            if (closeTransition != null)
            {
                closeTransition.Play(() =>
                {
                    AlignCloseState();
                    SetAllCanvasEnabled(false);
                });
            }
            else
            {
                AlignCloseState();
                SetAllCanvasEnabled(false);
            }
        }

        #endregion

        #region 私有与保护方法

        /// <summary>
        /// 仅供框架内部预加载时调用，用于静默对齐到完全关闭和禁用状态（不播放任何动画）
        /// </summary>
        internal void AlignCloseStateSilently()
        {
            IsVisible = false; // 权威状态置为关闭
            PreAlignCloseState();
            AlignCloseState();
            SetAllCanvasEnabled(false);
        }

        /// <summary>
        /// 开启前预先初始化为不可见不可交互状态
        /// </summary>
        private void PreAlignCloseState()
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = 0f;
                CanvasGroup.blocksRaycasts = false;
                CanvasGroup.interactable = false;
            }
        }

        /// <summary>
        /// 关闭前预先初始化为完全显示可交互状态
        /// </summary>
        private void PreAlignOpenState()
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = 1f;
                CanvasGroup.blocksRaycasts = true;
                CanvasGroup.interactable = true;
            }
        }

        /// <summary>
        /// 状态安全对齐：更新为完全显示且允许交互状态
        /// </summary>
        protected virtual void AlignOpenState()
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = 1f;
                CanvasGroup.blocksRaycasts = true;
                CanvasGroup.interactable = true;
            }

            // 触发完全打开时间点对应的钩子与事件
            OnOpenFinished();

            // 遍历通知组件 OnOpenFinished
            for (int i = 0; i < _uiComponents.Count; i++)
            {
                _uiComponents[i].OnOpenFinished();
            }

            OpenFinished?.Invoke(this);
        }

        /// <summary>
        /// 状态安全对齐：更新为完全隐藏且不可交互状态
        /// </summary>
        protected virtual void AlignCloseState()
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = 0f;
                CanvasGroup.blocksRaycasts = false;
                CanvasGroup.interactable = false;
            }

            // 触发完全关闭时间点对应的钩子与事件
            OnCloseFinished();

            // 遍历通知组件 OnCloseFinished
            for (int i = 0; i < _uiComponents.Count; i++)
            {
                _uiComponents[i].OnCloseFinished();
            }

            CloseFinished?.Invoke(this);
        }

        /// <summary>
        /// 统一启用或关闭当前面板及其管辖的子 Canvas 渲染使能状态
        /// </summary>
        private void SetAllCanvasEnabled(bool isEnabled)
        {
            if (Canvas != null)
            {
                Canvas.enabled = isEnabled;
            }

            if (_subCanvasBinders != null)
            {
                for (int i = 0; i < _subCanvasBinders.Length; i++)
                {
                    if (_subCanvasBinders[i] != null)
                    {
                        _subCanvasBinders[i].SetCanvasEnabled(isEnabled);
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 支持泛型参数传递的 UI 面板基类
    /// </summary>
    /// <typeparam name="TData">强类型参数的类型</typeparam>
    public abstract class CwcUIElement<TData> : CwcUIElement
    {
        #region 公共属性

        /// <summary>
        /// 传递进来的强类型数据
        /// </summary>
        public TData Data { get; private set; }

        #endregion

        #region 公共方法

        /// <summary>
        /// 重写非泛型分发入口，提供强类型的向下转型和安全保护
        /// </summary>
        public sealed override void OpenWithData(object data)
        {
            if (data is TData typedData)
            {
                OnOpen(typedData);
            }
            else
            {
                string dataTypeStr = data != null ? data.GetType().Name : "Null";
                Debug.LogError($"[CwcUIFramework] UI 面板 '{ScreenId}' 期待的参数类型是 {typeof(TData).Name}，但实际传入了 {dataTypeStr}！");
            }
        }

        /// <summary>
        /// 泛型特定的生命周期入口，供子类重写以获取并处理强类型参数
        /// </summary>
        /// <param name="data">强类型数据</param>
        public virtual void OnOpen(TData data)
        {
            Data = data;
            base.OnOpen(); // 调用基类 OnOpen 触发物理 Canvas 开启与过渡动效
        }

        #endregion
    }
}
