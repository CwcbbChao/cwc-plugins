using UnityEditor;
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 可锁定三轴比例的 Vector3 自定义属性绘制器。
    /// 提供在 Inspector 中等比缩放的锁定与解锁功能。
    /// </summary>
    [CustomPropertyDrawer(typeof(LockableVector3))]
    public class LockableVector3Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("value");
            SerializedProperty lockedProp = property.FindPropertyRelative("locked");

            // 自动纠偏：解决 Unity 从空列表点击 "+" 新增元素时，结构体被默认清零（value 为 0,0,0 且 locked 为 false）的问题
            if (valueProp.vector3Value == Vector3.zero && !lockedProp.boolValue)
            {
                valueProp.vector3Value = Vector3.one;
                lockedProp.boolValue = true;
            }

            Vector3 val = valueProp.vector3Value;
            bool locked = lockedProp.boolValue;

            // 开启属性绘制块
            EditorGUI.BeginProperty(position, label, property);

            // 绘制属性标签，PrefixLabel 会自动缩进并在左侧显示字段名称，返回右侧剩余的可编辑区域 Rect
            Rect contentRect = EditorGUI.PrefixLabel(position, label);

            // 锁按钮的物理尺寸与间距参数
            float lockWidth = 16f;
            float gap = 2f;

            // 划分左右：左边是小锁按钮，右边是三轴 Vector3 输入框 (移到前面，与 Unity 原生对齐)
            Rect lockRect = new Rect(contentRect.x, contentRect.y, lockWidth, contentRect.height);
            Rect vectorRect = new Rect(contentRect.x + lockWidth + gap, contentRect.y, contentRect.width - lockWidth - gap, contentRect.height);

            // 获取 Unity 内置的链接 (Linked/Unlinked) 链条贴图
            GUIContent lockContent = null;
            try
            {
                lockContent = locked ? EditorGUIUtility.IconContent("d_Linked") : EditorGUIUtility.IconContent("d_Unlinked");
                if (lockContent == null || lockContent.image == null)
                {
                    lockContent = locked ? EditorGUIUtility.IconContent("Linked") : EditorGUIUtility.IconContent("Unlinked");
                }
            }
            catch
            {
                // 防御性异常捕获
            }

            if (lockContent == null || lockContent.image == null)
            {
                // Fallback 回退字符
                lockContent = new GUIContent(locked ? "🔗" : "🔓", locked ? "已锁定比例 (等比缩放)" : "已解锁比例 (自由缩放)");
            }
            else
            {
                lockContent.tooltip = locked ? "已锁定比例 (等比缩放)" : "已解锁比例 (自由缩放)";
            }

            // 锁按钮的自定义样式：使用 none 避免背景色影响，直接渲染贴图，保证与 Unity 原生 UI 完美一致
            GUIStyle lockStyle = new GUIStyle(GUIStyle.none)
            {
                alignment = TextAnchor.MiddleCenter
            };

            // 1. 绘制锁定/解锁切换按钮
            if (GUI.Button(lockRect, lockContent, lockStyle))
            {
                lockedProp.boolValue = !locked;
            }

            // 2. 绘制 Vector3 属性输入框并捕获更改
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(vectorRect, valueProp, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 newVal = valueProp.vector3Value;
                if (locked)
                {
                    // 若锁定了三轴比例，当任意一轴改变时，其他两轴都同步设为相同的值
                    if (!Mathf.Approximately(newVal.x, val.x))
                    {
                        newVal.y = newVal.x;
                        newVal.z = newVal.x;
                    }
                    else if (!Mathf.Approximately(newVal.y, val.y))
                    {
                        newVal.x = newVal.y;
                        newVal.z = newVal.y;
                    }
                    else if (!Mathf.Approximately(newVal.z, val.z))
                    {
                        newVal.x = newVal.z;
                        newVal.y = newVal.z;
                    }
                }
                valueProp.vector3Value = newVal;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("value");
            return EditorGUI.GetPropertyHeight(valueProp, label);
        }
    }
}
