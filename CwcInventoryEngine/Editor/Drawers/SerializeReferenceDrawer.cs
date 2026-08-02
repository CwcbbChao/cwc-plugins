using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

namespace Cwc.InventoryEngine.Editor
{
    /// <summary>
    /// ItemDefinition 专属自定义 Inspector 编辑器。
    /// 彻底消除 ReorderableList 整块蓝色选中框，实现全宽扁平化 Header 条带与 1px 细割线，解决蓝底遮挡编辑框的视觉问题。
    /// </summary>
    [CustomEditor(typeof(ItemDefinition), true)]
    public class ItemDefinitionEditor : UnityEditor.Editor
    {
        #region Private Fields
        private SerializedProperty _componentDefsProp;

        private ReorderableList _reorderableList;
        private static List<TypeInfoCache> _cachedDerivedTypes;

        private static string _singleComponentCopyBufferJson = string.Empty;
        private static Type _singleComponentCopyBufferType = null;
        private static List<string> _allComponentsCopyBufferJsonList = new List<string>();
        private static List<Type> _allComponentsCopyBufferTypeList = new List<Type>();

        private AdvancedDropdownState _dropdownState;
        private GUIStyle _borderlessIconStyle;
        private GUIStyle _dragHandleStyle;
        #endregion

        #region Helper Structs
        internal struct TypeInfoCache
        {
            public Type Type;
            public string MenuPath;
            public string DisplayName;
        }
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            _componentDefsProp = serializedObject.FindProperty("ComponentDefinitions");
            _dropdownState = new AdvancedDropdownState();

            CacheDerivedTypes();
            InitReorderableList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 绘制除了 m_Script 与 ComponentDefinitions 之外的所有常规及子类属性（自动包含 maxStack、_addressableKey 等）
            DrawPropertiesExcluding(serializedObject, "m_Script", "ComponentDefinitions");

            EditorGUILayout.Space(6);

            // 顶部 Toolbar：组件列表与全局复制粘贴
            DrawListHeaderToolbar();

            EditorGUILayout.Space(2);

            // 绘制组件 ReorderableList
            _reorderableList.DoLayoutList();

            EditorGUILayout.Space(4);

            // 底部原生 AdvancedDropdown 添加按钮与粘贴为新组件按钮
            EditorGUILayout.BeginHorizontal();
            
            Rect btnRect = EditorGUILayout.GetControlRect(false, 24);
            if (GUI.Button(btnRect, new GUIContent("+ Add Component", "Add polymorphic component definition"), EditorStyles.miniButton))
            {
                ShowAdvancedDropdown(btnRect);
            }

            bool hasCopyBuffer = _singleComponentCopyBufferType != null && !string.IsNullOrEmpty(_singleComponentCopyBufferJson);
            GUI.enabled = hasCopyBuffer;
            if (GUILayout.Button(new GUIContent("Paste As New", "Paste copied component as a new component to this item"), EditorStyles.miniButton, GUILayout.Width(90), GUILayout.Height(24)))
            {
                PasteComponentAsNew();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
        #endregion

        #region ReorderableList Setup
        private void InitReorderableList()
        {
            _reorderableList = new ReorderableList(serializedObject, _componentDefsProp, true, false, false, false)
            {
                drawElementCallback = DrawListElement,
                elementHeightCallback = GetElementHeight,
                drawElementBackgroundCallback = (rect, index, isActive, isFocused) =>
                {
                    // 覆盖 ReorderableList 默认的大块蓝色选中框，绝不干扰展开属性编辑！
                },
                onReorderCallbackWithDetails = (list, oldIndex, newIndex) =>
                {
                    Undo.RecordObject(target, "Reorder Item Components");
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                }
            };
        }

        private float GetElementHeight(int index)
        {
            if (index < 0 || index >= _componentDefsProp.arraySize) return 24f;

            SerializedProperty elementProp = _componentDefsProp.GetArrayElementAtIndex(index);
            if (elementProp == null) return 24f;

            float height = 24f;

            if (elementProp.isExpanded && elementProp.managedReferenceValue != null)
            {
                height += 2f;
                SerializedProperty endProp = elementProp.GetEndProperty();
                SerializedProperty childProp = elementProp.Copy();
                bool enterChildren = true;

                while (childProp.NextVisible(enterChildren) && !SerializedProperty.EqualContents(childProp, endProp))
                {
                    height += EditorGUI.GetPropertyHeight(childProp, true) + 2f;
                    enterChildren = false;
                }
                height += 4f;
            }

            return height;
        }

        private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= _componentDefsProp.arraySize) return;

            SerializedProperty elementProp = _componentDefsProp.GetArrayElementAtIndex(index);
            if (elementProp == null) return;

            object instance = elementProp.managedReferenceValue;
            Type instanceType = instance?.GetType();

            float dragHandleWidth = 18f;
            float headerHeight = 22f;

            // 1. 扩展全宽通栏 Header Rect (向左推 18px 覆盖拖拽句柄背景，形成完整纯色 Header)
            Rect fullHeaderRect = new Rect(rect.x - dragHandleWidth, rect.y + 1f, rect.width + dragHandleWidth, headerHeight);

            // 2. 静态纯色背景 (彻底取消 Hover 动态计算，使用简洁平稳的纯色底色，绝不闪烁)
            Color bgColor = EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f, 1.0f)
                : new Color(0.86f, 0.86f, 0.86f, 1.0f);

            // 绘制整行纯色 Header 背景
            EditorGUI.DrawRect(fullHeaderRect, bgColor);

            // 3. Header 底部 1px 细分割线
            Rect dividerRect = new Rect(fullHeaderRect.x, fullHeaderRect.y + headerHeight - 1f, fullHeaderRect.width, 1f);
            Color dividerColor = EditorGUIUtility.isProSkin
                ? new Color(0.14f, 0.14f, 0.14f, 1f)
                : new Color(0.75f, 0.75f, 0.75f, 1f);
            EditorGUI.DrawRect(dividerRect, dividerColor);

            // 5. 事件响应与拖拽手势图标 (在左侧 30px 超大拖拽区绘制 ≡ 手势符号，绝不拦截拖拽手势)
            if (_dragHandleStyle == null)
            {
                _dragHandleStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.55f, 0.55f, 0.55f, 0.8f) : new Color(0.4f, 0.4f, 0.4f, 0.8f) }
                };
            }
            Rect dragHandleIconRect = new Rect(fullHeaderRect.x + 3.5f, fullHeaderRect.y, 14f, headerHeight);
            GUI.Label(dragHandleIconRect, "≡", _dragHandleStyle);

            Event evt = Event.current;
            Rect menuBtnRect = new Rect(fullHeaderRect.xMax - 22f, fullHeaderRect.y + 1f, 20f, 20f);

            if (evt.type == EventType.ContextClick && fullHeaderRect.Contains(evt.mousePosition))
            {
                ShowComponentContextMenu(fullHeaderRect, index, instanceType, instance, isContextMenu: true);
                evt.Use();
            }

            // 6. 折叠箭头与组件类型名称 (从 rect.x + 12f 开始，给左侧留出 30px 宽度的超大拖拽响应区)
            string friendlyName = GetFriendlyDisplayName(instanceType);
            Rect foldoutRect = new Rect(rect.x + 12f, rect.y + 2f, rect.width - 36f, 18f);
            elementProp.isExpanded = EditorGUI.Foldout(foldoutRect, elementProp.isExpanded, friendlyName, true);

            // 7. 右侧 Unity 原生 Component Header 同款三点菜单按钮
            GUIContent menuIcon = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_more" : "more");
            menuIcon.tooltip = "Component Options";

            if (GUI.Button(menuBtnRect, menuIcon, EditorStyles.iconButton))
            {
                ShowComponentContextMenu(menuBtnRect, index, instanceType, instance, isContextMenu: false);
            }

            // 8. 展开属性区域绘制
            if (elementProp.isExpanded && instance != null)
            {
                float currentY = rect.y + headerHeight + 3f;
                int originalIndent = EditorGUI.indentLevel;

                SerializedProperty endProp = elementProp.GetEndProperty();
                SerializedProperty childProp = elementProp.Copy();
                bool enterChildren = true;

                while (childProp.NextVisible(enterChildren) && !SerializedProperty.EqualContents(childProp, endProp))
                {
                    float propHeight = EditorGUI.GetPropertyHeight(childProp, true);
                    Rect propRect = new Rect(rect.x, currentY, rect.width, propHeight);
                    
                    EditorGUI.indentLevel = originalIndent;
                    EditorGUI.PropertyField(propRect, childProp, true);

                    currentY += propHeight + 2f;
                    enterChildren = false;
                }

                EditorGUI.indentLevel = originalIndent;
            }
        }
        #endregion

        #region Toolbar & Context Menu
        private void DrawListHeaderToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Component Definitions", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            // 复制所有组件
            if (GUILayout.Button("Copy All", EditorStyles.miniButtonLeft, GUILayout.Width(65)))
            {
                CopyAllComponents();
            }

            // 粘贴所有组件 (下拉)
            GUI.enabled = _allComponentsCopyBufferTypeList != null && _allComponentsCopyBufferTypeList.Count > 0;
            if (GUILayout.Button("Paste All ▼", EditorStyles.miniButtonRight, GUILayout.Width(75)))
            {
                ShowPasteAllMenu();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void ShowComponentContextMenu(Rect positionRect, int index, Type instanceType, object instance, bool isContextMenu)
        {
            GenericMenu menu = new GenericMenu();

            if (instance == null || instanceType == null)
            {
                menu.AddItem(new GUIContent("Remove Component"), false, () => RemoveComponentAtIndex(index));
                if (isContextMenu) menu.ShowAsContext();
                else menu.DropDown(positionRect);
                return;
            }

            // 1. Reset 与 Remove Component (最高频操作，置顶显示)
            menu.AddItem(new GUIContent("Reset"), false, () => ResetComponentAtIndex(index, instanceType));
            menu.AddItem(new GUIContent("Remove Component"), false, () => RemoveComponentAtIndex(index));

            menu.AddSeparator("");

            // 2. 复制与粘贴操作 (精简文案：Copy / Paste Values / Paste As New)
            menu.AddItem(new GUIContent("Copy"), false, () => CopySingleComponent(instance, instanceType));

            bool canPasteValues = _singleComponentCopyBufferType != null && _singleComponentCopyBufferType == instanceType;
            if (canPasteValues)
            {
                menu.AddItem(new GUIContent("Paste Values"), false, () => PasteSingleComponent(index));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste Values"));
            }

            bool hasCopyBuffer = _singleComponentCopyBufferType != null && !string.IsNullOrEmpty(_singleComponentCopyBufferJson);
            if (hasCopyBuffer)
            {
                string typeName = _singleComponentCopyBufferType.Name;
                menu.AddItem(new GUIContent($"Paste As New ({typeName})"), false, () => PasteComponentAsNew(index));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste As New"));
            }

            menu.AddSeparator("");

            // 3. 打开 C# 脚本
            menu.AddItem(new GUIContent("Open Script"), false, () => OpenScriptForType(instanceType));

            if (isContextMenu)
            {
                menu.ShowAsContext();
            }
            else
            {
                menu.DropDown(positionRect);
            }
        }

        private void OpenScriptForType(Type type)
        {
            if (type == null) return;

            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                {
                    AssetDatabase.OpenAsset(script);
                    return;
                }
            }

            EditorUtility.DisplayDialog("Notice", $"Could not find C# script for type {type.Name}.", "OK");
        }
        #endregion

        #region Component Manipulation Logic
        private void ResetComponentAtIndex(int index, Type instanceType)
        {
            Undo.RecordObject(target, "Reset Item Component");
            serializedObject.Update();

            object newInstance = Activator.CreateInstance(instanceType);
            SerializedProperty elementProp = _componentDefsProp.GetArrayElementAtIndex(index);
            elementProp.managedReferenceValue = newInstance;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        private void CopySingleComponent(object instance, Type instanceType)
        {
            try
            {
                _singleComponentCopyBufferJson = JsonUtility.ToJson(instance);
                _singleComponentCopyBufferType = instanceType;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Copy Component failed: {ex.Message}");
            }
        }

        private void PasteSingleComponent(int index)
        {
            if (_singleComponentCopyBufferType == null || string.IsNullOrEmpty(_singleComponentCopyBufferJson)) return;

            Undo.RecordObject(target, "Paste Item Component Values");
            serializedObject.Update();

            try
            {
                object newInstance = JsonUtility.FromJson(_singleComponentCopyBufferJson, _singleComponentCopyBufferType);
                SerializedProperty elementProp = _componentDefsProp.GetArrayElementAtIndex(index);
                elementProp.managedReferenceValue = newInstance;

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Paste Component Values failed: {ex.Message}");
            }
        }

        private void PasteComponentAsNew(int insertIndex = -1)
        {
            if (_singleComponentCopyBufferType == null || string.IsNullOrEmpty(_singleComponentCopyBufferJson)) return;

            Undo.RecordObject(target, "Paste Component As New");
            serializedObject.Update();

            try
            {
                object newInstance = JsonUtility.FromJson(_singleComponentCopyBufferJson, _singleComponentCopyBufferType);
                int targetIndex = insertIndex >= 0 ? insertIndex + 1 : _componentDefsProp.arraySize;

                _componentDefsProp.InsertArrayElementAtIndex(targetIndex);
                SerializedProperty newElement = _componentDefsProp.GetArrayElementAtIndex(targetIndex);
                newElement.managedReferenceValue = newInstance;
                newElement.isExpanded = true;

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Paste Component As New failed: {ex.Message}");
            }
        }

        private void RemoveComponentAtIndex(int index)
        {
            Undo.RecordObject(target, "Remove Item Component");
            serializedObject.Update();

            _componentDefsProp.DeleteArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        private void CopyAllComponents()
        {
            _allComponentsCopyBufferJsonList.Clear();
            _allComponentsCopyBufferTypeList.Clear();

            int count = _componentDefsProp.arraySize;
            for (int i = 0; i < count; i++)
            {
                SerializedProperty elementProp = _componentDefsProp.GetArrayElementAtIndex(i);
                object instance = elementProp.managedReferenceValue;
                if (instance != null)
                {
                    _allComponentsCopyBufferJsonList.Add(JsonUtility.ToJson(instance));
                    _allComponentsCopyBufferTypeList.Add(instance.GetType());
                }
            }

            Debug.Log($"Copied {_allComponentsCopyBufferTypeList.Count} components.");
        }

        private void ShowPasteAllMenu()
        {
            GenericMenu menu = new GenericMenu();

            Rect btnRect = EditorGUILayout.GetControlRect(false, 0);
            menu.AddItem(new GUIContent("Replace All"), false, () => PasteAllComponents(replace: true));
            menu.AddItem(new GUIContent("Append All"), false, () => PasteAllComponents(replace: false));
            menu.ShowAsContext();
        }

        private void PasteAllComponents(bool replace)
        {
            if (_allComponentsCopyBufferTypeList == null || _allComponentsCopyBufferTypeList.Count == 0) return;

            Undo.RecordObject(target, "Paste All Item Components");
            serializedObject.Update();

            if (replace)
            {
                _componentDefsProp.ClearArray();
            }

            for (int i = 0; i < _allComponentsCopyBufferTypeList.Count; i++)
            {
                Type type = _allComponentsCopyBufferTypeList[i];
                string json = _allComponentsCopyBufferJsonList[i];

                object instance = JsonUtility.FromJson(json, type);
                int newIndex = _componentDefsProp.arraySize;
                _componentDefsProp.InsertArrayElementAtIndex(newIndex);
                SerializedProperty newElement = _componentDefsProp.GetArrayElementAtIndex(newIndex);
                newElement.managedReferenceValue = instance;
                newElement.isExpanded = true;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }
        #endregion

        #region Native AdvancedDropdown
        private void ShowAdvancedDropdown(Rect buttonRect)
        {
            if (_cachedDerivedTypes == null || _cachedDerivedTypes.Count == 0)
            {
                EditorUtility.DisplayDialog("Notice", "No derived ItemComponentDefinition classes found.", "OK");
                return;
            }

            var dropdown = new ItemComponentAdvancedDropdown(_dropdownState, _cachedDerivedTypes, AddComponentOfType);
            dropdown.Show(buttonRect);
        }

        private void AddComponentOfType(Type type)
        {
            Undo.RecordObject(target, "Add Item Component");
            serializedObject.Update();

            object instance = Activator.CreateInstance(type);
            int newIndex = _componentDefsProp.arraySize;
            _componentDefsProp.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newElement = _componentDefsProp.GetArrayElementAtIndex(newIndex);
            newElement.managedReferenceValue = instance;
            newElement.isExpanded = true;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }
        #endregion

        #region Cache Helpers
        private static void CacheDerivedTypes()
        {
            if (_cachedDerivedTypes != null) return;

            _cachedDerivedTypes = new List<TypeInfoCache>();

            var derivedTypes = TypeCache.GetTypesDerivedFrom<ItemComponentDefinition>()
                .Where(t => !t.IsAbstract && !t.IsInterface && t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var type in derivedTypes)
            {
                var attr = type.GetCustomAttribute<ItemComponentPathAttribute>();
                string menuPath = attr != null && !string.IsNullOrEmpty(attr.Path) ? attr.Path : GetFallbackMenuPath(type);
                string displayName = attr != null && !string.IsNullOrEmpty(attr.Path) ? attr.Path : type.Name;

                _cachedDerivedTypes.Add(new TypeInfoCache
                {
                    Type = type,
                    MenuPath = menuPath,
                    DisplayName = displayName
                });
            }

            _cachedDerivedTypes.Sort((a, b) => string.Compare(a.MenuPath, b.MenuPath, StringComparison.Ordinal));
        }

        private static string GetFallbackMenuPath(Type type)
        {
            string name = type.Name;
            if (name.EndsWith("ComponentDefinition"))
            {
                name = name.Substring(0, name.Length - "ComponentDefinition".Length);
            }
            else if (name.EndsWith("Definition"))
            {
                name = name.Substring(0, name.Length - "Definition".Length);
            }
            return $"Other/{name}";
        }

        private static string GetFriendlyDisplayName(Type type)
        {
            if (type == null) return "Null Component";

            var attr = type.GetCustomAttribute<ItemComponentPathAttribute>();
            if (attr != null && !string.IsNullOrEmpty(attr.Path))
            {
                string path = attr.Path;
                int lastSlash = path.LastIndexOf('/');
                string leafName = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
                return $"{leafName} ({type.Name})";
            }

            return type.Name;
        }
        #endregion
    }

    /// <summary>
    /// Unity 原生标准 AdvancedDropdown 下拉菜单。
    /// </summary>
    internal class ItemComponentAdvancedDropdown : AdvancedDropdown
    {
        private readonly List<ItemDefinitionEditor.TypeInfoCache> _types;
        private readonly Action<Type> _onSelected;

        public ItemComponentAdvancedDropdown(AdvancedDropdownState state, List<ItemDefinitionEditor.TypeInfoCache> types, Action<Type> onSelected)
            : base(state)
        {
            _types = types;
            _onSelected = onSelected;
            this.minimumSize = new Vector2(260, 300);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Item Components");

            foreach (var info in _types)
            {
                string[] paths = info.MenuPath.Split('/');
                AdvancedDropdownItem currentParent = root;

                for (int i = 0; i < paths.Length; i++)
                {
                    string seg = paths[i];
                    if (i == paths.Length - 1)
                    {
                        var leaf = new ComponentDropdownItem(seg, info.Type);
                        currentParent.AddChild(leaf);
                    }
                    else
                    {
                        var folder = FindOrCreateFolder(currentParent, seg);
                        currentParent = folder;
                    }
                }
            }

            return root;
        }

        private AdvancedDropdownItem FindOrCreateFolder(AdvancedDropdownItem parent, string folderName)
        {
            foreach (var child in parent.children)
            {
                if (child.name == folderName)
                {
                    return child;
                }
            }

            var newFolder = new AdvancedDropdownItem(folderName);
            parent.AddChild(newFolder);
            return newFolder;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            if (item is ComponentDropdownItem compItem && compItem.TargetType != null)
            {
                _onSelected?.Invoke(compItem.TargetType);
            }
        }

        private class ComponentDropdownItem : AdvancedDropdownItem
        {
            public Type TargetType { get; }

            public ComponentDropdownItem(string name, Type targetType) : base(name)
            {
                TargetType = targetType;
            }
        }
    }
}
