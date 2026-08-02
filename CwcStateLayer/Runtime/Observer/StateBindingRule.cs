using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 普通状态绑定规则配置项，支持配置多组 FromPath -> ToPath 过渡条件（满足任意一条即触发）与匹配后中断标识 StopOnMatch。
    /// </summary>
    [Serializable]
    public class StateBindingRule
    {
        #region 序列化字段

        [Tooltip("触发本规则的状态过渡条件列表（支持多条件 OR 逻辑）")]
        [SerializeField] private List<StateTransitionCondition> _conditions = new List<StateTransitionCondition>();

        [Tooltip("规则匹配成功后是否中断后续规则的评估与触发")]
        [SerializeField] private bool _stopOnMatch = false;

        #endregion

        #region 构造函数

        public StateBindingRule()
        {
            _conditions.Add(new StateTransitionCondition("Any", "Any"));
        }

        public StateBindingRule(string fromPath, string toPath, bool stopOnMatch = false)
        {
            _conditions.Add(new StateTransitionCondition(fromPath, toPath));
            _stopOnMatch = stopOnMatch;
        }

        public StateBindingRule(List<StateTransitionCondition> conditions, bool stopOnMatch = false)
        {
            if (conditions != null && conditions.Count > 0)
            {
                _conditions = conditions;
            }
            else
            {
                _conditions.Add(new StateTransitionCondition("Any", "Any"));
            }
            _stopOnMatch = stopOnMatch;
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 触发本规则的状态过渡条件列表
        /// </summary>
        public List<StateTransitionCondition> Conditions
        {
            get
            {
                if (_conditions == null || _conditions.Count == 0)
                {
                    _conditions = new List<StateTransitionCondition> { new StateTransitionCondition("Any", "Any") };
                }
                return _conditions;
            }
            set => _conditions = value ?? new List<StateTransitionCondition>();
        }

        /// <summary>
        /// 规则匹配成功后是否中断后续规则评估
        /// </summary>
        public bool StopOnMatch
        {
            get => _stopOnMatch;
            set => _stopOnMatch = value;
        }

        /// <summary>
        /// 兼容接口：获取或设置首个过渡条件的来源状态路径
        /// </summary>
        public string FromPath
        {
            get
            {
                EnsureConditionsNotEmpty();
                return _conditions[0].FromPath;
            }
            set
            {
                EnsureConditionsNotEmpty();
                _conditions[0].FromPath = value;
            }
        }

        /// <summary>
        /// 兼容接口：获取或设置首个过渡条件的目标状态路径
        /// </summary>
        public string ToPath
        {
            get
            {
                EnsureConditionsNotEmpty();
                return _conditions[0].ToPath;
            }
            set
            {
                EnsureConditionsNotEmpty();
                _conditions[0].ToPath = value;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 评估给定的状态变更上下文是否符合本规则中的任意一个条件（OR 匹配）
        /// </summary>
        public bool IsMatched(in StateChangeContext context)
        {
            if (_conditions == null || _conditions.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < _conditions.Count; i++)
            {
                StateTransitionCondition cond = _conditions[i];
                if (cond != null && cond.IsMatched(context))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region 私有辅助方法

        private void EnsureConditionsNotEmpty()
        {
            if (_conditions == null)
            {
                _conditions = new List<StateTransitionCondition>();
            }

            if (_conditions.Count == 0)
            {
                _conditions.Add(new StateTransitionCondition("Any", "Any"));
            }
        }

        #endregion
    }

    /// <summary>
    /// 强类型数据载荷状态绑定规则配置项，继承自 StateBindingRule，附加自定义 TData 数据载荷。
    /// </summary>
    /// <typeparam name="TData">自定义强类型数据载荷类型</typeparam>
    [Serializable]
    public class StateBindingRule<TData> : StateBindingRule
    {
        #region 序列化字段

        [Tooltip("关联的自定义数据/配置载荷")]
        [SerializeField] private TData _data;

        #endregion

        #region 构造函数

        public StateBindingRule()
        {
        }

        public StateBindingRule(string fromPath, string toPath, TData data, bool stopOnMatch = false)
            : base(fromPath, toPath, stopOnMatch)
        {
            _data = data;
        }

        public StateBindingRule(List<StateTransitionCondition> conditions, TData data, bool stopOnMatch = false)
            : base(conditions, stopOnMatch)
        {
            _data = data;
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 关联的配置数据载荷
        /// </summary>
        public TData Data
        {
            get => _data;
            set => _data = value;
        }

        #endregion
    }
}
