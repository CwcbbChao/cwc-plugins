using System.Collections.Generic;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 可分类物品组件接口。
    /// 任何组件只需实现此接口，即可向物品贡献一个或多个分类/标签。
    /// 支持在一个物品上挂载多个不同的组件，每个组件各自贡献类型。
    /// </summary>
    public interface IItemCategorized
    {
        /// <summary>
        /// 零 GC 收集该组件贡献的所有物品分类/标签资产。
        /// </summary>
        /// <param name="results">输出结果列表</param>
        void GetCategories(List<ItemCategorySO> results);
    }
}
