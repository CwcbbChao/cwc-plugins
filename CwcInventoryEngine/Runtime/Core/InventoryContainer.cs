using System;
using System.Collections.Generic;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 背包物品容器。
    /// 核心事务控制中枢，负责处理物品的移入、拆分、合并、交换、删除以及批处理响应式更新。
    /// </summary>
    public class InventoryContainer : IReadOnlyInventoryContainer
    {
        #region Private Fields
        private readonly HashSet<int> _dirtySlots = new();
        private readonly List<ItemSlot> _sortSlotBuffer = new();
        private readonly List<ItemInstance> _sortItemBuffer = new();
        private int _batchDepth = 0;
        #endregion

        #region Public Events
        /// <summary>
        /// 当单个槽位发生数据更新时触发的响应式事件。(slotIndex, slotData)
        /// </summary>
        public event Action<int, ItemSlot> OnSlotUpdated;

        /// <summary>
        /// 当批处理 Block 完成，统一刷出更改时触发的事件。
        /// </summary>
        public event Action OnBatchCompleted;
        #endregion

        #region Public Properties
        /// <summary>
        /// 容器总容量。
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// 槽位数组。
        /// </summary>
        public ItemSlot[] Slots { get; private set; }
        #endregion

        #region Query Methods
        /// <summary>
        /// 安全获取指定索引槽位实例。
        /// </summary>
        public ItemSlot GetSlot(int slotIndex)
        {
            if (!IsValidIndex(slotIndex)) return null;
            return Slots[slotIndex];
        }

        /// <summary>
        /// 检查槽位索引是否在当前容器容量有效范围内。
        /// </summary>
        public bool IsValidIndex(int slotIndex)
        {
            return Slots != null && slotIndex >= 0 && slotIndex < Capacity;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// 构造一个具有指定容量的背包容器。
        /// </summary>
        /// <param name="capacity">槽位数量容量</param>
        public InventoryContainer(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "容器容量必须大于零");

            Capacity = capacity;
            Slots = new ItemSlot[capacity];

            for (int i = 0; i < capacity; i++)
            {
                Slots[i] = new ItemSlot(i);
            }
        }
        #endregion

        #region Batch Management
        /// <summary>
        /// 开启批处理作用域。建议使用 using (container.BatchScope())。
        /// </summary>
        public BatchScope BatchScope()
        {
            return new BatchScope(this);
        }

        internal void BeginBatch()
        {
            _batchDepth++;
        }

        internal void EndBatch()
        {
            _batchDepth--;
            if (_batchDepth <= 0)
            {
                _batchDepth = 0;
                FlushDirtySlots();
            }
        }

        public virtual void MarkSlotDirty(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Capacity) return;

            if (_batchDepth > 0)
            {
                _dirtySlots.Add(slotIndex);
            }
            else
            {
                OnSlotUpdated?.Invoke(slotIndex, Slots[slotIndex]);
            }
        }

        protected virtual void FlushDirtySlots()
        {
            if (_dirtySlots.Count == 0) return;

            foreach (int slotIndex in _dirtySlots)
            {
                OnSlotUpdated?.Invoke(slotIndex, Slots[slotIndex]);
            }

            _dirtySlots.Clear();
            OnBatchCompleted?.Invoke();
        }
        #endregion

        #region Core Transaction APIs
        /// <summary>
        /// 尝试向容器中添加物品。
        /// 自动处理溢出边界：优先寻找同类未满槽位合并，再寻找空槽位，多余的作为 remainder 导出。
        /// </summary>
        /// <param name="inputItem">待放入的物品</param>
        /// <param name="remainder">未能完全放入的剩余物品实例（完全放入则为 null）</param>
        /// <returns>若至少成功放入了 1 个数量返回 true，否则返回 false</returns>
        public virtual bool TryAddItem(ItemInstance inputItem, out ItemInstance remainder)
        {
            remainder = inputItem;
            if (inputItem == null || inputItem.StackCount <= 0) return false;

            int originalCount = inputItem.StackCount;

            using (BatchScope())
            {
                // 第一阶段：寻找现有未满堆叠的同类槽位合并
                if (inputItem.Definition.IsStackable)
                {
                    for (int i = 0; i < Capacity; i++)
                    {
                        var slot = Slots[i];
                        if (slot.IsEmpty) continue;
                        if (!slot.CanAccept(this, inputItem)) continue;

                        if (slot.Item.CanStackWith(inputItem))
                        {
                            int spaceLeft = slot.Item.Definition.MaxStack - slot.Item.StackCount;
                            if (spaceLeft > 0)
                            {
                                int transferCount = Math.Min(spaceLeft, inputItem.StackCount);

                                int oldTargetCount = slot.Item.StackCount;
                                slot.Item.StackCount += transferCount;
                                slot.Item.TriggerStackCountChanged(oldTargetCount, slot.Item.StackCount);
                                slot.Item.TriggerStackMerged(inputItem, transferCount);

                                int oldInputCount = inputItem.StackCount;
                                inputItem.StackCount -= transferCount;
                                inputItem.TriggerStackCountChanged(oldInputCount, inputItem.StackCount);

                                MarkSlotDirty(i);

                                if (inputItem.StackCount <= 0)
                                {
                                    inputItem.TriggerInstanceDestroyed();
                                    remainder = null;
                                    return true;
                                }
                            }
                        }
                    }
                }

                // 第二阶段：寻找空槽位放入
                for (int i = 0; i < Capacity; i++)
                {
                    var slot = Slots[i];
                    if (!slot.IsEmpty) continue;
                    if (!slot.CanAccept(this, inputItem)) continue;

                    int maxAllowed = inputItem.Definition.MaxStack;
                    if (inputItem.StackCount <= maxAllowed)
                    {
                        // 完全放入该空槽
                        slot.Item = inputItem;
                        slot.Item.TriggerAddedToContainer(this, i);
                        MarkSlotDirty(i);
                        remainder = null;
                        return true;
                    }
                    else
                    {
                        // 拆分一部分放入空槽
                        ItemInstance splitItem = inputItem.Definition.CreateInstanceWithId(ItemId.NewId(), maxAllowed);
                        inputItem.TriggerStackSplit(splitItem, maxAllowed);

                        int oldInputCount = inputItem.StackCount;
                        inputItem.StackCount -= maxAllowed;
                        inputItem.TriggerStackCountChanged(oldInputCount, inputItem.StackCount);

                        slot.Item = splitItem;
                        slot.Item.TriggerAddedToContainer(this, i);
                        MarkSlotDirty(i);
                    }
                }
            }

            remainder = inputItem.StackCount > 0 ? inputItem : null;
            return inputItem.StackCount < originalCount;
        }

        /// <summary>
        /// 零 GC 检查容器中是否有可以放入该物品的有效槽位 (现有同类可堆叠槽位或匹配 Filter 的空槽位)。
        /// </summary>
        /// <param name="inputItem">待放入的物品实例</param>
        /// <returns>若有匹配可用的槽位返回 true，否则返回 false</returns>
        public virtual bool HasAvailableSlotForItem(ItemInstance inputItem)
        {
            if (inputItem == null || inputItem.StackCount <= 0 || Slots == null) return false;

            // 1. 第一优先检查：是否存在可合并堆叠的相同物品槽位
            if (inputItem.Definition.IsStackable)
            {
                for (int i = 0; i < Capacity; i++)
                {
                    var slot = Slots[i];
                    if (slot == null || slot.IsEmpty) continue;
                    if (!slot.CanAccept(this, inputItem)) continue;

                    if (slot.Item.CanStackWith(inputItem))
                    {
                        int spaceLeft = slot.Item.Definition.MaxStack - slot.Item.StackCount;
                        if (spaceLeft > 0)
                        {
                            return true;
                        }
                    }
                }
            }

            // 2. 第二优先检查：是否存在允许放入该物品的空槽位
            for (int i = 0; i < Capacity; i++)
            {
                var slot = Slots[i];
                if (slot == null || !slot.IsEmpty) continue;
                if (slot.CanAccept(this, inputItem))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 尝试向指定槽位放入物品。
        /// </summary>
        public virtual bool TryAddItemToSlot(int slotIndex, ItemInstance inputItem, out ItemInstance remainder)
        {
            remainder = inputItem;
            if (slotIndex < 0 || slotIndex >= Capacity) return false;
            if (inputItem == null || inputItem.StackCount <= 0) return false;

            var targetSlot = Slots[slotIndex];
            if (!targetSlot.CanAccept(this, inputItem)) return false;

            using (BatchScope())
            {
                if (targetSlot.IsEmpty)
                {
                    targetSlot.Item = inputItem;
                    targetSlot.Item.TriggerAddedToContainer(this, slotIndex);
                    MarkSlotDirty(slotIndex);
                    remainder = null;
                    return true;
                }
                else if (targetSlot.Item.CanStackWith(inputItem))
                {
                    int spaceLeft = targetSlot.Item.Definition.MaxStack - targetSlot.Item.StackCount;
                    if (spaceLeft <= 0) return false;

                    int transferCount = Math.Min(spaceLeft, inputItem.StackCount);

                    int oldTargetCount = targetSlot.Item.StackCount;
                    targetSlot.Item.StackCount += transferCount;
                    targetSlot.Item.TriggerStackCountChanged(oldTargetCount, targetSlot.Item.StackCount);
                    targetSlot.Item.TriggerStackMerged(inputItem, transferCount);

                    int oldInputCount = inputItem.StackCount;
                    inputItem.StackCount -= transferCount;
                    inputItem.TriggerStackCountChanged(oldInputCount, inputItem.StackCount);

                    MarkSlotDirty(slotIndex);

                    if (inputItem.StackCount <= 0)
                    {
                        inputItem.TriggerInstanceDestroyed();
                        remainder = null;
                    }
                    else
                    {
                        remainder = inputItem;
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 交换或合并两个槽位的物品。
        /// </summary>
        public virtual bool SwapOrMergeSlots(int fromSlotIndex, int toSlotIndex)
        {
            if (fromSlotIndex == toSlotIndex) return true;
            if (fromSlotIndex < 0 || fromSlotIndex >= Capacity) return false;
            if (toSlotIndex < 0 || toSlotIndex >= Capacity) return false;

            var fromSlot = Slots[fromSlotIndex];
            var toSlot = Slots[toSlotIndex];

            if (fromSlot.IsEmpty) return false;

            // 过滤器校验
            if (!toSlot.CanAccept(this, fromSlot.Item)) return false;
            if (!toSlot.IsEmpty && !fromSlot.CanAccept(this, toSlot.Item)) return false;

            using (BatchScope())
            {
                // 1. 尝试合并堆叠
                if (!toSlot.IsEmpty && toSlot.Item.CanStackWith(fromSlot.Item))
                {
                    int spaceLeft = toSlot.Item.Definition.MaxStack - toSlot.Item.StackCount;
                    if (spaceLeft > 0)
                    {
                        int transferCount = Math.Min(spaceLeft, fromSlot.Item.StackCount);

                        int oldToCount = toSlot.Item.StackCount;
                        toSlot.Item.StackCount += transferCount;
                        toSlot.Item.TriggerStackCountChanged(oldToCount, toSlot.Item.StackCount);
                        toSlot.Item.TriggerStackMerged(fromSlot.Item, transferCount);

                        int oldFromCount = fromSlot.Item.StackCount;
                        fromSlot.Item.StackCount -= transferCount;
                        fromSlot.Item.TriggerStackCountChanged(oldFromCount, fromSlot.Item.StackCount);

                        if (fromSlot.Item.StackCount <= 0)
                        {
                            fromSlot.Item.TriggerRemovedFromContainer(this, fromSlotIndex);
                            fromSlot.Item.TriggerInstanceDestroyed();
                            fromSlot.Clear();
                        }

                        MarkSlotDirty(fromSlotIndex);
                        MarkSlotDirty(toSlotIndex);
                        return true;
                    }
                }

                // 2. 槽位交换 (Swap)
                ItemInstance tempItem = fromSlot.Item;
                ItemInstance targetItem = toSlot.Item;

                if (tempItem != null) tempItem.TriggerRemovedFromContainer(this, fromSlotIndex);
                if (targetItem != null) targetItem.TriggerRemovedFromContainer(this, toSlotIndex);

                fromSlot.Item = targetItem;
                toSlot.Item = tempItem;

                if (fromSlot.Item != null) fromSlot.Item.TriggerAddedToContainer(this, fromSlotIndex);
                if (toSlot.Item != null) toSlot.Item.TriggerAddedToContainer(this, toSlotIndex);

                MarkSlotDirty(fromSlotIndex);
                MarkSlotDirty(toSlotIndex);
            }

            return true;
        }

        /// <summary>
        /// 在两个不同的容器之间交换或转移槽位物品。
        /// </summary>
        public static bool TransferOrSwapSlots(
            InventoryContainer fromContainer, int fromSlotIndex,
            InventoryContainer toContainer, int toSlotIndex)
        {
            if (fromContainer == null || toContainer == null) return false;
            if (fromContainer == toContainer && fromSlotIndex == toSlotIndex) return true;
            if (fromSlotIndex < 0 || fromSlotIndex >= fromContainer.Capacity) return false;
            if (toSlotIndex < 0 || toSlotIndex >= toContainer.Capacity) return false;

            var fromSlot = fromContainer.Slots[fromSlotIndex];
            var toSlot = toContainer.Slots[toSlotIndex];

            if (fromSlot.IsEmpty) return false;

            // 过滤器校验
            if (!toSlot.CanAccept(toContainer, fromSlot.Item)) return false;
            if (!toSlot.IsEmpty && !fromSlot.CanAccept(fromContainer, toSlot.Item)) return false;

            using (fromContainer.BatchScope())
            using (toContainer.BatchScope())
            {
                // 1. 尝试合并堆叠
                if (!toSlot.IsEmpty && toSlot.Item.CanStackWith(fromSlot.Item))
                {
                    int spaceLeft = toSlot.Item.Definition.MaxStack - toSlot.Item.StackCount;
                    if (spaceLeft > 0)
                    {
                        int transferCount = Math.Min(spaceLeft, fromSlot.Item.StackCount);

                        int oldToCount = toSlot.Item.StackCount;
                        toSlot.Item.StackCount += transferCount;
                        toSlot.Item.TriggerStackCountChanged(oldToCount, toSlot.Item.StackCount);
                        toSlot.Item.TriggerStackMerged(fromSlot.Item, transferCount);

                        int oldFromCount = fromSlot.Item.StackCount;
                        fromSlot.Item.StackCount -= transferCount;
                        fromSlot.Item.TriggerStackCountChanged(oldFromCount, fromSlot.Item.StackCount);

                        if (fromSlot.Item.StackCount <= 0)
                        {
                            fromSlot.Item.TriggerRemovedFromContainer(fromContainer, fromSlotIndex);
                            fromSlot.Item.TriggerInstanceDestroyed();
                            fromSlot.Clear();
                        }

                        fromContainer.MarkSlotDirty(fromSlotIndex);
                        toContainer.MarkSlotDirty(toSlotIndex);
                        return true;
                    }
                }

                // 2. 槽位交换 (Swap)
                ItemInstance tempItem = fromSlot.Item;
                ItemInstance targetItem = toSlot.Item;

                if (tempItem != null) tempItem.TriggerRemovedFromContainer(fromContainer, fromSlotIndex);
                if (targetItem != null) targetItem.TriggerRemovedFromContainer(toContainer, toSlotIndex);

                fromSlot.Item = targetItem;
                toSlot.Item = tempItem;

                if (fromSlot.Item != null) fromSlot.Item.TriggerAddedToContainer(fromContainer, fromSlotIndex);
                if (toSlot.Item != null) toSlot.Item.TriggerAddedToContainer(toContainer, toSlotIndex);

                fromContainer.MarkSlotDirty(fromSlotIndex);
                toContainer.MarkSlotDirty(toSlotIndex);
            }

            return true;
        }

        /// <summary>
        /// 将源容器指定槽位的物品转移到目标容器的可用空栏或可堆叠槽位中。
        /// </summary>
        /// <param name="fromContainer">源背包容器</param>
        /// <param name="fromSlotIndex">源槽位索引</param>
        /// <param name="toContainer">目标背包容器</param>
        /// <param name="remainder">未能完全放入的剩余物品实例 (全量移入则为 null)</param>
        /// <returns>若至少成功转移了部分或全部物品返回 true，否则返回 false</returns>
        public static bool TransferToAnySlot(
            InventoryContainer fromContainer, int fromSlotIndex,
            InventoryContainer toContainer, out ItemInstance remainder)
        {
            remainder = null;
            if (fromContainer == null || toContainer == null) return false;
            if (fromSlotIndex < 0 || fromSlotIndex >= fromContainer.Capacity) return false;

            var fromSlot = fromContainer.Slots[fromSlotIndex];
            if (fromSlot == null || fromSlot.IsEmpty) return false;

            ItemInstance itemToTransfer = fromSlot.Item;

            using (fromContainer.BatchScope())
            {
                if (toContainer.TryAddItem(itemToTransfer, out remainder))
                {
                    if (remainder == null)
                    {
                        itemToTransfer.TriggerRemovedFromContainer(fromContainer, fromSlotIndex);
                        fromSlot.Clear();
                    }
                    else
                    {
                        fromSlot.Item = remainder;
                    }

                    fromContainer.MarkSlotDirty(fromSlotIndex);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 将源容器指定槽位的物品按数量拆分并精准转移到目标容器的指定槽位中。
        /// </summary>
        public static bool TransferSplitToSlot(
            InventoryContainer fromContainer, int fromSlotIndex, int splitCount,
            InventoryContainer toContainer, int toSlotIndex)
        {
            if (fromContainer == null || toContainer == null) return false;
            if (!fromContainer.SplitSlot(fromSlotIndex, splitCount, out ItemInstance splitItem)) return false;

            if (toContainer.TryAddItemToSlot(toSlotIndex, splitItem, out ItemInstance remainder))
            {
                if (remainder != null && remainder.StackCount > 0)
                {
                    var fromSlot = fromContainer.Slots[fromSlotIndex];
                    int oldFromCount = fromSlot.Item.StackCount;
                    fromSlot.Item.StackCount += remainder.StackCount;
                    fromSlot.Item.TriggerStackCountChanged(oldFromCount, fromSlot.Item.StackCount);
                    fromContainer.MarkSlotDirty(fromSlotIndex);
                }
                return true;
            }
            else
            {
                var fromSlot = fromContainer.Slots[fromSlotIndex];
                int oldFromCount = fromSlot.Item.StackCount;
                fromSlot.Item.StackCount += splitItem.StackCount;
                fromSlot.Item.TriggerStackCountChanged(oldFromCount, fromSlot.Item.StackCount);
                splitItem.TriggerInstanceDestroyed();
                fromContainer.MarkSlotDirty(fromSlotIndex);
                return false;
            }
        }

        /// <summary>
        /// 将源容器指定槽位的物品按数量拆分并自动转移到目标容器的可用空栏/堆叠中。
        /// </summary>
        public static bool TransferSplitToAnySlot(
            InventoryContainer fromContainer, int fromSlotIndex, int splitCount,
            InventoryContainer toContainer, out ItemInstance remainder)
        {
            remainder = null;
            if (fromContainer == null || toContainer == null) return false;
            if (!fromContainer.SplitSlot(fromSlotIndex, splitCount, out ItemInstance splitItem)) return false;

            if (toContainer.TryAddItem(splitItem, out remainder))
            {
                if (remainder != null && remainder.StackCount > 0)
                {
                    var fromSlot = fromContainer.Slots[fromSlotIndex];
                    int oldFromCount = fromSlot.Item.StackCount;
                    fromSlot.Item.StackCount += remainder.StackCount;
                    fromSlot.Item.TriggerStackCountChanged(oldFromCount, fromSlot.Item.StackCount);
                    fromContainer.MarkSlotDirty(fromSlotIndex);
                }
                return true;
            }
            else
            {
                var fromSlot = fromContainer.Slots[fromSlotIndex];
                int oldFromCount = fromSlot.Item.StackCount;
                fromSlot.Item.StackCount += splitItem.StackCount;
                fromSlot.Item.TriggerStackCountChanged(oldFromCount, fromSlot.Item.StackCount);
                splitItem.TriggerInstanceDestroyed();
                fromContainer.MarkSlotDirty(fromSlotIndex);
                remainder = splitItem;
                return false;
            }
        }

        /// <summary>
        /// 容器物理碎片整理 (Defragment)。
        /// 消除中间空隙，并将同类未满堆叠自动在容器内合并。
        /// 彻底保持未堆叠物品之间的原始相对顺序不变。
        /// </summary>
        public virtual bool Defragment()
        {
            using (BatchScope())
            {
                // 1. 同类未满堆叠自动向前合并
                for (int i = 0; i < Capacity; i++)
                {
                    var slotA = Slots[i];
                    if (slotA.IsEmpty || !slotA.Item.Definition.IsStackable) continue;

                    int maxStack = slotA.Item.Definition.MaxStack;
                    if (slotA.Item.StackCount >= maxStack) continue;

                    for (int j = i + 1; j < Capacity; j++)
                    {
                        var slotB = Slots[j];
                        if (slotB.IsEmpty || !slotB.Item.CanStackWith(slotA.Item)) continue;

                        int spaceLeft = maxStack - slotA.Item.StackCount;
                        if (spaceLeft <= 0) break;

                        int transferCount = Math.Min(spaceLeft, slotB.Item.StackCount);
                        int oldACount = slotA.Item.StackCount;
                        slotA.Item.StackCount += transferCount;
                        slotA.Item.TriggerStackCountChanged(oldACount, slotA.Item.StackCount);
                        slotA.Item.TriggerStackMerged(slotB.Item, transferCount);

                        int oldBCount = slotB.Item.StackCount;
                        slotB.Item.StackCount -= transferCount;
                        slotB.Item.TriggerStackCountChanged(oldBCount, slotB.Item.StackCount);

                        if (slotB.Item.StackCount <= 0)
                        {
                            slotB.Item.TriggerRemovedFromContainer(this, j);
                            slotB.Item.TriggerInstanceDestroyed();
                            slotB.Clear();
                        }

                        MarkSlotDirty(i);
                        MarkSlotDirty(j);
                    }
                }

                // 2. 消除空穴，将所有非空槽位往前靠拢
                int targetIndex = 0;
                for (int i = 0; i < Capacity; i++)
                {
                    if (!Slots[i].IsEmpty)
                    {
                        if (i != targetIndex)
                        {
                            var item = Slots[i].Item;
                            item.TriggerRemovedFromContainer(this, i);
                            Slots[i].Clear();

                            Slots[targetIndex].Item = item;
                            item.TriggerAddedToContainer(this, targetIndex);

                            MarkSlotDirty(i);
                            MarkSlotDirty(targetIndex);
                        }
                        targetIndex++;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 容器纯规则排序 (Sort)。
        /// 根据指定的比较器对容器内的非空物品进行重新排序与放置。
        /// </summary>
        public virtual bool Sort(IComparer<ItemSlot> comparer)
        {
            if (comparer == null) return false;

            using (BatchScope())
            {
                Defragment();

                _sortSlotBuffer.Clear();
                for (int i = 0; i < Capacity; i++)
                {
                    _sortSlotBuffer.Add(Slots[i]);
                }

                _sortSlotBuffer.Sort(comparer);

                _sortItemBuffer.Clear();
                for (int i = 0; i < _sortSlotBuffer.Count; i++)
                {
                    _sortItemBuffer.Add(_sortSlotBuffer[i].Item);
                }

                for (int i = 0; i < Capacity; i++)
                {
                    ItemInstance oldItem = Slots[i].Item;
                    ItemInstance newItem = _sortItemBuffer[i];

                    if (oldItem != newItem)
                    {
                        if (oldItem != null) oldItem.TriggerRemovedFromContainer(this, i);
                        Slots[i].Item = newItem;
                        if (newItem != null) newItem.TriggerAddedToContainer(this, i);
                        MarkSlotDirty(i);
                    }
                }

                _sortSlotBuffer.Clear();
                _sortItemBuffer.Clear();
            }

            return true;
        }

        /// <summary>
        /// 快捷堆叠存入 (Quick Stack)。
        /// 仅将源容器中“目标容器里已有同类物品”的未满堆叠快速补充存入目标容器。
        /// </summary>
        public static bool QuickStackTo(InventoryContainer fromContainer, InventoryContainer toContainer)
        {
            if (fromContainer == null || toContainer == null) return false;

            bool anyTransferred = false;
            using (fromContainer.BatchScope())
            using (toContainer.BatchScope())
            {
                for (int i = 0; i < fromContainer.Capacity; i++)
                {
                    var fromSlot = fromContainer.Slots[i];
                    if (fromSlot.IsEmpty || !fromSlot.Item.Definition.IsStackable) continue;

                    bool toHasSame = false;
                    for (int j = 0; j < toContainer.Capacity; j++)
                    {
                        var toSlot = toContainer.Slots[j];
                        if (!toSlot.IsEmpty && toSlot.Item.CanStackWith(fromSlot.Item) && toSlot.Item.StackCount < toSlot.Item.Definition.MaxStack)
                        {
                            toHasSame = true;
                            break;
                        }
                    }

                    if (toHasSame)
                    {
                        if (TransferToAnySlot(fromContainer, i, toContainer, out _))
                        {
                            anyTransferred = true;
                        }
                    }
                }
            }

            return anyTransferred;
        }

        /// <summary>
        /// 全量转移 (Transfer All)。
        /// 将源容器中所有非空槽位的物品依次转移到目标容器的可用空栏/堆叠中。
        /// </summary>
        public static bool TransferAll(InventoryContainer fromContainer, InventoryContainer toContainer)
        {
            if (fromContainer == null || toContainer == null) return false;

            bool anyTransferred = false;
            using (fromContainer.BatchScope())
            using (toContainer.BatchScope())
            {
                for (int i = 0; i < fromContainer.Capacity; i++)
                {
                    var fromSlot = fromContainer.Slots[i];
                    if (fromSlot.IsEmpty) continue;

                    if (TransferToAnySlot(fromContainer, i, toContainer, out _))
                    {
                        anyTransferred = true;
                    }
                }
            }

            return anyTransferred;
        }

        /// <summary>
        /// 设置指定槽位的锁定/禁用状态。
        /// </summary>
        public virtual bool SetSlotLock(int slotIndex, bool isLocked)
        {
            if (slotIndex < 0 || slotIndex >= Capacity) return false;

            if (Slots[slotIndex].IsDisabled != isLocked)
            {
                Slots[slotIndex].IsDisabled = isLocked;
                MarkSlotDirty(slotIndex);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 拆分指定槽位的物品。
        /// </summary>
        public virtual bool SplitSlot(int fromSlotIndex, int splitAmount, out ItemInstance splitItem)
        {
            splitItem = null;
            if (fromSlotIndex < 0 || fromSlotIndex >= Capacity) return false;

            var slot = Slots[fromSlotIndex];
            if (slot.IsEmpty || splitAmount <= 0 || splitAmount >= slot.Item.StackCount) return false;

            using (BatchScope())
            {
                splitItem = slot.Item.Definition.CreateInstanceWithId(ItemId.NewId(), splitAmount);
                slot.Item.TriggerStackSplit(splitItem, splitAmount);

                int oldCount = slot.Item.StackCount;
                slot.Item.StackCount -= splitAmount;
                slot.Item.TriggerStackCountChanged(oldCount, slot.Item.StackCount);

                MarkSlotDirty(fromSlotIndex);
            }

            return true;
        }

        /// <summary>
        /// 扣减指定槽位的物品堆叠数量。
        /// </summary>
        public virtual bool RemoveItemFromSlot(int slotIndex, int count)
        {
            if (slotIndex < 0 || slotIndex >= Capacity || count <= 0) return false;

            var slot = Slots[slotIndex];
            if (slot.IsEmpty) return false;

            using (BatchScope())
            {
                int oldCount = slot.Item.StackCount;
                if (count >= slot.Item.StackCount)
                {
                    var itemToDestroy = slot.Item;
                    itemToDestroy.TriggerRemovedFromContainer(this, slotIndex);
                    itemToDestroy.TriggerStackCountChanged(oldCount, 0);
                    itemToDestroy.TriggerInstanceDestroyed();
                    slot.Clear();
                }
                else
                {
                    slot.Item.StackCount -= count;
                    slot.Item.TriggerStackCountChanged(oldCount, slot.Item.StackCount);
                }

                MarkSlotDirty(slotIndex);
            }

            return true;
        }

        /// <summary>
        /// 跨槽位智能扣除指定定义与数量的物品（优先扣除非满堆叠槽位）。
        /// </summary>
        public virtual bool RemoveItemDefinition(ItemDefinition definition, int count)
        {
            if (definition == null || count <= 0) return false;
            if (!HasEnough(definition, count)) return false;

            int remainingToDeduct = count;

            using (BatchScope())
            {
                // 第一轮：优先扣除非满堆叠的槽位
                for (int i = 0; i < Capacity; i++)
                {
                    var slot = Slots[i];
                    if (slot.IsEmpty || slot.Item.Definition != definition) continue;
                    if (slot.Item.StackCount >= definition.MaxStack) continue;

                    int deduct = Math.Min(remainingToDeduct, slot.Item.StackCount);
                    RemoveItemFromSlot(i, deduct);
                    remainingToDeduct -= deduct;

                    if (remainingToDeduct <= 0) break;
                }

                // 第二轮：扣除满堆叠槽位
                if (remainingToDeduct > 0)
                {
                    for (int i = 0; i < Capacity; i++)
                    {
                        var slot = Slots[i];
                        if (slot.IsEmpty || slot.Item.Definition != definition) continue;

                        int deduct = Math.Min(remainingToDeduct, slot.Item.StackCount);
                        RemoveItemFromSlot(i, deduct);
                        remainingToDeduct -= deduct;

                        if (remainingToDeduct <= 0) break;
                    }
                }
            }

            return remainingToDeduct <= 0;
        }

        /// <summary>
        /// 修改槽位物品的数量。
        /// </summary>
        public virtual bool ChangeStackCount(int slotIndex, int newCount)
        {
            if (slotIndex < 0 || slotIndex >= Capacity) return false;

            var slot = Slots[slotIndex];
            if (slot.IsEmpty) return false;

            if (newCount <= 0)
            {
                return RemoveItemFromSlot(slotIndex, slot.Item.StackCount);
            }

            int clampedCount = Math.Min(newCount, slot.Item.Definition.MaxStack);
            int oldCount = slot.Item.StackCount;

            if (oldCount != clampedCount)
            {
                slot.Item.StackCount = clampedCount;
                slot.Item.TriggerStackCountChanged(oldCount, clampedCount);
                MarkSlotDirty(slotIndex);
            }

            return true;
        }

        /// <summary>
        /// 清空槽位。
        /// </summary>
        public virtual bool ClearSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Capacity) return false;
            var slot = Slots[slotIndex];
            if (slot.IsEmpty) return true;

            using (BatchScope())
            {
                var itemToDestroy = slot.Item;
                itemToDestroy.TriggerRemovedFromContainer(this, slotIndex);
                itemToDestroy.TriggerInstanceDestroyed();
                slot.Clear();
                MarkSlotDirty(slotIndex);
            }

            return true;
        }

        /// <summary>
        /// 清空整个容器。
        /// </summary>
        public virtual void ClearContainer()
        {
            using (BatchScope())
            {
                for (int i = 0; i < Capacity; i++)
                {
                    ClearSlot(i);
                }
            }
        }
        #endregion

        #region Query & Inspection APIs
        /// <summary>
        /// 零 GC 统计容器中特定物品定义的总数量。
        /// </summary>
        public virtual int GetTotalItemCount(ItemDefinition definition)
        {
            if (definition == null) return 0;
            int total = 0;
            for (int i = 0; i < Capacity; i++)
            {
                var slot = Slots[i];
                if (!slot.IsEmpty && slot.Item.Definition == definition)
                {
                    total += slot.Item.StackCount;
                }
            }
            return total;
        }

        /// <summary>
        /// 检查容器内特定物品定义的总数量是否满足要求。
        /// </summary>
        public virtual bool HasEnough(ItemDefinition definition, int count)
        {
            return GetTotalItemCount(definition) >= count;
        }

        /// <summary>
        /// 寻找第一个空槽位。
        /// </summary>
        public virtual bool FindFirstEmptySlot(out int slotIndex)
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (Slots[i].IsEmpty)
                {
                    slotIndex = i;
                    return true;
                }
            }
            slotIndex = -1;
            return false;
        }

        /// <summary>
        /// 寻找第一个可以与指定物品进行堆叠的槽位。
        /// </summary>
        public virtual bool FindFirstStackableSlot(ItemInstance item, out int slotIndex)
        {
            slotIndex = -1;
            if (item == null || !item.Definition.IsStackable) return false;

            for (int i = 0; i < Capacity; i++)
            {
                var slot = Slots[i];
                if (!slot.IsEmpty && slot.CanAccept(this, item) && slot.Item.CanStackWith(item))
                {
                    slotIndex = i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 无分配根据条件查找满足匹配规则的所有槽位。
        /// </summary>
        public virtual void FindSlots(Predicate<ItemSlot> match, List<ItemSlot> results)
        {
            if (match == null || results == null) return;
            results.Clear();

            for (int i = 0; i < Capacity; i++)
            {
                if (match(Slots[i]))
                {
                    results.Add(Slots[i]);
                }
            }
        }
        #endregion

        #region Sort & Optimization APIs
        /// <summary>
        /// 自动归拢合并未满堆叠的同类物品。
        /// </summary>
        public virtual void ConsolidateStacks()
        {
            using (BatchScope())
            {
                for (int i = 0; i < Capacity; i++)
                {
                    var sourceSlot = Slots[i];
                    if (sourceSlot.IsEmpty || !sourceSlot.Item.Definition.IsStackable) continue;
                    if (sourceSlot.Item.StackCount >= sourceSlot.Item.Definition.MaxStack) continue;

                    for (int j = i + 1; j < Capacity; j++)
                    {
                        var targetSlot = Slots[j];
                        if (targetSlot.IsEmpty) continue;

                        if (sourceSlot.Item.CanStackWith(targetSlot.Item))
                        {
                            SwapOrMergeSlots(j, i);
                            if (sourceSlot.Item.StackCount >= sourceSlot.Item.Definition.MaxStack)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 对背包容器进行自定义排序（零 GC 分配）。
        /// </summary>
        public virtual void AutoSort(IComparer<ItemSlot> comparer)
        {
            if (comparer == null) return;

            using (BatchScope())
            {
                // 先合并堆叠
                ConsolidateStacks();

                // 复用缓冲区提取现存槽位引用并排序
                _sortSlotBuffer.Clear();
                _sortSlotBuffer.AddRange(Slots);
                _sortSlotBuffer.Sort(comparer);

                // 提取所有 Item 引用
                _sortItemBuffer.Clear();
                for (int i = 0; i < Capacity; i++)
                {
                    _sortItemBuffer.Add(_sortSlotBuffer[i].Item);
                }

                // 重新写回 Slot 保持数组结构不变
                for (int i = 0; i < Capacity; i++)
                {
                    if (Slots[i].Item != _sortItemBuffer[i])
                    {
                        Slots[i].Item = _sortItemBuffer[i];
                        MarkSlotDirty(i);
                    }
                }
            }
        }

        /// <summary>
        /// 动态调整/扩展容器容量上限。
        /// </summary>
        /// <param name="newCapacity">新的目标容量</param>
        /// <returns>若成功调整返回 true，若缩容导致未空槽位截断丢失则拒绝并返回 false</returns>
        public virtual bool ResizeCapacity(int newCapacity)
        {
            if (newCapacity <= 0 || newCapacity == Capacity) return false;

            // 缩容安全保护：被截断的槽位如果含有物品，拒绝缩容
            if (newCapacity < Capacity)
            {
                for (int i = newCapacity; i < Capacity; i++)
                {
                    if (Slots[i] != null && !Slots[i].IsEmpty)
                    {
                        return false;
                    }
                }
            }

            using (BatchScope())
            {
                int oldCapacity = Capacity;
                ItemSlot[] oldSlots = Slots;
                ItemSlot[] newSlots = new ItemSlot[newCapacity];
                int copyCount = Math.Min(oldCapacity, newCapacity);

                for (int i = 0; i < copyCount; i++)
                {
                    newSlots[i] = oldSlots[i];
                }

                for (int i = copyCount; i < newCapacity; i++)
                {
                    newSlots[i] = new ItemSlot(i);
                }

                Capacity = newCapacity;
                Slots = newSlots;

                for (int i = 0; i < newCapacity; i++)
                {
                    MarkSlotDirty(i);
                }
            }

            return true;
        }
        #endregion
    }
}
