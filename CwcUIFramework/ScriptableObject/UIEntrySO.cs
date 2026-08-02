using UnityEngine;

namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// UI 注册配置基类，抽象预制件的获取方式，支持同步或多种加载流实现（如直接引用、Addressables等）
    /// </summary>
    public abstract class UIEntrySO : ScriptableObject
    {
        /// <summary>
        /// 唯一屏幕标识符
        /// </summary>
        public abstract string ScreenId { get; }

        /// <summary>
        /// 归属层级配置
        /// </summary>
        public abstract CwcUILayerSO TargetLayer { get; }

        /// <summary>
        /// 是否在框架启动时进行预加载（预实例化）
        /// </summary>
        public abstract bool Preload { get; }

        /// <summary>
        /// 同步获取 UI 预制件（可能触发同步加载或直接返回）
        /// </summary>
        public abstract CwcUIElement GetPrefab();

        /// <summary>
        /// 释放加载的预制件资源（主要用于 Addressables 卸载，默认为空）
        /// </summary>
        public virtual void ReleasePrefab() { }
    }
}
