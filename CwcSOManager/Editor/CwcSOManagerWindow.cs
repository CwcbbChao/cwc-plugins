using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CwcSOManager
{
    /// <summary>
    /// 基于 UI Toolkit 实现的 CwcSOManager 编辑器主窗口，提供自适应的三栏式布局与卓越 savings 的交互响应性能。
    /// </summary>
    public class CwcSOManagerWindow : EditorWindow
    {
        #region 常量与静态字段

        private const float DefaultLeftWidth = 250f;
        private const float DefaultCenterWidth = 450f;

        #endregion

        #region 私有字段

        // 分割器位置参数
        private float _splitterPos1 = DefaultLeftWidth;
        private float _splitterPos2 = DefaultLeftWidth + DefaultCenterWidth;

        // 路径树与搜索过滤
        private CwcPathNode _pathTreeRoot;
        private string _leftSearchText = "";
        private string _centerSearchText = "";

        // 选中状态
        private Type _selectedType;
        private List<ScriptableObject> _instances = new List<ScriptableObject>();
        private readonly List<ScriptableObject> _filteredInstances = new List<ScriptableObject>();
        private readonly List<ScriptableObject> _selectedInstances = new List<ScriptableObject>();

        #endregion

        #region 公共属性

        public ScriptableObject SelectedInstance => _selectedInstances.FirstOrDefault();
        public IReadOnlyList<ScriptableObject> SelectedInstances => _selectedInstances;

        // 缓存字典与映射
        private List<CwcColumnInfo> _cachedColumns = new List<CwcColumnInfo>();
        private readonly Dictionary<ScriptableObject, SerializedObject> _serializedObjectCache = new Dictionary<ScriptableObject, SerializedObject>();
        private readonly List<SerializedObject> _tempUpdateList = new List<SerializedObject>();
        private readonly Dictionary<Type, int> _typeToTreeItemId = new Dictionary<Type, int>();
        private int _nextTreeItemId = 0;

        // 排序规则
        private string _sortKey = "Asset Name";
        private bool _sortAscending = true;
        private double _lastUpdateTime;
        private bool _isExpandingAll;

        // UI 元素
        private TwoPaneSplitView _mainSplitView;
        private TwoPaneSplitView _rightContainerSplitView;

        // 左侧组件
        private TreeView _treeView;

        // 中间组件
        private Label _placeholderLabel;
        private VisualElement _centerMainContent;
        private Label _typeHeaderLabel;
        private Label _typeFolderLabel;
        private Label _instanceCountLabel;
        private MultiColumnListView _table;

        // 右侧组件
        private ScrollView _inspectorScrollView;
        private Editor _cachedEditor;

        #endregion

        #region 生命周期方法

        [MenuItem("Tools/CwcSOManager/Open Manager Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<CwcSOManagerWindow>();
            window.titleContent = new GUIContent("Cwc SO Manager");
            window.minSize = new Vector2(900, 530);
            window.Show();
        }

        private void OnEnable()
        {
            // 从 EditorPrefs 恢复分割线位置
            _splitterPos1 = EditorPrefs.GetFloat("CwcSOManager_SplitterPos1", DefaultLeftWidth);
            float offset2 = EditorPrefs.GetFloat("CwcSOManager_SplitterPos2_Offset", DefaultCenterWidth);
            _splitterPos2 = _splitterPos1 + offset2;

            RefreshPathTree();

            // 监听项目资源改变事件，保证外部或右侧 Inspector 更改名字时自动刷新列表
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable()
        {
            // 注销事件监听，防止内存泄漏
            EditorApplication.projectChanged -= OnProjectChanged;

            // 保存分割线位置
            if (_mainSplitView != null && _mainSplitView.fixedPane != null)
            {
                EditorPrefs.SetFloat("CwcSOManager_SplitterPos1", _mainSplitView.fixedPane.resolvedStyle.width);
            }
            if (_rightContainerSplitView != null && _rightContainerSplitView.fixedPane != null)
            {
                EditorPrefs.SetFloat("CwcSOManager_SplitterPos2_Offset", _rightContainerSplitView.fixedPane.resolvedStyle.width);
            }

            if (_cachedEditor != null)
            {
                DestroyImmediate(_cachedEditor);
                _cachedEditor = null;
            }

            // 显式清理缓存，防止内存泄漏
            _serializedObjectCache.Clear();
        }

        /// <summary>
        /// 项目资源更改时的事件回调，执行刷新以保持 UI 状态同步
        /// </summary>
        private void OnProjectChanged()
        {
            RefreshInstances();
            if (_table != null)
            {
                _table.RefreshItems();
            }
        }

        /// <summary>
        /// 创建并初始化 UI 树结构
        /// </summary>
        public void CreateGUI()
        {
            // 统一深色底色，作为三栏之间分割的底色衬线
            var splitterBgColor = new Color(0.13f, 0.13f, 0.13f);

            // 1. 最外层水平分割线（左栏 垂直分割 右栏复合）
            _mainSplitView = new TwoPaneSplitView(0, _splitterPos1, TwoPaneSplitViewOrientation.Horizontal);
            _mainSplitView.style.flexGrow = 1;
            _mainSplitView.style.backgroundColor = splitterBgColor;
            rootVisualElement.Add(_mainSplitView);

            // 左侧折叠树容器
            var leftView = new VisualElement { name = "LeftView" };
            leftView.style.flexGrow = 1;
            leftView.style.flexShrink = 0;
            leftView.style.minWidth = 150;
            leftView.style.marginRight = 3;
            leftView.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            _mainSplitView.Add(leftView);

            // 2. 内层水平分割线（中栏表格 垂直分割 右栏检查器）
            float innerOffset = _splitterPos2 - _splitterPos1;
            _rightContainerSplitView = new TwoPaneSplitView(0, innerOffset, TwoPaneSplitViewOrientation.Horizontal);
            _rightContainerSplitView.style.flexGrow = 1;
            _rightContainerSplitView.style.flexShrink = 0;
            _rightContainerSplitView.style.backgroundColor = splitterBgColor;
            _mainSplitView.Add(_rightContainerSplitView);

            // 中间数据表容器
            var centerView = new VisualElement { name = "CenterView" };
            centerView.style.flexGrow = 1;
            centerView.style.flexShrink = 0;
            centerView.style.minWidth = 250;
            centerView.style.marginLeft = 3;
            centerView.style.marginRight = 3;
            centerView.style.backgroundColor = new Color(0.20f, 0.20f, 0.20f);
            _rightContainerSplitView.Add(centerView);

            // 右侧属性面板容器
            var rightView = new VisualElement { name = "RightView" };
            rightView.style.flexGrow = 1;
            rightView.style.flexShrink = 0;
            rightView.style.minWidth = 200;
            rightView.style.marginLeft = 3;
            rightView.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            _rightContainerSplitView.Add(rightView);

            // 3. 构建左侧区域
            BuildLeftArea(leftView);

            // 4. 构建中间区域
            BuildCenterArea(centerView);

            // 5. 构建右侧区域
            BuildRightArea(rightView);

            // 6. 恢复上一次选中状态
            RestoreLastSelection();
        }

        #endregion

        #region 左侧区域构建

        private void BuildLeftArea(VisualElement container)
        {
            // 搜索过滤条
            var toolbar = new Toolbar();
            container.Add(toolbar);

            var filterLabel = new Label("Filter: ") { style = { unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 4, flexShrink = 1 } };
            toolbar.Add(filterLabel);

            var searchField = new ToolbarSearchField { style = { flexGrow = 1, flexShrink = 1, minWidth = 40 } };
            searchField.RegisterValueChangedCallback(evt =>
            {
                _leftSearchText = evt.newValue;
                ReloadTreeData();
            });
            toolbar.Add(searchField);

            // TreeView 列表
            _treeView = new TreeView
            {
                style = { flexGrow = 1, borderRightWidth = 1, borderRightColor = new Color(0.12f, 0.12f, 0.12f, 0.5f) },
                showBorder = false,
                fixedItemHeight = 22
            };

            _treeView.makeItem = () =>
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.height = 20;

                var icon = new Image { style = { width = 16, height = 16, marginRight = 4 } };
                var label = new Label();

                row.Add(icon);
                row.Add(label);
                return row;
            };

            _treeView.bindItem = (VisualElement element, int index) =>
            {
                var icon = element.Q<Image>();
                var label = element.Q<Label>();
                var item = _treeView.GetItemDataForIndex<CwcTreeItem>(index);
                label.text = item.Name;

                if (item.IsType)
                {
                    icon.image = EditorGUIUtility.IconContent("ScriptableObject Icon").image;
                    label.style.unityFontStyleAndWeight = FontStyle.Normal;
                }
                else
                {
                    icon.image = EditorGUIUtility.IconContent("Folder Icon").image;
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                }
            };

            _treeView.selectionChanged += OnTreeSelectionChanged;
            _treeView.itemExpandedChanged += OnTreeViewItemExpandedChanged;
            container.Add(_treeView);

            ReloadTreeData();
        }

        private void ReloadTreeData()
        {
            _nextTreeItemId = 0;
            _typeToTreeItemId.Clear();
            _treeView.ClearSelection();

            var roots = BuildTreeViewItems(_pathTreeRoot);
            _treeView.SetRootItems(roots);
            _treeView.RefreshItems();

            // 若有搜索内容，默认全部展开以展露匹配项
            if (!string.IsNullOrEmpty(_leftSearchText))
            {
                ExpandAllTreeViewItems();
            }
        }

        private List<TreeViewItemData<CwcTreeItem>> BuildTreeViewItems(CwcPathNode node)
        {
            var list = new List<TreeViewItemData<CwcTreeItem>>();
            if (node == null) return list;

            // 1. 递归转换子目录
            foreach (var pair in node.Children.OrderBy(p => p.Key))
            {
                var childNode = pair.Value;
                if (!string.IsNullOrEmpty(_leftSearchText) && !IsNodeMatchSearch(childNode, _leftSearchText))
                {
                    continue;
                }

                var itemData = new CwcTreeItem
                {
                    Name = childNode.Name,
                    FullPath = childNode.FullPath,
                    Type = null
                };

                int id = _nextTreeItemId++;
                var grandChildren = BuildTreeViewItems(childNode);
                list.Add(new TreeViewItemData<CwcTreeItem>(id, itemData, grandChildren));
            }

            // 2. 转换类型叶子节点
            foreach (var type in node.SOTypes.OrderBy(t => t.Name))
            {
                if (!string.IsNullOrEmpty(_leftSearchText) && !type.Name.Contains(_leftSearchText, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var itemData = new CwcTreeItem
                {
                    Name = type.Name,
                    FullPath = node.FullPath + "/" + type.Name,
                    Type = type
                };

                int id = _nextTreeItemId++;
                _typeToTreeItemId[type] = id;
                list.Add(new TreeViewItemData<CwcTreeItem>(id, itemData));
            }

            return list;
        }

        private void ExpandAllTreeViewItems()
        {
            for (int i = 0; i < _nextTreeItemId; i++)
            {
                _treeView.ExpandItem(i);
            }
        }

        private bool IsNodeMatchSearch(CwcPathNode node, string search)
        {
            if (node == null) return false;

            foreach (var type in node.SOTypes)
            {
                if (type.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            foreach (var pair in node.Children)
            {
                if (IsNodeMatchSearch(pair.Value, search))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnTreeSelectionChanged(IEnumerable<object> selectedItems)
        {
            var item = selectedItems.FirstOrDefault() as CwcTreeItem;
            if (item != null && item.IsType)
            {
                SelectType(item.Type);
            }
            else
            {
                SelectType(null);
            }
        }

        #endregion

        #region 中间数据表区域构建

        private void BuildCenterArea(VisualElement container)
        {
            container.style.borderRightWidth = 1;
            container.style.borderRightColor = new Color(0.12f, 0.12f, 0.12f, 0.5f);

            // 1. 无选中时的占位 Label
            _placeholderLabel = new Label("Please select a ScriptableObject type from the left list.")
            {
                style =
                {
                    flexGrow = 1,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    color = Color.gray
                }
            };
            container.Add(_placeholderLabel);

            // 2. 主内容容器
            _centerMainContent = new VisualElement { style = { flexGrow = 1, display = DisplayStyle.None } };
            container.Add(_centerMainContent);

            // 头部基本信息面板
            var infoBox = new VisualElement
            {
                style =
                {
                    paddingTop = 6,
                    paddingBottom = 6,
                    paddingLeft = 8,
                    paddingRight = 8,
                    backgroundColor = new Color(0.16f, 0.16f, 0.16f, 0.2f),
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0.1f, 0.1f, 0.1f, 0.3f)
                }
            };
            _centerMainContent.Add(infoBox);

            _typeHeaderLabel = new Label { style = { fontSize = 15, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } };
            infoBox.Add(_typeHeaderLabel);

            var pathRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
            infoBox.Add(pathRow);

            pathRow.Add(new Label("Default Save Path: ") { style = { color = Color.gray } });
            _typeFolderLabel = new Label { style = { unityFontStyleAndWeight = FontStyle.Bold, marginRight = 6 } };
            pathRow.Add(_typeFolderLabel);

            var btnBrowseFolder = new Button(OnBrowseDefaultFolderClick)
            {
                style =
                {
                    width = 24,
                    height = 18,
                    backgroundImage = new StyleBackground(EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D),
                    borderTopWidth = 0, borderRightWidth = 0, borderBottomWidth = 0, borderLeftWidth = 0,
                    backgroundColor = StyleKeyword.Null
                }
            };
            pathRow.Add(btnBrowseFolder);

            _instanceCountLabel = new Label { style = { fontSize = 10, color = Color.gray } };
            infoBox.Add(_instanceCountLabel);

            // 3. 构建中栏工具栏
            CreateCenterToolbar(_centerMainContent);

            // 4. 构建数据表格 (MultiColumnListView)
            _table = new MultiColumnListView
            {
                style = { flexGrow = 1 },
                viewDataKey = "CwcSOManagerTable",
                itemsSource = _filteredInstances,
                sortingMode = ColumnSortingMode.Custom,
                fixedItemHeight = 22,
                selectionType = SelectionType.Multiple
            };
            _table.selectionChanged += OnTableSelectionChanged;
            _table.columnSortingChanged += OnTableColumnSortingChanged;
            _table.AddManipulator(new ContextualMenuManipulator(OnTableContextualMenuPopulate));
            _table.RegisterCallback<KeyDownEvent>(OnTableKeyDown);
            _centerMainContent.Add(_table);
        }

        private void CreateCenterToolbar(VisualElement container)
        {
            var toolbar = new Toolbar();
            container.Add(toolbar);

            var btnNew = new Button(() =>
            {
                if (_selectedType == null) return;
                string folderPath = GetDefaultSaveFolderPath(_selectedType);
                var newAsset = CwcSOManagerHelper.CreateAndSaveAsset(_selectedType, folderPath, $"{_selectedType.Name}");
                RefreshInstances();
                _table.RefreshItems();
                SelectInstance(newAsset);
                SelectInstanceInTable(newAsset);
            }) { text = "Create" };
            toolbar.Add(btnNew);

            var btnCreateAt = new Button(() =>
            {
                if (_selectedType == null) return;
                string folderPath = GetDefaultSaveFolderPath(_selectedType);
                string targetFile = EditorUtility.SaveFilePanelInProject($"Choose Save Path for New {_selectedType.Name}", _selectedType.Name, "asset", "", folderPath);
                if (!string.IsNullOrEmpty(targetFile))
                {
                    string targetDir = Path.GetDirectoryName(targetFile).Replace("\\", "/");
                    string assetName = Path.GetFileNameWithoutExtension(targetFile);
                    var newAsset = CwcSOManagerHelper.CreateAndSaveAsset(_selectedType, targetDir, assetName);
                    RefreshInstances();
                    _table.RefreshItems();
                    SelectInstance(newAsset);
                    SelectInstanceInTable(newAsset);
                }
            }) { text = "Create At..." };
            toolbar.Add(btnCreateAt);

            var btnOrganize = new Button(() =>
            {
                if (_selectedType == null) return;
                string folderPath = GetDefaultSaveFolderPath(_selectedType);
                bool confirm = EditorUtility.DisplayDialog("Confirm Organization",
                    $"This action will move all {_selectedType.Name} instances in the project to the following folder:\n\n{folderPath}\n\nDo you want to proceed?",
                    "Proceed", "Cancel");
                if (confirm)
                {
                    CwcSOManagerHelper.MoveAllSOInstancesToDefaultFolder(_selectedType, folderPath);
                    RefreshInstances();
                    _table.RefreshItems();
                }
            }) { text = "Organize Assets" };
            toolbar.Add(btnOrganize);

            // 弹性填充器
            var spacer = new VisualElement { style = { flexGrow = 1, flexShrink = 1, minWidth = 10 } };
            toolbar.Add(spacer);

            var filterLabel = new Label("Filter: ") { style = { unityTextAlign = TextAnchor.MiddleLeft, flexShrink = 1 } };
            toolbar.Add(filterLabel);

            var searchField = new ToolbarSearchField { style = { flexGrow = 1, flexShrink = 1, minWidth = 40, maxWidth = 140 } };
            searchField.RegisterValueChangedCallback(evt =>
            {
                _centerSearchText = evt.newValue;
                ApplyFilterAndRefreshTable();
            });
            toolbar.Add(searchField);
        }

        #endregion

        #region 右侧检查器区域构建

        private void BuildRightArea(VisualElement container)
        {
            // 显式包裹在 ScrollView 中，设为自动显示（Auto），仅在超出时显示对应的拖拽条，符合原生习惯
            _inspectorScrollView = new ScrollView
            {
                style = 
                { 
                    flexGrow = 1,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 6,
                    paddingBottom = 6
                },
                horizontalScrollerVisibility = ScrollerVisibility.Auto,
                verticalScrollerVisibility = ScrollerVisibility.Auto
            };
            container.Add(_inspectorScrollView);

            SelectInstance(null);
        }

        #endregion

        #region 类型与实例管理逻辑

        private void RefreshPathTree()
        {
            _pathTreeRoot = CwcSOManagerHelper.BuildPathTree();
        }

        private void SelectType(Type type)
        {
            _selectedType = type;
            _centerSearchText = "";
            _serializedObjectCache.Clear();

            // 显式清空表格的选择状态，防止切换类型后选中状态残留导致无法响应点击的问题
            if (_table != null)
            {
                _table.ClearSelection();
            }
            _table.columns.Clear();

            if (_selectedType != null)
            {
                EditorPrefs.SetString("CwcSOManager_LastSelectedType", _selectedType.AssemblyQualifiedName);
                _typeHeaderLabel.text = _selectedType.Name;
                _typeFolderLabel.text = GetDefaultSaveFolderPath(_selectedType);

                RefreshInstances();
                _cachedColumns = CwcSOManagerHelper.GetCachedColumns(_selectedType);

                // 根据是否包含缩略图字段动态计算表格行高 (跟原版 IMGUI 对齐)
                float tableRowHeight = 22f;
                foreach (var col in _cachedColumns)
                {
                    if (col.IsPreview)
                    {
                        tableRowHeight = Mathf.Max(tableRowHeight, col.Width - 10f);
                    }
                }
                _table.fixedItemHeight = tableRowHeight + 2f;

                _placeholderLabel.style.display = DisplayStyle.None;
                _centerMainContent.style.display = DisplayStyle.Flex;

                // 动态构建列结构
                BuildAssetNameColumn();
                BuildPropertyColumns();

                _table.Rebuild();
            }
            else
            {
                EditorPrefs.SetString("CwcSOManager_LastSelectedType", "");
                _instances.Clear();
                _filteredInstances.Clear();
                _cachedColumns.Clear();
                _table.Rebuild();

                _placeholderLabel.style.display = DisplayStyle.Flex;
                _centerMainContent.style.display = DisplayStyle.None;
                SelectInstance(null);
            }
        }

        private void RefreshInstances()
        {
            if (_selectedType == null)
            {
                _instances.Clear();
                _filteredInstances.Clear();
                return;
            }

            _instances = CwcSOManagerHelper.FindAllInstances(_selectedType);
            
            // 优化：不再全量预先创建所有实例的 SerializedObject。
            // 延迟到需要显示时才懒加载创建，减少大型项目切换类型时的性能开销。
            _serializedObjectCache.Clear();

            ApplySortAndFilter();
            _instanceCountLabel.text = $"Total: {_instances.Count} instances";
        }

        private void ApplySortAndFilter()
        {
            _filteredInstances.Clear();
            if (string.IsNullOrEmpty(_centerSearchText))
            {
                _filteredInstances.AddRange(_instances);
            }
            else
            {
                _filteredInstances.AddRange(_instances.Where(asset => asset != null && asset.name.Contains(_centerSearchText, StringComparison.OrdinalIgnoreCase)));
            }

            CwcSOManagerHelper.SortInstances(_filteredInstances, _selectedType, _sortKey, _sortAscending);
        }

        private void ApplyFilterAndRefreshTable()
        {
            ApplySortAndFilter();
            _table.RefreshItems();
        }

        private void SelectInstance(ScriptableObject asset)
        {
            if (asset != null)
            {
                SelectInstances(new[] { asset });
            }
            else
            {
                SelectInstances(Enumerable.Empty<ScriptableObject>());
            }
        }

        private void SelectInstances(IEnumerable<ScriptableObject> assets)
        {
            _selectedInstances.Clear();
            if (assets != null)
            {
                _selectedInstances.AddRange(assets.Where(a => a != null));
            }

            // 同步给 Unity 编辑器 Selection.objects，以便在 Project View / Hierarchy 同步高亮
            Selection.objects = _selectedInstances.ToArray();

            _inspectorScrollView.Clear();

            if (_cachedEditor != null)
            {
                DestroyImmediate(_cachedEditor);
                _cachedEditor = null;
            }

            if (_selectedInstances.Count > 0)
            {
                string path = AssetDatabase.GetAssetPath(_selectedInstances[0]);
                EditorPrefs.SetString("CwcSOManager_LastSelectedInstancePath", path);

                // 使用 IMGUIContainer 完美桥接 Unity 原生 Editor 绘制（原生支持多选 Inspector）
                var imguiContainer = new IMGUIContainer(DrawIMGUIInspector)
                {
                    style = { flexGrow = 1 }
                };
                _inspectorScrollView.Add(imguiContainer);
            }
            else
            {
                EditorPrefs.SetString("CwcSOManager_LastSelectedInstancePath", "");
                var placeholder = new Label("Select one or more instances to view properties.")
                {
                    style =
                    {
                        flexGrow = 1,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        color = Color.gray,
                        fontSize = 13,
                        marginTop = 60
                    }
                };
                _inspectorScrollView.Add(placeholder);
            }
        }

        private void DrawIMGUIInspector()
        {
            if (_selectedInstances == null || _selectedInstances.Count == 0) return;

            // 检查缓存的 Editor 是否有效，或者 targets 是否改变
            bool targetsMatch = _cachedEditor != null
                && _cachedEditor.targets != null
                && _cachedEditor.targets.Length == _selectedInstances.Count
                && !_selectedInstances.Where((t, i) => i >= _cachedEditor.targets.Length || _cachedEditor.targets[i] != t).Any();

            if (!targetsMatch)
            {
                if (_cachedEditor != null) DestroyImmediate(_cachedEditor);
                _cachedEditor = Editor.CreateEditor(_selectedInstances.ToArray());
            }

            if (_cachedEditor != null)
            {
                _cachedEditor.DrawHeader();
                EditorGUI.BeginChangeCheck();
                _cachedEditor.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var instance in _selectedInstances)
                    {
                        if (instance != null)
                        {
                            EditorUtility.SetDirty(instance);
                        }
                    }
                }
            }
        }

        #endregion

        #region 列定义与数据绑定 (Rebuild Columns)

        private void BuildAssetNameColumn()
        {
            var assetNameColumn = new Column
            {
                title = "Asset Name",
                name = "Asset Name",
                width = 120,
                sortable = true,
                makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft, overflow = Overflow.Hidden, paddingLeft = 10 } },
                bindCell = (VisualElement element, int index) =>
                {
                    if (index >= _filteredInstances.Count) return;
                    var asset = _filteredInstances[index];
                    var label = element as Label;
                    if (asset == null) return;

                    label.text = asset.name;
                },
                unbindCell = (VisualElement element, int index) => {}
            };

            _table.columns.Add(assetNameColumn);
        }



        private void BuildPropertyColumns()
        {
            foreach (var col in _cachedColumns)
            {
                var fieldName = col.Field.Name;
                var column = new Column
                {
                    title = col.DisplayName,
                    name = fieldName,
                    width = col.Width,
                    sortable = true
                };

                if (col.IsPreview)
                {
                    column.makeCell = () => new CwcPreviewElement(col.Width - 10f);
                    column.bindCell = (VisualElement element, int index) =>
                    {
                        if (index >= _filteredInstances.Count) return;
                        var asset = _filteredInstances[index];
                        if (asset == null) return;

                        var serializedObject = GetOrCreateSerializedObject(asset);
                        var property = serializedObject.FindProperty(fieldName);
                        if (property != null)
                        {
                            var previewElement = element as CwcPreviewElement;
                            previewElement.Type = CwcPreviewElement.GetPropertyType(property);
                            previewElement.BindProperty(property);
                        }
                    };
                    column.unbindCell = (VisualElement element, int index) =>
                    {
                        (element as CwcPreviewElement).Unbind();
                    };
                }
                else
                {
                    column.makeCell = () =>
                    {
                        var cell = new VisualElement();
                        cell.style.justifyContent = Justify.Center;
                        cell.style.flexGrow = 1;

                        var propField = new PropertyField { label = "" };
                        propField.style.height = 18;
                        cell.Add(propField);
                        return cell;
                    };
                    column.bindCell = (VisualElement element, int index) =>
                    {
                        if (index >= _filteredInstances.Count) return;
                        var asset = _filteredInstances[index];
                        if (asset == null) return;

                        var serializedObject = GetOrCreateSerializedObject(asset);
                        var property = serializedObject.FindProperty(fieldName);
                        if (property != null)
                        {
                            var propField = element.Q<PropertyField>();
                            propField.BindProperty(property);
                        }
                    };
                    column.unbindCell = (VisualElement element, int index) =>
                    {
                        element.Q<PropertyField>().Unbind();
                    };
                }

                _table.columns.Add(column);
            }
        }

        #endregion

        #region 事件与辅助方法

        private SerializedObject GetOrCreateSerializedObject(ScriptableObject asset)
        {
            if (asset == null) return null;
            if (!_serializedObjectCache.TryGetValue(asset, out var serializedObject) || serializedObject.targetObject == null)
            {
                serializedObject = new SerializedObject(asset);
                _serializedObjectCache[asset] = serializedObject;
            }
            return serializedObject;
        }

        private void OnBrowseDefaultFolderClick()
        {
            if (_selectedType == null) return;

            string folderPath = GetDefaultSaveFolderPath(_selectedType);
            var newPath = EditorUtility.OpenFolderPanel($"Choose Default Save Folder for {_selectedType.Name}", folderPath, "");
            if (!string.IsNullOrEmpty(newPath))
            {
                newPath = CwcSOManagerHelper.GlobalPathToLocal(newPath);
                if (!string.IsNullOrEmpty(newPath))
                {
                    SetOverrideSaveFolderPath(_selectedType, newPath);
                    _typeFolderLabel.text = newPath;
                }
            }
        }

        private void OnTableSelectionChanged(IEnumerable<object> selectedItems)
        {
            var items = selectedItems != null ? selectedItems.OfType<ScriptableObject>().ToList() : new List<ScriptableObject>();
            SelectInstances(items);
        }

        private void OnTableKeyDown(KeyDownEvent evt)
        {
            // 若当前焦点处于文本输入组件中，避免响应表格快捷键，防止误删或误全选
            var focusElement = rootVisualElement.focusController?.focusedElement;
            if (focusElement is TextInputBaseField<string> || focusElement is TextElement || (focusElement != null && focusElement.GetType().Name.Contains("TextInput")))
            {
                return;
            }

            // Ctrl+A / Cmd+A 全选
            if (evt.actionKey && evt.keyCode == KeyCode.A)
            {
                if (_filteredInstances.Count > 0 && _table != null)
                {
                    _table.SetSelection(Enumerable.Range(0, _filteredInstances.Count));
                    evt.StopPropagation();
                }
            }
            // Delete 或 Backspace 批量删除（兼顾 Windows 与 macOS）
            else if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                DeleteSelectedAssets();
                evt.StopPropagation();
            }
            // Ctrl+D / Cmd+D 批量复制/生成副本
            else if (evt.actionKey && evt.keyCode == KeyCode.D)
            {
                CopySelectedAssets();
                evt.StopPropagation();
            }
            // F2 重命名（单选时生效）
            else if (evt.keyCode == KeyCode.F2)
            {
                if (_selectedInstances.Count == 1)
                {
                    RenameAsset(_selectedInstances[0]);
                    evt.StopPropagation();
                }
            }
        }

        private void OnTableColumnSortingChanged()
        {
            var sortedColumn = _table.sortedColumns.FirstOrDefault();
            if (sortedColumn == null) return;

            bool ascending = sortedColumn.direction == SortDirection.Ascending;
            _sortKey = sortedColumn.columnName;
            _sortAscending = ascending;

            ApplySortAndFilter();
            _table.RefreshItems();
        }

        private string GetDefaultSaveFolderPath(Type type)
        {
            if (type == null) return "";

            string prefKey = $"CwcSOManager_FolderOverride_{type.FullName}";
            string savedOverride = EditorPrefs.GetString(prefKey, "");
            if (!string.IsNullOrEmpty(savedOverride))
            {
                return savedOverride;
            }

            var attr = type.GetCustomAttribute<CwcSOManageableAttribute>();
            if (attr != null && !string.IsNullOrEmpty(attr.AssetsFolder))
            {
                return attr.AssetsFolder;
            }

            string path = attr != null ? attr.Path : "Uncategorized";
            return $"Assets/Data/{path}/{type.Name}";
        }

        private void SetOverrideSaveFolderPath(Type type, string path)
        {
            if (type == null) return;
            string prefKey = $"CwcSOManager_FolderOverride_{type.FullName}";
            EditorPrefs.SetString(prefKey, path);
        }

        private void RestoreLastSelection()
        {
            string lastTypeName = EditorPrefs.GetString("CwcSOManager_LastSelectedType", "");
            if (!string.IsNullOrEmpty(lastTypeName))
            {
                Type type = Type.GetType(lastTypeName);
                if (type != null)
                {
                    SelectType(type);
                    SelectTypeInTreeView(type);
                }
            }

            string lastInstancePath = EditorPrefs.GetString("CwcSOManager_LastSelectedInstancePath", "");
            if (!string.IsNullOrEmpty(lastInstancePath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(lastInstancePath);
                if (asset != null)
                {
                    SelectInstances(new[] { asset });
                    SelectInstanceInTable(asset);
                }
            }
        }

        private void SelectTypeInTreeView(Type type)
        {
            if (type == null) return;
            if (_typeToTreeItemId.TryGetValue(type, out int id))
            {
                _treeView.SetSelection(id);
            }
        }

        private void SelectInstanceInTable(ScriptableObject asset)
        {
            if (asset == null || _table == null) return;
            int index = _filteredInstances.IndexOf(asset);
            if (index >= 0)
            {
                _table.SetSelection(index);
            }
        }

        private void SelectInstancesInTable(IEnumerable<ScriptableObject> assets)
        {
            if (assets == null || _table == null) return;
            var indices = assets.Select(a => _filteredInstances.IndexOf(a)).Where(idx => idx >= 0).ToList();
            if (indices.Count > 0)
            {
                _table.SetSelection(indices);
            }
        }

        /// <summary>
        /// 填充右键上下文菜单
        /// </summary>
        private void OnTableContextualMenuPopulate(ContextualMenuPopulateEvent evt)
        {
            if (_selectedInstances.Count == 0) return;

            if (_selectedInstances.Count == 1)
            {
                var asset = _selectedInstances[0];
                evt.menu.AppendAction("Rename", (a) => RenameAsset(asset));
                evt.menu.AppendAction("Copy", (a) => CopySelectedAssets());
                evt.menu.AppendAction("Ping", (a) => PingSelectedAssets());
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Delete", (a) => DeleteSelectedAssets());
            }
            else
            {
                evt.menu.AppendAction($"Copy Selection ({_selectedInstances.Count})", (a) => CopySelectedAssets());
                evt.menu.AppendAction($"Ping Selection ({_selectedInstances.Count})", (a) => PingSelectedAssets());
                evt.menu.AppendSeparator();
                evt.menu.AppendAction($"Delete Selected ({_selectedInstances.Count})", (a) => DeleteSelectedAssets());
            }
        }

        /// <summary>
        /// 重命名资产的辅助逻辑
        /// </summary>
        private void RenameAsset(ScriptableObject asset)
        {
            if (asset == null) return;
            RenamePopup.Show(asset, () =>
            {
                RefreshInstances();
                if (_table != null)
                {
                    _table.RefreshItems();
                }
            });
        }

        /// <summary>
        /// 批量复制选中资产的辅助逻辑
        /// </summary>
        private void CopySelectedAssets()
        {
            var targets = _selectedInstances.Where(a => a != null).ToList();
            if (targets.Count == 0 || _selectedType == null) return;

            string folderPath = GetDefaultSaveFolderPath(_selectedType);
            var createdAssets = new List<ScriptableObject>();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var asset in targets)
                {
                    var newAsset = CwcSOManagerHelper.CreateAndSaveAsset(_selectedType, folderPath, $"{asset.name}_Copy", asset);
                    if (newAsset != null)
                    {
                        createdAssets.Add(newAsset);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            RefreshInstances();
            if (_table != null)
            {
                _table.RefreshItems();
            }

            if (createdAssets.Count > 0)
            {
                SelectInstances(createdAssets);
                SelectInstancesInTable(createdAssets);
            }
        }

        /// <summary>
        /// 定位/高亮选中资产的辅助逻辑
        /// </summary>
        private void PingSelectedAssets()
        {
            if (_selectedInstances.Count == 0) return;
            if (_selectedInstances.Count == 1)
            {
                EditorGUIUtility.PingObject(_selectedInstances[0]);
            }
            else
            {
                Selection.objects = _selectedInstances.ToArray();
            }
        }

        /// <summary>
        /// 批量删除选中资产的辅助逻辑
        /// </summary>
        private void DeleteSelectedAssets()
        {
            var targets = _selectedInstances.Where(a => a != null).ToList();
            if (targets.Count == 0) return;

            string message = targets.Count == 1
                ? $"Are you sure you want to delete asset '{targets[0].name}'?"
                : $"Are you sure you want to delete {targets.Count} selected assets?";

            bool confirm = EditorUtility.DisplayDialog("Confirm Delete", message, "Yes", "No");
            if (confirm)
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var asset in targets)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(asset);
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            AssetDatabase.DeleteAsset(assetPath);
                        }
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                SelectInstances(Enumerable.Empty<ScriptableObject>());
                RefreshInstances();
                if (_table != null)
                {
                    _table.RefreshItems();
                }
            }
        }

        /// <summary>
        /// 简易重命名弹出小窗口
        /// </summary>
        private class RenamePopup : EditorWindow
        {
            #region 非序列化私有字段

            private string _newName;
            private ScriptableObject _asset;
            private Action _onSuccess;

            #endregion

            #region 公共方法

            public static void Show(ScriptableObject asset, Action onSuccess)
            {
                var window = CreateInstance<RenamePopup>();
                window.titleContent = new GUIContent("Rename Asset");
                window._asset = asset;
                window._newName = asset.name;
                window._onSuccess = onSuccess;
                window.minSize = new Vector2(280, 95);
                window.maxSize = new Vector2(280, 95);
                
                // 将弹窗位置定位到鼠标点击位置附近
                Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current != null ? Event.current.mousePosition : Vector2.zero);
                window.position = new Rect(mousePos.x - 140, mousePos.y - 48, 280, 95);
                window.ShowUtility();
            }

            #endregion

            #region 生命周期方法

            private void OnGUI()
            {
                GUILayout.Space(8);
                GUILayout.Label("New Name:", EditorStyles.boldLabel);
                
                // 不带 Label 标签的 TextField 会自动撑满当前行，提供最充裕的输入宽度
                _newName = EditorGUILayout.TextField(_newName);
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                // 支持点击按钮或按下 Enter 确认重命名
                if (GUILayout.Button("Confirm") || (Event.current != null && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
                {
                    if (!string.IsNullOrEmpty(_newName) && _newName != _asset.name)
                    {
                        string path = AssetDatabase.GetAssetPath(_asset);
                        string error = AssetDatabase.RenameAsset(path, _newName);
                        if (string.IsNullOrEmpty(error))
                        {
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            _onSuccess?.Invoke();
                            Close();
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error", error, "OK");
                        }
                    }
                    else
                    {
                        Close();
                    }
                }

                if (GUILayout.Button("Cancel"))
                {
                    Close();
                }
                GUILayout.EndHorizontal();
            }

            #endregion
        }

        private void Update()
        {
            // 限频：每 200 毫秒（0.2 秒）统一对缓存中的 SerializedObject 执行一次 Update，
            // 采用复用成员变量 _tempUpdateList 的方式，彻底消除周期性的 new List 垃圾内存分配，防止 GC 抖动。
            if (EditorApplication.timeSinceStartup - _lastUpdateTime > 0.2)
            {
                _lastUpdateTime = EditorApplication.timeSinceStartup;

                if (_serializedObjectCache.Count > 0)
                {
                    _tempUpdateList.Clear();
                    foreach (var so in _serializedObjectCache.Values)
                    {
                        if (so != null && so.targetObject != null)
                        {
                            _tempUpdateList.Add(so);
                        }
                    }

                    for (int i = 0; i < _tempUpdateList.Count; i++)
                    {
                        _tempUpdateList[i].Update();
                    }
                    _tempUpdateList.Clear();
                }
            }
        }

        private void OnTreeViewItemExpandedChanged(TreeViewExpansionChangedArgs args)
        {
            if (args == null || !args.isExpanded) return;
            if (_isExpandingAll) return;
            if (_treeView == null) return;

            _isExpandingAll = true;
            try
            {
                _treeView.ExpandItem(args.id, true);
            }
            finally
            {
                _isExpandingAll = false;
            }
        }

        #endregion
    }

    #region 树节点与自定义预览元素数据结构

    public class CwcTreeItem
    {
        public string Name;
        public string FullPath;
        public Type Type;
        public bool IsType => Type != null;
    }

    /// <summary>
    /// 支持在数据表内进行大缩略图展示，并支持拖拽直接赋值和双向绑定的自定义 UI Toolkit 元素。
    /// </summary>
    public class CwcPreviewElement : BindableElement, INotifyValueChanged<UnityEngine.Object>
    {
        private readonly Image _previewImage;
        private readonly ObjectField _objectField;
        private Type _type = typeof(UnityEngine.Object);
        private UnityEngine.Object _value;
        private SerializedProperty _property;

        public UnityEngine.Object value
        {
            get => _value;
            set
            {
                if (value == _value)
                    return;

                var previous = _value;
                SetValueWithoutNotify(value);

                using var evt = ChangeEvent<UnityEngine.Object>.GetPooled(previous, value);
                evt.target = this;
                SendEvent(evt);
            }
        }

        public Type Type
        {
            get => _type;
            set
            {
                _type = value ?? typeof(UnityEngine.Object);
                _objectField.objectType = _type;
            }
        }

        public CwcPreviewElement(float size)
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;

            float imgSize = Mathf.Max(20f, size - 4f);

            _previewImage = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    width = imgSize,
                    height = imgSize,
                    backgroundColor = new Color(42f / 255f, 42f / 255f, 42f / 255f),
                    borderTopColor = Color.black,
                    borderRightColor = Color.black,
                    borderBottomColor = Color.black,
                    borderLeftColor = Color.black,
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                }
            };
            Add(_previewImage);

            _objectField = new ObjectField
            {
                allowSceneObjects = false,
                style = { width = 18, height = 18, marginLeft = -18, marginBottom = 0 }
            };
            _objectField.RegisterValueChangedCallback(OnObjectFieldValueChanged);
            Add(_objectField);

            // 注册 Drag & Drop 原生回调
            RegisterCallback<DragEnterEvent>(OnDragEnter);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
        }

        public void BindProperty(SerializedProperty property)
        {
            _property = property;
            UnityEditor.UIElements.BindingExtensions.BindProperty(this, property);
        }

        public void SetValueWithoutNotify(UnityEngine.Object newValue)
        {
            _value = newValue;
            _objectField.SetValueWithoutNotify(_value);

            if (newValue is Sprite sprite)
            {
                _previewImage.image = null;
                _previewImage.sprite = sprite;
            }
            else if (newValue is Texture2D t2d)
            {
                _previewImage.sprite = null;
                _previewImage.image = t2d;
            }
            else if (newValue != null)
            {
                _previewImage.sprite = null;
                _previewImage.image = AssetPreview.GetAssetPreview(newValue);
            }
            else
            {
                _previewImage.sprite = null;
                _previewImage.image = null;
            }
        }

        public static Type GetPropertyType(SerializedProperty property)
        {
            if (property == null) return typeof(UnityEngine.Object);

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                string typeName = property.type;
                if (typeName.Contains("Sprite"))
                {
                    return typeof(Sprite);
                }
                if (typeName.Contains("Texture"))
                {
                    return typeof(Texture);
                }
                if (typeName.Contains("GameObject"))
                {
                    return typeof(GameObject);
                }
            }
            return typeof(UnityEngine.Object);
        }

        private void OnObjectFieldValueChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            UpdatePropertyValue(evt.newValue);
        }

        private void OnDragEnter(DragEnterEvent evt)
        {
            if (IsValidDrag())
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            }
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (IsValidDrag())
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            }
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            if (IsValidDrag())
            {
                DragAndDrop.AcceptDrag();
                var validObj = GetValidDraggedObject();
                if (validObj != null)
                {
                    UpdatePropertyValue(validObj);
                }
                evt.StopPropagation();
            }
        }

        private bool IsValidDrag()
        {
            if (DragAndDrop.objectReferences.Length == 0) return false;
            var obj = DragAndDrop.objectReferences[0];
            return TryGetCompatibleObject(obj, out _);
        }

        private UnityEngine.Object GetValidDraggedObject()
        {
            if (DragAndDrop.objectReferences.Length == 0) return null;
            TryGetCompatibleObject(DragAndDrop.objectReferences[0], out var validObj);
            return validObj;
        }

        private bool TryGetCompatibleObject(UnityEngine.Object dragged, out UnityEngine.Object compatible)
        {
            compatible = null;
            if (dragged == null) return false;

            var targetType = _type ?? typeof(UnityEngine.Object);

            // 1. 直接匹配类型
            if (targetType.IsInstanceOfType(dragged))
            {
                compatible = dragged;
                return true;
            }

            // 2. 如果目标是 Sprite，拖入的是 Texture2D 资源
            if (targetType == typeof(Sprite) && dragged is Texture2D)
            {
                string path = AssetDatabase.GetAssetPath(dragged);
                if (!string.IsNullOrEmpty(path))
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null)
                    {
                        compatible = sprite;
                        return true;
                    }
                    var allSubAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var asset in allSubAssets)
                    {
                        if (asset is Sprite s)
                        {
                            compatible = s;
                            return true;
                        }
                    }
                }
            }

            // 3. 如果目标是 Texture/Texture2D，拖入的是 Sprite 资源
            if ((targetType == typeof(Texture) || targetType == typeof(Texture2D)) && dragged is Sprite spriteAsset)
            {
                if (spriteAsset.texture != null)
                {
                    compatible = spriteAsset.texture;
                    return true;
                }
            }

            // 4. 通用 Object 类型
            if (targetType == typeof(UnityEngine.Object))
            {
                compatible = dragged;
                return true;
            }

            return false;
        }

        private void UpdatePropertyValue(UnityEngine.Object newValue)
        {
            value = newValue;
            if (_property != null && _property.serializedObject != null)
            {
                _property.objectReferenceValue = newValue;
                _property.serializedObject.ApplyModifiedProperties();
            }
        }
    }

    #endregion
}
