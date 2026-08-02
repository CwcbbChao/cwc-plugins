using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Cwcbb.Tools.NewRoomBuilder.Editor
{
    /// <summary>
    /// 自定义 RoomPreset 资源面板编辑器。
    /// 支持在 Preset 面板中直接对所引用的 StructureGroup 和 DecorationGroup 进行原生的可折叠内嵌展开编辑。
    /// </summary>
    [CustomEditor(typeof(RoomPreset))]
    public class RoomPresetEditor : UnityEditor.Editor
    {
        #region 1. 私有字段 (缓存折叠状态与序列化对象)

        /// <summary>
        /// 缓存各个组 SO 的展开状态，以防面板重绘时折叠信息丢失。
        /// </summary>
        private readonly Dictionary<Object, bool> _soFoldouts = new Dictionary<Object, bool>();

        /// <summary>
        /// 缓存各个组 SO 的 SerializedObject，避免每帧创建导致丢失键盘焦点或数组字段不可编辑。
        /// </summary>
        private readonly Dictionary<Object, SerializedObject> _soSerializedObjects = new Dictionary<Object, SerializedObject>();

        #endregion

        #region 2. 生命周期与绘制重写 (Unity Editor Overrides)

        /// <summary>
        /// 编辑器销毁或隐藏时调用，清理缓存以防内存泄漏。
        /// </summary>
        private void OnDisable()
        {
            _soSerializedObjects.Clear();
        }

        /// <summary>
        /// 重写 Inspector 绘制逻辑。
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty structGroupsProp = serializedObject.FindProperty("_structureGroups");
            SerializedProperty decorGroupsProp = serializedObject.FindProperty("_decorationGroups");

            if (structGroupsProp != null)
            {
                EditorGUILayout.Space(5);
                DrawSOListWithFoldouts(structGroupsProp, "结构瓦片组列表 (Structure Groups)");
            }

            EditorGUILayout.Space(15);

            if (decorGroupsProp != null)
            {
                DrawSOListWithFoldouts(decorGroupsProp, "装饰摆件组列表 (Decoration Groups)");
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region 3. 辅助绘制方法 (Private UI Helpers)

        /// <summary>
        /// 绘制支持内部展开折叠的 ScriptableObject 列表 UI。
        /// 对齐顺序重构为：[ 折叠箭头 ] -> [ 资源字段 ] -> [ 移除按钮 X ]，且支持 Null 占位对齐。
        /// </summary>
        private void DrawSOListWithFoldouts(SerializedProperty listProp, string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("Box");

            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty elementProp = listProp.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();

                // 1. 绘制折叠箭头（放在最左侧，若为 null 则用空矩形占位以保持输入框左端完美对齐）
                Object soRef = elementProp.objectReferenceValue;
                bool expanded = false;
                if (soRef != null)
                {
                    if (!_soFoldouts.ContainsKey(soRef))
                    {
                        _soFoldouts[soRef] = false;
                    }

                    _soFoldouts[soRef] = EditorGUILayout.Toggle(_soFoldouts[soRef], EditorStyles.foldout, GUILayout.Width(15));
                    expanded = _soFoldouts[soRef];
                }
                else
                {
                    GUILayout.Space(15); // 占位保持左端对齐
                }

                // 2. 原生 SO 选择输入框
                EditorGUILayout.PropertyField(elementProp, GUIContent.none);

                // 3. 移除元素按钮（放在最右侧）
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    listProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break; // 结构改变，立即退出本次循环防数组越界
                }

                EditorGUILayout.EndHorizontal();

                // 4. 折叠展开：在正下方使用带 Box 边框的容器，通过迭代器反射渲染 SO 内的所有属性
                if (expanded && soRef != null)
                {
                    EditorGUILayout.BeginVertical("Box");

                    // 优先从缓存获取 SerializedObject，若不存在或目标对象变更则新建缓存
                    if (!_soSerializedObjects.TryGetValue(soRef, out SerializedObject soSerialized) || soSerialized.targetObject != soRef)
                    {
                        soSerialized = new SerializedObject(soRef);
                        _soSerializedObjects[soRef] = soSerialized;
                    }
                    soSerialized.Update();

                    SerializedProperty iterator = soSerialized.GetIterator();
                    bool enterChildren = true;
                    while (iterator.NextVisible(enterChildren))
                    {
                        enterChildren = false; // 只做首层反射遍历，嵌套渲染交由 PropertyField 自动分发
                        if (iterator.name == "m_Script") continue; // 排除 m_Script 默认字段

                        // 绘制属性字段并支持列表等多层嵌套
                        EditorGUILayout.PropertyField(iterator, true);
                    }

                    soSerialized.ApplyModifiedProperties();
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(2);
            }

            EditorGUILayout.Space(5);

            // 提供“快速添加”按钮
            if (GUILayout.Button("+ 添加新元素"))
            {
                listProp.arraySize++;
                listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = null;
            }

            EditorGUILayout.EndVertical();
        }

        #endregion
    }
}
