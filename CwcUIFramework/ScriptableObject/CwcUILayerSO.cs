using UnityEngine;

namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// UI 层级配置资源，每个资源的名称即为该层的名称
    /// </summary>
    [CreateAssetMenu(fileName = "NewLayer", menuName = "CwcUIFramework/Layer Settings")]
    public class CwcUILayerSO : ScriptableObject
    {
        #region 序列化属性与字段

        [Header("是否为此层级根节点创建 Canvas")]
        [SerializeField] private bool createRootCanvas = true;

        [Header("层级渲染顺序")]
        [SerializeField] private int sortingOrder;

        [Header("是否覆盖全局缩放适配配置")]
        [SerializeField] private bool overrideScalerConfig;

        [Header("可选的自定义缩放配置")]
        [SerializeField] private CanvasScalerConfig customScalerConfig;

        #endregion

        #region 公共属性

        /// <summary>
        /// 是否在此层级根节点创建 Canvas。
        /// 开启后该层下所有 UI 默认共享此根 Canvas 并支持合批。
        /// </summary>
        public bool CreateRootCanvas => createRootCanvas;

        /// <summary>
        /// 层级的渲染排序次序，无论是否创建根 Canvas，子 Canvas 均可读取此属性用于渲染排序
        /// </summary>
        public int SortingOrder => sortingOrder;

        /// <summary>
        /// 是否覆盖全局 CanvasScaler 配置
        /// </summary>
        public bool OverrideScalerConfig => overrideScalerConfig;

        /// <summary>
        /// 覆盖的 CanvasScaler 配置
        /// </summary>
        public CanvasScalerConfig CustomScalerConfig => customScalerConfig;

        #endregion
    }
}
