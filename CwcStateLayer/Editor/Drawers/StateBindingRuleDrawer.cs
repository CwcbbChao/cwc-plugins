using UnityEditor;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer.Editor
{
    /// <summary>
    /// 单个状态过渡条件项 StateTransitionCondition 紧凑排版绘制器，将 FromPath 与 ToPath 强制在同一行绘制，中间以箭头连接。
    /// </summary>
    [CustomPropertyDrawer(typeof(StateTransitionCondition))]
    public class StateTransitionConditionDrawer : PropertyDrawer
    {
        #region 重写 PropertyDrawer 方法

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight + 2f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty fromProp = property.FindPropertyRelative("_fromPath");
            SerializedProperty toProp = property.FindPropertyRelative("_toPath");

            // 计算同一行的分割宽度
            float arrowWidth = 24f;
            float availableWidth = position.width - arrowWidth;
            float halfWidth = availableWidth * 0.5f;

            Rect fromRect = new Rect(position.x, position.y, halfWidth, EditorGUIUtility.singleLineHeight);
            Rect arrowRect = new Rect(position.x + halfWidth, position.y, arrowWidth, EditorGUIUtility.singleLineHeight);
            Rect toRect = new Rect(position.x + halfWidth + arrowWidth, position.y, halfWidth, EditorGUIUtility.singleLineHeight);

            // 绘制来源路径 FromPath
            EditorGUI.PropertyField(fromRect, fromProp, GUIContent.none);

            // 绘制中间连接箭头
            GUIStyle arrowStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(arrowRect, "->", arrowStyle);

            // 绘制目标路径 ToPath
            EditorGUI.PropertyField(toRect, toProp, GUIContent.none);

            EditorGUI.EndProperty();
        }

        #endregion
    }

    /// <summary>
    /// 普通状态绑定规则 StateBindingRule 结构化绘制器，采用融合标头风格（同标准 Unity foldout 样式无深色矩形背景）。
    /// </summary>
    [CustomPropertyDrawer(typeof(StateBindingRule))]
    public class StateBindingRuleDrawer : PropertyDrawer
    {
        #region 重写 PropertyDrawer 方法

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty conditionsProp = property.FindPropertyRelative("_conditions");
            return GetConditionsHeight(conditionsProp);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty conditionsProp = property.FindPropertyRelative("_conditions");
            SerializedProperty stopProp = property.FindPropertyRelative("_stopOnMatch");

            DrawConditions(position, position.y, conditionsProp, stopProp);

            EditorGUI.EndProperty();
        }

        #endregion

        #region 内部排版辅助绘制

        internal static float GetConditionsHeight(SerializedProperty conditionsProp)
        {
            if (conditionsProp == null) return EditorGUIUtility.singleLineHeight + 2f;

            float h = EditorGUIUtility.singleLineHeight + 2f; // 折叠 Header 行
            if (conditionsProp.isExpanded)
            {
                int count = conditionsProp.arraySize;
                h += count * (EditorGUIUtility.singleLineHeight + 2f); // 每个 Item
                h += EditorGUIUtility.singleLineHeight + 2f; // [+] 增加按钮行
            }

            return h;
        }

        internal static float DrawConditions(Rect position, float currentY, SerializedProperty conditionsProp, SerializedProperty stopProp)
        {
            float startY = currentY;

            if (conditionsProp != null)
            {
                int count = conditionsProp.arraySize;
                float toggleWidth = 56f;

                // 折叠头（使用标准 Unity foldout 样式，无深色矩形黑框）
                Rect foldoutRect = new Rect(position.x, currentY, position.width - toggleWidth - 4f, EditorGUIUtility.singleLineHeight);
                conditionsProp.isExpanded = EditorGUI.Foldout(foldoutRect, conditionsProp.isExpanded, $"Conditions [{count}]", true, EditorStyles.foldout);

                // Stop On Match 极简 Toggle 控制组件（右侧同行，附带 Tooltip 提示）
                if (stopProp != null)
                {
                    Rect stopRect = new Rect(position.x + position.width - toggleWidth, currentY, toggleWidth, EditorGUIUtility.singleLineHeight);

                    float prevLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 36f;
                    EditorGUI.PropertyField(
                        stopRect, 
                        stopProp, 
                        new GUIContent("Stop", "中断评估 (StopOnMatch)：本规则匹配成功后直接跳出，忽略后续所有规则")
                    );
                    EditorGUIUtility.labelWidth = prevLabelWidth;
                }

                currentY += EditorGUIUtility.singleLineHeight + 2f;

                // 展开列表项绘制
                if (conditionsProp.isExpanded)
                {
                    float indentOffset = 14f;
                    float deleteBtnWidth = 22f;

                    for (int i = 0; i < count; i++)
                    {
                        SerializedProperty elemProp = conditionsProp.GetArrayElementAtIndex(i);
                        float itemWidth = position.width - indentOffset - deleteBtnWidth - 4f;

                        Rect elemRect = new Rect(position.x + indentOffset, currentY, itemWidth, EditorGUIUtility.singleLineHeight);
                        Rect deleteRect = new Rect(position.x + indentOffset + itemWidth + 2f, currentY, deleteBtnWidth, EditorGUIUtility.singleLineHeight);

                        EditorGUI.PropertyField(elemRect, elemProp, GUIContent.none);

                        if (GUI.Button(deleteRect, new GUIContent("-", "删除此条件"), EditorStyles.miniButton))
                        {
                            conditionsProp.DeleteArrayElementAtIndex(i);
                            break;
                        }

                        currentY += EditorGUIUtility.singleLineHeight + 2f;
                    }

                    // 底部 [+] 增加条件按钮
                    Rect addBtnRect = new Rect(position.x + indentOffset, currentY, position.width - indentOffset, EditorGUIUtility.singleLineHeight);
                    if (GUI.Button(addBtnRect, new GUIContent("+ Add Condition", "新增一条 From -> To 路径匹配条件"), EditorStyles.miniButton))
                    {
                        conditionsProp.arraySize++;
                        SerializedProperty newElem = conditionsProp.GetArrayElementAtIndex(conditionsProp.arraySize - 1);
                        SerializedProperty from = newElem.FindPropertyRelative("_fromPath");
                        SerializedProperty to = newElem.FindPropertyRelative("_toPath");
                        if (from != null) from.stringValue = "Any";
                        if (to != null) to.stringValue = "Any";
                    }

                    currentY += EditorGUIUtility.singleLineHeight + 2f;
                }
            }

            return currentY - startY;
        }

        #endregion
    }

    /// <summary>
    /// 强类型数据载荷状态绑定规则 StateBindingRule<TData> 结构化绘制器，展示纯净 Conditions 与 Data 标签。
    /// </summary>
    [CustomPropertyDrawer(typeof(StateBindingRule<>), true)]
    public class StateBindingRuleGenericDrawer : PropertyDrawer
    {
        #region 重写 PropertyDrawer 方法

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty conditionsProp = property.FindPropertyRelative("_conditions");
            SerializedProperty dataProp = property.FindPropertyRelative("_data");

            float height = StateBindingRuleDrawer.GetConditionsHeight(conditionsProp);
            if (dataProp != null)
            {
                height += EditorGUI.GetPropertyHeight(dataProp, true) + 2f;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty conditionsProp = property.FindPropertyRelative("_conditions");
            SerializedProperty stopProp = property.FindPropertyRelative("_stopOnMatch");
            SerializedProperty dataProp = property.FindPropertyRelative("_data");

            float currentY = position.y;

            // 绘制自绘表头与 Conditions 列表
            float condUsedHeight = StateBindingRuleDrawer.DrawConditions(position, currentY, conditionsProp, stopProp);
            currentY += condUsedHeight;

            // 下方绘制 Data 载荷
            if (dataProp != null)
            {
                float dataHeight = EditorGUI.GetPropertyHeight(dataProp, true);
                Rect dataRect = new Rect(position.x, currentY, position.width, dataHeight);
                EditorGUI.PropertyField(dataRect, dataProp, new GUIContent("Data"), true);
            }

            EditorGUI.EndProperty();
        }

        #endregion
    }
}
