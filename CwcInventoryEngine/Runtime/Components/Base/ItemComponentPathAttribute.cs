using System;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 标记物品组件在 Inspector 添加菜单中的分类路径与显示名称。
    /// 示例: [ItemComponentPath("UI 显示/基础 Display")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ItemComponentPathAttribute : Attribute
    {
        #region Public Properties
        /// <summary>
        /// 菜单分类路径（如 "基础/UI Display"）。
        /// </summary>
        public string Path { get; }
        #endregion

        #region Constructors
        /// <summary>
        /// 构造函数，指定组件的菜单分类路径。
        /// </summary>
        /// <param name="path">菜单分类路径</param>
        public ItemComponentPathAttribute(string path)
        {
            Path = path;
        }
        #endregion
    }
}
