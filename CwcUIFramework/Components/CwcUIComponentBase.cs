using UnityEngine;

namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// UI 扩展组件的抽象基类，类似于 CharacterAbility。实现 ICwcUIComponent 接口。
    /// 提供默认的生命周期空实现，子类只需重写所需的生命周期方法即可。
    /// </summary>
    public abstract class CwcUIComponentBase : MonoBehaviour, ICwcUIComponent
    {
        #region 公共属性

        /// <summary>
        /// 关联的 UI 面板宿主实例
        /// </summary>
        public CwcUIElement Owner { get; private set; }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化组件，保存宿主面板引用。
        /// </summary>
        /// <param name="owner">拥有该组件的 CwcUIElement 面板实例。</param>
        public virtual void Initialize(CwcUIElement owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// 面板开始打开时触发。
        /// </summary>
        public virtual void OnOpen() { }

        /// <summary>
        /// 面板播放完过渡动画完全打开时触发。
        /// </summary>
        public virtual void OnOpenFinished() { }

        /// <summary>
        /// 面板开始关闭时触发。
        /// </summary>
        public virtual void OnClose() { }

        /// <summary>
        /// 面板播放完过渡动画完全关闭时触发。
        /// </summary>
        public virtual void OnCloseFinished() { }

        #endregion
    }
}
