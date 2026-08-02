using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwc.InventoryEngine.UI
{
    /// <summary>
    /// 背包物品排序模式。
    /// </summary>
    public enum InventorySortMode
    {
        /// <summary>
        /// 保持原始物理槽位顺序。
        /// </summary>
        None = 0,

        /// <summary>
        /// 按物品显示名称升序 (A-Z)。
        /// </summary>
        NameAscending = 1,

        /// <summary>
        /// 按物品显示名称降序 (Z-A)。
        /// </summary>
        NameDescending = 2,

        /// <summary>
        /// 按堆叠数量降序（数量多的在前）。
        /// </summary>
        CountDescending = 3,

        /// <summary>
        /// 按物品分类名称排序。
        /// </summary>
        Category = 4,
    }

    /// <summary>
    /// 背包排序比较器工具集。
    /// 支持 UI 视图排序 (View Sorting) 以及容器物理数据排序 (Data Sorting)。
    /// </summary>
    public static class InventorySortComparers
    {
        #region Public Methods
        /// <summary>
        /// 获取物品槽位在指定名称/分类/数量条件下的对比结果。
        /// </summary>
        public static int CompareSlots(ItemSlot slotA, ItemSlot slotB, InventorySortMode sortMode)
        {
            if (slotA.IsEmpty && slotB.IsEmpty) return 0;
            if (slotA.IsEmpty) return 1; // 空槽位靠后
            if (slotB.IsEmpty) return -1;

            ItemInstance itemA = slotA.Item;
            ItemInstance itemB = slotB.Item;

            switch (sortMode)
            {
                case InventorySortMode.NameAscending:
                    return string.Compare(GetItemDisplayName(itemA), GetItemDisplayName(itemB), StringComparison.OrdinalIgnoreCase);

                case InventorySortMode.NameDescending:
                    return string.Compare(GetItemDisplayName(itemB), GetItemDisplayName(itemA), StringComparison.OrdinalIgnoreCase);

                case InventorySortMode.CountDescending:
                    int countCompare = itemB.StackCount.CompareTo(itemA.StackCount);
                    if (countCompare != 0) return countCompare;
                    return string.Compare(GetItemDisplayName(itemA), GetItemDisplayName(itemB), StringComparison.OrdinalIgnoreCase);

                case InventorySortMode.Category:
                    int categoryCompare = string.Compare(GetItemCategory(itemA), GetItemCategory(itemB), StringComparison.OrdinalIgnoreCase);
                    if (categoryCompare != 0) return categoryCompare;
                    return string.Compare(GetItemDisplayName(itemA), GetItemDisplayName(itemB), StringComparison.OrdinalIgnoreCase);

                case InventorySortMode.None:
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 零 GC 获取物品显示名称。
        /// 读取实现了 IItemDisplay 接口且优先级最高的运行时组件，若未包含显示组件则回退使用 ScriptableObject 资产名称。
        /// </summary>
        public static string GetItemDisplayName(ItemInstance item)
        {
            if (item == null) return string.Empty;

            if (item.TryGetComponent<IItemDisplay>(out var display) && !string.IsNullOrEmpty(display.DisplayName))
            {
                return display.DisplayName;
            }

            return item.Definition != null ? item.Definition.name : string.Empty;
        }

        /// <summary>
        /// 零 GC 获取物品分类信息。
        /// 从挂载的 IItemCategorized 组件获取分类名称。
        /// </summary>
        public static string GetItemCategory(ItemInstance item)
        {
            if (item == null) return string.Empty;

            if (item.TryGetComponent<IItemCategorized>(out var categorized))
            {
                var cats = new List<ItemCategorySO>();
                categorized.GetCategories(cats);
                if (cats.Count > 0 && cats[0] != null)
                {
                    return cats[0].DisplayName;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 零 GC 获取物品图标 Sprite。
        /// 读取实现了 IItemDisplay 接口且优先级最高的运行时组件。
        /// </summary>
        public static Sprite GetItemIcon(ItemInstance item)
        {
            if (item == null) return null;

            if (item.TryGetComponent<IItemDisplay>(out var display) && display.Icon != null)
            {
                return display.Icon;
            }

            return null;
        }

        /// <summary>
        /// 零 GC 获取物品详细描述信息。
        /// 读取实现了 IItemDisplay 接口且优先级最高的运行时组件。
        /// </summary>
        public static string GetItemDescription(ItemInstance item)
        {
            if (item == null) return string.Empty;

            if (item.TryGetComponent<IItemDisplay>(out var display) && !string.IsNullOrEmpty(display.Description))
            {
                return display.Description;
            }

            return string.Empty;
        }
        #endregion
    }

    /// <summary>
    /// 用于底层数据排序 InventoryContainer.AutoSort 的 IComparer 实现。
    /// </summary>
    public class InventoryDataSlotComparer : IComparer<ItemSlot>
    {
        #region Private Fields
        private readonly InventorySortMode _sortMode;
        #endregion

        #region Constructors
        public InventoryDataSlotComparer(InventorySortMode sortMode)
        {
            _sortMode = sortMode;
        }
        #endregion

        #region Public Methods
        public int Compare(ItemSlot x, ItemSlot y)
        {
            return InventorySortComparers.CompareSlots(x, y, _sortMode);
        }
        #endregion
    }
}
