using System;
using System.Collections.Generic;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 背包槽位存储映射项 DTO。
    /// </summary>
    [Serializable]
    public class SlotSaveEntry
    {
        public int SlotIndex;
        public ItemSaveData ItemData;
    }

    /// <summary>
    /// 整个背包容器的存盘 DTO。
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        #region Serialized DTO Fields
        /// <summary>
        /// 容器的导出容量。
        /// </summary>
        public int Capacity;

        /// <summary>
        /// 非空槽位存盘列表。
        /// </summary>
        public List<SlotSaveEntry> Slots = new();
        #endregion

        #region Export / Restore Logic
        /// <summary>
        /// 将指定容器导出为存盘 DTO 数据。
        /// </summary>
        public static InventorySaveData Export(InventoryContainer container, IItemAssetResolver resolver)
        {
            if (container == null || resolver == null) return null;

            InventorySaveData saveData = new InventorySaveData
            {
                Capacity = container.Capacity,
                Slots = new List<SlotSaveEntry>()
            };

            for (int i = 0; i < container.Capacity; i++)
            {
                var slot = container.Slots[i];
                if (!slot.IsEmpty)
                {
                    ItemSaveData itemData = ItemSaveData.Export(slot.Item, resolver);
                    if (itemData != null)
                    {
                        saveData.Slots.Add(new SlotSaveEntry
                        {
                            SlotIndex = i,
                            ItemData = itemData
                        });
                    }
                }
            }

            return saveData;
        }

        /// <summary>
        /// 将存盘 DTO 还原回指定容器中。
        /// </summary>
        public void RestoreToContainer(InventoryContainer container, IItemAssetResolver resolver)
        {
            if (container == null || resolver == null) return;

            using (container.BatchScope())
            {
                // 先清空容器
                container.ClearContainer();

                if (Slots == null) return;

                int count = Slots.Count;
                for (int i = 0; i < count; i++)
                {
                    var entry = Slots[i];
                    if (entry == null || entry.SlotIndex < 0 || entry.SlotIndex >= container.Capacity) continue;
                    if (entry.ItemData == null) continue;

                    ItemInstance item = entry.ItemData.Restore(resolver);
                    if (item != null)
                    {
                        container.TryAddItemToSlot(entry.SlotIndex, item, out _);
                    }
                }
            }
        }
        #endregion
    }
}
