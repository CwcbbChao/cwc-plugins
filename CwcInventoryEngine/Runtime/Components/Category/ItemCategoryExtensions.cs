using System;
using System.Collections.Generic;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 物品分类高内聚判定扩展方法集合。
    /// 将分类提取、继承链匹配彻底收拢内聚在核心功能层，彻底解耦 UI 视图层。
    /// </summary>
    public static class ItemCategoryExtensions
    {
        #region Private Static Cache
        private static readonly List<ItemCategorySO> s_TempCategoryCache = new();
        #endregion

        #region Extension Methods
        /// <summary>
        /// 高内聚判定目标物品是否属于指定的分类 SO（包含多层级继承链支持，零 GC）。
        /// </summary>
        public static bool IsInCategory(this ItemInstance item, ItemCategorySO targetCategory)
        {
            if (item == null || targetCategory == null) return false;

            var components = item.Components;
            if (components == null) return false;

            lock (s_TempCategoryCache)
            {
                s_TempCategoryCache.Clear();

                int compCount = components.Count;
                for (int i = 0; i < compCount; i++)
                {
                    if (components[i] is IItemCategorized categorized)
                    {
                        categorized.GetCategories(s_TempCategoryCache);
                    }
                }

                int catCount = s_TempCategoryCache.Count;
                for (int c = 0; c < catCount; c++)
                {
                    var cat = s_TempCategoryCache[c];
                    if (cat != null && cat.IsSubCategoryOf(targetCategory))
                    {
                        s_TempCategoryCache.Clear();
                        return true; // 多层级继承判定通过！
                    }
                }

                s_TempCategoryCache.Clear();
            }

            return false;
        }

        /// <summary>
        /// 高内聚判定目标物品是否属于指定的分类 String ID / SO 资产名（支持精准比对与层级继承匹配，零 GC）。
        /// </summary>
        public static bool IsInCategory(this ItemInstance item, string categoryId)
        {
            if (item == null || string.IsNullOrEmpty(categoryId)) return false;

            var components = item.Components;
            if (components == null) return false;

            lock (s_TempCategoryCache)
            {
                s_TempCategoryCache.Clear();

                int compCount = components.Count;
                for (int i = 0; i < compCount; i++)
                {
                    if (components[i] is IItemCategorized categorized)
                    {
                        categorized.GetCategories(s_TempCategoryCache);
                    }
                }

                int catCount = s_TempCategoryCache.Count;
                for (int c = 0; c < catCount; c++)
                {
                    var cat = s_TempCategoryCache[c];
                    if (cat == null) continue;

                    // 向上追溯当前分类及其父级链，匹配 SO 资产文件名 (Id)
                    var current = cat;
                    int depth = 0;
                    while (current != null && depth < 20)
                    {
                        if (string.Equals(current.name, categoryId, StringComparison.OrdinalIgnoreCase))
                        {
                            s_TempCategoryCache.Clear();
                            return true;
                        }
                        current = current.ParentCategory;
                        depth++;
                    }
                }

                s_TempCategoryCache.Clear();
            }

            return false;
        }
        #endregion
    }
}
