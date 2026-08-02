using System;
using UnityEngine;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// Demo 药水组件定义。
    /// 配置药水物品的使用效果（如治疗数值）。
    /// </summary>
    [Serializable]
    [ItemComponentPath("Demo 示例/药水 Potion")]
    public class DemoPotionComponentDefinition : ItemComponentDefinition
    {
        #region Serialized Fields
        [SerializeField]
        [Tooltip("使用后恢复的生命值数量")]
        private int _healAmount = 5;
        #endregion

        #region Public Properties
        /// <summary>
        /// 对应的运行时组件类型。
        /// </summary>
        public override System.Type ComponentType => typeof(DemoPotionComponent);

        /// <summary>
        /// 恢复生命值数量。
        /// </summary>
        public int HealAmount => _healAmount;
        #endregion

        #region Factory Method
        /// <summary>
        /// 创建对应的动态运行时组件。
        /// </summary>
        public override ItemComponentBase CreateRuntime()
        {
            return new DemoPotionComponent(this);
        }
        #endregion
    }
}
