using UnityEngine;
using Cwc.InventoryEngine.UI;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 库存原生内生请求处理者。
    /// 自动监听并处理 `InventoryMoveRequest` 等容器固有数据请求。
    /// 自动完成同一容器内/跨容器间的槽位交换、移动、拆分、碎片整理、规则排序与快捷存取。开箱即用，无需手挂组件。
    /// </summary>
    public static class InventoryBuiltinRequestHandler
    {
        #region 初始化与自动注册
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            // 自动注册容器内生行为订阅
            InventoryRequestPipeline.Subscribe<InventoryMoveRequest>(HandleMoveRequest);
            InventoryRequestPipeline.Subscribe<InventorySplitRequest>(HandleSplitRequest);
            InventoryRequestPipeline.Subscribe<InventoryDefragmentRequest>(HandleDefragmentRequest);
            InventoryRequestPipeline.Subscribe<InventorySortRequest>(HandleSortRequest);
            InventoryRequestPipeline.Subscribe<InventoryQuickStackRequest>(HandleQuickStackRequest);
            InventoryRequestPipeline.Subscribe<InventoryTransferAllRequest>(HandleTransferAllRequest);
            InventoryRequestPipeline.Subscribe<InventorySetSlotLockRequest>(HandleSetSlotLockRequest);
        }
        #endregion

        #region 私有处理方法
        /// <summary>
        /// 处理移动/交换/合并请求
        /// </summary>
        private static void HandleMoveRequest(InventoryMoveRequest request)
        {
            if (request == null) return;

            if (string.IsNullOrEmpty(request.SourceInventoryId) || string.IsNullOrEmpty(request.TargetInventoryId)) return;

            if (!InventoryRegistry.TryGetContainer(request.SourceInventoryId, out var sourceContainer)) return;
            if (!InventoryRegistry.TryGetContainer(request.TargetInventoryId, out var targetContainer)) return;

            if (request.SourceSlotIndex < 0 || request.SourceSlotIndex >= sourceContainer.Capacity) return;

            ItemSlot sourceSlot = sourceContainer.Slots[request.SourceSlotIndex];
            if (sourceSlot == null || sourceSlot.IsEmpty) return;

            if (request.AutoFindTargetSlot)
            {
                InventoryContainer.TransferToAnySlot(sourceContainer, request.SourceSlotIndex, targetContainer, out _);
                return;
            }

            if (request.TargetSlotIndex < 0 || request.TargetSlotIndex >= targetContainer.Capacity) return;

            InventoryContainer.TransferOrSwapSlots(sourceContainer, request.SourceSlotIndex, targetContainer, request.TargetSlotIndex);
        }

        /// <summary>
        /// 处理拆分转移请求
        /// </summary>
        private static void HandleSplitRequest(InventorySplitRequest request)
        {
            if (request == null || request.SplitCount <= 0) return;
            if (string.IsNullOrEmpty(request.SourceInventoryId) || string.IsNullOrEmpty(request.TargetInventoryId)) return;

            if (!InventoryRegistry.TryGetContainer(request.SourceInventoryId, out var sourceContainer)) return;
            if (!InventoryRegistry.TryGetContainer(request.TargetInventoryId, out var targetContainer)) return;

            if (request.AutoFindTargetSlot)
            {
                InventoryContainer.TransferSplitToAnySlot(sourceContainer, request.SourceSlotIndex, request.SplitCount, targetContainer, out _);
            }
            else
            {
                InventoryContainer.TransferSplitToSlot(sourceContainer, request.SourceSlotIndex, request.SplitCount, targetContainer, request.SlotIndex);
            }
        }

        /// <summary>
        /// 处理原子碎片整理请求 (消除空穴与自动同类堆叠合并)
        /// </summary>
        private static void HandleDefragmentRequest(InventoryDefragmentRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.TargetInventoryId)) return;
            if (!InventoryRegistry.TryGetContainer(request.TargetInventoryId, out var container)) return;

            container.Defragment();
        }

        /// <summary>
        /// 处理原子规则排序请求 (根据指定 Mode 排列非空槽位)
        /// </summary>
        private static void HandleSortRequest(InventorySortRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.TargetInventoryId)) return;
            if (!InventoryRegistry.TryGetContainer(request.TargetInventoryId, out var container)) return;

            container.Sort(new InventoryDataSlotComparer(request.SortMode));
        }

        /// <summary>
        /// 处理快捷堆叠存入请求
        /// </summary>
        private static void HandleQuickStackRequest(InventoryQuickStackRequest request)
        {
            if (request == null) return;
            if (string.IsNullOrEmpty(request.SourceInventoryId) || string.IsNullOrEmpty(request.TargetInventoryId)) return;

            if (!InventoryRegistry.TryGetContainer(request.SourceInventoryId, out var sourceContainer)) return;
            if (!InventoryRegistry.TryGetContainer(request.TargetInventoryId, out var targetContainer)) return;

            InventoryContainer.QuickStackTo(sourceContainer, targetContainer);
        }

        /// <summary>
        /// 处理全量转移请求
        /// </summary>
        private static void HandleTransferAllRequest(InventoryTransferAllRequest request)
        {
            if (request == null) return;
            if (string.IsNullOrEmpty(request.SourceInventoryId) || string.IsNullOrEmpty(request.TargetInventoryId)) return;

            if (!InventoryRegistry.TryGetContainer(request.SourceInventoryId, out var sourceContainer)) return;
            if (!InventoryRegistry.TryGetContainer(request.TargetInventoryId, out var targetContainer)) return;

            InventoryContainer.TransferAll(sourceContainer, targetContainer);
        }

        /// <summary>
        /// 处理槽位锁定/解锁请求
        /// </summary>
        private static void HandleSetSlotLockRequest(InventorySetSlotLockRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.TargetInventoryId)) return;
            if (!InventoryRegistry.TryGetContainer(request.TargetInventoryId, out var container)) return;

            container.SetSlotLock(request.SlotIndex, request.IsLocked);
        }
        #endregion
    }
}
