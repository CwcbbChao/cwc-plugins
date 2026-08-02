using System;
using UnityEngine;

namespace CwcSOManager
{
    /// <summary>
    /// 标记 ScriptableObject 类型，以支持在 CwcSOManager 中进行无限层级的分类管理及指定存储路径。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CwcSOManageableAttribute : Attribute
    {
        #region 序列化属性与字段
        
        // 允许外部设置的属性字段
        public string Path { get; set; }
        public string AssetsFolder { get; set; }

        #endregion

        #region 公共方法

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="path">分类折叠路径，例如 "系统/道具/武器/近战"</param>
        /// <param name="assetsFolder">指定实例创建的默认存放路径（相对路径，如 Assets/Data/...）。如果为空，则默认保存在 Assets/Data/{Path}/{TypeName} 下</param>
        public CwcSOManageableAttribute(string path = "未分类", string assetsFolder = "")
        {
            Path = path;
            AssetsFolder = assetsFolder;
        }

        #endregion
    }

    /// <summary>
    /// 标记 ScriptableObject 字段，使其在 CwcSOManager 数据表中作为一列显示并支持编辑。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class CwcSOColumnAttribute : Attribute
    {
        #region 序列化属性与字段

        public string DisplayName { get; set; }
        public float Width { get; set; }

        #endregion

        #region 公共方法

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="displayName">列的显示名称。若为 null，则默认使用字段名称</param>
        /// <param name="width">列的宽度，默认值为 100f</param>
        public CwcSOColumnAttribute(string displayName = null, float width = 100f)
        {
            DisplayName = displayName;
            Width = width;
        }

        #endregion
    }

    /// <summary>
    /// 标记 Sprite 或 Texture2D 字段，使其在 CwcSOManager 数据表中以缩略图形式展示。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class CwcSOPreviewAttribute : Attribute
    {
        #region 序列化属性与字段

        public float Size { get; set; }

        #endregion

        #region 公共方法

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="size">缩略图预览大小（宽高度），默认值为 40f</param>
        public CwcSOPreviewAttribute(float size = 40f)
        {
            Size = size;
        }

        #endregion
    }
}
