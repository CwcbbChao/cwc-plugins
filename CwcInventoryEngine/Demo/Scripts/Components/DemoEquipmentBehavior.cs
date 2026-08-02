using System.Collections.Generic;
using UnityEngine;
using Cwc.InventoryEngine;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// Demo 装备行为适配组件 (Demo Equipment Behavior)。
    /// 挂载在装备栏 Inventory 节点或角色节点上。
    /// 
    /// 核心理念（解耦行为模式）：
    /// 1. 允许普通的 Inventory 零改动、零子类化即可直接作为装备栏使用。
    /// 2. 本组件通过监听容器的动态内容变更，自动追踪每个装备槽位的物品进出。
    /// 3. 当有装备被放进槽位时，自动对其触发 TryEquip(user) 增加角色属性；
    ///    当装备被拿出/卸下/替换时，自动对旧装备触发 TryUnequip(user) 扣除加成。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/Demo/Demo Equipment Behavior")]
    public class DemoEquipmentBehavior : MonoBehaviour
    {
        #region Serialized Fields
        [Header("绑定的装备容器与角色")]
        [SerializeField]
        [Tooltip("关联的装备栏 Inventory 组件（若未拖拽，将在 Awake 时自动获取）")]
        private Inventory _equipmentInventory;

        [SerializeField]
        [Tooltip("关联的演示角色实体 DemoCharacter（若未拖拽，将在 Awake 时自动查找）")]
        private DemoCharacter _targetCharacter;
        #endregion

        #region Private Fields
        /// <summary>
        /// 槽位历史物品快照列表，用于对比物品的进出变更。
        /// </summary>
        private readonly List<ItemInstance> _lastSlotItemsSnapshot = new();
        private bool _isSubscribed = false;
        #endregion

        #region Public Properties
        /// <summary>
        /// 关联的装备栏容器。
        /// </summary>
        public Inventory EquipmentInventory => _equipmentInventory;

        /// <summary>
        /// 目标角色实体。
        /// </summary>
        public DemoCharacter TargetCharacter => _targetCharacter;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_equipmentInventory == null)
            {
                _equipmentInventory = GetComponent<Inventory>();
            }

            if (_targetCharacter == null)
            {
                _targetCharacter = GetComponent<DemoCharacter>();
                if (_targetCharacter == null)
                {
                    _targetCharacter = FindFirstObjectByType<DemoCharacter>();
                }
            }
        }

        private void OnEnable()
        {
            SubscribeToEvents();
            EvaluateEquipmentChanges();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            // 当禁用行为组件时，卸下所有当前装备栏中的物品加成
            ClearAllEquippedBonuses();
        }

        private void Start()
        {
            EvaluateEquipmentChanges();
        }
        #endregion

        #region Event Management & Evaluation
        private void SubscribeToEvents()
        {
            if (_isSubscribed) return;
            InventoryEventPipeline.OnEvent += HandleInventoryPipelineEvent;
            _isSubscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (!_isSubscribed) return;
            InventoryEventPipeline.OnEvent -= HandleInventoryPipelineEvent;
            _isSubscribed = false;
        }

        private void HandleInventoryPipelineEvent(InventoryEvent evt)
        {
            if (_equipmentInventory == null || _equipmentInventory.Container == null) return;

            // 当事件与绑定的装备容器关联时，重新校验槽位变更
            if (evt.Container == _equipmentInventory.Container ||
                (_equipmentInventory.IsInitialized && string.Equals(evt.InventoryId, _equipmentInventory.InventoryId, System.StringComparison.OrdinalIgnoreCase)))
            {
                EvaluateEquipmentChanges();
            }
        }

        /// <summary>
        /// 评估并比对装备槽位中的物品进出变更，自动触发 OnEquip / OnUnequip。
        /// </summary>
        public void EvaluateEquipmentChanges()
        {
            if (_equipmentInventory == null || !_equipmentInventory.IsInitialized || _equipmentInventory.Container == null)
            {
                return;
            }

            GameObject user = _targetCharacter != null ? _targetCharacter.gameObject : gameObject;
            var slots = _equipmentInventory.Slots;
            int currentSlotCount = slots.Length;

            // 调整快照列表尺寸
            while (_lastSlotItemsSnapshot.Count < currentSlotCount)
            {
                _lastSlotItemsSnapshot.Add(null);
            }

            for (int i = 0; i < currentSlotCount; i++)
            {
                ItemSlot slot = slots[i];
                ItemInstance currentItem = slot != null && !slot.IsEmpty ? slot.Item : null;
                ItemInstance lastItem = _lastSlotItemsSnapshot[i];

                // 如果槽位物品发生了变更
                if (currentItem != lastItem)
                {
                    // 1. 旧物品被拿走/替换：触发脱下逻辑
                    if (lastItem != null)
                    {
                        lastItem.TryUnequip(user);
                        Debug.Log($"[DemoEquipmentBehavior] 自动触发卸下装备回调 -> 槽位 [{i}], 物品: '{lastItem.Definition?.name}'");
                    }

                    // 2. 新物品被放入：触发穿戴逻辑
                    if (currentItem != null)
                    {
                        currentItem.TryEquip(user);
                        Debug.Log($"[DemoEquipmentBehavior] 自动触发穿戴装备回调 -> 槽位 [{i}], 物品: '{currentItem.Definition?.name}'");
                    }

                    // 3. 更新快照
                    _lastSlotItemsSnapshot[i] = currentItem;
                }
            }
        }

        /// <summary>
        /// 清除所有已被应用装备加成（组件被禁用或重置时调用）。
        /// </summary>
        private void ClearAllEquippedBonuses()
        {
            GameObject user = _targetCharacter != null ? _targetCharacter.gameObject : gameObject;
            for (int i = 0; i < _lastSlotItemsSnapshot.Count; i++)
            {
                ItemInstance item = _lastSlotItemsSnapshot[i];
                if (item != null)
                {
                    item.TryUnequip(user);
                    _lastSlotItemsSnapshot[i] = null;
                }
            }
        }
        #endregion
    }
}
