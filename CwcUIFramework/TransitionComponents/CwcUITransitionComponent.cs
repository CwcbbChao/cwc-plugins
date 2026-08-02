using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// UI 过渡动画基类，定义打开或关闭时的动画播放与打断机制
    /// </summary>
    public abstract class CwcUITransitionComponent : MonoBehaviour
    {
        #region 非序列化私有字段

        /// <summary>
        /// 当前播放完毕后需要触发的回调函数
        /// </summary>
        protected Action currentCallback;

        #endregion

        #region 公共方法

        /// <summary>
        /// 播放过渡动画，结束后执行 onComplete 回调
        /// </summary>
        /// <param name="onComplete">动画播放完毕后的回调动作</param>
        public virtual void Play(Action onComplete)
        {
            currentCallback = onComplete;
        }

        /// <summary>
        /// 强行终止动画，清空回调，并立刻跳过/重置到最终状态（防止残留回调引起的延迟错乱）
        /// </summary>
        public virtual void Stop()
        {
            currentCallback = null;
        }

        #endregion
    }
}
