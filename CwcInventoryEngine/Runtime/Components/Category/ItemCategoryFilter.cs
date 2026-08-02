using System.Collections.Generic;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 基于 ItemCategorySO 层级继承链的通用多分类槽位过滤器。
    /// 遍历物品上的所有组件，收集每一个实现 IItemCategorized 接口的组件贡献的全部分类，
    /// 采用 Any 逻辑：只要有任意一个分类符合槽位的继承链，即判定匹配成功！
    /// </summary>
    public class ItemCategoryFilter : IItemFilter
    {
        #region Static Cache
        // 零 GC 临时分类收集缓存列表
        private static readonly List<ItemCategorySO> s_TempCategoryCache = new();
        #endregion

        #region Private Fields
        private readonly List<ItemCategorySO> _allowedCategories;
        #endregion

        #region Public Properties
        /// <summary>
        /// 当前槽位允许接收的物品分类资产列表。
        /// </summary>
        public IReadOnlyList<ItemCategorySO> AllowedCategories => _allowedCategories;
        #endregion

        #region Constructors
        /// <summary>
        /// 构造单分类槽位过滤器。
        /// </summary>
        /// <param name="allowedCategory">槽位允许的物品分类（或父级分类）</param>
        public ItemCategoryFilter(ItemCategorySO allowedCategory)
        {
            _allowedCategories = new List<ItemCategorySO>();
            if (allowedCategory != null)
            {
                _allowedCategories.Add(allowedCategory);
            }
        }

        /// <summary>
        /// 构造多分类槽位过滤器。
        /// </summary>
        /// <param name="allowedCategories">槽位允许的物品分类集合</param>
        public ItemCategoryFilter(IEnumerable<ItemCategorySO> allowedCategories)
        {
            _allowedCategories = new List<ItemCategorySO>();
            if (allowedCategories != null)
            {
                foreach (var cat in allowedCategories)
                {
                    if (cat != null)
                    {
                        _allowedCategories.Add(cat);
                    }
                }
            }
        }
        #endregion

        #region Public Verification Methods
        /// <summary>
        /// 判定目标物品是否可以放入当前槽位。
        /// </summary>
        /// <param name="container">所属容器</param>
        /// <param name="slotIndex">槽位索引</param>
        /// <param name="item">待放入的物品实例</param>
        /// <returns>允许放入返回 true</returns>
        public bool CanPlaceInSlot(IReadOnlyInventoryContainer container, int slotIndex, ItemInstance item)
        {
            // 允许放空（拿走物品）
            if (item == null) return true;

            if (_allowedCategories == null || _allowedCategories.Count == 0) return true;

            var components = item.Components;
            if (components == null) return false;

            lock (s_TempCategoryCache)
            {
                s_TempCategoryCache.Clear();

                // 1. 遍历物品上的【所有组件】，收集每个实现 IItemCategorized 的组件贡献的分类
                int compCount = components.Count;
                for (int i = 0; i < compCount; i++)
                {
                    if (components[i] is IItemCategorized categorized)
                    {
                        categorized.GetCategories(s_TempCategoryCache);
                    }
                }

                // 2. 校验是否有【任意一个】分类（及其父级继承链）符合槽位允许的 AllowedCategories 中的任意一个
                int catCount = s_TempCategoryCache.Count;
                int allowedCount = _allowedCategories.Count;

                for (int i = 0; i < catCount; i++)
                {
                    var cat = s_TempCategoryCache[i];
                    if (cat == null) continue;

                    for (int j = 0; j < allowedCount; j++)
                    {
                        var allowedCat = _allowedCategories[j];
                        if (allowedCat != null && cat.IsSubCategoryOf(allowedCat))
                        {
                            s_TempCategoryCache.Clear();
                            return true; // 任意一个维度匹配即成功！
                        }
                    }
                }

                s_TempCategoryCache.Clear();
            }

            return false;
        }
        #endregion
    }
}
