using UnityEditor;
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// Decoration 类的自定义属性绘制器。
    /// 在折叠标题栏右侧添加一键 "Reset" 按钮，解决空列表添加项时默认值被 Unity 清零的缺陷。
    /// </summary>
    [CustomPropertyDrawer(typeof(Decoration))]
    public class DecorationDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 1. 提取 Header Rect 用来绘制折叠三角和自定义 Label
            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // 开启属性绘制块
            EditorGUI.BeginProperty(position, label, property);

            // 2. 解析当前元素的数组索引 (Index)
            int index = 0;
            string path = property.propertyPath;
            int lastBracketOpen = path.LastIndexOf('[');
            int lastBracketClose = path.LastIndexOf(']');
            if (lastBracketOpen >= 0 && lastBracketClose > lastBracketOpen)
            {
                string indexStr = path.Substring(lastBracketOpen + 1, lastBracketClose - lastBracketOpen - 1);
                int.TryParse(indexStr, out index);
            }

            // 3. 解析当前分配的预制件名称 (Prefab Name)
            string prefabName = "未分配预制件";
            SerializedProperty prefabProp = property.FindPropertyRelative("prefab");
            if (prefabProp != null && prefabProp.objectReferenceValue != null)
            {
                prefabName = prefabProp.objectReferenceValue.name;
            }

            // 4. 组装行标题 Label
            string headerTitle = $"[{index}] {prefabName}";
            GUIContent headerLabel = new GUIContent(headerTitle);

            // 5. 绘制带有折叠状态的 Foldout 标题栏
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, headerLabel, true);

            // 6. 如果折叠展开，在下方绘制一键重置按钮和子级属性字段
            if (property.isExpanded)
            {
                float yOffset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // 在展开部分的顶部单独占一行绘制 Reset 按钮，应用当前缩进
                Rect resetRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                Rect indentedResetRect = EditorGUI.IndentedRect(resetRect);
                if (GUI.Button(indentedResetRect, "重置当前项为默认配置 (Reset to Default)"))
                {
                    ResetDecorationProperties(property);
                }

                // 子属性往下移动
                yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                Rect childRect = new Rect(position.x, position.y + yOffset, position.width, position.height - yOffset);
                DrawChildProperties(childRect, property);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// 借助已有运行时实例的 Reset 成员方法，一键重置其所有字段值。
        /// 保证重置默认逻辑由类自身定义控制，实现默认值单一数据源。
        /// </summary>
        private void ResetDecorationProperties(SerializedProperty property)
        {
            try
            {
                // 1. 寻找 property 映射的底层运行时真实对象引用
                object targetObj = GetTargetObjectWithPropertyPaths(property);
                if (targetObj is Decoration decoration)
                {
                    // 2. 注册撤销记录，使用 targetObject
                    Undo.RecordObject(property.serializedObject.targetObject, "Reset Decoration Properties");

                    // 3. 调用底层的成员方法重置配置
                    decoration.Reset();

                    // 4. 将更改同步回 SerializedObject 并重绘 UI
                    property.serializedObject.Update();
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"重置 Decoration 属性失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 沿序列化属性路径反射提取其对应的底层实际 C# 运行时对象实例。
        /// </summary>
        private static object GetTargetObjectWithPropertyPaths(SerializedProperty prop)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetFieldValue(obj, elementName, index);
                }
                else
                {
                    obj = GetFieldValue(obj, element);
                }
            }
            return obj;
        }

        private static object GetFieldValue(object source, string name)
        {
            if (source == null) return null;
            var type = source.GetType();
            while (type != null)
            {
                var f = type.GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (f != null) return f.GetValue(source);
                var p = type.GetProperty(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (p != null) return p.GetValue(source, null);
                type = type.BaseType;
            }
            return null;
        }

        private static object GetFieldValue(object source, string name, int index)
        {
            var enumerable = GetFieldValue(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var en = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++)
            {
                if (!en.MoveNext()) return null;
            }
            return en.Current;
        }

        /// <summary>
        /// 递归向下顺次绘制子级可见字段，保持与 Unity 默认排版高度一致
        /// </summary>
        private void DrawChildProperties(Rect rect, SerializedProperty property)
        {
            SerializedProperty endProperty = property.GetEndProperty();
            SerializedProperty nextProperty = property.Copy();
            nextProperty.NextVisible(true); // 进入第一个子元素

            float currentY = rect.y;
            EditorGUI.indentLevel++;

            while (SerializedProperty.EqualContents(nextProperty, endProperty) == false)
            {
                float height = EditorGUI.GetPropertyHeight(nextProperty, true);
                Rect fieldRect = new Rect(rect.x, currentY, rect.width, height);

                EditorGUI.PropertyField(fieldRect, nextProperty, true);

                currentY += height + EditorGUIUtility.standardVerticalSpacing;
                if (!nextProperty.NextVisible(false)) // 只移动到下一个兄弟节点，不进入子节点深度
                    break;
            }

            EditorGUI.indentLevel--;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            // 基础高度：Foldout 标题行 + Reset 按钮行 + 两个间距
            float totalHeight = (EditorGUIUtility.singleLineHeight * 2) + (EditorGUIUtility.standardVerticalSpacing * 2);

            SerializedProperty endProperty = property.GetEndProperty();
            SerializedProperty nextProperty = property.Copy();
            nextProperty.NextVisible(true);

            while (SerializedProperty.EqualContents(nextProperty, endProperty) == false)
            {
                totalHeight += EditorGUI.GetPropertyHeight(nextProperty, true) + EditorGUIUtility.standardVerticalSpacing;
                if (!nextProperty.NextVisible(false))
                    break;
            }

            return totalHeight;
        }
    }
}
