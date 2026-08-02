using System.Collections.Generic;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 通用物品分类运行时组件，挂载在 ItemInstance 上。
    /// 包含引用的分类列表，实现 IItemCategorized 接口。
    /// 适用于任意类型的物品（药水、材料、卷轴、任务物品等）。
    /// </summary>
    /// <summary>
    /// 通用物品分类运行时组件，挂载在 ItemInstance 上。
    /// 继承泛型基类 ItemComponentBase<ItemCategoryComponentDefinition>，直接持有一份配对的静态定义引用。
    /// 实现 IItemCategorized 接口，代理分类列表并支持动态添加分类标签。
    /// </summary>
    public class ItemCategoryComponent : ItemComponentBase<ItemCategoryComponentDefinition>, IItemCategorized
    {
        #region Private Fields
        private readonly List<ItemCategorySO> _dynamicCategories = new();
        #endregion

        #region Public Properties
        /// <summary>
        /// 物品分类 SO 资产只读列表。
        /// 包含静态定义中的分类与运行时动态追加的分类。
        /// </summary>
        public IReadOnlyList<ItemCategorySO> Categories
        {
            get
            {
                if (_dynamicCategories.Count == 0 && Definition != null)
                {
                    return Definition.Categories;
                }

                var combined = new List<ItemCategorySO>();
                if (Definition != null && Definition.Categories != null)
                {
                    int defCount = Definition.Categories.Count;
                    for (int i = 0; i < defCount; i++)
                    {
                        var cat = Definition.Categories[i];
                        if (cat != null && !combined.Contains(cat))
                        {
                            combined.Add(cat);
                        }
                    }
                }

                int dynCount = _dynamicCategories.Count;
                for (int i = 0; i < dynCount; i++)
                {
                    var cat = _dynamicCategories[i];
                    if (cat != null && !combined.Contains(cat))
                    {
                        combined.Add(cat);
                    }
                }

                return combined;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// 由 ItemCategoryComponentDefinition 传递自身引用初始化的标准构造函数。
        /// </summary>
        public ItemCategoryComponent(ItemCategoryComponentDefinition definition) : base(definition)
        {
        }
        #endregion

        #region Dynamic Mutation Methods
        /// <summary>
        /// 动态向物品追加额外的分类标签（如给普通武器附加动态“神圣”分类）。
        /// </summary>
        public void AddDynamicCategory(ItemCategorySO category)
        {
            if (category != null && !_dynamicCategories.Contains(category))
            {
                _dynamicCategories.Add(category);
            }
        }

        /// <summary>
        /// 移除动态追加的分类标签。
        /// </summary>
        public bool RemoveDynamicCategory(ItemCategorySO category)
        {
            return _dynamicCategories.Remove(category);
        }
        #endregion

        #region IItemCategorized Implementation
        /// <summary>
        /// 实现 IItemCategorized 接口，向物品贡献分类/标签列表。
        /// </summary>
        public void GetCategories(List<ItemCategorySO> results)
        {
            if (results == null) return;
            var cats = Categories;
            int count = cats.Count;
            for (int i = 0; i < count; i++)
            {
                if (cats[i] != null)
                {
                    results.Add(cats[i]);
                }
            }
        }
        #endregion
    }
}
