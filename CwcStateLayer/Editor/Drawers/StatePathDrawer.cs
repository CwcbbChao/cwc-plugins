using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer.Editor
{
    /// <summary>
    /// 状态路径属性绘制器，为标记 [StatePath] 的字符串字段提供支持实时关键字搜索与层级树状选择的 AdvancedDropdown 搜索下拉框。
    /// </summary>
    [CustomPropertyDrawer(typeof(StatePathAttribute))]
    public class StatePathDrawer : PropertyDrawer
    {
        #region 私有字段

        private static AdvancedDropdownState _dropdownState;

        #endregion

        #region 重写 PropertyDrawer 方法

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            StatePathAttribute pathAttr = (StatePathAttribute)attribute;
            StateLayerConfig configAsset = FindStateLayerConfig(property, pathAttr?.ConfigFieldName);

            if (configAsset == null)
            {
                // 如果没有关联的 StateLayerConfig，回退为普通输入框附带提示
                Rect fieldRect = new Rect(position.x, position.y, position.width - 24f, position.height);
                Rect warnRect = new Rect(position.x + position.width - 20f, position.y, 20f, position.height);

                EditorGUI.PropertyField(fieldRect, property, label);
                GUI.Label(warnRect, new GUIContent("?", "请先在组件或资产中配置关联的 StateLayerConfig 以启用高级搜索路径选择。"));
                return;
            }

            // 绘制标签与按钮 rect
            Rect controlRect = EditorGUI.PrefixLabel(position, label);

            string currentValue = property.stringValue;
            string displayTitle = string.IsNullOrEmpty(currentValue) ? "Any" : currentValue;

            GUIContent buttonContent = new GUIContent(displayTitle, "点击打开带搜索功能的路径选择菜单");

            if (EditorGUI.DropdownButton(controlRect, buttonContent, FocusType.Keyboard))
            {
                List<string> fullPaths = configAsset.CollectAllFullPaths();
                _dropdownState ??= new AdvancedDropdownState();

                StatePathAdvancedDropdown dropdown = new StatePathAdvancedDropdown(
                    _dropdownState,
                    fullPaths,
                    selectedPath =>
                    {
                        property.serializedObject.Update();
                        property.stringValue = selectedPath;
                        property.serializedObject.ApplyModifiedProperties();
                    });

                dropdown.Show(controlRect);
            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 递归寻找当前属性宿主对象或所属 StateObserver 作用域内的 StateLayerConfig 引用
        /// </summary>
        private StateLayerConfig FindStateLayerConfig(SerializedProperty property, string configFieldName)
        {
            SerializedObject serializedObject = property.serializedObject;

            // 1. 如果指定了特定的字段名，优先寻找
            if (!string.IsNullOrEmpty(configFieldName))
            {
                SerializedProperty prop = serializedObject.FindProperty(configFieldName);
                if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (prop.objectReferenceValue is StateLayerConfig namedConfig)
                    {
                        return namedConfig;
                    }
                }
            }

            // 2. 基于 propertyPath 向上层节点递归查找作用域内的 _layerConfig 属性
            string path = property.propertyPath;
            int lastDotIndex = path.LastIndexOf('.');
            while (lastDotIndex > 0)
            {
                path = path.Substring(0, lastDotIndex);
                string configPath = path + "._layerConfig";
                SerializedProperty parentConfigProp = serializedObject.FindProperty(configPath);
                if (parentConfigProp != null && parentConfigProp.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (parentConfigProp.objectReferenceValue is StateLayerConfig localConfig)
                    {
                        return localConfig;
                    }
                }
                lastDotIndex = path.LastIndexOf('.');
            }

            // 3. 尝试寻找宿主根层级的 _layerConfig 字段
            SerializedProperty defaultProp = serializedObject.FindProperty("_layerConfig");
            if (defaultProp != null && defaultProp.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (defaultProp.objectReferenceValue is StateLayerConfig rootConfig)
                {
                    return rootConfig;
                }
            }

            // 4. 遍历宿主对象中的属性寻找第一个 StateLayerConfig 引用
            SerializedProperty iterator = serializedObject.GetIterator();
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                        iterator.objectReferenceValue is StateLayerConfig config)
                    {
                        return config;
                    }
                } while (iterator.NextVisible(false));
            }

            return null;
        }

        #endregion
    }
}
