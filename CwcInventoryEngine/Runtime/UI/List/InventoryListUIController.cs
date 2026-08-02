using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Cwc.InventoryEngine.UI
{
    /// <summary>
    /// 静态/滑动列表布局的主轴方向。
    /// </summary>
    public enum InventoryListLayoutDirection
    {
        /// <summary>
        /// 纵向主轴（常规列表）：上下按行滚动，左右同行切换。
        /// </summary>
        Vertical,

        /// <summary>
        /// 横向主轴：左右按列滚动，上下同列切换。
        /// </summary>
        Horizontal
    }

    /// <summary>
    /// 背包 UI 列表的数据视图映射模式。
    /// </summary>
    public enum InventoryListMappingMode
    {
        /// <summary>
        /// 紧凑填补模式（Compact）：数据集中靠前/向上填补排列在最前面，多余的槽位仅渲染空背景框。
        /// </summary>
        Compact,

        /// <summary>
        /// 槽位直接映射模式（DirectMapping）：UI 格子与背包容器 SlotIndex 严格 1:1 物理映射，空槽位在原位置保留背景框。
        /// </summary>
        DirectMapping
    }

    /// <summary>
    /// 通用库存滑动列表 UI 控制器。
    /// 基于数据窗口平移（Data Window Sliding）机制，
    /// 无需耗费性能的动态 ScrollRect 实例化，完美支持 1D 纵向列表与 2D 矩阵网格。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/UI/Inventory List UI Controller")]
    public class InventoryListUIController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("列表视图配置")]
        [SerializeField]
        [Tooltip("包含所有列表单元 (实现了 IInventoryListItem) 的 Transform 根节点。控制器自动查找其直接子对象。")]
        private Transform _contentRoot;

        [SerializeField]
        [Tooltip("列表主轴布局方向：Vertical 纵向（上下滚动），Horizontal 横向（左右滚动）。")]
        private InventoryListLayoutDirection _layoutDirection = InventoryListLayoutDirection.Vertical;

        [SerializeField]
        [Tooltip("交叉轴容量（列数/行数）：纵向布局时表示每行有几列 (Columns)；1 表示单列纵向列表；>1 表示网格模式。")]
        [Min(1)]
        private int _columnsCount = 1;

        [SerializeField]
        [Tooltip("边界触发平移距离：在网格/列表模式下，当选中单元靠近可视边缘此行数时，自动平移数据窗口。")]
        [Min(0)]
        private int _boundaryDistance = 1;

        [SerializeField]
        [Tooltip("是否允许导航循环（到达顶部再按上跳至底部）。")]
        private bool _enableLooping = false;

        [SerializeField]
        [Tooltip("到达列表边缘（如第一列/最后一列、第一行/最后一行）继续移动时是否触发列表翻页。若开启，在边缘按方向键触发上一页/下一页；若关闭，按键将在同行/异行间连续折行导航。")]
        private bool _enableEdgePaging = true;

        [SerializeField]
        [Tooltip("数据视图映射模式：\n- Compact (紧凑填补模式)：数据集中向上靠前排列，多余槽位渲染为空背景框。\n- DirectMapping (槽位一对一物理映射模式)：UI 格子与背包 Container 的 SlotIndex 严格一对一物理映射。")]
        private InventoryListMappingMode _mappingMode = InventoryListMappingMode.Compact;

        [Header("事件监听")]
        [SerializeField]
        private UnityEvent<int, object> _onSelectionChangedEvent;

        [SerializeField]
        private UnityEvent<int, object> _onItemSubmittedEvent;

        [SerializeField]
        [Tooltip("当列表内容刷新（滑动、翻页或数据更新）时触发的事件")]
        private UnityEvent _onContentUpdatedEvent;
        #endregion

        #region Private Fields
        private readonly List<IInventoryListItem> _managedItems = new();
        private System.Collections.IList _currentDataSource;
        private int _selectedVisualIndex = 0; // 当前在可视 UI 单元列表中的索引 (0 ~ ManagedItemsCount-1)
        private int _dataStartIndex = 0;      // 窗口对应的全局数据源起始索引
        private bool _isInitialized = false;
        private bool _isExternalDataSource = false; // 标记数据源是否由外部 View (如 MainInventoryView) 显式托管设置

        // 策略模式：通用的槽位筛选谓词策略与绑定的容器引用
        private Predicate<ItemSlot> _activeFilter;
        private IReadOnlyInventoryContainer _boundContainer;
        private string _boundInventoryId;
        private readonly List<ItemSlot> _filteredSlotsBuffer = new();
        #endregion

        #region Events & Action Properties
        /// <summary>
        /// 当焦点选中项改变时触发的 Action：(dataIndex, dataObject)
        /// </summary>
        public event Action<int, object> OnSelectionChanged;

        /// <summary>
        /// 当选中项被确认提交时触发的 Action：(dataIndex, dataObject)
        /// </summary>
        public event Action<int, object> OnItemSubmitted;

        /// <summary>
        /// 当列表内容刷新或翻页平移时触发的 Action。
        /// </summary>
        public event Action OnContentUpdated;
        #endregion

        #region Public Properties
        /// <summary>
        /// 布局的主轴方向。
        /// </summary>
        public InventoryListLayoutDirection LayoutDirection
        {
            get => _layoutDirection;
            set => _layoutDirection = value;
        }

        /// <summary>
        /// 交叉轴单元格容量（列数/行数）。
        /// </summary>
        public int ColumnsCount
        {
            get => Mathf.Max(1, _columnsCount);
            set => _columnsCount = Mathf.Max(1, value);
        }

        /// <summary>
        /// 到达列表边缘继续移动时是否自动触发上一页/下一页翻页。
        /// </summary>
        public bool EnableEdgePaging
        {
            get => _enableEdgePaging;
            set => _enableEdgePaging = value;
        }

        /// <summary>
        /// [已弃用] 请使用 <see cref="EnableEdgePaging"/>
        /// </summary>
        public bool AutoPageOnSingleCrossAxis
        {
            get => _enableEdgePaging;
            set => _enableEdgePaging = value;
        }

        /// <summary>
        /// 数据视图映射模式 (Compact 紧凑填补 vs DirectMapping 槽位物理映射)。
        /// </summary>
        public InventoryListMappingMode MappingMode
        {
            get => _mappingMode;
            set
            {
                _mappingMode = value;
                RefreshView();
            }
        }

        /// <summary>
        /// 可视 UI 单元格总总数。
        /// </summary>
        public int VisualCapacity => _managedItems.Count;

        /// <summary>
        /// 数据源中的元素总数。
        /// </summary>
        public int TotalDataCount => _currentDataSource != null ? _currentDataSource.Count : 0;

        /// <summary>
        /// 计算当前数据总页数。
        /// </summary>
        public int TotalPages => VisualCapacity > 0 ? Mathf.Max(1, Mathf.CeilToInt((float)TotalDataCount / VisualCapacity)) : 1;

        /// <summary>
        /// 当前选中项所在的页码索引（0-indexed）。
        /// </summary>
        public int CurrentPageIndex => VisualCapacity > 0 ? SelectedDataIndex / VisualCapacity : 0;

        /// <summary>
        /// 当前选中项所在的页码数字（1-indexed，适合 UI 直接显示）。
        /// </summary>
        public int CurrentPageNumber => CurrentPageIndex + 1;

        /// <summary>
        /// 当前全局选中项的数据索引。
        /// </summary>
        public int SelectedDataIndex => _dataStartIndex + _selectedVisualIndex;

        /// <summary>
        /// 当前全局选中项的数据对象。
        /// </summary>
        public object SelectedDataObject
        {
            get
            {
                int index = SelectedDataIndex;
                if (_currentDataSource != null && index >= 0 && index < _currentDataSource.Count)
                {
                    return _currentDataSource[index];
                }
                return null;
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeManagedItems();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 绑定目标 InventoryContainer 实体。绑定后控制器将自动自治响应容器更新与策略过滤！
        /// </summary>
        /// <param name="container">背包容器实体</param>
        /// <param name="inventoryId">可选的背包 ID 标识（如 MainInventory），用于广播事件精准比对</param>
        public virtual void BindContainer(IReadOnlyInventoryContainer container, string inventoryId = null)
        {
            _boundContainer = container;
            _boundInventoryId = inventoryId;
            _isExternalDataSource = false;
            RefreshView();
        }

        /// <summary>
        /// 设置通用的槽位筛选策略 (Predicate)。列表将在后续所有的底层事件更新中自动自治使用该策略！
        /// </summary>
        /// <param name="filter">筛选谓词委托 (传入 null 表示取消过滤策略)</param>
        public virtual void SetFilter(Predicate<ItemSlot> filter)
        {
            _activeFilter = filter;
            RefreshView();
        }

        /// <summary>
        /// 清除当前的筛选策略，恢复为全量展示。
        /// </summary>
        public virtual void ClearFilter()
        {
            _activeFilter = null;
            RefreshView();
        }

        /// <summary>
        /// 设置并绑定新的数据源。
        /// </summary>
        /// <param name="dataSource">数据源列表</param>
        /// <param name="resetSelection">是否重置选择到第 0 项</param>
        public virtual void SetDataSource(System.Collections.IList dataSource, bool resetSelection = true)
        {
            InitializeManagedItems();

            _currentDataSource = dataSource;
            _isExternalDataSource = true; // 标记数据源已由外部托管

            if (resetSelection || _currentDataSource == null || _currentDataSource.Count == 0)
            {
                _dataStartIndex = 0;
                _selectedVisualIndex = 0;
            }
            else
            {
                ClampSelectionBounds();
            }

            RefreshView();
            NotifySelectionChanged();
        }

        /// <summary>
        /// 响应方向向量导航输入。
        /// </summary>
        /// <param name="direction">方向向量 (X: -1/0/1, Y: -1/0/1)</param>
        /// <returns>若索引或视图发生了有效移动返回 true</returns>
        public virtual bool Navigate(Vector2Int direction)
        {
            if (_currentDataSource == null || _currentDataSource.Count == 0 || _managedItems.Count == 0) return false;
            if (direction == Vector2Int.zero) return false;

            int totalDataCount = _currentDataSource.Count;
            int stride = ColumnsCount;
            int currentDataIdx = SelectedDataIndex;

            // 计算当前可视窗口中的相对行列坐标
            int visualCap = _managedItems.Count;
            int visualRow = _selectedVisualIndex / stride;
            int visualCol = _selectedVisualIndex % stride;
            int maxVisualRow = Mathf.Max(0, (visualCap - 1) / stride);

            if (_layoutDirection == InventoryListLayoutDirection.Vertical)
            {
                // 纵向列表导航
                // 垂直方向 (Y)：向上 (Y > 0) / 向下 (Y < 0)
                if (direction.y > 0)
                {
                    if (visualRow == 0)
                    {
                        if (_enableEdgePaging) { PreviousPage(); return true; }
                        return false;
                    }
                    int targetIdx = currentDataIdx - stride;
                    if (targetIdx >= 0) { SetSelectedDataIndex(targetIdx); return true; }
                    else if (_enableEdgePaging) { PreviousPage(); return true; }
                }
                else if (direction.y < 0)
                {
                    if (visualRow >= maxVisualRow)
                    {
                        if (_enableEdgePaging) { NextPage(); return true; }
                        return false;
                    }
                    int targetIdx = currentDataIdx + stride;
                    if (targetIdx < totalDataCount) { SetSelectedDataIndex(targetIdx); return true; }
                    else if (_enableEdgePaging) { NextPage(); return true; }
                }

                // 水平方向 (X)：向右 (X > 0) / 向左 (X < 0)
                if (direction.x > 0)
                {
                    if (visualCol == stride - 1 || _selectedVisualIndex == visualCap - 1)
                    {
                        if (_enableEdgePaging) { NextPage(); return true; }
                        else { SelectNext(); return true; }
                    }
                    int targetIdx = currentDataIdx + 1;
                    if (targetIdx < totalDataCount) { SetSelectedDataIndex(targetIdx); return true; }
                    else if (_enableEdgePaging) { NextPage(); return true; }
                }
                else if (direction.x < 0)
                {
                    if (visualCol == 0)
                    {
                        if (_enableEdgePaging) { PreviousPage(); return true; }
                        else { SelectPrevious(); return true; }
                    }
                    int targetIdx = currentDataIdx - 1;
                    if (targetIdx >= 0) { SetSelectedDataIndex(targetIdx); return true; }
                    else if (_enableEdgePaging) { PreviousPage(); return true; }
                }
            }
            else
            {
                // 横向列表导航
                int visualRowHoriz = _selectedVisualIndex % stride;
                int visualColHoriz = _selectedVisualIndex / stride;
                int maxVisualColHoriz = Mathf.Max(0, (visualCap - 1) / stride);

                // 水平方向 (X)：向右 (X > 0) / 向左 (X < 0)
                if (direction.x > 0)
                {
                    if (visualColHoriz >= maxVisualColHoriz)
                    {
                        if (_enableEdgePaging) { NextPage(); return true; }
                        return false;
                    }
                    int targetIdx = currentDataIdx + stride;
                    if (targetIdx < totalDataCount) { SetSelectedDataIndex(targetIdx); return true; }
                    else if (_enableEdgePaging) { NextPage(); return true; }
                }
                else if (direction.x < 0)
                {
                    if (visualColHoriz == 0)
                    {
                        if (_enableEdgePaging) { PreviousPage(); return true; }
                        return false;
                    }
                    int targetIdx = currentDataIdx - stride;
                    if (targetIdx >= 0) { SetSelectedDataIndex(targetIdx); return true; }
                    else if (_enableEdgePaging) { PreviousPage(); return true; }
                }

                // 垂直方向 (Y)：向上 (Y > 0) / 向下 (Y < 0)
                if (direction.y > 0)
                {
                    if (visualRowHoriz == 0)
                    {
                        if (_enableEdgePaging) { PreviousPage(); return true; }
                        else { SelectPrevious(); return true; }
                    }
                    int targetIdx = currentDataIdx - 1;
                    if (targetIdx >= 0) { SetSelectedDataIndex(targetIdx); return true; }
                    else if (_enableEdgePaging) { PreviousPage(); return true; }
                }
                else if (direction.y < 0)
                {
                    if (visualRowHoriz == stride - 1 || _selectedVisualIndex == visualCap - 1)
                    {
                        if (_enableEdgePaging) { NextPage(); return true; }
                        else { SelectNext(); return true; }
                    }
                    int targetIdx = currentDataIdx + 1;
                    if (targetIdx < totalDataCount) { SetSelectedDataIndex(targetIdx); return true; }
                    else if (_enableEdgePaging) { NextPage(); return true; }
                }
            }

            return false;
        }

        #region 翻页与单步 API
        /// <summary>
        /// 下一页：按可视全屏网格容量平移数据窗口。若无法跨页，则直接跳至页面末尾或触发循环。
        /// </summary>
        public virtual void NextPage()
        {
            if (_currentDataSource == null || _currentDataSource.Count == 0 || _managedItems.Count == 0) return;

            int totalDataCount = _currentDataSource.Count;
            int visualCap = _managedItems.Count;

            // 1. 如果数据总数不超过一页容量（不够翻页）
            if (totalDataCount <= visualCap)
            {
                int lastIndex = totalDataCount - 1;
                if (SelectedDataIndex < lastIndex)
                {
                    SetSelectedDataIndex(lastIndex);
                }
                else if (_enableLooping)
                {
                    SetSelectedDataIndex(0);
                }
                return;
            }

            // 2. 正常跨页平移
            int pageStep = Mathf.Max(ColumnsCount, (visualCap / ColumnsCount) * ColumnsCount);
            int targetDataIndex = SelectedDataIndex + pageStep;

            if (targetDataIndex >= totalDataCount)
            {
                int lastIndex = totalDataCount - 1;
                if (SelectedDataIndex < lastIndex)
                {
                    SetSelectedDataIndex(lastIndex);
                }
                else if (_enableLooping)
                {
                    SetSelectedDataIndex(0);
                }
            }
            else
            {
                SetSelectedDataIndex(targetDataIndex);
            }
        }

        /// <summary>
        /// 上一页：按可视全屏网格容量平移数据窗口。若无法跨页，则直接跳至页面开头或触发循环。
        /// </summary>
        public virtual void PreviousPage()
        {
            if (_currentDataSource == null || _currentDataSource.Count == 0 || _managedItems.Count == 0) return;

            int totalDataCount = _currentDataSource.Count;
            int visualCap = _managedItems.Count;

            // 1. 如果数据总数不超过一页容量（不够翻页）
            if (totalDataCount <= visualCap)
            {
                if (SelectedDataIndex > 0)
                {
                    SetSelectedDataIndex(0);
                }
                else if (_enableLooping)
                {
                    SetSelectedDataIndex(totalDataCount - 1);
                }
                return;
            }

            // 2. 正常跨页平移
            int pageStep = Mathf.Max(ColumnsCount, (visualCap / ColumnsCount) * ColumnsCount);
            int targetDataIndex = SelectedDataIndex - pageStep;

            if (targetDataIndex < 0)
            {
                if (SelectedDataIndex > 0)
                {
                    SetSelectedDataIndex(0);
                }
                else if (_enableLooping)
                {
                    SetSelectedDataIndex(totalDataCount - 1);
                }
            }
            else
            {
                SetSelectedDataIndex(targetDataIndex);
            }
        }

        /// <summary>
        /// 选择下一个数据项。
        /// </summary>
        public virtual void SelectNext()
        {
            if (_currentDataSource == null || _currentDataSource.Count == 0) return;
            SetSelectedDataIndex(SelectedDataIndex + 1);
        }

        /// <summary>
        /// 选择上一个数据项。
        /// </summary>
        public virtual void SelectPrevious()
        {
            if (_currentDataSource == null || _currentDataSource.Count == 0) return;
            SetSelectedDataIndex(SelectedDataIndex - 1);
        }

        /// <summary>
        /// 向上选择单元格（可供 UI 按钮 UnityEvent 绑定）。
        /// </summary>
        public virtual void SelectUp() => Navigate(new Vector2Int(0, 1));

        /// <summary>
        /// 向下选择单元格（可供 UI 按钮 UnityEvent 绑定）。
        /// </summary>
        public virtual void SelectDown() => Navigate(new Vector2Int(0, -1));

        /// <summary>
        /// 向左选择单元格（可供 UI 按钮 UnityEvent 绑定）。
        /// </summary>
        public virtual void SelectLeft() => Navigate(new Vector2Int(-1, 0));

        /// <summary>
        /// 向右选择单元格（可供 UI 按钮 UnityEvent 绑定）。
        /// </summary>
        public virtual void SelectRight() => Navigate(new Vector2Int(1, 0));
        #endregion

        /// <summary>
        /// 触发当前选中项的确认/提交动作。
        /// </summary>
        public virtual void SubmitCurrentSelection()
        {
            if (_currentDataSource == null || _currentDataSource.Count == 0) return;

            int dataIdx = SelectedDataIndex;
            object dataObj = SelectedDataObject;

            OnItemSubmitted?.Invoke(dataIdx, dataObj);
            _onItemSubmittedEvent?.Invoke(dataIdx, dataObj);
        }

        /// <summary>
        /// 强行选中指定的数据源索引，并根据 _boundaryDistance 边界平移距离自动调整数据滑动窗口。
        /// </summary>
        /// <param name="dataIndex">目标数据源索引</param>
        public virtual void SetSelectedDataIndex(int dataIndex)
        {
            if (_currentDataSource == null || _currentDataSource.Count == 0) return;

            int totalDataCount = _currentDataSource.Count;
            int clampedDataIndex = Mathf.Clamp(dataIndex, 0, totalDataCount - 1);
            int visualCap = _managedItems.Count;
            if (visualCap == 0) return;

            int stride = ColumnsCount;
            int visRows = Mathf.CeilToInt((float)visualCap / stride);
            int totalRows = Mathf.CeilToInt((float)totalDataCount / stride);
            int maxStartRow = Mathf.Max(0, totalRows - visRows);
            int maxStartIndex = maxStartRow * stride;

            // 计算有效的边界预留行数
            int effectiveBoundary = Mathf.Clamp(_boundaryDistance, 0, Mathf.Max(0, (visRows - 1) / 2));

            int targetRow = clampedDataIndex / stride;
            int currentStartRow = _dataStartIndex / stride;
            int relativeRow = targetRow - currentStartRow;

            // 检查是否靠近顶部/左侧预留边界
            if (relativeRow < effectiveBoundary)
            {
                int targetStartRow = targetRow - effectiveBoundary;
                _dataStartIndex = Mathf.Clamp(targetStartRow * stride, 0, maxStartIndex);
            }
            // 检查是否靠近底部/右侧预留边界
            else if (relativeRow >= visRows - effectiveBoundary)
            {
                int targetStartRow = targetRow - (visRows - 1 - effectiveBoundary);
                _dataStartIndex = Mathf.Clamp(targetStartRow * stride, 0, maxStartIndex);
            }
            // 兜底：处理索引不在当前滑动窗口内的极端越界情况
            else if (clampedDataIndex < _dataStartIndex || clampedDataIndex >= _dataStartIndex + visualCap)
            {
                int targetStartRow = Mathf.Clamp(targetRow, 0, maxStartRow);
                _dataStartIndex = targetStartRow * stride;
            }

            // 更新可视索引
            _selectedVisualIndex = clampedDataIndex - _dataStartIndex;
            ClampSelectionBounds();

            RefreshView();
            NotifySelectionChanged();
        }

        /// <summary>
        /// 重新扫描并初始化可视 UI 单元列表 (当运行时动态生成或销毁了子节点时调用)。
        /// </summary>
        public virtual void ReinitializeManagedItems()
        {
            _isInitialized = false;
            InitializeManagedItems();
            RefreshView();
        }

        /// <summary>
        /// 当列表项 UI 收到指针点击时调用的通知接口。
        /// </summary>
        /// <param name="item">被点击的 UI 单元项</param>
        /// <param name="submitImmediately">是否同时触发确认提交回调</param>
        public virtual void NotifyItemClicked(IInventoryListItem item, bool submitImmediately = false)
        {
            if (item == null || _managedItems == null) return;
            int visualIdx = _managedItems.IndexOf(item);
            if (visualIdx >= 0)
            {
                int targetDataIdx = _dataStartIndex + visualIdx;
                SetSelectedDataIndex(targetDataIdx);
                if (submitImmediately)
                {
                    SubmitCurrentSelection();
                }
            }
        }

        /// <summary>
        /// 重新刷新当前视图展示。
        /// </summary>
        public virtual void RefreshView()
        {
            InitializeManagedItems();

            // 若绑定了 Container，自动在内部根据当前策略与 MappingMode 生成渲染数据源
            if (_boundContainer != null && _boundContainer.Slots != null)
            {
                _filteredSlotsBuffer.Clear();
                int capacity = _boundContainer.Capacity;

                if (_mappingMode == InventoryListMappingMode.DirectMapping)
                {
                    for (int i = 0; i < capacity; i++)
                    {
                        var slot = _boundContainer.Slots[i];
                        bool isMatched = _activeFilter == null || (_activeFilter(slot));
                        _filteredSlotsBuffer.Add(isMatched ? slot : null);
                    }
                }
                else
                {
                    for (int i = 0; i < capacity; i++)
                    {
                        var slot = _boundContainer.Slots[i];
                        bool isMatched = _activeFilter == null ? !slot.IsEmpty : (_activeFilter(slot));
                        if (isMatched)
                        {
                            _filteredSlotsBuffer.Add(slot);
                        }
                    }
                }

                _currentDataSource = _filteredSlotsBuffer;
            }

            ClampSelectionBounds();

            int visualCap = _managedItems.Count;
            int totalDataCount = _currentDataSource != null ? _currentDataSource.Count : 0;

            for (int i = 0; i < visualCap; i++)
            {
                var item = _managedItems[i];
                int dataIdx = _dataStartIndex + i;

                // 槽位 UI 元素本身永远保持开启激活（SetActive(true)），绝不隐藏背景框
                item.SetVisible(true);

                if (dataIdx < totalDataCount)
                {
                    // 数据向上填补绑定给靠前的槽位
                    item.OnBindData(_currentDataSource[dataIdx], dataIdx);
                    item.OnSelectionChanged(i == _selectedVisualIndex);
                }
                else
                {
                    // 超出有效数据范围的多余槽位统一下发 null 数据，仅隐藏内部 Content 容器，保留背景底图
                    item.OnBindData(null, dataIdx);
                    item.OnSelectionChanged(false);
                }
            }

            OnContentUpdated?.Invoke();
            _onContentUpdatedEvent?.Invoke();
        }
        #endregion

        #region Private Helper Methods
        protected virtual void InitializeManagedItems()
        {
            if (_isInitialized && _managedItems.Count > 0) return;

            _managedItems.Clear();

            Transform root = _contentRoot != null ? _contentRoot : transform;
            int childCount = root.childCount;

            for (int i = 0; i < childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.TryGetComponent<IInventoryListItem>(out var listItem))
                {
                    _managedItems.Add(listItem);
                }
            }

            _isInitialized = true;
        }

        protected virtual void ClampSelectionBounds()
        {
            int totalDataCount = _currentDataSource != null ? _currentDataSource.Count : 0;
            int visualCap = _managedItems.Count;

            if (totalDataCount == 0 || visualCap == 0)
            {
                _selectedVisualIndex = 0;
                _dataStartIndex = 0;
                return;
            }

            // 修正窗口界限
            if (_dataStartIndex >= totalDataCount)
            {
                _dataStartIndex = Mathf.Max(0, totalDataCount - visualCap);
            }

            // 修正可视选中界限
            int maxVisualIndex = Mathf.Min(visualCap - 1, totalDataCount - _dataStartIndex - 1);
            _selectedVisualIndex = Mathf.Clamp(_selectedVisualIndex, 0, Mathf.Max(0, maxVisualIndex));
        }

        protected virtual void NotifySelectionChanged()
        {
            int dataIdx = SelectedDataIndex;
            object dataObj = SelectedDataObject;

            OnSelectionChanged?.Invoke(dataIdx, dataObj);
            _onSelectionChangedEvent?.Invoke(dataIdx, dataObj);
        }

        protected virtual void SubscribeEvents()
        {
            InventoryEventPipeline.OnEvent += HandleInventoryPipelineEvent;
        }

        protected virtual void UnsubscribeEvents()
        {
            InventoryEventPipeline.OnEvent -= HandleInventoryPipelineEvent;
        }

        protected virtual void HandleInventoryPipelineEvent(InventoryEvent evt)
        {
            // 列表控制器默认 100% 不绑定任何背包。唯有外部/上层显式调用 BindContainer(...) 绑定了有效容器后，才响应管线刷新
            if (_boundContainer == null) return;

            bool isMatch = evt.Container == _boundContainer ||
                (!string.IsNullOrEmpty(_boundInventoryId) && string.Equals(evt.InventoryId, _boundInventoryId, StringComparison.OrdinalIgnoreCase));

            if (isMatch)
            {
                if (evt.EventType == InventoryEventType.ContentChanged ||
                    evt.EventType == InventoryEventType.SlotUpdated ||
                    evt.EventType == InventoryEventType.Registered)
                {
                    RefreshView();
                }
            }
        }
        #endregion
    }
}
