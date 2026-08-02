using System;
using UnityEngine;
using Cwc.InventoryEngine.UI;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 背包抽象请求基类。
    /// 仅包含 UI 知晓的核心定位数据 (TargetInventoryId, SlotIndex)。
    /// UI 视图无需关心玩家角色引用或物理掉落细节，实现 100% 极简发起。
    /// </summary>
    public abstract class InventoryRequest
    {
        #region 公共属性
        /// <summary>
        /// 目标背包的唯一标识 ID (例如: "MainInventory", "EquipmentInventory")。
        /// </summary>
        public string TargetInventoryId { get; set; }

        /// <summary>
        /// 目标槽位索引 (>=0 表示确切槽位，-1 表示未指定/由 Handler 自动推断)。
        /// </summary>
        public int SlotIndex { get; set; } = -1;
        #endregion

        #region 构造函数
        protected InventoryRequest() { }

        protected InventoryRequest(string targetInventoryId, int slotIndex = -1)
        {
            TargetInventoryId = targetInventoryId;
            SlotIndex = slotIndex;
        }
        #endregion
    }

    /// <summary>
    /// 物品丢弃请求。
    /// UI 仅需提供目标背包 ID 与槽位索引，落点玩家与拾取物 Prefab 由 DropHandler 自动解析。
    /// </summary>
    public class InventoryDropRequest : InventoryRequest
    {
        #region 公共属性
        /// <summary>
        /// 打算丢弃的堆叠数量 (默认 1，-1 表示全量丢弃)。
        /// </summary>
        public int Count { get; set; } = 1;

        /// <summary>
        /// 可选的显式落点位置。若为空，将由 DropHandler 自动查找玩家位置进行离散坐标摆放。
        /// </summary>
        public Vector3? CustomSpawnPosition { get; set; }
        #endregion

        #region 构造函数
        public InventoryDropRequest() { }

        public InventoryDropRequest(string targetInventoryId, int slotIndex, int count = 1, Vector3? customSpawnPosition = null)
            : base(targetInventoryId, slotIndex)
        {
            Count = count;
            CustomSpawnPosition = customSpawnPosition;
        }
        #endregion
    }

    /// <summary>
    /// 物品使用请求。
    /// </summary>
    public class InventoryUseRequest : InventoryRequest
    {
        #region 公共属性
        /// <summary>
        /// 使用数量 (默认为 1)。
        /// </summary>
        public int Count { get; set; } = 1;
        #endregion

        #region 构造函数
        public InventoryUseRequest() { }

        public InventoryUseRequest(string targetInventoryId, int slotIndex, int count = 1)
            : base(targetInventoryId, slotIndex)
        {
            Count = count;
        }
        #endregion
    }

    /// <summary>
    /// 槽位移动 / 交换 / 跨背包转移请求。
    /// 支持指定目标槽位转移，以及自动寻找目标库存可用空栏转移。
    /// </summary>
    public class InventoryMoveRequest : InventoryRequest
    {
        #region 公共属性
        /// <summary>
        /// 源背包唯一标识 ID。
        /// </summary>
        public string SourceInventoryId { get; set; }

        /// <summary>
        /// 源槽位索引。
        /// </summary>
        public int SourceSlotIndex { get; set; }

        /// <summary>
        /// 目标槽位索引 (继承自 SlotIndex 的快捷别名)。
        /// </summary>
        public int TargetSlotIndex => SlotIndex;

        /// <summary>
        /// 是否自动寻找目标库存内的可用空栏/堆叠。
        /// 若为 true，将忽略 TargetSlotIndex，自动寻找目标容器可用的槽位放入。
        /// </summary>
        public bool AutoFindTargetSlot { get; set; } = false;

        /// <summary>
        /// 转移数量 (-1 表示全量移动/交换)。
        /// </summary>
        public int Count { get; set; } = -1;
        #endregion

        #region 静态工厂 API
        /// <summary>
        /// 创建移动到指定目标槽位的请求。
        /// </summary>
        public static InventoryMoveRequest ToSlot(string sourceInventoryId, int sourceSlotIndex, string targetInventoryId, int targetSlotIndex, int count = -1)
        {
            return new InventoryMoveRequest(sourceInventoryId, sourceSlotIndex, targetInventoryId, targetSlotIndex, count);
        }

        /// <summary>
        /// 创建移动到目标库存任意可用空栏/堆叠的请求（如快捷卸下装备到主背包空栏）。
        /// </summary>
        public static InventoryMoveRequest ToAnySlot(string sourceInventoryId, int sourceSlotIndex, string targetInventoryId, int count = -1)
        {
            return new InventoryMoveRequest(sourceInventoryId, sourceSlotIndex, targetInventoryId, autoFindTargetSlot: true, count: count);
        }
        #endregion

        #region 构造函数
        public InventoryMoveRequest() { }

        public InventoryMoveRequest(string sourceInventoryId, int sourceSlotIndex, string targetInventoryId, int targetSlotIndex, int count = -1)
            : base(targetInventoryId, targetSlotIndex)
        {
            SourceInventoryId = sourceInventoryId;
            SourceSlotIndex = sourceSlotIndex;
            AutoFindTargetSlot = false;
            Count = count;
        }

        public InventoryMoveRequest(string sourceInventoryId, int sourceSlotIndex, string targetInventoryId, bool autoFindTargetSlot, int count = -1)
            : base(targetInventoryId, -1)
        {
            SourceInventoryId = sourceInventoryId;
            SourceSlotIndex = sourceSlotIndex;
            AutoFindTargetSlot = autoFindTargetSlot;
            Count = count;
        }
        #endregion
    }

    /// <summary>
    /// 槽位拆分转移请求。
    /// 支持拆分指定数量并转移到目标槽位或自动寻空栏。
    /// </summary>
    public class InventorySplitRequest : InventoryRequest
    {
        #region 公共属性
        public string SourceInventoryId { get; set; }
        public int SourceSlotIndex { get; set; }
        public int SplitCount { get; set; } = 1;
        public bool AutoFindTargetSlot { get; set; } = false;
        #endregion

        #region 静态工厂 API
        public static InventorySplitRequest ToSlot(string sourceInvId, int sourceSlotIndex, int splitCount, string targetInvId, int targetSlotIndex)
        {
            return new InventorySplitRequest(sourceInvId, sourceSlotIndex, splitCount, targetInvId, targetSlotIndex);
        }

        public static InventorySplitRequest ToAnySlot(string sourceInvId, int sourceSlotIndex, int splitCount, string targetInvId)
        {
            return new InventorySplitRequest(sourceInvId, sourceSlotIndex, splitCount, targetInvId, autoFindTargetSlot: true);
        }
        #endregion

        #region 构造函数
        public InventorySplitRequest() { }

        public InventorySplitRequest(string sourceInventoryId, int sourceSlotIndex, int splitCount, string targetInventoryId, int targetSlotIndex)
            : base(targetInventoryId, targetSlotIndex)
        {
            SourceInventoryId = sourceInventoryId;
            SourceSlotIndex = sourceSlotIndex;
            SplitCount = splitCount;
            AutoFindTargetSlot = false;
        }

        public InventorySplitRequest(string sourceInventoryId, int sourceSlotIndex, int splitCount, string targetInventoryId, bool autoFindTargetSlot)
            : base(targetInventoryId, -1)
        {
            SourceInventoryId = sourceInventoryId;
            SourceSlotIndex = sourceSlotIndex;
            SplitCount = splitCount;
            AutoFindTargetSlot = autoFindTargetSlot;
        }
        #endregion
    }

    /// <summary>
    /// 背包原子碎片整理请求 (消除空穴 + 同类未满堆叠自动合并，不改变未堆叠物品相对顺序)。
    /// </summary>
    public class InventoryDefragmentRequest : InventoryRequest
    {
        public InventoryDefragmentRequest() { }

        public InventoryDefragmentRequest(string targetInventoryId)
            : base(targetInventoryId) { }
    }

    /// <summary>
    /// 背包原子规则排序请求 (根据指定 Comparison / Mode 进行重新排序)。
    /// </summary>
    public class InventorySortRequest : InventoryRequest
    {
        #region 公共属性
        public InventorySortMode SortMode { get; set; } = InventorySortMode.NameAscending;
        #endregion

        #region 构造函数
        public InventorySortRequest() { }

        public InventorySortRequest(string targetInventoryId, InventorySortMode sortMode)
            : base(targetInventoryId)
        {
            SortMode = sortMode;
        }
        #endregion
    }

    /// <summary>
    /// 快捷堆叠存入请求 (仅将源容器中“目标容器里已有同类物品”的未满堆叠快速补充存入)。
    /// </summary>
    public class InventoryQuickStackRequest : InventoryRequest
    {
        #region 公共属性
        public string SourceInventoryId { get; set; }
        #endregion

        #region 构造函数
        public InventoryQuickStackRequest() { }

        public InventoryQuickStackRequest(string sourceInventoryId, string targetInventoryId)
            : base(targetInventoryId)
        {
            SourceInventoryId = sourceInventoryId;
        }
        #endregion
    }

    /// <summary>
    /// 背包全量转移请求 (将源容器中所有非空槽位的物品依次转移到目标容器中)。
    /// </summary>
    public class InventoryTransferAllRequest : InventoryRequest
    {
        #region 公共属性
        public string SourceInventoryId { get; set; }
        #endregion

        #region 构造函数
        public InventoryTransferAllRequest() { }

        public InventoryTransferAllRequest(string sourceInventoryId, string targetInventoryId)
            : base(targetInventoryId)
        {
            SourceInventoryId = sourceInventoryId;
        }
        #endregion
    }

    /// <summary>
    /// 槽位锁定 / 解锁状态设置请求。
    /// </summary>
    public class InventorySetSlotLockRequest : InventoryRequest
    {
        #region 公共属性
        public bool IsLocked { get; set; }
        #endregion

        #region 构造函数
        public InventorySetSlotLockRequest() { }

        public InventorySetSlotLockRequest(string targetInventoryId, int slotIndex, bool isLocked)
            : base(targetInventoryId, slotIndex)
        {
            IsLocked = isLocked;
        }
        #endregion
    }
}
