using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 单个状态过渡条件项，定义具体的 FromPath -> ToPath 路径匹配对。
    /// 包含预解析 Pattern 用于零堆内存匹配。
    /// </summary>
    [Serializable]
    public class StateTransitionCondition
    {
        #region 序列化字段

        [Tooltip("匹配的来源状态路径或 ID（支持 Any、父层通配如 MainLayer/UI/Any）")]
        [StatePath]
        [SerializeField] private string _fromPath = "Any";

        [Tooltip("匹配的目标状态路径或 ID（支持 Any、父层通配如 MainLayer/InGame/Any）")]
        [StatePath]
        [SerializeField] private string _toPath = "Any";

        #endregion

        #region 非序列化私有字段

        [NonSerialized] private CompiledStatePathPattern _compiledFrom;
        [NonSerialized] private CompiledStatePathPattern _compiledTo;
        [NonSerialized] private bool _isCompiled;

        #endregion

        #region 构造函数

        public StateTransitionCondition()
        {
        }

        public StateTransitionCondition(string fromPath, string toPath)
        {
            _fromPath = string.IsNullOrEmpty(fromPath) ? "Any" : fromPath;
            _toPath = string.IsNullOrEmpty(toPath) ? "Any" : toPath;
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 来源状态路径规则
        /// </summary>
        public string FromPath
        {
            get => _fromPath;
            set
            {
                _fromPath = string.IsNullOrEmpty(value) ? "Any" : value;
                _isCompiled = false;
            }
        }

        /// <summary>
        /// 目标状态路径规则
        /// </summary>
        public string ToPath
        {
            get => _toPath;
            set
            {
                _toPath = string.IsNullOrEmpty(value) ? "Any" : value;
                _isCompiled = false;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 评估给定的状态变更上下文是否符合本条件
        /// </summary>
        public bool IsMatched(in StateChangeContext context)
        {
            if (!_isCompiled)
            {
                _compiledFrom = new CompiledStatePathPattern(_fromPath);
                _compiledTo = new CompiledStatePathPattern(_toPath);
                _isCompiled = true;
            }

            return StatePathMatcher.IsContextMatched(in _compiledFrom, in _compiledTo, in context);
        }

        #endregion
    }
}
