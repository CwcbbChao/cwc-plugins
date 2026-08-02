using UnityEditor;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer.Editor
{
    /// <summary>
    /// StateChangeHistory 属性绘制器，在 Inspector 中提供一体化折叠面板与日志可视化展示。
    /// </summary>
    [CustomPropertyDrawer(typeof(StateChangeHistory))]
    public class StateChangeHistoryDrawer : PropertyDrawer
    {
        #region 重写 PropertyDrawer 方法

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
            {
                return height;
            }

            // 包含 EnableLog + MaxCapacity 工具栏
            height += EditorGUIUtility.singleLineHeight + 2f;
            // 清空按钮工具栏
            height += EditorGUIUtility.singleLineHeight + 2f;

            SerializedProperty entriesProp = property.FindPropertyRelative("_entries");
            if (entriesProp != null && entriesProp.isArray)
            {
                int count = entriesProp.arraySize;
                if (count == 0)
                {
                    height += EditorGUIUtility.singleLineHeight + 2f;
                }
                else
                {
                    // 每条日志占据 1 行（或可按条目计算）
                    height += (EditorGUIUtility.singleLineHeight + 2f) * Mathf.Min(count, 10) + 4f;
                }
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty enableLogProp = property.FindPropertyRelative("_enableLog");
            SerializedProperty maxCapacityProp = property.FindPropertyRelative("_maxCapacity");
            SerializedProperty entriesProp = property.FindPropertyRelative("_entries");

            int logCount = (entriesProp != null && entriesProp.isArray) ? entriesProp.arraySize : 0;
            string titleLabel = $"{label.text} ({logCount} 条)";

            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, titleLabel, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float currentY = position.y + EditorGUIUtility.singleLineHeight + 2f;

                // 1. 工具栏第一行：Enable 勾选框 + 容量设置
                float halfWidth = (position.width - 10f) * 0.5f;
                if (enableLogProp != null)
                {
                    Rect enableRect = new Rect(position.x, currentY, halfWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(enableRect, enableLogProp, new GUIContent("启用日志", "是否开启运行时日志收集"));
                }

                if (maxCapacityProp != null)
                {
                    Rect capRect = new Rect(position.x + halfWidth + 10f, currentY, halfWidth, EditorGUIUtility.singleLineHeight);
                    int capVal = EditorGUI.IntField(capRect, new GUIContent("最大容量", "日志队列最大保留容量"), maxCapacityProp.intValue);
                    if (capVal != maxCapacityProp.intValue && capVal > 0)
                    {
                        maxCapacityProp.intValue = capVal;
                    }
                }

                currentY += EditorGUIUtility.singleLineHeight + 2f;

                // 2. 工具栏第二行：清空日志按钮
                Rect btnRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(btnRect, "清空日志记录"))
                {
                    if (entriesProp != null && entriesProp.isArray)
                    {
                        entriesProp.ClearArray();
                    }
                }

                currentY += EditorGUIUtility.singleLineHeight + 2f;

                // 3. 绘制日志列表面板
                if (entriesProp != null && entriesProp.isArray)
                {
                    if (entriesProp.arraySize == 0)
                    {
                        Rect emptyRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                        EditorGUI.LabelField(emptyRect, "暂无历史记录...", EditorStyles.centeredGreyMiniLabel);
                    }
                    else
                    {
                        // 倒序显示最新的记录
                        int displayCount = Mathf.Min(entriesProp.arraySize, 10);
                        for (int i = entriesProp.arraySize - 1; i >= entriesProp.arraySize - displayCount; i--)
                        {
                            SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(i);
                            if (entryProp == null) continue;

                            SerializedProperty timeProp = entryProp.FindPropertyRelative("_timestamp");
                            SerializedProperty oldProp = entryProp.FindPropertyRelative("_oldFullPath");
                            SerializedProperty newProp = entryProp.FindPropertyRelative("_newFullPath");
                            SerializedProperty reasonProp = entryProp.FindPropertyRelative("_reason");
                            SerializedProperty ruleProp = entryProp.FindPropertyRelative("_ruleDescription");

                            string timeStr = timeProp != null ? timeProp.stringValue : "";
                            string oldStr = oldProp != null ? oldProp.stringValue : "";
                            string newStr = newProp != null ? newProp.stringValue : "";
                            string ruleStr = (ruleProp != null && !string.IsNullOrEmpty(ruleProp.stringValue)) ? $" | 规则: {ruleProp.stringValue}" : "";
                            string reasonStr = reasonProp != null ? ((StateChangeReason)reasonProp.enumValueIndex).ToString() : "";

                            string displayLine = $"[{timeStr}] {oldStr} -> {newStr} ({reasonStr}{ruleStr})";

                            Rect entryRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                            EditorGUI.SelectableLabel(entryRect, displayLine, EditorStyles.miniTextField);

                            currentY += EditorGUIUtility.singleLineHeight + 2f;
                        }
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        #endregion
    }
}
