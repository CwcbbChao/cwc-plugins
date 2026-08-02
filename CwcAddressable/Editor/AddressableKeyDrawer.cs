// FileName: AddressableKeyDrawer.cs
using UnityEditor;
using UnityEngine;

namespace CwcAddressable.Editor
{
    /// <summary>
    /// AddressableKey 结构体的专属 PropertyDrawer。
    /// 自动将 AddressableKey 字段渲染为灰底只读预览状态，无需给字段添加任何额外的 Field Attribute。
    /// </summary>
    [CustomPropertyDrawer(typeof(AddressableKey))]
    public class AddressableKeyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("_value");
            string currentVal = valueProp != null ? valueProp.stringValue : string.Empty;

            EditorGUI.BeginProperty(position, label, property);

            // 1. 绘制 Label
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            Rect fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);

            EditorGUI.LabelField(labelRect, label);

            // 2. 强行锁定为 Disabled 状态（只读预览，禁止输入）
            EditorGUI.BeginDisabledGroup(true);
            
            string displayStr = !string.IsNullOrEmpty(currentVal) ? currentVal : "(未注入 Addressable Key)";
            EditorGUI.TextField(fieldRect, displayStr);
            
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
