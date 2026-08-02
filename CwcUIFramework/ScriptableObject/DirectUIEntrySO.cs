using UnityEngine;

namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// 直接引用预制件的 UI 注册配置实现，适用于不需要异步加载/Addressable 的本地 UI 配置
    /// </summary>
    [CreateAssetMenu(fileName = "NewDirectUIEntry", menuName = "CwcUIFramework/Direct UI Entry")]
    public class DirectUIEntrySO : UIEntrySO
    {
        #region 序列化属性与字段

        [Header("归属层级")]
        [SerializeField] private CwcUILayerSO targetLayer;

        [Header("UI 预制件引用")]
        [SerializeField] private CwcUIElement prefab;

        [Header("UI 唯一标识符(为空时默认使用预制件名称)")]
        [SerializeField] private string screenId;

        [Header("是否在框架启动时预加载")]
        [SerializeField] private bool preload = true;

        #endregion

        #region 公共属性重写

        /// <summary>
        /// 唯一屏幕标识符
        /// </summary>
        public override string ScreenId
        {
            get
            {
                if (string.IsNullOrEmpty(screenId) && prefab != null)
                {
                    return prefab.name;
                }
                return screenId;
            }
        }

        /// <summary>
        /// 归属层级配置
        /// </summary>
        public override CwcUILayerSO TargetLayer => targetLayer;

        /// <summary>
        /// 是否在框架启动时进行预加载（预实例化）
        /// </summary>
        public override bool Preload => preload;

        #endregion

        #region 接口实现

        /// <summary>
        /// 同步获取 UI 预制件（直接返回配置的 Prefab 强引用）
        /// </summary>
        public override CwcUIElement GetPrefab()
        {
            return prefab;
        }

        #endregion
    }
}
