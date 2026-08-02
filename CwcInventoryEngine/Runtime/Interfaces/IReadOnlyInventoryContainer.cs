using System;
using System.Collections.Generic;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 背包物品容器只读接口。
    /// 为 UI 视图层与只读查询服务提供类型安全的只读视图。
    /// 仅暴露只读属性、插槽访问与变更通知事件，严禁任何写或数据修改 API 入口，彻底实现读写隔离。
    /// </summary>
    public interface IReadOnlyInventoryContainer
    {
        #region Events
        /// <summary>
        /// 当单个槽位发生数据更新时触发的响应式事件。(slotIndex, slotData)
        /// </summary>
        event Action<int, ItemSlot> OnSlotUpdated;

        /// <summary>
        /// 当批处理 Block 完成，统一刷出更改时触发的事件。
        /// </summary>
        event Action OnBatchCompleted;
        #endregion

        #region Properties
        /// <summary>
        /// 容器总容量。
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// 槽位数组。
        /// </summary>
        ItemSlot[] Slots { get; }
        #endregion

        #region Query Methods
        /// <summary>
        /// 安全获取指定索引槽位实例。
        /// </summary>
        /// <param name="slotIndex">槽位索引</param>
        /// <returns>若索引有效则返回 ItemSlot，否则返回 null</returns>
        ItemSlot GetSlot(int slotIndex);

        /// <summary>
        /// 检查槽位索引是否在当前容器容量有效范围内。
        /// </summary>
        /// <param name="slotIndex">槽位索引</param>
        /// <returns>是否有效</returns>
        bool IsValidIndex(int slotIndex);
        #endregion
    }
}
