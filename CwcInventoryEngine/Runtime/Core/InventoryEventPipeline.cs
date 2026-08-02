using System;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 库存事件管线广播事件类型。
    /// </summary>
    public enum InventoryEventType
    {
        /// <summary>
        /// 新库存容器已注册到注册表。
        /// </summary>
        Registered = 0,

        /// <summary>
        /// 库存容器已注销。
        /// </summary>
        Unregistered = 1,

        /// <summary>
        /// 背包整体内容或批量改变。
        /// </summary>
        ContentChanged = 2,

        /// <summary>
        /// 单个槽位更新。
        /// </summary>
        SlotUpdated = 3,

        /// <summary>
        /// 请求打开指定 ID 的背包 UI 界面。
        /// </summary>
        OpenRequest = 4,

        /// <summary>
        /// 请求关闭指定 ID 的背包 UI 界面。
        /// </summary>
        CloseRequest = 5,

        /// <summary>
        /// UI 中的选中项目改变。
        /// </summary>
        SelectionChanged = 6,
    }

    /// <summary>
    /// 全局库存事件管线数据包 (只读结构体, 零 GC)。
    /// </summary>
    public readonly struct InventoryEvent
    {
        #region Readonly Fields
        public readonly InventoryEventType EventType;
        public readonly string InventoryId;
        public readonly int SlotIndex;
        public readonly InventoryContainer Container;
        public readonly ItemInstance Item;
        #endregion

        #region Constructors
        public InventoryEvent(InventoryEventType eventType, string inventoryId, InventoryContainer container = null, int slotIndex = -1, ItemInstance item = null)
        {
            EventType = eventType;
            InventoryId = inventoryId;
            Container = container;
            SlotIndex = slotIndex;
            Item = item;
        }
        #endregion
    }

    /// <summary>
    /// 全局库存事件管线 (Event Pipeline)。
    /// 解耦 UI 视图与逻辑数据层。任何位置均可通过管线发布/监听库存广播。
    /// </summary>
    public static class InventoryEventPipeline
    {
        #region Public Static Events
        /// <summary>
        /// 全局事件管线委托广播。
        /// </summary>
        public static event Action<InventoryEvent> OnEvent;
        #endregion

        #region Public Static Methods
        /// <summary>
        /// 发布事件到全局事件管线。
        /// 使用 in 关键字传递结构体，实现零 GC 开销。
        /// </summary>
        public static void Publish(in InventoryEvent evt)
        {
            OnEvent?.Invoke(evt);
        }

        /// <summary>
        /// 快捷发布：指定库存内容发生改变。
        /// </summary>
        public static void PublishContentChanged(string inventoryId, InventoryContainer container = null)
        {
            Publish(new InventoryEvent(InventoryEventType.ContentChanged, inventoryId, container));
        }

        /// <summary>
        /// 快捷发布：请求打开指定 InventoryID 的界面。
        /// </summary>
        public static void PublishOpenRequest(string inventoryId)
        {
            Publish(new InventoryEvent(InventoryEventType.OpenRequest, inventoryId));
        }

        /// <summary>
        /// 快捷发布：请求关闭指定 InventoryID 的界面。
        /// </summary>
        public static void PublishCloseRequest(string inventoryId)
        {
            Publish(new InventoryEvent(InventoryEventType.CloseRequest, inventoryId));
        }
        #endregion
    }
}
