using System;
using System.Collections;
using System.Collections.Generic;

namespace Cwc.InventoryEngine.Query
{
    #region Interfaces
    /// <summary>
    /// 物品过滤筛选规则接口。
    /// 所有原子筛选逻辑（相等、区间、包含）以及组合逻辑均实现此接口。
    /// </summary>
    public interface IItemRule
    {
        #region Public Methods
        /// <summary>
        /// 评估指定物品实例是否满足当前规则条件。
        /// </summary>
        /// <param name="item">待评估的物品实例</param>
        /// <returns>若满足条件返回 true，否则返回 false</returns>
        bool Matches(ItemInstance item);
        #endregion
    }
    #endregion

    #region Atomic Rules
    /// <summary>
    /// 属性相等/对象匹配规则。
    /// 检查指定 Key 的属性值是否与目标值相等。
    /// </summary>
    public class PropertyEqualsRule : IItemRule
    {
        #region Private Fields
        private readonly string _key;
        private readonly ItemPropertyValue _targetValue;
        private readonly StringComparison _stringComparison;
        #endregion

        #region Constructors
        /// <summary>
        /// 构造一个属性相等规则。
        /// </summary>
        /// <param name="key">目标属性 Key</param>
        /// <param name="targetValue">期望匹配的目标轻量属性值</param>
        /// <param name="stringComparison">字符串比较模式 (默认忽略大小写)</param>
        public PropertyEqualsRule(string key, ItemPropertyValue targetValue, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            _key = key;
            _targetValue = targetValue;
            _stringComparison = stringComparison;
        }
        #endregion

        #region Public Methods
        public bool Matches(ItemInstance item)
        {
            if (item == null) return false;
            if (!ItemPropertyEvaluator.TryGetPropertyValue(item, _key, out var val) || val.IsEmpty) return false;

            if (val.Type == ItemPropertyValueType.String && _targetValue.Type == ItemPropertyValueType.String)
            {
                return string.Equals(val.StringValue, _targetValue.StringValue, _stringComparison);
            }

            return val.Equals(_targetValue);
        }
        #endregion
    }

    /// <summary>
    /// 数值范围匹配规则。
    /// 检查数值属性是否在 [Min, Max] 区间之内 (零 GC)。
    /// </summary>
    public class PropertyRangeRule : IItemRule
    {
        #region Private Fields
        private readonly string _key;
        private readonly ItemPropertyValue _min;
        private readonly ItemPropertyValue _max;
        #endregion

        #region Constructors
        /// <summary>
        /// 构造一个数值范围限制规则。
        /// </summary>
        /// <param name="key">目标属性 Key</param>
        /// <param name="min">最小值（传入 ItemPropertyValue.Empty 表示无下限）</param>
        /// <param name="max">最大值（传入 ItemPropertyValue.Empty 表示无上限）</param>
        public PropertyRangeRule(string key, ItemPropertyValue min = default, ItemPropertyValue max = default)
        {
            _key = key;
            _min = min;
            _max = max;
        }
        #endregion

        #region Public Methods
        public bool Matches(ItemInstance item)
        {
            if (item == null) return false;
            if (!ItemPropertyEvaluator.TryGetPropertyValue(item, _key, out var val) || val.IsEmpty) return false;

            if (!_min.IsEmpty && val.CompareTo(_min) < 0) return false;
            if (!_max.IsEmpty && val.CompareTo(_max) > 0) return false;
            return true;
        }
        #endregion
    }

    /// <summary>
    /// 包含匹配规则。
    /// 检查属性值（如词缀列表、字符串名称）是否包含指定的元素或子串。
    /// </summary>
    public class PropertyContainsRule : IItemRule
    {
        #region Private Fields
        private readonly string _key;
        private readonly ItemPropertyValue _elementOrSubString;
        private readonly StringComparison _stringComparison;
        #endregion

        #region Constructors
        /// <summary>
        /// 构造一个包含匹配规则。
        /// </summary>
        /// <param name="key">目标属性 Key</param>
        /// <param name="elementOrSubString">待包含的元素或字符串</param>
        /// <param name="stringComparison">字符串比较模式 (默认忽略大小写)</param>
        public PropertyContainsRule(string key, ItemPropertyValue elementOrSubString, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            _key = key;
            _elementOrSubString = elementOrSubString;
            _stringComparison = stringComparison;
        }
        #endregion

        #region Public Methods
        public bool Matches(ItemInstance item)
        {
            if (item == null) return false;
            if (!ItemPropertyEvaluator.TryGetPropertyValue(item, _key, out var val) || val.IsEmpty) return false;

            // 1. 若属性本身是字符串
            if (val.Type == ItemPropertyValueType.String && _elementOrSubString.Type == ItemPropertyValueType.String)
            {
                return val.StringValue.IndexOf(_elementOrSubString.StringValue, _stringComparison) >= 0;
            }

            // 2. 若属性是集合类型（例如 List<string> 词缀列表）
            if (val.ObjectValue is IEnumerable enumerable)
            {
                string matchStr = _elementOrSubString.StringValue;
                foreach (var elem in enumerable)
                {
                    if (elem is string elemStr && _elementOrSubString.Type == ItemPropertyValueType.String)
                    {
                        if (string.Equals(elemStr, matchStr, _stringComparison)) return true;
                    }
                    else if (Equals(elem, _elementOrSubString.ObjectValue))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        #endregion
    }
    #endregion

    #region Composite Rules
    /// <summary>
    /// 逻辑与 (AND) 组合规则链。
    /// 必须所有子规则均满足才通过匹配。
    /// </summary>
    public class CompositeAndRule : IItemRule
    {
        #region Private Fields
        private readonly List<IItemRule> _rules = new();
        #endregion

        #region Constructors
        /// <summary>
        /// 构造一个空逻辑与规则链。
        /// </summary>
        public CompositeAndRule() { }

        /// <summary>
        /// 构造包含初始规则的逻辑与规则链。
        /// </summary>
        public CompositeAndRule(params IItemRule[] rules)
        {
            if (rules != null)
            {
                _rules.AddRange(rules);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 链式添加子规则。
        /// </summary>
        public CompositeAndRule Add(IItemRule rule)
        {
            if (rule != null)
            {
                _rules.Add(rule);
            }
            return this;
        }

        public bool Matches(ItemInstance item)
        {
            int count = _rules.Count;
            for (int i = 0; i < count; i++)
            {
                if (!_rules[i].Matches(item)) return false;
            }
            return true;
        }
        #endregion
    }

    /// <summary>
    /// 逻辑或 (OR) 组合规则链。
    /// 只要任意一个子规则满足即通过匹配。
    /// </summary>
    public class CompositeOrRule : IItemRule
    {
        #region Private Fields
        private readonly List<IItemRule> _rules = new();
        #endregion

        #region Constructors
        /// <summary>
        /// 构造一个空逻辑或规则链。
        /// </summary>
        public CompositeOrRule() { }

        /// <summary>
        /// 构造包含初始规则的逻辑或规则链。
        /// </summary>
        public CompositeOrRule(params IItemRule[] rules)
        {
            if (rules != null)
            {
                _rules.AddRange(rules);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 链式添加子规则。
        /// </summary>
        public CompositeOrRule Add(IItemRule rule)
        {
            if (rule != null)
            {
                _rules.Add(rule);
            }
            return this;
        }

        public bool Matches(ItemInstance item)
        {
            int count = _rules.Count;
            if (count == 0) return true;

            for (int i = 0; i < count; i++)
            {
                if (_rules[i].Matches(item)) return true;
            }
            return false;
        }
        #endregion
    }

    /// <summary>
    /// 逻辑非 (NOT) 规则。
    /// 取目标规则评估结果的反值。
    /// </summary>
    public class NotRule : IItemRule
    {
        #region Private Fields
        private readonly IItemRule _rule;
        #endregion

        #region Constructors
        /// <summary>
        /// 构造取反规则。
        /// </summary>
        public NotRule(IItemRule rule)
        {
            _rule = rule;
        }
        #endregion

        #region Public Methods
        public bool Matches(ItemInstance item)
        {
            if (_rule == null) return true;
            return !_rule.Matches(item);
        }
        #endregion
    }
    #endregion
}
