using UnityEditor;
using UnityEngine;

namespace Cwc.InventoryEngine.Editor
{
    /// <summary>
    /// Inventory 的 Unity Editor 编辑器类。
    /// 遵循原生 Inspector 绘制风格，并将调试槽位列表设为只读防修改模式。
    /// </summary>
    [CustomEditor(typeof(Inventory), true)]
    public class InventoryComponentEditor : UnityEditor.Editor
    {
        #region Private Fields
        private static bool _showDebugSlots = true;
        private static bool _hideEmptySlots = false;
        #endregion

        #region Unity Lifecycle
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 1. 绘制除调试槽位列表之外的原生默认属性
            DrawPropertiesExcluding(serializedObject, "_debugSlots");

            // 2. 将调试槽位列表自定义高效绘制
            SerializedProperty debugSlotsProp = serializedObject.FindProperty("_debugSlots");
            if (debugSlotsProp != null)
            {
                DrawDebugSlotsSection(debugSlotsProp);
            }

            serializedObject.ApplyModifiedProperties();

            Inventory component = (Inventory)target;
            if (component == null) return;

            // 3. 在运行模式下提供必要的快捷调试按钮
            if (Application.isPlaying && component.IsInitialized)
            {
                EditorGUILayout.Space(5);
                if (GUILayout.Button("清空背包"))
                {
                    if (EditorUtility.DisplayDialog("确认清空", "是否确认清空当前背包中的所有物品？", "确定", "取消"))
                    {
                        component.Clear();
                    }
                }
            }
        }
        #endregion

        #region Private Methods
        private void DrawDebugSlotsSection(SerializedProperty debugSlotsProp)
        {
            EditorGUILayout.Space(8);

            // 计算已用槽位与总数统计
            int totalSlots = debugSlotsProp.arraySize;
            int usedSlots = 0;
            for (int i = 0; i < totalSlots; i++)
            {
                SerializedProperty element = debugSlotsProp.GetArrayElementAtIndex(i);
                SerializedProperty isEmptyProp = element.FindPropertyRelative("_isEmpty");
                if (isEmptyProp != null && !isEmptyProp.boolValue)
                {
                    usedSlots++;
                }
            }

            // 绘制 Header 工具栏
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _showDebugSlots = EditorGUILayout.Foldout(_showDebugSlots, $"Debug Slots (Used: {usedSlots} / {totalSlots})", true, EditorStyles.foldoutHeader);

            GUILayout.FlexibleSpace();
            _hideEmptySlots = GUILayout.Toggle(_hideEmptySlots, "Hide Empty", EditorStyles.toolbarButton, GUILayout.Width(75));
            EditorGUILayout.EndHorizontal();

            if (!_showDebugSlots) return;

            // 列表内容
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (totalSlots == 0)
            {
                EditorGUILayout.LabelField("No Slots Initialized", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                int drawnCount = 0;
                for (int i = 0; i < totalSlots; i++)
                {
                    SerializedProperty element = debugSlotsProp.GetArrayElementAtIndex(i);
                    SerializedProperty isEmptyProp = element.FindPropertyRelative("_isEmpty");
                    bool isEmpty = isEmptyProp != null && isEmptyProp.boolValue;

                    if (_hideEmptySlots && isEmpty)
                    {
                        continue;
                    }

                    // 保持只读绘制
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(element);
                    EditorGUI.EndDisabledGroup();
                    drawnCount++;
                }

                if (_hideEmptySlots && drawnCount == 0 && totalSlots > 0)
                {
                    EditorGUILayout.LabelField("All slots are empty (Hide Empty enabled)", EditorStyles.centeredGreyMiniLabel);
                }
            }
            EditorGUILayout.EndVertical();
        }
        #endregion
    }
}
