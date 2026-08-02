namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// UI 元素扩展组件接口，类似于 CharacterAbility。由 CwcUIElement 统一管理并在生命周期各节点主动调用。
    /// </summary>
    public interface ICwcUIComponent
    {
        /// <summary>
        /// 初始化组件，在 CwcUIElement.Awake 时被调用。
        /// </summary>
        /// <param name="owner">拥有该组件的 CwcUIElement 实例。</param>
        void Initialize(CwcUIElement owner);

        /// <summary>
        /// 面板开始打开时触发。
        /// </summary>
        void OnOpen();

        /// <summary>
        /// 面板播放完过渡动画完全打开时触发。
        /// </summary>
        void OnOpenFinished();

        /// <summary>
        /// 面板开始关闭时触发。
        /// </summary>
        void OnClose();

        /// <summary>
        /// 面板播放完过渡动画完全关闭时触发。
        /// </summary>
        void OnCloseFinished();
    }
}
