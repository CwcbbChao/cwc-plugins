using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 通用物品分类静态组件定义。
    /// 可挂载在任意 ItemDefinition 上，赋予物品一个或多个分类/标签。
    /// </summary>
    [Serializable]
    [ItemComponentPath("常规/通用物品分类 Item Category")]
    public class ItemCategoryComponentDefinition : ItemComponentDefinition
    {
        #region Serialized Fields
        [Header("分类与标签配置")]
        [SerializeField]
        [Tooltip("物品分类/类型资产列表 (SO)，可支持一个或多个分类标签")]
        private List<ItemCategorySO> _categories = new();
        #endregion

        #region Public Properties
        /// <summary>
        /// 对应的运行时组件类型。
        /// </summary>
        public override Type ComponentType => typeof(ItemCategoryComponent);

        /// <summary>
        /// 物品分类 SO 资产列表。
        /// </summary>
        public IReadOnlyList<ItemCategorySO> Categories => _categories;
        #endregion

        #region Factory Method
        /// <summary>
        /// 创建与之对应的动态运行时组件。
        /// </summary>
        public override ItemComponentBase CreateRuntime()
        {
            return new ItemCategoryComponent(this);
        }
        #endregion
    }
}
