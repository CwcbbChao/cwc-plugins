using System;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 运行时物品组件基类。
    /// 跟随 ItemInstance 存在于动态内存中，代表物品的可变状态与行为扩展。
    /// </summary>
    public abstract class ItemComponentBase : IComparable<ItemComponentBase>
    {
        #region Properties & Fields
        /// <summary>
        /// 组件当前是否处于启用状态。
        /// </summary>
        public virtual bool IsEnabled => true;

        /// <summary>
        /// 管道与事件回调执行优先级（数值越大优先级越高，类似 Unity Volume 机制）。
        /// </summary>
        public virtual int Priority => 0;
        #endregion

        #region Priority Logic
        /// <summary>
        /// 判定两个组件的运行时状态是否兼容堆叠。
        /// 例如：不同耐久度或不同动态词缀的物品默认不兼容堆叠。
        /// </summary>
        /// <param name="other">目标物品的对应组件</param>
        /// <returns>若兼容返回 true，否则返回 false</returns>
        public virtual bool IsStackCompatible(ItemComponentBase other) => true;

        public int CompareTo(ItemComponentBase other)
        {
            if (other == null) return 1;
            return other.Priority.CompareTo(Priority);
        }
        #endregion

        #region Lifecycle Hooks
        /// <summary>
        /// 当物品实例首次在内存中被新建时调用。
        /// </summary>
        /// <param name="instance">所属物品实例</param>
        public virtual void OnInstanceCreated(ItemInstance instance) { }

        /// <summary>
        /// 当物品实例从存档恢复反序列化完成后调用。
        /// </summary>
        /// <param name="instance">所属物品实例</param>
        public virtual void OnInstanceLoaded(ItemInstance instance) { }

        /// <summary>
        /// 当物品放入容器槽位时调用。
        /// </summary>
        /// <param name="container">目标容器</param>
        /// <param name="slotIndex">目标槽位</param>
        public virtual void OnAddedToContainer(InventoryContainer container, int slotIndex) { }

        /// <summary>
        /// 当物品从容器槽位移出时调用。
        /// </summary>
        /// <param name="container">目标容器</param>
        /// <param name="slotIndex">源槽位</param>
        public virtual void OnRemovedFromContainer(InventoryContainer container, int slotIndex) { }

        /// <summary>
        /// 当物品堆叠数量发生变化时调用。
        /// </summary>
        /// <param name="oldCount">变更前的堆叠数量</param>
        /// <param name="newCount">变更后的堆叠数量</param>
        public virtual void OnStackCountChanged(int oldCount, int newCount) { }

        /// <summary>
        /// 当源物品堆叠合并入当前物品时调用。
        /// </summary>
        /// <param name="sourceInstance">源物品实例</param>
        /// <param name="transferredCount">转移的堆叠数量</param>
        public virtual void OnStackMerged(ItemInstance sourceInstance, int transferredCount) { }

        /// <summary>
        /// 当从当前物品拆分出新物品实例时调用。
        /// </summary>
        /// <param name="newInstance">拆分出的新物品实例</param>
        /// <param name="splitCount">拆分出的数量</param>
        public virtual void OnStackSplit(ItemInstance newInstance, int splitCount) { }

        /// <summary>
        /// 当物品实例被彻底销毁或数量降为零时调用。
        /// </summary>
        public virtual void OnInstanceDestroyed() { }
        #endregion
    }

    /// <summary>
    /// 强类型运行时物品组件基类。
    /// 跟随 ItemInstance 存在于动态内存中，自动持有与其配对的静态组件定义引用 TDefinition。
    /// </summary>
    /// <typeparam name="TDefinition">配对静态定义组件类型</typeparam>
    public abstract class ItemComponentBase<TDefinition> : ItemComponentBase
        where TDefinition : ItemComponentDefinition
    {
        #region Properties & Fields
        /// <summary>
        /// 配对的静态定义组件引用 (只读)。
        /// </summary>
        public TDefinition Definition { get; }
        #endregion

        #region Constructors
        /// <summary>
        /// 泛型基类构造函数，强制传入绑定的静态定义组件。
        /// </summary>
        /// <param name="definition">静态定义组件实例</param>
        protected ItemComponentBase(TDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
        #endregion
    }
}
