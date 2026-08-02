using UnityEngine;
using UnityEngine.Events;

namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// 通用 UI 生命周期事件桥接器，用于在 Inspector 中配置打开与关闭过渡的开始/结束事件。
    /// 改为继承 CwcUIComponentBase，通过主动推送机制触发，无任何事件订阅开销，杜绝内存泄漏。
    /// </summary>
    [RequireComponent(typeof(CwcUIElement))]
    [AddComponentMenu("CwcUIFramework/Components/CwcUI Screen Event Bridge")]
    public class CwcUIScreenEventBridge : CwcUIComponentBase
    {
        #region 序列化属性与字段

        [Header("打开事件")]
        [SerializeField] private UnityEvent onShowStarted;
        [SerializeField] private UnityEvent onShowFinished;

        [Header("关闭事件")]
        [SerializeField] private UnityEvent onHideStarted;
        [SerializeField] private UnityEvent onHideFinished;

        #endregion

        #region 公共方法重写

        /// <summary>
        /// 响应过渡开始打开事件。
        /// </summary>
        public override void OnOpen()
        {
            onShowStarted?.Invoke();
        }

        /// <summary>
        /// 响应过渡完全打开事件。
        /// </summary>
        public override void OnOpenFinished()
        {
            onShowFinished?.Invoke();
        }

        /// <summary>
        /// 响应过渡开始关闭事件。
        /// </summary>
        public override void OnClose()
        {
            onHideStarted?.Invoke();
        }

        /// <summary>
        /// 响应过渡完全关闭事件。
        /// </summary>
        public override void OnCloseFinished()
        {
            onHideFinished?.Invoke();
        }

        #endregion
    }
}
