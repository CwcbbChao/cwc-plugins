namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 掉落物/可拾取对象通用接口。
    /// 所有可在场景中被玩家或路由器拾取吸收的 GameObject/组件需实现此接口。
    /// </summary>
    public interface IItemPickup
    {
        /// <summary>
        /// 当前掉落物持有的运行时物品实例。
        /// </summary>
        ItemInstance CurrentItem { get; }

        /// <summary>
        /// 尝试向目标库存组件放入该掉落物中的物品。
        /// </summary>
        /// <param name="targetInventory">目标库存组件</param>
        /// <returns>若成功放入至少 1 个数量返回 true，否则返回 false</returns>
        bool TryPickup(Inventory targetInventory);
    }
}
