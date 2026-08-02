using System;
using UnityEngine;
using Cwc.InventoryEngine.Query;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// 静态装备组件定义，保存在 ItemDefinition 的 ComponentDefinitions 中。
    /// 统一使用通用 ItemCategorySO 资产配置部位分类。实现 IItemPropertyProvider 接口。
    /// </summary>
    [Serializable]
    [ItemComponentPath("Demo 示例/装备组件 Equipment")]
    public class DemoEquipmentComponentDefinition : ItemComponentDefinition
    {
        #region Serialized Fields
        [Header("装备属性")]
        [SerializeField]
        [Tooltip("装备部位/分类资产 (SO)")]
        private ItemCategorySO _equipmentCategory;

        [SerializeField]
        [Tooltip("需求等级")]
        private int _requiredLevel = 1;

        [SerializeField]
        [Tooltip("攻击力加成")]
        private int _attackBonus = 10;

        [SerializeField]
        [Tooltip("防御力加成")]
        private int _defenseBonus = 0;
        #endregion

        #region Public Properties
        /// <summary>
        /// 对应的运行时组件类型。
        /// </summary>
        public override Type ComponentType => typeof(DemoEquipmentComponent);

        /// <summary>
        /// 装备部位/分类 SO。
        /// </summary>
        public ItemCategorySO EquipmentCategory => _equipmentCategory;

        /// <summary>
        /// 需求等级。
        /// </summary>
        public int RequiredLevel => _requiredLevel;

        /// <summary>
        /// 攻击力加成。
        /// </summary>
        public int AttackBonus => _attackBonus;

        /// <summary>
        /// 防御力加成。
        /// </summary>
        public int DefenseBonus => _defenseBonus;
        #endregion

        #region Factory Method
        /// <summary>
        /// 创建与之对应的动态运行时组件。
        /// </summary>
        public override ItemComponentBase CreateRuntime()
        {
            return new DemoEquipmentComponent(this);
        }
        #endregion
    }
}

