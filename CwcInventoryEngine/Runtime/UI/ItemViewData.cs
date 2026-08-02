using UnityEngine;

namespace Cwc.InventoryEngine.UI
{
    /// <summary>
    /// 纯粹的 UI 视图数据包 (ViewModel / DTO)。
    /// 不包含任何底层数据层引用。UI 视图 (View) 仅消费此结构体，实现与 Model 的彻底解耦。
    /// 使用 readonly struct 保证高性能与零 GC 开销。
    /// </summary>
    public readonly struct ItemViewData
    {
        #region Public Readonly Fields
        /// <summary>
        /// 物品唯一实例标识 (值类型)。
        /// </summary>
        public readonly ItemId InstanceId;

        /// <summary>
        /// 物品显示名称。
        /// </summary>
        public readonly string DisplayName;

        /// <summary>
        /// 物品图标 Sprite。
        /// </summary>
        public readonly Sprite Icon;

        /// <summary>
        /// 物品详细描述信息。
        /// </summary>
        public readonly string Description;

        /// <summary>
        /// 物品分类/类型。
        /// </summary>
        public readonly string Category;

        /// <summary>
        /// 当前堆叠数量。
        /// </summary>
        public readonly int StackCount;

        /// <summary>
        /// 最大堆叠上限。
        /// </summary>
        public readonly int MaxStack;

        /// <summary>
        /// 是否属于空槽位视图。
        /// </summary>
        public readonly bool IsEmpty;
        #endregion

        #region Static Default Property
        /// <summary>
        /// 默认的空槽位视图数据包。
        /// </summary>
        public static ItemViewData Empty => new ItemViewData(default, string.Empty, null, string.Empty, string.Empty, 0, 0, true);
        #endregion

        #region Constructors
        /// <summary>
        /// 构造只读视图数据结构体。
        /// </summary>
        public ItemViewData(
            ItemId instanceId,
            string displayName,
            Sprite icon,
            string description,
            string category,
            int stackCount,
            int maxStack,
            bool isEmpty)
        {
            InstanceId = instanceId;
            DisplayName = displayName;
            Icon = icon;
            Description = description;
            Category = category;
            StackCount = stackCount;
            MaxStack = maxStack;
            IsEmpty = isEmpty;
        }
        #endregion
    }

    /// <summary>
    /// Presenter 转换层扩展方法。
    /// 负责将领域层 Model (ItemSlot/ItemInstance) 转换/解构为纯 UI 的 ItemViewData。
    /// </summary>
    public static class ItemSlotUIExtensions
    {
        #region Extension Methods
        /// <summary>
        /// 将底层 ItemSlot 转换为 UI 专属的 ItemViewData。
        /// </summary>
        /// <param name="slot">底层槽位数据</param>
        /// <returns>只读视图数据包</returns>
        public static ItemViewData ToViewData(this ItemSlot slot)
        {
            if (slot == null || slot.IsEmpty)
            {
                return ItemViewData.Empty;
            }

            ItemInstance item = slot.Item;
            string displayName = InventorySortComparers.GetItemDisplayName(item);
            Sprite icon = InventorySortComparers.GetItemIcon(item);
            string description = InventorySortComparers.GetItemDescription(item);
            string category = InventorySortComparers.GetItemCategory(item);
            int maxStack = item.Definition != null ? item.Definition.MaxStack : 1;

            return new ItemViewData(
                item.InstanceID,
                displayName,
                icon,
                description,
                category,
                item.StackCount,
                maxStack,
                false
            );
        }
        #endregion
    }
}
