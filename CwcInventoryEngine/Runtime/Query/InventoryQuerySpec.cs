using System;
using System.Collections.Generic;
using Cwc.InventoryEngine.UI;

namespace Cwc.InventoryEngine.Query
{
    /// <summary>
    /// 背包统一查询描述符 (Inventory Query Specification)。
    /// 将【筛选规则 FilterRule】与【排序 Key / 规则 SortKey】聚合打包为一个轻量结构体。
    /// 驱动 UIListView 实现完全面向 Key 的动态筛选与零 GC 排序。
    /// </summary>
    public struct InventoryQuerySpec
    {
        #region Public Fields
        /// <summary>
        /// 物品筛选匹配规则（为 null 表示显示全部未过滤项）。
        /// </summary>
        public IItemRule FilterRule;

        /// <summary>
        /// 排序依据的属性 Key（例如: "RequiredLevel", "AttackBonus", "Name", "StackCount" 等）。
        /// 若为空或为 null，则回退使用 FallbackSortMode。
        /// </summary>
        public string SortKey;

        /// <summary>
        /// 是否升序排列（true 表示升序 A-Z/小到大，false 表示降序 Z-A/大到小）。
        /// </summary>
        public bool SortAscending;

        /// <summary>
        /// 当 SortKey 未提供或两个物品属性相等时的回退排序模式。
        /// </summary>
        public InventorySortMode FallbackSortMode;
        #endregion

        #region Constructors
        /// <summary>
        /// 构造一个统一查询描述符。
        /// </summary>
        public InventoryQuerySpec(IItemRule filterRule, string sortKey = null, bool sortAscending = false, InventorySortMode fallbackSortMode = InventorySortMode.None)
        {
            FilterRule = filterRule;
            SortKey = sortKey;
            SortAscending = sortAscending;
            FallbackSortMode = fallbackSortMode;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 评估槽位是否满足当前查询的筛选条件。
        /// </summary>
        public bool Matches(ItemSlot slot)
        {
            if (slot == null || slot.IsEmpty) return false;
            if (FilterRule == null) return true;

            return FilterRule.Matches(slot.Item);
        }

        /// <summary>
        /// 对两个槽位进行基于动态 SortKey 或回退模式的零 GC 比较。
        /// </summary>
        public int Compare(ItemSlot slotA, ItemSlot slotB)
        {
            if (slotA == null && slotB == null) return 0;
            if (slotA == null || slotA.IsEmpty) return 1; // 空槽位在后
            if (slotB == null || slotB.IsEmpty) return -1;

            // 1. 若指定了动态 SortKey，尝试按 Key 提取轻量属性进行零 GC 比较
            if (!string.IsNullOrEmpty(SortKey))
            {
                ItemPropertyValue valA = ItemPropertyEvaluator.GetPropertyValue(slotA.Item, SortKey);
                ItemPropertyValue valB = ItemPropertyEvaluator.GetPropertyValue(slotB.Item, SortKey);

                int keyCompare = valA.CompareTo(valB);
                if (keyCompare != 0)
                {
                    return SortAscending ? keyCompare : -keyCompare;
                }
            }

            // 2. 若无 SortKey 或两项 Key 属性完全相等，尝试回退模式排序
            if (FallbackSortMode != InventorySortMode.None)
            {
                return InventorySortComparers.CompareSlots(slotA, slotB, FallbackSortMode);
            }

            return 0;
        }
        #endregion

        #region Static Factory Methods
        /// <summary>
        /// 便捷工厂方法：创建针对特定装备槽位的筛选与排序查询描述符（例如：头盔/武器槽，按等级降序）。
        /// </summary>
        /// <param name="slotType">装备槽位名称/分类 (如 "Head", "Weapon")</param>
        /// <param name="maxRequiredLevel">最大装备等级限制 (可选，如当前玩家等级)</param>
        /// <param name="sortKey">排序 Key (默认 "RequiredLevel")</param>
        /// <param name="sortAscending">是否升序 (默认 false 降序，等级高的在前面)</param>
        public static InventoryQuerySpec ForEquipmentSlot(string slotType, ItemPropertyValue maxRequiredLevel = default, string sortKey = "RequiredLevel", bool sortAscending = false)
        {
            var ruleChain = new CompositeAndRule();

            if (!string.IsNullOrEmpty(slotType))
            {
                ruleChain.Add(new PropertyEqualsRule("SlotType", slotType));
            }

            if (!maxRequiredLevel.IsEmpty)
            {
                ruleChain.Add(new PropertyRangeRule("RequiredLevel", max: maxRequiredLevel));
            }

            return new InventoryQuerySpec(ruleChain, sortKey, sortAscending, InventorySortMode.NameAscending);
        }

        /// <summary>
        /// 便捷工厂方法：创建按大分类筛选与排序的描述符。
        /// </summary>
        public static InventoryQuerySpec ForCategory(string categoryName, InventorySortMode sortMode = InventorySortMode.NameAscending)
        {
            IItemRule rule = !string.IsNullOrEmpty(categoryName) ? new PropertyEqualsRule("Category", categoryName) : null;
            return new InventoryQuerySpec(rule, fallbackSortMode: sortMode);
        }

        /// <summary>
        /// 便捷工厂方法：创建全字段模糊关键字搜索的描述符。
        /// </summary>
        public static InventoryQuerySpec ForSearch(string keyword, string sortKey = "Name", bool sortAscending = true)
        {
            IItemRule rule = !string.IsNullOrEmpty(keyword) ? new PropertyContainsRule("Name", keyword) : null;
            return new InventoryQuerySpec(rule, sortKey, sortAscending);
        }
        #endregion
    }
}
