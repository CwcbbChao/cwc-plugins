namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 背包容器槽位。
    /// 承载具体物品实例，并提供特定过滤规则校验。
    /// </summary>
    public class ItemSlot
    {
        #region Public Properties
        /// <summary>
        /// 槽位在容器中的索引位置。
        /// </summary>
        public int SlotIndex { get; }

        /// <summary>
        /// 当前槽位存放的物品实例。
        /// </summary>
        public ItemInstance Item { get; internal set; }

        /// <summary>
        /// 槽位特化的过滤器规则。为 null 表示不限制。
        /// </summary>
        public IItemFilter Filter { get; set; }

        /// <summary>
        /// 槽位是否处于禁用状态。若为 true，则禁止移入或放入物品。
        /// </summary>
        public bool IsDisabled { get; set; }

        /// <summary>
        /// 当前槽位是否为空。
        /// </summary>
        public bool IsEmpty => Item == null || Item.StackCount <= 0;
        #endregion

        #region Constructors
        /// <summary>
        /// 构造一个槽位。
        /// </summary>
        /// <param name="slotIndex">槽位索引</param>
        /// <param name="filter">槽位过滤器（可选）</param>
        public ItemSlot(int slotIndex, IItemFilter filter = null)
        {
            SlotIndex = slotIndex;
            Filter = filter;
            Item = null;
            IsDisabled = false;
        }
        #endregion

        #region Public Verification Methods
        /// <summary>
        /// 判定当前槽位是否允许接收目标物品。
        /// </summary>
        /// <param name="container">所属容器</param>
        /// <param name="item">待放入的物品实例</param>
        /// <returns>若允许放入返回 true，否则返回 false</returns>
        public bool CanAccept(IReadOnlyInventoryContainer container, ItemInstance item)
        {
            if (IsDisabled) return false;
            if (item == null) return true;
            if (Filter == null) return true;

            return Filter.CanPlaceInSlot(container, SlotIndex, item);
        }

        /// <summary>
        /// 清空当前槽位引用的物品。
        /// </summary>
        public void Clear()
        {
            Item = null;
        }
        #endregion
    }
}
