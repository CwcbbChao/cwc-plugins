using System.Text;
using UnityEditor;
using UnityEngine;

namespace Cwc.InventoryEngine.Editor
{
    /// <summary>
    /// DebugSlotInfo 的自定义属性绘制器。
    /// 将深层折叠的槽位结构扁平化为 1 行显示，直观展示道具定义、数量和行内组件摘要。
    /// </summary>
    [CustomPropertyDrawer(typeof(DebugSlotInfo))]
    public class DebugSlotInfoDrawer : PropertyDrawer
    {
        #region Unity Lifecycle
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight + 2f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float y = position.y + 1f;
            float height = EditorGUIUtility.singleLineHeight;

            SerializedProperty slotIndexProp = property.FindPropertyRelative("_slotIndex");
            SerializedProperty isDisabledProp = property.FindPropertyRelative("_isDisabled");
            SerializedProperty isEmptyProp = property.FindPropertyRelative("_isEmpty");
            SerializedProperty definitionProp = property.FindPropertyRelative("_definition");
            SerializedProperty stackCountProp = property.FindPropertyRelative("_stackCount");
            SerializedProperty componentsProp = property.FindPropertyRelative("_components");

            int slotIndex = slotIndexProp != null ? slotIndexProp.intValue : -1;
            bool isDisabled = isDisabledProp != null && isDisabledProp.boolValue;
            bool isEmpty = isEmptyProp != null && isEmptyProp.boolValue;
            int stackCount = stackCountProp != null ? stackCountProp.intValue : 0;

            // 1. 槽位编号
            Rect indexRect = new Rect(position.x, y, 65f, height);
            string indexText = $"Slot {slotIndex:D2}:";
            EditorGUI.LabelField(indexRect, indexText, EditorStyles.miniBoldLabel);

            float currentX = position.x + 65f;
            float remainingWidth = position.width - 65f;

            if (isDisabled)
            {
                Rect disabledRect = new Rect(currentX, y, 70f, height);
                GUIStyle disabledStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.red } };
                EditorGUI.LabelField(disabledRect, "(Disabled)", disabledStyle);
                currentX += 70f;
                remainingWidth -= 70f;
            }

            if (isEmpty)
            {
                Rect emptyRect = new Rect(currentX, y, remainingWidth, height);
                GUIStyle emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleLeft };
                EditorGUI.LabelField(emptyRect, "Empty", emptyStyle);
            }
            else
            {
                // 2. ItemDefinition 引用框
                float defWidth = Mathf.Min(180f, remainingWidth * 0.45f);
                Rect defRect = new Rect(currentX, y, defWidth, height);
                if (definitionProp != null)
                {
                    EditorGUI.PropertyField(defRect, definitionProp, GUIContent.none);
                }
                currentX += defWidth + 6f;
                remainingWidth -= (defWidth + 6f);

                // 3. 堆叠数量
                float countWidth = 45f;
                Rect countRect = new Rect(currentX, y, countWidth, height);
                EditorGUI.LabelField(countRect, $"x{stackCount}", EditorStyles.boldLabel);
                currentX += countWidth;
                remainingWidth -= countWidth;

                // 4. 组件摘要 (行内简短展示)
                if (componentsProp != null && componentsProp.arraySize > 0 && remainingWidth > 30f)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("[");
                    int compCount = componentsProp.arraySize;
                    for (int i = 0; i < compCount; i++)
                    {
                        SerializedProperty compProp = componentsProp.GetArrayElementAtIndex(i);
                        SerializedProperty typeProp = compProp.FindPropertyRelative("_componentType");
                        string typeName = typeProp != null ? typeProp.stringValue : "Null";

                        if (typeName.EndsWith("Component"))
                        {
                            typeName = typeName.Substring(0, typeName.Length - "Component".Length);
                        }

                        sb.Append(typeName);
                        if (i < compCount - 1)
                        {
                            sb.Append(", ");
                        }
                    }
                    sb.Append("]");

                    Rect compRect = new Rect(currentX, y, remainingWidth, height);
                    GUIStyle compStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 1f) } };
                    EditorGUI.LabelField(compRect, sb.ToString(), compStyle);
                }
            }

            EditorGUI.EndProperty();
        }
        #endregion
    }
}
