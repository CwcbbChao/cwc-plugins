namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 槽位放入限制过滤器接口。
    /// 用于限制特定槽位只能放入符合规则的物品（如装备槽只能放入装备类物品）。
    /// </summary>
    public interface IItemFilter
    {
        /// <summary>
        /// 判定指定物品是否可以放入容器的特定槽位。
        /// </summary>
        /// <param name="container">目标容器</param>
        /// <param name="slotIndex">目标槽位索引</param>
        /// <param name="item">待放入的物品实例</param>
        /// <returns>若允许放入返回 true，否则返回 false</returns>
        bool CanPlaceInSlot(IReadOnlyInventoryContainer container, int slotIndex, ItemInstance item);
    }
}
