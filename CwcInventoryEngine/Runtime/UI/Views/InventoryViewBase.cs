using UnityEngine;

namespace Cwc.InventoryEngine.UI
{
    /// <summary>
    /// 库存 UI 视图抽象基类。
    /// 提供纯逻辑门禁状态 (IsActive) 管理，防止视图在非激活状态下误触发输入响应或跑耗时 UI 计算。
    /// 遵循逻辑与视觉解耦原则：只控制内部 logic 变量，不强行干涉 GameObject 或 CanvasGroup 表现层。
    /// 纯粹独立设计，无任何外部 UI 框架依赖。
    /// </summary>
    public abstract class InventoryViewBase : MonoBehaviour
    {
        #region Serialized Fields
        [Header("逻辑激活配置")]
        [SerializeField]
        [Tooltip("初始时是否处于逻辑激活状态 (默认为 true，若由外部 UI 框架驱动可在 Inspector 中设为 false)")]
        private bool _isActiveOnStart = true;
        #endregion

        #region Private Fields
        private bool _isActive = true;
        #endregion

        #region Public Properties
        /// <summary>
        /// 获取当前视图是否处于逻辑激活状态。
        /// 当为 false 时，应拦截离散输入响应与数据更新重绘。
        /// </summary>
        public bool IsActive => _isActive;
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            _isActive = _isActiveOnStart;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 激活当前视图逻辑。
        /// 仅切内部状态标志，不干涉外围 GameObject 或 CanvasGroup 的物理显隐。
        /// </summary>
        public virtual void Activate()
        {
            if (_isActive) return;

            _isActive = true;
            OnActivated();
        }

        /// <summary>
        /// 停用当前视图逻辑。
        /// 仅切内部状态标志，不干涉外围 GameObject 或 CanvasGroup 的物理显隐。
        /// </summary>
        public virtual void Deactivate()
        {
            if (!_isActive) return;

            _isActive = false;
            OnDeactivated();
        }

        /// <summary>
        /// 快捷切换视图逻辑激活状态。
        /// </summary>
        /// <param name="active">是否激活</param>
        public virtual void SetActivated(bool active)
        {
            if (active)
            {
                Activate();
            }
            else
            {
                Deactivate();
            }
        }

        /// <summary>
        /// Activate 的别名，便于上层框架或事件桥接器统一映射。
        /// </summary>
        public virtual void Show()
        {
            Activate();
        }

        /// <summary>
        /// Deactivate 的别名，便于上层框架或事件桥接器统一映射。
        /// </summary>
        public virtual void Hide()
        {
            Deactivate();
        }

        /// <summary>
        /// 兼容 Unity 按钮或 Inspector 事件绑定的激活入口。
        /// </summary>
        public void UI_Activate()
        {
            Activate();
        }

        /// <summary>
        /// 兼容 Unity 按钮或 Inspector 事件绑定的停用入口。
        /// </summary>
        public void UI_Deactivate()
        {
            Deactivate();
        }
        #endregion

        #region Protected Virtual Methods
        /// <summary>
        /// 当视图由停用切换为激活时触发的虚回调。
        /// 子类可重写此方法以全量拉取并刷新最新的界面数据。
        /// </summary>
        protected virtual void OnActivated() { }

        /// <summary>
        /// 当视图由激活切换为停用时触发的虚回调。
        /// 子类可重写此方法以清理选中的高亮框、二级弹窗或 Tooltip 等临时逻辑状态。
        /// </summary>
        protected virtual void OnDeactivated() { }
        #endregion
    }
}
