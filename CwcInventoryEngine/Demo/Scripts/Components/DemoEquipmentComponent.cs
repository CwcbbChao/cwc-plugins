using System;
using System.Collections.Generic;
using UnityEngine;
using Cwc.InventoryEngine.Query;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// 运行时装备组件，挂载在 ItemInstance 上。
    /// 包含引用的 ItemCategorySO 分类与攻防数值加成。实现 IItemCategorized、IEquippable 与 IItemPropertyProvider 接口。
    /// </summary>
    /// <summary>
    /// 运行时装备组件，挂载在 ItemInstance 上。
    /// 继承泛型基类 ItemComponentBase<DemoEquipmentComponentDefinition>，直接持有一份配对的静态定义引用。
    /// 实现 IItemCategorized、IEquippable 与 IItemPropertyProvider 接口。
    /// </summary>
    public class DemoEquipmentComponent : ItemComponentBase<DemoEquipmentComponentDefinition>, IItemCategorized, IEquippable, IItemPropertyProvider
    {
        #region Dynamic Overrides (e.g. Equipment Enhancement)
        private int? _customAttackBonus;
        private int? _customDefenseBonus;
        #endregion

        #region Public Properties (Default to Definition)
        /// <summary>
        /// 装备部位/分类 SO。
        /// </summary>
        public ItemCategorySO EquipmentCategory => Definition != null ? Definition.EquipmentCategory : null;

        /// <summary>
        /// 需求等级。
        /// </summary>
        public int RequiredLevel => Definition != null ? Definition.RequiredLevel : 1;

        /// <summary>
        /// 攻击加成（若有强化等动态覆盖值使用覆盖值，否则使用静态定义中的配置加成）。
        /// </summary>
        public int AttackBonus => _customAttackBonus ?? (Definition != null ? Definition.AttackBonus : 0);

        /// <summary>
        /// 防御加成（若有强化等动态覆盖值使用覆盖值，否则使用静态定义中的配置加成）。
        /// </summary>
        public int DefenseBonus => _customDefenseBonus ?? (Definition != null ? Definition.DefenseBonus : 0);
        #endregion

        #region Constructors
        /// <summary>
        /// 标准构造函数，由 DemoEquipmentComponentDefinition 传递自身引用初始化。
        /// </summary>
        public DemoEquipmentComponent(DemoEquipmentComponentDefinition definition) : base(definition)
        {
        }
        #endregion

        #region Dynamic Mutation Methods
        /// <summary>
        /// 动态设置强化后的额外攻击加成。
        /// </summary>
        public void SetCustomAttackBonus(int attackBonus)
        {
            _customAttackBonus = attackBonus;
        }

        /// <summary>
        /// 动态设置强化后的额外防御加成。
        /// </summary>
        public void SetCustomDefenseBonus(int defenseBonus)
        {
            _customDefenseBonus = defenseBonus;
        }
        #endregion

        #region IItemCategorized Implementation
        /// <summary>
        /// 实现 IItemCategorized 接口，向物品贡献分类 SO。
        /// </summary>
        public void GetCategories(List<ItemCategorySO> results)
        {
            if (results != null && EquipmentCategory != null)
            {
                results.Add(EquipmentCategory);
            }
        }
        #endregion

        #region IEquippable Implementation
        public bool CanEquip(GameObject user)
        {
            return user != null && user.GetComponent<DemoCharacter>() != null;
        }

        public void OnEquip(GameObject user)
        {
            if (user != null && user.TryGetComponent<DemoCharacter>(out var character))
            {
                int atk = AttackBonus;
                int def = DefenseBonus;
                if (atk != 0) character.ModifyAttack(atk);
                if (def != 0) character.ModifyDefense(def);
                Debug.Log($"[DemoEquipmentComponent] 穿戴装备！读取组件配置攻击加成 +{atk}, 防御加成 +{def}");
            }
        }

        public void OnUnequip(GameObject user)
        {
            if (user != null && user.TryGetComponent<DemoCharacter>(out var character))
            {
                int atk = AttackBonus;
                int def = DefenseBonus;
                if (atk != 0) character.ModifyAttack(-atk);
                if (def != 0) character.ModifyDefense(-def);
                Debug.Log($"[DemoEquipmentComponent] 脱下装备！扣除装备配置攻击加成 {atk}, 扣除防御加成 {def}");
            }
        }
        #endregion

        #region IItemPropertyProvider Implementation
        /// <summary>
        /// 统一向查询引擎暴露 Key-Value 属性 (零 GC)。
        /// </summary>
        public bool TryGetProperty(string key, out ItemPropertyValue value)
        {
            value = ItemPropertyValue.Empty;

            if (string.Equals(key, "SlotType", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "EquipmentSlot", StringComparison.OrdinalIgnoreCase))
            {
                var cat = EquipmentCategory;
                if (cat != null)
                {
                    value = cat.DisplayName;
                    return true;
                }
            }

            if (string.Equals(key, "EquipmentCategory", StringComparison.OrdinalIgnoreCase))
            {
                var cat = EquipmentCategory;
                if (cat != null)
                {
                    value = new ItemPropertyValue(cat);
                    return true;
                }
            }

            if (string.Equals(key, "RequiredLevel", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "Level", StringComparison.OrdinalIgnoreCase))
            {
                value = RequiredLevel;
                return true;
            }

            if (string.Equals(key, "AttackBonus", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "Attack", StringComparison.OrdinalIgnoreCase))
            {
                value = AttackBonus;
                return true;
            }

            if (string.Equals(key, "DefenseBonus", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "Defense", StringComparison.OrdinalIgnoreCase))
            {
                value = DefenseBonus;
                return true;
            }

            return false;
        }
        #endregion
    }
}

