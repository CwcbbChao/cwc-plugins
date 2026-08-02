using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 静态物品资产定义 (ScriptableObject)。
    /// 极简根数据，仅包含通用堆叠上限与不可变的组件定义列表。
    /// 严禁在此处硬编码任何业务扩展属性（如攻击力、防御力、图标等）。
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemDefinition", menuName = "Cwc/Inventory/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        #region Serialized Fields
        [Tooltip("最大堆叠上限。<=1 表示不可堆叠物品。")]
        [SerializeField] private int maxStack = 99;

        [SerializeReference]
        [Tooltip("多态组件定义列表。借助 [SerializeReference] 保存在当前物品资产内部。")]
        public List<ItemComponentDefinition> ComponentDefinitions = new();
        #endregion

        #region Public Properties
        /// <summary>
        /// 堆叠上限。<=1 表示不可堆叠。
        /// </summary>
        public int MaxStack => Mathf.Max(1, maxStack);

        /// <summary>
        /// 是否允许堆叠。
        /// </summary>
        public bool IsStackable => MaxStack > 1;
        #endregion

        #region Public Methods
        /// <summary>
        /// 创建一个新的运行时物品实例 (ID 自动生成)。
        /// </summary>
        /// <param name="count">初始堆叠数量</param>
        /// <returns>运行时物品实例</returns>
        public ItemInstance CreateInstance(int count = 1)
        {
            return CreateInstanceWithId(ItemId.NewId(), count);
        }

        /// <summary>
        /// 使用指定的 ItemId 创建运行时物品实例（常用于读盘还原）。
        /// </summary>
        /// <param name="instanceId">特定的物品实例标识</param>
        /// <param name="count">初始堆叠数量</param>
        /// <returns>运行时物品实例</returns>
        public ItemInstance CreateInstanceWithId(ItemId instanceId, int count = 1)
        {
            int clampedCount = Mathf.Clamp(count, 1, MaxStack);
            ItemInstance instance = new ItemInstance(instanceId, this, clampedCount);
            return instance;
        }

        /// <summary>
        /// 使用指定的 Guid 创建运行时物品实例（向后兼容重载）。
        /// </summary>
        public ItemInstance CreateInstanceWithGuid(Guid instanceId, int count = 1)
        {
            return CreateInstanceWithId(new ItemId(instanceId), count);
        }

        /// <summary>
        /// 零 GC 获取指定的静态组件定义。
        /// ⚠️ 仅供存盘自治与工厂匹配等底层策略内部使用！外部业务逻辑与 UI 渲染严禁越过 ItemInstance 直接查询静态定义组件。
        /// </summary>
        /// <typeparam name="T">组件定义类型</typeparam>
        /// <param name="compDef">组件定义输出</param>
        /// <returns>若存在返回 true</returns>
        public bool TryGetComponentDefinition<T>(out T compDef) where T : ItemComponentDefinition
        {
            int count = ComponentDefinitions.Count;
            for (int i = 0; i < count; i++)
            {
                if (ComponentDefinitions[i] is T target)
                {
                    compDef = target;
                    return true;
                }
            }
            compDef = null;
            return false;
        }

        /// <summary>
        /// 根据运行时组件 Type 获取对应的静态组件定义。
        /// </summary>
        /// <param name="runtimeComponentType">运行时组件 Type</param>
        /// <param name="compDef">组件定义输出</param>
        /// <returns>若存在返回 true</returns>
        public bool TryGetComponentDefinition(Type runtimeComponentType, out ItemComponentDefinition compDef)
        {
            if (runtimeComponentType != null && ComponentDefinitions != null)
            {
                int count = ComponentDefinitions.Count;
                for (int i = 0; i < count; i++)
                {
                    var candidateDef = ComponentDefinitions[i];
                    if (candidateDef != null && candidateDef.ComponentType == runtimeComponentType)
                    {
                        compDef = candidateDef;
                        return true;
                    }
                }
            }
            compDef = null;
            return false;
        }
        #endregion
    }
}
