namespace Cwc.InventoryEngine
{
    /// <summary>
    /// ItemCategoryFilter 的别名包装。
    /// 统一使用基类 ItemCategoryFilter，此处仅做向下兼容。
    /// </summary>
    public class EquipmentSlotFilter : ItemCategoryFilter
    {
        public EquipmentSlotFilter(ItemCategorySO category) : base(category)
        {
        }
    }
}
