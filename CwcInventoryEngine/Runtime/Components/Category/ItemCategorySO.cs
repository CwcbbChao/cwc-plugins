using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 纯粹的分类/类型节点资产定义 (ScriptableObject)。
    /// 包含显示名称覆盖项与可选的 ParentCategory 父级引用，支持原生的多层级继承链追溯。
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemCategory", menuName = "Cwc/Inventory/Item Category")]
    public class ItemCategorySO : ScriptableObject
    {
        #region Inspector Fields
        [Header("Basic Settings")]
        [SerializeField]
        [Tooltip("UI display name override (Leave empty to fallback to SO asset file name)")]
        private string _displayName = string.Empty;

        [SerializeField]
        [Tooltip("Parent category asset (Optional). Enables hierarchy inheritance tracking")]
        private ItemCategorySO _parentCategory;
        #endregion

        #region Public Properties
        /// <summary>
        /// 分类 UI 展示名称（可由子类重写以支持多语言本地化）。
        /// </summary>
        public virtual string DisplayName => GetDisplayName();

        /// <summary>
        /// 分类 String ID（直接使用 ScriptableObject 资产名称 name，无额外覆盖字段）。
        /// </summary>
        public string Id => name;

        /// <summary>
        /// 父级分类资产。
        /// </summary>
        public ItemCategorySO ParentCategory => _parentCategory;
        #endregion

        #region Public Hierarchy Verification APIs
        /// <summary>
        /// 获取当前分类在 UI 上显示的文本名称。
        /// 默认回退链：_displayName -> name (SO 资产文件名，极大方便 Debug 调试)。
        /// 可在子类中重写此方法以对接具体的本地化/I18N 框架。
        /// </summary>
        public virtual string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(_displayName))
            {
                return _displayName;
            }

            return name;
        }

        /// <summary>
        /// 校验当前分类是否属于目标分类（通过 ParentCategory 向上父级链追溯，带防死循环防护）。
        /// </summary>
        public bool IsSubCategoryOf(ItemCategorySO targetCategory)
        {
            if (targetCategory == null) return false;
            if (this == targetCategory) return true;

            // 零递归 while 循环追溯父级继承链，带最大深度防死循环
            var current = _parentCategory;
            int depth = 0;
            while (current != null && depth < 20)
            {
                if (current == targetCategory) return true;
                current = current._parentCategory;
                depth++;
            }

            return false;
        }

        /// <summary>
        /// IsSubCategoryOf 的向后兼容别名。
        /// </summary>
        public bool IsSubTypeOf(ItemCategorySO targetType) => IsSubCategoryOf(targetType);
        #endregion
    }
}
