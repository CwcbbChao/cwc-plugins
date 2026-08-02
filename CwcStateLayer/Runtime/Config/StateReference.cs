using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 可在 Inspector 中直接可视化选择的状态引用结构，方便外部组件精确引用状态并发起零堆内存状态切换。
    /// </summary>
    [Serializable]
    public struct StateReference
    {
        #region 序列化字段

        [Tooltip("引用的状态层配置资产")]
        [SerializeField] private StateLayerConfig _layerConfig;

        [Tooltip("目标状态路径")]
        [StatePath("_layerConfig")]
        [SerializeField] private string _statePath;

        #endregion

        #region 非序列化私有字段

        [NonSerialized] private int _cachedPathHash;
        [NonSerialized] private bool _isHashCached;

        #endregion

        #region 构造函数

        public StateReference(StateLayerConfig config, string statePath)
        {
            _layerConfig = config;
            _statePath = statePath;
            _cachedPathHash = 0;
            _isHashCached = false;
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 关联的配置资产
        /// </summary>
        public StateLayerConfig LayerConfig => _layerConfig;

        /// <summary>
        /// 引用的状态路径
        /// </summary>
        public string StatePath => _statePath;

        /// <summary>
        /// 引用的状态路径哈希值（首次访问时延迟计算并缓存）
        /// </summary>
        public int StatePathHash
        {
            get
            {
                if (!_isHashCached)
                {
                    _cachedPathHash = StatePathUtility.StringToHash(_statePath);
                    _isHashCached = true;
                }

                return _cachedPathHash;
            }
        }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => _layerConfig != null && !string.IsNullOrEmpty(_statePath);

        #endregion

        #region 公共切换接口

        /// <summary>
        /// 对指定的 StateLayer 发起零 GC 状态切换
        /// </summary>
        /// <param name="targetLayer">目标 StateLayer 实例</param>
        /// <returns>切换是否成功</returns>
        public bool ApplyTo(StateLayer targetLayer)
        {
            if (targetLayer == null || string.IsNullOrEmpty(_statePath))
            {
                return false;
            }

            return targetLayer.ChangeState(StatePathHash);
        }

        #endregion
    }
}
