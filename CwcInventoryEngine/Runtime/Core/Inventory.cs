using System;
using System.Collections.Generic;
using UnityEngine;
using Cwc.InventoryEngine.UI;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 仅用于 Inspector 调试显示的物品组件信息包装结构体。
    /// </summary>
    [Serializable]
    public struct DebugComponentInfo
    {
        [SerializeField] private string _componentType;
        [SerializeField] private bool _isEnabled;
        [SerializeField] private int _priority;

        public string ComponentType => _componentType;
        public bool IsEnabled => _isEnabled;
        public int Priority => _priority;

        public DebugComponentInfo(ItemComponentBase comp)
        {
            _componentType = comp != null ? comp.GetType().Name : "Null";
            _isEnabled = comp != null && comp.IsEnabled;
            _priority = comp != null ? comp.Priority : 0;
        }
    }

    /// <summary>
    /// 仅用于 Inspector 调试显示的背包槽位信息包装结构体。
    /// </summary>
    [Serializable]
    public struct DebugSlotInfo
    {
        [SerializeField] private int _slotIndex;
        [SerializeField] private bool _isDisabled;
        [SerializeField] private bool _isEmpty;
        [SerializeField] private ItemDefinition _definition;
        [SerializeField] private int _stackCount;
        [SerializeField] private string _instanceId;
        [SerializeField] private List<DebugComponentInfo> _components;

        public int SlotIndex => _slotIndex;
        public bool IsDisabled => _isDisabled;
        public bool IsEmpty => _isEmpty;
        public ItemDefinition Definition => _definition;
        public int StackCount => _stackCount;
        public string InstanceId => _instanceId;
        public IReadOnlyList<DebugComponentInfo> Components => _components;

        public DebugSlotInfo(ItemSlot slot)
        {
            if (slot == null)
            {
                _slotIndex = -1;
                _isDisabled = false;
                _isEmpty = true;
                _definition = null;
                _stackCount = 0;
                _instanceId = string.Empty;
                _components = new List<DebugComponentInfo>();
                return;
            }

            _slotIndex = slot.SlotIndex;
            _isDisabled = slot.IsDisabled;
            ItemInstance item = slot.Item;
            _isEmpty = slot.IsEmpty || item == null;

            if (!_isEmpty && item != null)
            {
                _definition = item.Definition;
                _stackCount = item.StackCount;
                _instanceId = item.InstanceID.ToString();
                _components = new List<DebugComponentInfo>();

                if (item.Components != null)
                {
                    int count = item.Components.Count;
                    for (int i = 0; i < count; i++)
                    {
                        _components.Add(new DebugComponentInfo(item.Components[i]));
                    }
                }
            }
            else
            {
                _definition = null;
                _stackCount = 0;
                _instanceId = string.Empty;
                _components = new List<DebugComponentInfo>();
            }
        }
    }

    /// <summary>
    /// MonoBehaviour 实际库存封装组件。
    /// 纯事件驱动的开箱即用基础组件，可直接挂载到 Player、Chest、Vendor、Drop-bag 上。
    /// 自动向全局 InventoryRegistry 注册 InventoryID，并通过 InventoryEventPipeline 事件管线交流解耦。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/Inventory")]
    public class Inventory : MonoBehaviour
    {
        #region Serialized Fields
        [Header("基础配置")]
        [SerializeField]
        [Tooltip("库存唯一标识 ID/名称（例如：MainInventory, PlayerEquipment, Chest_01）。UI 通过此 ID 事件管线交流")]
        private string _inventoryId = "MainInventory";

        [SerializeField]
        [Tooltip("背包容量上限")]
        [Min(1)]
        private int _capacity = 20;

        [SerializeField]
        [Tooltip("是否在 Awake() 时自动初始化容器")]
        private bool _autoInitialize = true;

        [SerializeField]
        [Tooltip("是否允许被 InventoryRouter 拾取路由自动分发 (玩家主背包/装备栏设为 true，宝箱/商人货架/地图掉落袋设为 false)")]
        private bool _isRoutable = true;

        [SerializeField]
        [Tooltip("槽位限制预设资产 SO (可选)。若配置此项，将优先覆盖本地的 _slotRestrictions 列表。便于实现多职业/姿态配置一键替换")]
        private SlotRestrictionPresetSO _restrictionPreset;

        [SerializeField]
        [Tooltip("本地槽位类型限制配置表（可选）。列表索引(0, 1, 2...)隐式 1:1 对应槽位索引。若配置了 _restrictionPreset 则优先使用预设")]
        private List<SlotRestriction> _slotRestrictions = new();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Header("调试视图 (Debug Only)")]
        [SerializeField]
        [Tooltip("运行时背包槽位列表状态（仅供 Inspector 调试观察，不可直接编辑）")]
        private List<DebugSlotInfo> _debugSlots = new();
#endif
        #endregion

        #region Private Fields
        private InventoryContainer _container;
        private bool _isInitialized = false;
        private bool _isRegistered = false;
        #endregion

        #region Public Properties
        /// <summary>
        /// 库存唯一标识 ID。
        /// </summary>
        public string InventoryId => _inventoryId;

        /// <summary>
        /// 容量上限。
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// 是否已经初始化。
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 核心纯 C# 领域库存容器实体 (Domain Model)。
        /// </summary>
        public InventoryContainer Container => _container;

        /// <summary>
        /// 快捷获取当前槽位数组。
        /// </summary>
        public ItemSlot[] Slots => _container != null ? _container.Slots : null;

        /// <summary>
        /// 检查当前库存是否允许被全局路由器 (InventoryRouter) 拾取路由自动分发。
        /// </summary>
        public bool IsRoutable => _isRoutable;

        /// <summary>
        /// 当前生效的限制预设 SO。
        /// </summary>
        public SlotRestrictionPresetSO RestrictionPreset => _restrictionPreset;

        /// <summary>
        /// 获取当前生效的槽位限制规则列表（优先使用 _restrictionPreset，若为空退回使用本地 _slotRestrictions）。
        /// </summary>
        public IReadOnlyList<SlotRestriction> ActiveSlotRestrictions
        {
            get
            {
                if (_restrictionPreset != null && _restrictionPreset.SlotRestrictions != null)
                {
                    return _restrictionPreset.SlotRestrictions;
                }
                return _slotRestrictions;
            }
        }

        /// <summary>
        /// 检查当前库存是否配置了特定的槽位分类限制 (如装备栏、药水栏等专用限制库存)。
        /// </summary>
        public bool HasCategoryRestriction
        {
            get
            {
                var restrictions = ActiveSlotRestrictions;
                if (restrictions == null || restrictions.Count == 0) return false;
                int count = restrictions.Count;
                for (int i = 0; i < count; i++)
                {
                    var restriction = restrictions[i];
                    if (restriction.AllowedCategories != null && restriction.AllowedCategories.Count > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// 仅用于调试的只读槽位视图列表。
        /// </summary>
        public IReadOnlyList<DebugSlotInfo> DebugSlots => _debugSlots;
#endif
        #endregion

        #region Public Events
        /// <summary>
        /// 槽位数据变更事件转发。
        /// </summary>
        public event Action<int, ItemSlot> OnSlotUpdated;

        /// <summary>
        /// 批处理事务完成事件转发。
        /// </summary>
        public event Action OnBatchCompleted;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_autoInitialize)
            {
                InitializeContainer();
            }
        }

        private void OnEnable()
        {
            if (_isInitialized && _container != null)
            {
                RegisterToPipeline();
            }
        }

        private void OnDisable()
        {
            UnregisterFromPipeline();
        }

        private void OnDestroy()
        {
            UnbindContainerEvents();
            UnregisterFromPipeline();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 初始化底层 InventoryContainer 容器并注册全局 ID。
        /// </summary>
        public virtual void InitializeContainer()
        {
            if (_isInitialized) return;

            _container = new InventoryContainer(_capacity);

            // 自动装配槽位类型限制 Filter
            RebuildSlotFilters();

            BindContainerEvents();
            _isInitialized = true;

            UpdateDebugSlots();

            if (gameObject.activeInHierarchy)
            {
                RegisterToPipeline();
            }
        }

        /// <summary>
        /// 运行时动态应用/切换槽位限制预设资产（适用于职业切换、姿态切换、装备模板切换等场景）。
        /// 自动重构当前容器所有槽位的过滤器，并广播内容变更消息通知 UI 刷新。
        /// </summary>
        /// <param name="preset">新的限制预设 SO (若传入 null 则退回使用本地 Inspector 列表)</param>
        public virtual void ApplyRestrictionPreset(SlotRestrictionPresetSO preset)
        {
            _restrictionPreset = preset;

            if (_isInitialized && _container != null)
            {
                RebuildSlotFilters();
            }
        }

        /// <summary>
        /// 动态重命名/修改 InventoryID 并重新注册。
        /// </summary>
        public virtual void SetInventoryId(string newInventoryId)
        {
            if (string.Equals(_inventoryId, newInventoryId, StringComparison.OrdinalIgnoreCase)) return;

            UnregisterFromPipeline();
            _inventoryId = newInventoryId;
            if (_isInitialized && _container != null)
            {
                RegisterToPipeline();
            }
        }

        /// <summary>
        /// 智能自查：零 GC 判断当前库存是否有可以接收该物品的有效槽位 (包含现有同类可堆叠槽位与匹配 Filter 的空槽位)。
        /// </summary>
        /// <param name="item">待放入的物品实例</param>
        /// <returns>若有匹配可用的槽位返回 true，否则返回 false</returns>
        public virtual bool CanAcceptItem(ItemInstance item)
        {
            if (!_isInitialized || _container == null || item == null) return false;
            return _container.HasAvailableSlotForItem(item);
        }

        /// <summary>
        /// 尝试添加一个运行时物品实例。
        /// </summary>
        public virtual bool TryAddItem(ItemInstance item, out ItemInstance remainder)
        {
            if (!_isInitialized || _container == null)
            {
                remainder = item;
                return false;
            }
            return _container.TryAddItem(item, out remainder);
        }

        /// <summary>
        /// 快捷添加指定 ScriptableObject 定义的物品。
        /// </summary>
        public virtual bool TryAddItem(ItemDefinition definition, int count = 1)
        {
            if (!_isInitialized || _container == null || definition == null) return false;
            ItemInstance instance = definition.CreateInstance(count);
            return _container.TryAddItem(instance, out _);
        }

        /// <summary>
        /// 根据物品定义按数量移除物品。
        /// </summary>
        public virtual bool RemoveItem(ItemDefinition definition, int count)
        {
            if (!_isInitialized || _container == null || definition == null) return false;
            return _container.RemoveItemDefinition(definition, count);
        }

        /// <summary>
        /// 扣减或清空指定槽位的物品堆叠数量。
        /// </summary>
        /// <param name="slotIndex">槽位索引</param>
        /// <param name="count">扣减数量 (<=0 或大于等于堆叠数表示清空/全扣)</param>
        /// <returns>若成功扣减返回 true，否则返回 false</returns>
        public virtual bool RemoveItemFromSlot(int slotIndex, int count)
        {
            if (!_isInitialized || _container == null) return false;
            bool success = _container.RemoveItemFromSlot(slotIndex, count);
            if (success) UpdateDebugSlots();
            return success;
        }

        /// <summary>
        /// 清空整个背包。
        /// </summary>
        public virtual void Clear()
        {
            if (!_isInitialized || _container == null) return;
            _container.ClearContainer();
            UpdateDebugSlots();
        }

        #region 高频查询与统计 API
        /// <summary>
        /// 零 GC 统计指定物品定义在当前背包中的总堆叠数量。
        /// </summary>
        public virtual int GetItemCount(ItemDefinition definition)
        {
            if (!_isInitialized || _container == null || definition == null) return 0;
            return _container.GetTotalItemCount(definition);
        }

        /// <summary>
        /// 检查当前背包中特定物品定义的总数量是否满足指定要求。
        /// </summary>
        public virtual bool HasItem(ItemDefinition definition, int requiredCount = 1)
        {
            if (!_isInitialized || _container == null || definition == null) return false;
            return _container.HasEnough(definition, requiredCount);
        }

        /// <summary>
        /// 查询指定物品实例所在的槽位索引。
        /// </summary>
        public virtual bool TryFindSlotWithItem(ItemInstance instance, out int slotIndex)
        {
            slotIndex = -1;
            if (!_isInitialized || _container == null || instance == null) return false;

            int capacity = _container.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                var slot = _container.Slots[i];
                if (!slot.IsEmpty && slot.Item == instance)
                {
                    slotIndex = i;
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region 动态扩容与锁槽 API
        /// <summary>
        /// 动态调整/扩容背包容量上限。
        /// </summary>
        public virtual bool ResizeCapacity(int newCapacity)
        {
            if (!_isInitialized || _container == null) return false;
            bool success = _container.ResizeCapacity(newCapacity);
            if (success)
            {
                _capacity = newCapacity;
                UpdateDebugSlots();
            }
            return success;
        }

        /// <summary>
        /// 锁定/解锁指定索引的槽位 (用于开启/关闭特定槽位的交互支持)。
        /// </summary>
        public virtual bool SetSlotLocked(int slotIndex, bool isLocked)
        {
            if (!_isInitialized || _container == null || slotIndex < 0 || slotIndex >= _container.Capacity) return false;
            _container.Slots[slotIndex].IsDisabled = isLocked;
            UpdateDebugSlots();
            InventoryEventPipeline.Publish(new InventoryEvent(InventoryEventType.SlotUpdated, _inventoryId, _container, slotIndex, _container.Slots[slotIndex].Item));
            return true;
        }
        #endregion

        #region 整理与排序 API
        /// <summary>
        /// 一键归拢合并未满堆叠的同类物品。
        /// </summary>
        public virtual void Consolidate()
        {
            if (!_isInitialized || _container == null) return;
            _container.ConsolidateStacks();
            UpdateDebugSlots();
        }

        /// <summary>
        /// 一键按指定模式进行排序 (结合 BatchScope 只产生一次 UI 视图刷新广播)。
        /// </summary>
        public virtual void Sort(InventorySortMode sortMode)
        {
            if (!_isInitialized || _container == null || sortMode == InventorySortMode.None) return;

            _container.AutoSort(new InventorySortComparerAdapter(sortMode));
            UpdateDebugSlots();
        }

        private class InventorySortComparerAdapter : IComparer<ItemSlot>
        {
            private readonly InventorySortMode _mode;
            public InventorySortComparerAdapter(InventorySortMode mode) => _mode = mode;
            public int Compare(ItemSlot x, ItemSlot y) => InventorySortComparers.CompareSlots(x, y, _mode);
        }
        #endregion

        #region 一行代码 JSON 持久化 API
        /// <summary>
        /// 一行代码导出当前库存数据为 JSON 格式字符串。
        /// </summary>
        public virtual string SaveToJson(IItemAssetResolver resolver)
        {
            if (!_isInitialized || _container == null || resolver == null) return string.Empty;

            InventorySaveData saveData = InventorySaveData.Export(_container, resolver);
            return JsonUtility.ToJson(saveData);
        }

        /// <summary>
        /// 一行代码从 JSON 格式文本还原当前库存数据，并触发全局事件广播。
        /// </summary>
        public virtual bool LoadFromJson(string json, IItemAssetResolver resolver)
        {
            if (string.IsNullOrEmpty(json) || resolver == null) return false;

            try
            {
                InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);
                if (saveData == null) return false;

                if (!_isInitialized)
                {
                    InitializeContainer();
                }

                saveData.RestoreToContainer(_container, resolver);
                UpdateDebugSlots();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Inventory] 从 JSON 还原背包 '{_inventoryId}' 失败: {ex.Message}");
                return false;
            }
        }
        #endregion
        #endregion

        #region Private Methods
        /// <summary>
        /// 根据当前激活的 Restriction 配置重建所有槽位的 Filter，并广播更新通知。
        /// </summary>
        protected virtual void RebuildSlotFilters()
        {
            if (_container == null || _container.Slots == null) return;

            var restrictions = ActiveSlotRestrictions;
            int capacity = _container.Capacity;
            int restrictionCount = restrictions != null ? restrictions.Count : 0;

            for (int i = 0; i < capacity; i++)
            {
                if (i < restrictionCount)
                {
                    var restriction = restrictions[i];
                    if (restriction.AllowedCategories != null && restriction.AllowedCategories.Count > 0)
                    {
                        _container.Slots[i].Filter = new ItemCategoryFilter(restriction.AllowedCategories);
                        continue;
                    }
                }

                // 若该槽位无限制则重置 Filter 为 null
                _container.Slots[i].Filter = null;
            }

            UpdateDebugSlots();
            InventoryEventPipeline.PublishContentChanged(_inventoryId, _container);
        }

        protected virtual void RegisterToPipeline()
        {
            if (_isRegistered || string.IsNullOrEmpty(_inventoryId) || _container == null) return;

            InventoryRegistry.Register(_inventoryId, _container, this);
            InventoryEventPipeline.Publish(new InventoryEvent(InventoryEventType.Registered, _inventoryId, _container));
            _isRegistered = true;
        }

        protected virtual void UnregisterFromPipeline()
        {
            if (!_isRegistered || string.IsNullOrEmpty(_inventoryId)) return;

            InventoryRegistry.Unregister(_inventoryId);
            InventoryEventPipeline.Publish(new InventoryEvent(InventoryEventType.Unregistered, _inventoryId));
            _isRegistered = false;
        }

        protected virtual void BindContainerEvents()
        {
            if (_container != null)
            {
                _container.OnSlotUpdated += HandleSlotUpdated;
                _container.OnBatchCompleted += HandleBatchCompleted;
            }
        }

        protected virtual void UnbindContainerEvents()
        {
            if (_container != null)
            {
                _container.OnSlotUpdated -= HandleSlotUpdated;
                _container.OnBatchCompleted -= HandleBatchCompleted;
            }
        }

        protected virtual void HandleSlotUpdated(int slotIndex, ItemSlot slot)
        {
            UpdateDebugSlots();
            OnSlotUpdated?.Invoke(slotIndex, slot);
            InventoryEventPipeline.Publish(new InventoryEvent(InventoryEventType.SlotUpdated, _inventoryId, _container, slotIndex, slot?.Item));
        }

        protected virtual void HandleBatchCompleted()
        {
            UpdateDebugSlots();
            OnBatchCompleted?.Invoke();
            InventoryEventPipeline.PublishContentChanged(_inventoryId, _container);
        }

        protected virtual void UpdateDebugSlots()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _debugSlots.Clear();
            if (_container == null || _container.Slots == null) return;

            int count = _container.Capacity;
            for (int i = 0; i < count; i++)
            {
                ItemSlot slot = _container.Slots[i];
                _debugSlots.Add(new DebugSlotInfo(slot));
            }
#endif
        }
        #endregion
    }
}
