using System;
using UnityEngine;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// Demo 专属 durability 持久化存盘数据结构 DTO。
    /// </summary>
    [Serializable]
    public class DemoDurabilitySaveData
    {
        public int CurrentDurability;
    }

    /// <summary>
    /// Demo 物品耐久度组件静态定义。
    /// 允许在 Inspector 中配置物品的最大耐久度，并提供运行时组件工厂与自治存盘恢复逻辑。
    /// </summary>
    [Serializable]
    [ItemComponentPath("Demo 示例/耐久度 Durability")]
    public class DemoDurabilityComponentDefinition : ItemComponentDefinition
    {
        #region Serialized Fields
        [Header("耐久度基础配置")]
        [SerializeField]
        [Tooltip("物品的最大耐久度")]
        [Min(1)]
        private int _maxDurability = 100;
        #endregion

        #region Public Properties
        /// <summary>
        /// 对应的运行时组件类型。
        /// </summary>
        public override Type ComponentType => typeof(DemoDurabilityComponent);

        /// <summary>
        /// 最大耐久度。
        /// </summary>
        public int MaxDurability => _maxDurability;
        #endregion

        #region Factory Method
        /// <summary>
        /// 创建动态内存中的 DemoDurabilityComponent 实例。
        /// </summary>
        public override ItemComponentBase CreateRuntime()
        {
            return new DemoDurabilityComponent(_maxDurability);
        }
        #endregion

        #region Save / Load Autonomy
        /// <summary>
        /// 存盘自治：导出运行时组件的当前耐久度数据。
        /// </summary>
        public override bool TryExportState(ItemInstance instance, out string jsonData)
        {
            if (instance != null && instance.TryGetComponent<DemoDurabilityComponent>(out var durabilityComp))
            {
                var saveData = new DemoDurabilitySaveData
                {
                    CurrentDurability = durabilityComp.CurrentDurability
                };
                jsonData = JsonUtility.ToJson(saveData);
                return true;
            }

            jsonData = null;
            return false;
        }

        /// <summary>
        /// 读盘自治：还原当前耐久度数据到运行时组件。
        /// </summary>
        public override void ImportState(ItemInstance instance, ItemComponentBase runtimeComp, string jsonData)
        {
            if (runtimeComp is DemoDurabilityComponent durabilityComp && !string.IsNullOrEmpty(jsonData))
            {
                var saveData = JsonUtility.FromJson<DemoDurabilitySaveData>(jsonData);
                if (saveData != null)
                {
                    durabilityComp.SetCurrentDurability(saveData.CurrentDurability);
                }
            }
        }
        #endregion
    }
}
