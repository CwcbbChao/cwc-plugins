using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 预置物品项配置 DTO（用于在 Inspector 中配置初始物品）。
    /// </summary>
    [Serializable]
    public struct InitialItemConfig
    {
        [Tooltip("物品静态 ScriptableObject 定义")]
        public ItemDefinition Definition;

        [Tooltip("初始堆叠数量")]
        [Min(1)]
        public int Count;
    }

    /// <summary>
    /// 背包初始物品填充组合组件。
    /// 配合 Inventory 使用，在游戏开始或需要时向目标库存中自动填充指定的初始物品列表。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Cwc/Inventory/Inventory Initial Items")]
    public class InventoryInitialItems : MonoBehaviour
    {
        #region Serialized Fields
        [Header("目标库存")]
        [SerializeField]
        [Tooltip("目标库存组件。若为空，将在 Awake 时自动获取同 GameObject 上的 Inventory")]
        private Inventory _inventoryComponent;

        [Header("初始物品配置")]
        [SerializeField]
        [Tooltip("游戏开始或填充时自动放入背包的初始物品配置列表")]
        private List<InitialItemConfig> _initialItems = new();

        [Header("自动策略")]
        [SerializeField]
        [Tooltip("是否在 Start() 时自动执行物品填充逻辑")]
        private bool _autoPopulateOnStart = true;
        #endregion

        #region Public Properties
        /// <summary>
        /// 目标库存组件。
        /// </summary>
        public Inventory TargetInventory => _inventoryComponent;

        /// <summary>
        /// 初始物品配置列表。
        /// </summary>
        public IReadOnlyList<InitialItemConfig> InitialItems => _initialItems;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_inventoryComponent == null)
            {
                _inventoryComponent = GetComponent<Inventory>();
            }
        }

        private void Start()
        {
            if (_autoPopulateOnStart)
            {
                PopulateInitialItems();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 向目标库存中填充配置的初始物品列表。
        /// </summary>
        public virtual void PopulateInitialItems()
        {
            if (_inventoryComponent == null)
            {
                _inventoryComponent = GetComponent<Inventory>();
            }

            if (_inventoryComponent == null)
            {
                Debug.LogWarning($"[InventoryInitialItems] 未找到有效的 Inventory，无法填充初始物品。对象名称: {gameObject.name}");
                return;
            }

            if (!_inventoryComponent.IsInitialized)
            {
                _inventoryComponent.InitializeContainer();
            }

            InventoryContainer container = _inventoryComponent.Container;
            if (container == null || _initialItems == null || _initialItems.Count == 0) return;

            using (container.BatchScope())
            {
                int count = _initialItems.Count;
                for (int i = 0; i < count; i++)
                {
                    InitialItemConfig config = _initialItems[i];
                    if (config.Definition != null && config.Count > 0)
                    {
                        ItemInstance instance = config.Definition.CreateInstance(config.Count);
                        container.TryAddItem(instance, out _);
                    }
                }
            }
        }
        #endregion
    }
}
