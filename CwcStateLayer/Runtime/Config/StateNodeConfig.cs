using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 状态节点配置项，定义单个状态节点的标识与嵌套子状态层引用。
    /// </summary>
    [Serializable]
    public class StateNodeConfig
    {
        #region 序列化字段

        [Tooltip("状态唯一标识 ID")]
        [SerializeField] private string _stateId = string.Empty;

        [Tooltip("可选：嵌套引用的子状态层配置。若配置则表示该节点包含子状态机")]
        [SerializeField] private StateLayerConfig _subLayerConfig;

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public StateNodeConfig()
        {
        }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        /// <param name="stateId">状态唯一标识 ID</param>
        /// <param name="subLayerConfig">嵌套子 Layer 配置</param>
        public StateNodeConfig(string stateId, StateLayerConfig subLayerConfig = null)
        {
            _stateId = stateId;
            _subLayerConfig = subLayerConfig;
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 状态唯一标识 ID
        /// </summary>
        public string StateId => _stateId;

        /// <summary>
        /// 嵌套子状态层配置引用
        /// </summary>
        public StateLayerConfig SubLayerConfig => _subLayerConfig;

        /// <summary>
        /// 是否包含子状态层
        /// </summary>
        public bool HasSubLayer => _subLayerConfig != null;

        #endregion
    }
}
