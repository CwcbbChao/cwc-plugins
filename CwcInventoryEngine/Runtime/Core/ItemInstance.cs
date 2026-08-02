using System;
using System.Collections.Generic;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 运行时动态物品实例。
    /// 代表实际在游戏内存中流转的物品实体（例如玩家背包中的一格具体物品）。
    /// </summary>
    public class ItemInstance
    {
        #region Private Fields
        private readonly List<ItemComponentBase> _components = new();
        #endregion

        #region Public Properties
        /// <summary>
        /// 全局唯一物品实例标识 (零 GC 值类型)。
        /// </summary>
        public ItemId InstanceID { get; }

        /// <summary>
        /// 关联的静态 ScriptableObject 资产定义。
        /// </summary>
        public ItemDefinition Definition { get; }

        /// <summary>
        /// 当前堆叠数量。
        /// 严格收口 Setter，外部仅能通过容器事务或受控方法修改。
        /// </summary>
        public int StackCount { get; internal set; }

        /// <summary>
        /// 运行时组件只读列表。
        /// </summary>
        public IReadOnlyList<ItemComponentBase> Components => _components;
        #endregion

        #region Constructors
        /// <summary>
        /// 构造函数，根据静态定义初始化运行时组件。
        /// </summary>
        /// <param name="instanceID">唯一物品标识</param>
        /// <param name="definition">静态资产定义</param>
        /// <param name="stackCount">初始堆叠数</param>
        public ItemInstance(ItemId instanceID, ItemDefinition definition, int stackCount)
        {
            InstanceID = instanceID;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            StackCount = stackCount;

            // 根据静态 Definition 初始化运行时组件
            InitializeComponents();
        }
        #endregion

        #region Component Lookup
        /// <summary>
        /// 检查物品上是否存在指定的组件或接口实现（且处于启用状态）。
        /// </summary>
        /// <typeparam name="T">组件类型或接口类型</typeparam>
        /// <returns>若存在且已启用返回 true，否则返回 false</returns>
        public bool HasComponent<T>() where T : class
        {
            int count = _components.Count;
            for (int i = 0; i < count; i++)
            {
                if (_components[i] is T target && _components[i].IsEnabled)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取优先级最高且处于启用状态的指定组件或接口实现（零 GC）。
        /// 由于内部组件列表在构造时已按 Priority 降序排列，找到的首个匹配项即为优先级最高的实现。
        /// </summary>
        /// <typeparam name="T">组件类型或接口类型</typeparam>
        /// <param name="comp">匹配的组件/接口输出</param>
        /// <returns>若找到返回 true，否则返回 false</returns>
        public bool TryGetComponent<T>(out T comp) where T : class
        {
            int count = _components.Count;
            for (int i = 0; i < count; i++)
            {
                if (_components[i] is T target && _components[i].IsEnabled)
                {
                    comp = target;
                    return true;
                }
            }
            comp = null;
            return false;
        }

        /// <summary>
        /// 快捷方法：显式语义化获取优先级最高的接口/组件实现。
        /// 内部等价于 TryGetComponent&lt;T&gt;(out comp)。
        /// </summary>
        /// <typeparam name="T">组件类型或接口类型</typeparam>
        /// <param name="comp">匹配的最高优先级组件/接口输出</param>
        /// <returns>若找到返回 true，否则返回 false</returns>
        public bool GetHighestPriorityComponent<T>(out T comp) where T : class
        {
            return TryGetComponent<T>(out comp);
        }

        /// <summary>
        /// 获取优先级最高且处于启用状态的组件或接口，若不存在则返回 null。
        /// </summary>
        /// <typeparam name="T">组件类型或接口类型</typeparam>
        /// <returns>组件/接口实例或 null</returns>
        public T GetComponent<T>() where T : class
        {
            TryGetComponent<T>(out T comp);
            return comp;
        }

        /// <summary>
        /// 零 GC 获取所有匹配且已启用的组件或接口实现列表。
        /// 写入 results 中的顺序严格按照 Priority 从高到低排列。
        /// </summary>
        /// <typeparam name="T">组件类型或接口类型</typeparam>
        /// <param name="results">外部传入的结果列表，用以避免堆内存分配 GC Alloc</param>
        public void GetComponents<T>(List<T> results) where T : class
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();

            int count = _components.Count;
            for (int i = 0; i < count; i++)
            {
                var comp = _components[i];
                if (comp is T target && comp.IsEnabled)
                {
                    results.Add(target);
                }
            }
        }

        /// <summary>
        /// 获取所有匹配且已启用的组件或接口实现数组（按 Priority 降序排列）。
        /// 便捷方法，内部会 new 数组，在性能敏感/高频循环中建议改用 GetComponents(List&lt;T&gt; results)。
        /// </summary>
        /// <typeparam name="T">组件类型或接口类型</typeparam>
        /// <returns>匹配的组件/接口数组</returns>
        public T[] GetComponents<T>() where T : class
        {
            var list = new List<T>();
            GetComponents(list);
            return list.ToArray();
        }
        #endregion

        #region Stacking & Splitting
        /// <summary>
        /// 拆分当前物品堆叠数量，扣减自身堆叠数并产生并返回一个新的物品实例。
        /// </summary>
        /// <param name="splitCount">要剥离拆出的数量</param>
        /// <returns>新拆分出来的物品实例</returns>
        public ItemInstance Split(int splitCount)
        {
            if (splitCount <= 0 || StackCount <= 0) return null;
            int actualCount = Math.Min(splitCount, StackCount);

            int oldStackCount = StackCount;
            StackCount -= actualCount;
            TriggerStackCountChanged(oldStackCount, StackCount);

            ItemInstance newInstance = Definition.CreateInstance(actualCount);
            TriggerStackSplit(newInstance, actualCount);

            return newInstance;
        }

        /// <summary>
        /// 校验当前物品是否可以与目标物品进行堆叠合并。
        /// 条件：Definition 相同 + 属于可堆叠物品 + 目标未达上限 + 所有组件 IsStackCompatible 均通过。
        /// </summary>
        /// <param name="target">目标物品实例</param>
        /// <returns>若可以堆叠返回 true，否则返回 false</returns>
        public bool CanStackWith(ItemInstance target)
        {
            if (target == null) return false;
            if (ReferenceEquals(this, target)) return false;
            if (Definition != target.Definition) return false;
            if (!Definition.IsStackable) return false;
            if (target.StackCount >= target.Definition.MaxStack) return false;

            // 校验各层组件的兼容性
            int count = _components.Count;
            for (int i = 0; i < count; i++)
            {
                var sourceComp = _components[i];
                if (!sourceComp.IsEnabled) continue;

                // 查找目标物品中对应的组件类型
                if (target.TryGetComponent(sourceComp.GetType(), out var targetComp))
                {
                    if (!sourceComp.IsStackCompatible(targetComp))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 根据 Type 获取运行时组件（内部/私有辅助）。
        /// </summary>
        internal bool TryGetComponent(Type type, out ItemComponentBase comp)
        {
            int count = _components.Count;
            for (int i = 0; i < count; i++)
            {
                if (_components[i].GetType() == type)
                {
                    comp = _components[i];
                    return true;
                }
            }
            comp = null;
            return false;
        }
        #endregion

        #region Internal Lifecycle Triggers
        private void InitializeComponents()
        {
            if (Definition.ComponentDefinitions == null) return;

            int count = Definition.ComponentDefinitions.Count;
            for (int i = 0; i < count; i++)
            {
                var def = Definition.ComponentDefinitions[i];
                if (def == null) continue;

                var runtimeComp = def.CreateRuntime();
                if (runtimeComp != null)
                {
                    _components.Add(runtimeComp);
                }
            }

            // 按优先级在构造时进行一次预排序，保障管道高效率
            _components.Sort();

            // 触发创建钩子
            int compCount = _components.Count;
            for (int i = 0; i < compCount; i++)
            {
                _components[i].OnInstanceCreated(this);
            }
        }

        internal void TriggerLoadedLifecycle()
        {
            int count = _components.Count;
            for (int i = 0; i < count; i++)
            {
                _components[i].OnInstanceLoaded(this);
            }
        }

        internal void TriggerAddedToContainer(InventoryContainer container, int slotIndex)
        {
            int count = _components.Count;
            for (int i = 0; i < count; i++)
            {
                _components[i].OnAddedToContainer(container, slotIndex);
            }
        }

        internal void TriggerRemovedFromContainer(InventoryContainer container, int slotIndex)
        {
            int count = _components.Count;
            for (int i = 0; i < count; i++)
            {
                _components[i].OnRemovedFromContainer(container, slotIndex);
            }
        }

        internal void TriggerStackCountChanged(int oldCount, int newCount)
        {
            int count = _components.Count;
            for (int i = 0; i < count; i++)
            {
                _components[i].OnStackCountChanged(oldCount, newCount);
            }
        }

        internal void TriggerStackMerged(ItemInstance sourceInstance, int transferredCount)
        {
            int count = _components.Count;
            for (int i = 0; i < count; i++)
            {
                _components[i].OnStackMerged(sourceInstance, transferredCount);
            }
        }

        internal void TriggerStackSplit(ItemInstance newInstance, int splitCount)
        {
            int count = _components.Count;
            for (int i = 0; i < count; i++)
            {
                _components[i].OnStackSplit(newInstance, splitCount);
            }
        }

        internal void TriggerInstanceDestroyed()
        {
            int count = _components.Count;
            for (int i = 0; i < count; i++)
            {
                _components[i].OnInstanceDestroyed();
            }
        }
        #endregion
    }
}
