using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using Cwc.InventoryEngine.Query;

namespace Cwc.InventoryEngine.UI
{
    /// <summary>
    /// 背包分类 Tab 定义。
    /// 完全由 ItemCategorySO 驱动，并提供 UnityEvent 供设计师直接在 Inspector 中配置视觉/动画效果。
    /// </summary>
    [Serializable]
    public struct CategoryTabDefinition
    {
        [Tooltip("对应的分类 SO 资产（若为 null，则代表显示全部物品）")]
        public ItemCategorySO CategorySO;

        [Tooltip("当前页签被选中时触发的 UI 效果/动画事件（例如背景高亮、放大、播音效等）")]
        public UnityEvent OnSelected;

        [Tooltip("当前页签取消选中时触发的 UI 效果/恢复事件（例如背景变暗、缩回原样等）")]
        public UnityEvent OnDeselected;

        /// <summary>
        /// 界面显示名称（只读，直接从 CategorySO 提取，若为 null 则默认显示 'All'）。
        /// </summary>
        public string DisplayName => GetDisplayName();

        /// <summary>
        /// 获取 Tab 页签在 UI 上显示的文本名称。
        /// </summary>
        public string GetDisplayName(string fallbackDisplayName = "All")
        {
            return CategorySO != null ? CategorySO.GetDisplayName() : fallbackDisplayName;
        }
    }

    /// <summary>
    /// 主库存视图组件。
    /// 管理分类 Tab 页签切换、动态物品筛选列表以及关联的详情面板渲染。
    /// 继承自 InventoryViewBase，提供安全的逻辑门禁控制。
    /// 使用现代化 TextMeshProUGUI 进行文本渲染。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/UI/Main Inventory View")]
    public class MainInventoryView : InventoryViewBase
    {
        #region Serialized Fields
        [Header("核心 UI 控制器绑定")]
        [SerializeField]
        [Tooltip("物品主列表滑动控制器")]
        private InventoryListUIController _itemListController;

        [SerializeField]
        [Tooltip("右侧/下方的物品详情显示面板")]
        private ItemDetailView _itemDetailView;

        [Header("库存 ID 配置 (发送 Request 区分)")]
        [SerializeField]
        [Tooltip("主背包唯一标识 ID")]
        private string _inventoryId = "MainInventory";

        [Header("分类 Tab 标签页配置")]
        [SerializeField]
        [Tooltip("未指定 CategorySO 时（例如'全部物品' Tab 页签）显示的默认显示文本")]
        private string _allCategoryDisplayName = "All";

        [SerializeField]
        [Tooltip("分类页签文本显示 TextMeshProUGUI (可选，如果挂载了组件可自动显示当前分类)")]
        private TextMeshProUGUI _currentCategoryText;

        [SerializeField]
        [Tooltip("页码指示器显示 TextMeshProUGUI (可选，如显示 '1 / 4')")]
        private TextMeshProUGUI _pageText;

        [SerializeField]
        [Tooltip("可配置的分类标签列表")]
        private List<CategoryTabDefinition> _categoryTabs = new()
        {
            new CategoryTabDefinition { CategorySO = null }
        };
        #endregion

        #region Private Fields
        private IReadOnlyInventoryContainer _container;
        private int _currentTabIndex = 0;
        private int _previousTabIndex = -1;
        private readonly List<ItemSlot> _filteredSlotsBuffer = new();
        #endregion

        #region Public Properties & Events
        /// <summary>
        /// 绑定的主背包唯一标识 ID。
        /// </summary>
        public string InventoryId => _inventoryId;

        /// <summary>
        /// 物品列表控制器引用。
        /// </summary>
        public InventoryListUIController ItemListController => _itemListController;

        /// <summary>
        /// 当前选中的分类 Tab 定义。
        /// </summary>
        public CategoryTabDefinition CurrentCategory => _categoryTabs != null && _categoryTabs.Count > 0
            ? _categoryTabs[Mathf.Clamp(_currentTabIndex, 0, _categoryTabs.Count - 1)]
            : default;

        /// <summary>
        /// 分类 Tab 切换时触发的全局代码事件。(参数分别为 newTabIndex, categoryTab)
        /// </summary>
        public event Action<int, CategoryTabDefinition> OnTabChanged;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            InventoryEventPipeline.OnEvent += HandlePipelineEventForView;
            InventoryRegistry.OnRegistered += HandleInventoryRegistered;
            if (_itemListController != null)
            {
                _itemListController.OnSelectionChanged += HandleSelectionChanged;
                _itemListController.OnContentUpdated += UpdatePageText;
            }
            TryAutoBindFromRegistry();
            ApplyCategoryFilter();
        }

        private void OnDisable()
        {
            InventoryEventPipeline.OnEvent -= HandlePipelineEventForView;
            InventoryRegistry.OnRegistered -= HandleInventoryRegistered;
            if (_itemListController != null)
            {
                _itemListController.OnSelectionChanged -= HandleSelectionChanged;
                _itemListController.OnContentUpdated -= UpdatePageText;
            }
        }
        #endregion

        #region Private Pipeline Listener
        private void HandlePipelineEventForView(InventoryEvent evt)
        {
            if (_container == null && !string.IsNullOrEmpty(_inventoryId) &&
                string.Equals(evt.InventoryId, _inventoryId, StringComparison.OrdinalIgnoreCase) &&
                evt.Container != null)
            {
                Initialize(evt.Container, _inventoryId);
            }
        }

        private void HandleInventoryRegistered(string inventoryId, InventoryContainer container)
        {
            if (_container == null && string.Equals(inventoryId, _inventoryId, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"[MainInventoryView] 监听到 InventoryRegistry 注册事件 '{inventoryId}'，自动执行绑定。");
                Initialize(container, _inventoryId);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 尝试从全局 InventoryRegistry 检索并自动绑定主背包容器。
        /// </summary>
        public virtual bool TryAutoBindFromRegistry()
        {
            if (_container != null) return true;

            if (!string.IsNullOrEmpty(_inventoryId) && InventoryRegistry.TryGetContainer(_inventoryId, out var container))
            {
                Debug.Log($"[MainInventoryView] 通过 InventoryRegistry 主动检索成功！主背包容器 ID='{_inventoryId}'");
                Initialize(container, _inventoryId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 初始化主视图并绑定关联的 InventoryContainer。
        /// </summary>
        /// <param name="container">主背包容器</param>
        /// <param name="inventoryId">主背包唯一标识 ID (可选)</param>
        public virtual void Initialize(IReadOnlyInventoryContainer container, string inventoryId = null)
        {
            if (!string.IsNullOrEmpty(inventoryId))
            {
                _inventoryId = inventoryId;
            }

            Debug.Log($"[MainInventoryView] 开始绑定容器: InventoryID='{_inventoryId}'");

            if (_container != null)
            {
                _container.OnSlotUpdated -= HandleContainerSlotUpdated;
                _container.OnBatchCompleted -= HandleContainerBatchCompleted;
            }

            _container = container;

            if (_container != null)
            {
                _container.OnSlotUpdated += HandleContainerSlotUpdated;
                _container.OnBatchCompleted += HandleContainerBatchCompleted;
                Debug.Log($"[MainInventoryView] 主背包容器 '{_inventoryId}' 绑定成功！槽位数: {_container.Slots?.Length ?? 0}");
            }
            else
            {
                Debug.LogWarning($"[MainInventoryView] 主背包容器 '{_inventoryId}' 绑定失败或为空！请检查场景中对应 Inventory 是否初始化并注册。");
            }

            if (_itemListController != null)
            {
                _itemListController.BindContainer(_container);
                _itemListController.OnSelectionChanged -= HandleSelectionChanged;
                _itemListController.OnSelectionChanged += HandleSelectionChanged;
                _itemListController.OnContentUpdated -= UpdatePageText;
                _itemListController.OnContentUpdated += UpdatePageText;
            }

            RefreshCategoryText();
            ApplyCategoryFilter();
            UpdatePageText();
        }

        /// <summary>
        /// BindContainer 别名绑定。
        /// </summary>
        public virtual void BindContainer(IReadOnlyInventoryContainer container, string inventoryId = null)
        {
            Initialize(container, inventoryId);
        }

        /// <summary>
        /// 切换到上一个分类页签。
        /// </summary>
        public virtual void SwitchToPrevCategory()
        {
            if (_categoryTabs == null || _categoryTabs.Count == 0) return;
            _currentTabIndex = (_currentTabIndex - 1 + _categoryTabs.Count) % _categoryTabs.Count;
            RefreshCategoryText();
            ApplyCategoryFilter();
        }

        /// <summary>
        /// 切换到下一个分类页签。
        /// </summary>
        public virtual void SwitchToNextCategory()
        {
            if (_categoryTabs == null || _categoryTabs.Count == 0) return;
            _currentTabIndex = (_currentTabIndex + 1) % _categoryTabs.Count;
            RefreshCategoryText();
            ApplyCategoryFilter();
        }

        /// <summary>
        /// 切换列表到下一页。
        /// </summary>
        public virtual void NextPage()
        {
            if (_itemListController != null)
            {
                _itemListController.NextPage();
            }
        }

        /// <summary>
        /// 切换列表到上一页。
        /// </summary>
        public virtual void PreviousPage()
        {
            if (_itemListController != null)
            {
                _itemListController.PreviousPage();
            }
        }

        /// <summary>
        /// 刷新页码显示文本（如 "1 / 4"）。
        /// </summary>
        public virtual void UpdatePageText()
        {
            if (_pageText != null && _itemListController != null)
            {
                _pageText.text = $"{_itemListController.CurrentPageNumber} / {_itemListController.TotalPages}";
            }
        }

        /// <summary>
        /// 响应离散输入逻辑。
        /// </summary>
        public virtual void ProcessInput(InventoryInputData inputData)
        {
            if (!IsActive) return;

            if (inputData.TabPrev)
            {
                SwitchToPrevCategory();
                return;
            }

            if (inputData.TabNext)
            {
                SwitchToNextCategory();
                return;
            }

            if (inputData.PagePrev)
            {
                PreviousPage();
                return;
            }

            if (inputData.PageNext)
            {
                NextPage();
                return;
            }

            if (inputData.Use)
            {
                RequestUseSelectedItem();
                return;
            }

            if (inputData.Drop)
            {
                RequestDropSelectedItem();
                return;
            }

            if (_itemListController != null && inputData.MoveDirection != Vector2Int.zero)
            {
                _itemListController.Navigate(inputData.MoveDirection);
            }

            if (_itemListController != null && inputData.Submit)
            {
                _itemListController.SubmitCurrentSelection();
            }
        }

        /// <summary>
        /// 触发当前选中物品的使用请求。
        /// </summary>
        public virtual void RequestUseSelectedItem()
        {
            if (_itemListController == null) return;
            if (_itemListController.SelectedDataObject is ItemSlot slot && !slot.IsEmpty)
            {
                InventoryRequestPipeline.Send(new InventoryUseRequest(_inventoryId, slot.SlotIndex));
            }
        }

        /// <summary>
        /// 触发当前选中物品的丢弃请求。
        /// </summary>
        public virtual void RequestDropSelectedItem()
        {
            if (_itemListController == null) return;
            if (_itemListController.SelectedDataObject is ItemSlot slot && !slot.IsEmpty)
            {
                InventoryRequestPipeline.Send(new InventoryDropRequest(_inventoryId, slot.SlotIndex));
            }
        }

        /// <summary>
        /// 按索引直接切换到指定的分类页签。
        /// </summary>
        public virtual void SelectCategoryTab(int tabIndex)
        {
            if (_categoryTabs == null || _categoryTabs.Count == 0) return;
            _currentTabIndex = Mathf.Clamp(tabIndex, 0, _categoryTabs.Count - 1);
            RefreshCategoryText();
            ApplyCategoryFilter();
        }

        /// <summary>
        /// 重新根据当前分类更新列表控制器的筛选过滤器策略。
        /// </summary>
        public virtual void ApplyCategoryFilter()
        {
            if (_itemListController == null) return;

            var currentTab = CurrentCategory;
            ItemCategorySO targetCategorySO = currentTab.CategorySO;

            if (targetCategorySO == null)
            {
                // 无分类 SO 条件 ("All" 页签)：清除过滤器策略，回复默认展示
                _itemListController.ClearFilter();
            }
            else
            {
                // 策略下沉：构建 Predicate<ItemSlot> 纯 SO 继承链匹配逻辑传递给列表控制器
                _itemListController.SetFilter(slot => !slot.IsEmpty && slot.Item.IsInCategory(targetCategorySO));
            }

            // 联动驱动详情渲染
            RenderSelectedDetail(_itemListController.SelectedDataObject);

            // 广播触发页签视觉/动画事件
            NotifyTabEvents();
        }

        /// <summary>
        /// 获取未指定 CategorySO 时（例如'全部物品' Tab 页签）的显示文本名称。
        /// 默认为 _allCategoryDisplayName 配置。可在子类中重写此虚方法以接入本地化 Key/I18N 解析。
        /// </summary>
        public virtual string GetAllCategoryDisplayName()
        {
            return _allCategoryDisplayName;
        }

        /// <summary>
        /// 获取指定 CategoryTabDefinition 在 UI 上显示的最终文本名称。
        /// 优先提取 CategorySO 的名称；若未指定 SO 则使用 GetAllCategoryDisplayName() 的结果。
        /// </summary>
        public virtual string GetTabDisplayName(CategoryTabDefinition tab)
        {
            return tab.CategorySO != null ? tab.CategorySO.GetDisplayName() : GetAllCategoryDisplayName();
        }
        #endregion

        #region Private Helper Methods
        protected virtual void NotifyTabEvents()
        {
            if (_categoryTabs == null || _categoryTabs.Count == 0) return;

            int clampedIndex = Mathf.Clamp(_currentTabIndex, 0, _categoryTabs.Count - 1);

            // 触发上一个 Tab 的反选事件
            if (_previousTabIndex >= 0 && _previousTabIndex < _categoryTabs.Count && _previousTabIndex != clampedIndex)
            {
                _categoryTabs[_previousTabIndex].OnDeselected?.Invoke();
            }

            // 触发当前 Tab 的选中事件
            _categoryTabs[clampedIndex].OnSelected?.Invoke();
            _previousTabIndex = clampedIndex;

            // 广播 C# 全局事件
            OnTabChanged?.Invoke(clampedIndex, _categoryTabs[clampedIndex]);
        }

        protected virtual void RefreshCategoryText()
        {
            if (_currentCategoryText != null)
            {
                _currentCategoryText.text = GetTabDisplayName(CurrentCategory);
            }
        }

        protected virtual void HandleContainerSlotUpdated(int slotIndex, ItemSlot slot)
        {
            if (!IsActive) return;
            ApplyCategoryFilter();
        }

        protected virtual void HandleContainerBatchCompleted()
        {
            if (!IsActive) return;
            ApplyCategoryFilter();
        }

        protected virtual void HandleSelectionChanged(int dataIndex, object dataObject)
        {
            if (!IsActive) return;
            RenderSelectedDetail(dataObject);
        }

        protected virtual void RenderSelectedDetail(object dataObject)
        {
            if (_itemDetailView != null)
            {
                _itemDetailView.RenderItem(dataObject);
            }
        }

        /// <summary>
        /// 当视图由停用切换为激活时触发的虚回调。
        /// 全量拉取并刷新最新的界面数据。
        /// </summary>
        protected override void OnActivated()
        {
            base.OnActivated();
            Debug.Log($"[MainInventoryView] 视图被激活 (OnActivated / Show)。当前 _container 状态: {(_container != null ? "已绑定" : "未绑定")}。");
            if (_container == null)
            {
                TryAutoBindFromRegistry();
            }
            ApplyCategoryFilter();
        }
        #endregion
    }
}
