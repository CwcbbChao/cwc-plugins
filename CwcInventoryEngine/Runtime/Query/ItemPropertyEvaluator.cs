using System;
using System.Collections.Generic;
using UnityEngine;
using Cwc.InventoryEngine.UI;

namespace Cwc.InventoryEngine.Query
{
    /// <summary>
    /// 物品动态属性提取评估器。
    /// 【定位】提供零 GC 分配的通用属性提取机制，专用于支撑 UI 的动态条件筛选 (Filtering) 和 Key 数值/文本排序 (Sorting)。
    /// 优先查询组件实现的 <see cref="IItemPropertyProvider"/> 接口，同时对框架通用属性提供自动兜底代理。
    /// 
    /// ⚠️ 避坑提示：
    /// 基础图文渲染（如 Icon Sprite、DisplayName）请优先调用强类型接口 <see cref="IItemDisplay"/> 或 <see cref="InventorySortComparers.GetItemIcon(ItemInstance)"/>。
    /// </summary>
    public static class ItemPropertyEvaluator
    {
        #region Public Methods
        /// <summary>
        /// 尝试从 ItemInstance 中提取指定 Key 的轻量属性值 (TryGetPropertyValue 的快捷别名)。
        /// </summary>
        public static bool TryGetProperty(ItemInstance item, string key, out ItemPropertyValue value)
        {
            return TryGetPropertyValue(item, key, out value);
        }

        /// <summary>
        /// 尝试从 ItemInstance 中提取指定 Key 的轻量属性值 (零 GC)。
        /// </summary>
        /// <param name="item">目标物品实例</param>
        /// <param name="key">属性 Key (不区分大小写)</param>
        /// <param name="value">提取到的轻量属性值</param>
        /// <returns>若成功提取返回 true，否则返回 false</returns>
        public static bool TryGetPropertyValue(ItemInstance item, string key, out ItemPropertyValue value)
        {
            value = ItemPropertyValue.Empty;
            if (item == null || string.IsNullOrEmpty(key)) return false;

            // 1. 查询运行时组件 (ItemComponentBase) 是否实现了 IItemPropertyProvider
            var components = item.Components;
            if (components != null)
            {
                int compCount = components.Count;
                for (int i = 0; i < compCount; i++)
                {
                    if (components[i] is IItemPropertyProvider provider && provider.TryGetProperty(key, out value))
                    {
                        return true;
                    }
                }
            }

            // 2. 对通用框架内置属性提供兜底回退代理 (Fallback Adapters)
            return TryGetFallbackProperty(item, key, out value);
        }

        /// <summary>
        /// 直接获取轻量属性值，若未找到则返回 ItemPropertyValue.Empty。
        /// </summary>
        public static ItemPropertyValue GetPropertyValue(ItemInstance item, string key)
        {
            TryGetPropertyValue(item, key, out var val);
            return val;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 兜底提取物品的通用内置框架属性 (零 GC)。
        /// </summary>
        private static bool TryGetFallbackProperty(ItemInstance item, string key, out ItemPropertyValue value)
        {
            value = ItemPropertyValue.Empty;

            if (string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "DisplayName", StringComparison.OrdinalIgnoreCase))
            {
                value = InventorySortComparers.GetItemDisplayName(item);
                return true;
            }

            if (string.Equals(key, "Icon", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "Sprite", StringComparison.OrdinalIgnoreCase))
            {
                Sprite icon = InventorySortComparers.GetItemIcon(item);
                if (icon != null)
                {
                    value = new ItemPropertyValue(icon);
                    return true;
                }
            }

            if (string.Equals(key, "Category", StringComparison.OrdinalIgnoreCase))
            {
                value = InventorySortComparers.GetItemCategory(item);
                return true;
            }

            if (string.Equals(key, "StackCount", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "Count", StringComparison.OrdinalIgnoreCase))
            {
                value = item.StackCount;
                return true;
            }

            if (string.Equals(key, "DefinitionName", StringComparison.OrdinalIgnoreCase))
            {
                value = item.Definition != null ? item.Definition.name : string.Empty;
                return true;
            }

            if (string.Equals(key, "IsStackable", StringComparison.OrdinalIgnoreCase))
            {
                value = item.Definition != null && item.Definition.IsStackable;
                return true;
            }

            if (string.Equals(key, "MaxStack", StringComparison.OrdinalIgnoreCase))
            {
                value = item.Definition != null ? item.Definition.MaxStack : 1;
                return true;
            }

            return false;
        }
        #endregion
    }
}

