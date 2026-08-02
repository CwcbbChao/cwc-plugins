using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 槽位多分类限制配置结构体。
    /// 列表/数组索引 (从 0 开始) 隐式 1:1 对应背包槽位索引 (SlotIndex)。
    /// </summary>
    [Serializable]
    public struct SlotRestriction
    {
        [Tooltip("该槽位允许放入的物品分类 SO 列表。若列表为空，则该槽位不限制物品类型")]
        public List<ItemCategorySO> AllowedCategories;

        public SlotRestriction(List<ItemCategorySO> allowedCategories)
        {
            AllowedCategories = allowedCategories;
        }
    }

    /// <summary>
    /// 背包/装备栏槽位限制预设配置资产 (ScriptableObject)。
    /// 用于配置不同职业（如战士/法师/弓箭手）、角色姿态或装备模板的槽位限制规则，
    /// 支持在运行时通过代码动态切换，实现一键换职或装备槽限制重置。
    /// </summary>
    [CreateAssetMenu(fileName = "SlotRestrictionPreset_", menuName = "Cwc/Inventory/Slot Restriction Preset")]
    public class SlotRestrictionPresetSO : ScriptableObject
    {
        #region Serialized Fields
        [SerializeField]
        [Tooltip("预设描述名称（例如：战士装备配置、法师装备配置）")]
        private string _presetName = "Default Preset";

        [SerializeField]
        [Tooltip("槽位限制配置列表。列表索引(0, 1, 2...)隐式 1:1 对应槽位索引")]
        private List<SlotRestriction> _slotRestrictions = new();
        #endregion

        #region Public Properties
        /// <summary>
        /// 预设描述名称。
        /// </summary>
        public string PresetName => _presetName;

        /// <summary>
        /// 槽位限制规则列表。
        /// </summary>
        public IReadOnlyList<SlotRestriction> SlotRestrictions => _slotRestrictions;
        #endregion
    }
}
