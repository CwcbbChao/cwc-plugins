using System;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 静态物品 UI 显示组件定义类。
    /// 配置在 ItemDefinition 的 ComponentDefinitions 列表中。
    /// </summary>
    [Serializable]
    [ItemComponentPath("UI 显示/基础 Display")]
    public class ItemDisplayComponentDefinition : ItemComponentDefinition
    {
        #region Serialized Fields
        [SerializeField]
        [Tooltip("物品显示名称（留空则默认使用 Definition 资产名称）")]
        private string _displayName;

        [SerializeField]
        [Tooltip("物品 UI 图标")]
        private Sprite _icon;

        [SerializeField]
        [TextArea(3, 6)]
        [Tooltip("物品详细描述信息")]
        private string _description;
        #endregion

        #region Public Properties
        public override Type ComponentType => typeof(ItemDisplayComponent);

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
        #endregion

        #region Factory Method
        public override ItemComponentBase CreateRuntime()
        {
            return new ItemDisplayComponent(this);
        }
        #endregion
    }

    /// <summary>
    /// 运行时物品基础 UI 显示组件。
    /// 继承泛型基类 ItemComponentBase<ItemDisplayComponentDefinition>，直接持有一份配对的静态定义引用。
    /// 包含名称 (DisplayName)、图标 (Icon)、详情描述 (Description) 三项核心显示信息。
    /// </summary>
    public class ItemDisplayComponent : ItemComponentBase<ItemDisplayComponentDefinition>, IItemDisplay
    {
        #region Dynamic Overrides
        private string _customDisplayName;
        private Sprite _customIcon;
        private string _customDescription;
        #endregion

        #region Public Properties (Default to Definition, with Override Support)
        /// <summary>
        /// 物品显示名称（若存在动态覆盖则使用覆盖值，否则指向静态定义配置）。
        /// </summary>
        public string DisplayName => _customDisplayName ?? Definition.DisplayName;

        /// <summary>
        /// 物品显示图标（若存在动态覆盖则使用覆盖值，否则指向静态定义配置）。
        /// </summary>
        public Sprite Icon => _customIcon != null ? _customIcon : Definition.Icon;

        /// <summary>
        /// 物品详细描述信息（若存在动态覆盖则使用覆盖值，否则指向静态定义配置）。
        /// </summary>
        public string Description => _customDescription ?? Definition.Description;
        #endregion

        #region Constructors
        /// <summary>
        /// 构造函数，由 ItemDisplayComponentDefinition 传递自身引用初始化。
        /// </summary>
        public ItemDisplayComponent(ItemDisplayComponentDefinition definition) : base(definition)
        {
        }
        #endregion

        #region Dynamic Mutation Methods
        /// <summary>
        /// 动态重命名物品显示名称。
        /// </summary>
        public void SetCustomDisplayName(string customDisplayName)
        {
            _customDisplayName = customDisplayName;
        }

        /// <summary>
        /// 动态替换物品显示图标。
        /// </summary>
        public void SetCustomIcon(Sprite customIcon)
        {
            _customIcon = customIcon;
        }

        /// <summary>
        /// 动态替换物品描述。
        /// </summary>
        public void SetCustomDescription(string customDescription)
        {
            _customDescription = customDescription;
        }
        #endregion
    }
}
