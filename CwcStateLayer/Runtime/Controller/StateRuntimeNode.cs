using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 运行时状态节点内存对象，包含构建后的层级路径、预计算哈希与父级引用。
    /// </summary>
    public class StateRuntimeNode
    {
        #region 非序列化私有字段

        private readonly StateNodeConfig _config;
        private readonly string _fullPath;
        private readonly int _fullPathHash;
        private readonly int _stateIdHash;
        private readonly int _depth;
        private readonly StateRuntimeNode _parent;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化运行时状态节点
        /// </summary>
        /// <param name="config">静态配置节点</param>
        /// <param name="fullPath">完整层级路径</param>
        /// <param name="depth">当前树层级深度</param>
        /// <param name="parent">父节点引用</param>
        public StateRuntimeNode(
            StateNodeConfig config,
            string fullPath,
            int depth,
            StateRuntimeNode parent = null)
        {
            _config = config;
            _fullPath = fullPath ?? string.Empty;
            _fullPathHash = StatePathUtility.StringToHash(_fullPath);
            _stateIdHash = StatePathUtility.StringToHash(_config?.StateId);
            _depth = depth;
            _parent = parent;
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 对应的静态节点配置
        /// </summary>
        public StateNodeConfig Config => _config;

        /// <summary>
        /// 状态 ID
        /// </summary>
        public string StateId => _config?.StateId ?? string.Empty;

        /// <summary>
        /// 状态 ID 哈希值
        /// </summary>
        public int StateIdHash => _stateIdHash;

        /// <summary>
        /// 完整层级路径
        /// </summary>
        public string FullPath => _fullPath;

        /// <summary>
        /// 完整层级路径哈希值
        /// </summary>
        public int FullPathHash => _fullPathHash;

        /// <summary>
        /// 当前节点的树层级深度
        /// </summary>
        public int Depth => _depth;

        /// <summary>
        /// 父节点引用
        /// </summary>
        public StateRuntimeNode Parent => _parent;

        #endregion
    }
}
