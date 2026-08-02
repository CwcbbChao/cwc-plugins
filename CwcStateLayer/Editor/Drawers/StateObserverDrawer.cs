using UnityEditor;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer.Editor
{
    /// <summary>
    /// 纯 C# StateObserver 与 StateObserver<TData> 属性绘制器，为编辑器界面提供一体化内聚显示体验。
    /// </summary>
    [CustomPropertyDrawer(typeof(StateObserver), true)]
    [CustomPropertyDrawer(typeof(StateObserver<>), true)]
    public class StateObserverDrawer : PropertyDrawer
    {
        #region 重写 PropertyDrawer 方法

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
            {
                return height;
            }

            SerializedProperty configProp = property.FindPropertyRelative("_layerConfig");
            SerializedProperty layerIdProp = property.FindPropertyRelative("_layerId");
            SerializedProperty rulesProp = property.FindPropertyRelative("_rules");
            SerializedProperty currentPathProp = property.FindPropertyRelative("_currentFullPath");
            SerializedProperty prevPathProp = property.FindPropertyRelative("_previousFullPath");
            SerializedProperty historyLogProp = property.FindPropertyRelative("_historyLog");

            height += EditorGUIUtility.singleLineHeight + 2f;

            if (layerIdProp != null)
            {
                height += EditorGUIUtility.singleLineHeight + 2f;
            }

            if (configProp != null && configProp.objectReferenceValue == null)
            {
                height += EditorGUIUtility.singleLineHeight * 2f + 4f;
            }

            if (rulesProp != null)
            {
                height += EditorGUI.GetPropertyHeight(rulesProp, true) + 2f;
            }

            // 当前状态与上一个状态只读展示面板
            if (currentPathProp != null && prevPathProp != null)
            {
                height += EditorGUIUtility.singleLineHeight * 2f + 4f;
            }

            // 历史日志面板
            if (historyLogProp != null)
            {
                height += EditorGUI.GetPropertyHeight(historyLogProp, true) + 2f;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                float currentY = position.y + EditorGUIUtility.singleLineHeight + 2f;

                SerializedProperty configProp = property.FindPropertyRelative("_layerConfig");
                SerializedProperty layerIdProp = property.FindPropertyRelative("_layerId");
                SerializedProperty rulesProp = property.FindPropertyRelative("_rules");
                SerializedProperty currentPathProp = property.FindPropertyRelative("_currentFullPath");
                SerializedProperty prevPathProp = property.FindPropertyRelative("_previousFullPath");
                SerializedProperty historyLogProp = property.FindPropertyRelative("_historyLog");

                // 1. 绘制配置资产字段与可选 Layer ID 字段
                if (configProp != null)
                {
                    Rect configRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(configRect, configProp, new GUIContent("Layer Config", "引用的状态层配置资产"));
                    currentY += EditorGUIUtility.singleLineHeight + 2f;

                    if (layerIdProp != null)
                    {
                        Rect layerIdRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                        EditorGUI.PropertyField(layerIdRect, layerIdProp, new GUIContent("Layer ID (Optional)", "区分相同 Config 的多实例识别 ID（可选）"));
                        currentY += EditorGUIUtility.singleLineHeight + 2f;
                    }

                    if (configProp.objectReferenceValue == null)
                    {
                        Rect warnRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight * 2f);
                        EditorGUI.HelpBox(warnRect, "提示：未绑定 StateLayerConfig 资产，Inspector 中的路径选择将退化为文本模式。绑定配置后可使用 3 层路径下拉选框。", MessageType.Warning);
                        currentY += EditorGUIUtility.singleLineHeight * 2f + 4f;
                    }
                }

                // 2. 绘制规则列表字段
                if (rulesProp != null)
                {
                    float rulesHeight = EditorGUI.GetPropertyHeight(rulesProp, true);
                    Rect rulesRect = new Rect(position.x, currentY, position.width, rulesHeight);
                    EditorGUI.PropertyField(rulesRect, rulesProp, new GUIContent("Rules", "强类型状态绑定规则列表"), true);
                    currentY += rulesHeight + 2f;
                }

                // 3. 绘制只读可视化调试字段
                if (currentPathProp != null && prevPathProp != null)
                {
                    bool wasEnabled = GUI.enabled;
                    GUI.enabled = false;

                    string curVal = string.IsNullOrEmpty(currentPathProp.stringValue) ? "(None)" : currentPathProp.stringValue;
                    string prevVal = string.IsNullOrEmpty(prevPathProp.stringValue) ? "(None)" : prevPathProp.stringValue;

                    Rect curRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.TextField(curRect, "Current State (Debug)", curVal);
                    currentY += EditorGUIUtility.singleLineHeight + 2f;

                    Rect prevRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.TextField(prevRect, "Previous State (Debug)", prevVal);
                    currentY += EditorGUIUtility.singleLineHeight + 2f;

                    GUI.enabled = wasEnabled;
                }

                // 4. 绘制历史日志面板
                if (historyLogProp != null)
                {
                    float historyHeight = EditorGUI.GetPropertyHeight(historyLogProp, true);
                    Rect historyRect = new Rect(position.x, currentY, position.width, historyHeight);
                    EditorGUI.PropertyField(historyRect, historyLogProp, new GUIContent("History Log", "评估与匹配历史日志记录"), true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        #endregion
    }
}
