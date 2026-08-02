using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 状态路径特性标记，修饰字符串字段后，Inspector 将配合 PropertyDrawer 渲染下拉菜单。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class StatePathAttribute : PropertyAttribute
    {
        /// <summary>
        /// 关联引用的 StateLayerConfig 字段名称（可选，留空则由 Drawer 寻找同一组件上的 _layerConfig）
        /// </summary>
        public string ConfigFieldName { get; }

        /// <summary>
        /// 构造状态路径特性标记
        /// </summary>
        /// <param name="configFieldName">引用的配置字段名</param>
        public StatePathAttribute(string configFieldName = null)
        {
            ConfigFieldName = configFieldName;
        }
    }
}
