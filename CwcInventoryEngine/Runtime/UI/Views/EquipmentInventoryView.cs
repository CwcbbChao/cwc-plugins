using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cwc.InventoryEngine.Query;

namespace Cwc.InventoryEngine.UI
{
    /// <summary>
    /// 装备库存视图与二级选择菜单控制器。
    /// 管理装备槽位导航列表以及穿戴替换时唤起的二级弹窗列表。
    /// 继承自 InventoryViewBase，提供安全的逻辑门禁控制。
    /// 保证职责单一与流程高度内聚。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/UI/Equipment Inventory View")]
    public class EquipmentInventoryView : InventoryViewBase
    {
        #region Serialized Fields
        [Header("装备主列表控制器")]
        [SerializeField]
        [Tooltip("装备栏槽位列表控制器 (展示 Head, Chest, Weapon 等装备槽)")]
        private InventoryListUIController _equipmentListController;

        [Header("二级选择菜单列表控制器")]
        [SerializeField]
        [Tooltip("二级装备选择弹窗面板容器 GameObject")]
        private GameObject _selectorMenuContainer;

        [SerializeField]
        [Tooltip("二级可替换装备选择列表控制器")]
        private InventoryListUIController _selectorListController;

        [SerializeField]
        [Tooltip("二级选择菜单页码指示器 TextMeshProUGUI (可选，如显示 '1 / 2')")]
        private TextMeshProUGUI _selectorPageText;

        [SerializeField]
        [Tooltip("二级选择菜单 CanvasGroup (可选，若未挂载将在运行时自动获取/添加)")]
        private CanvasGroup _selectorCanvasGroup;

        [Header("二级菜单显隐与动效事件")]
        [SerializeField]
        [Tooltip("当二级选择菜单开启时触发的事件")]
        private UnityEngine.Events.UnityEvent _onSubMenuOpenedEvent;

        [SerializeField]
        [Tooltip("当二级选择菜单关闭时触发的事件")]
        private UnityEngine.Events.UnityEvent _onSubMenuClosedEvent;

        [SerializeField]
        [Tooltip("当二级选择菜单显隐状态改变时触发的事件 (传递 visible 布尔值与 CanvasGroup 实例，方便动画系统绑定插值)")]
        private UnityEngine.Events.UnityEvent<bool, CanvasGroup> _onSubMenuVisibilityChangedEvent;

        [Header("关联详情面板")]
        [SerializeField]
        [Tooltip("装备栏(已穿戴装备槽位)详情显示面板 (可选，若配置可在二级菜单打开时锁定显示已有装备)")]
        private ItemDetailView _equippedItemDetailView;

        [SerializeField]
        [Tooltip("二级选择菜单(待替换备选物品)详情显示面板 (可选，若配置可与已穿戴装备天然属性对比)")]
        private ItemDetailView _selectorItemDetailView;

        [SerializeField]
        [Tooltip("通用详情面板备用后备 (若未配置单独的 _equippedItemDetailView 或 _selectorItemDetailView 则退回使用此面板)")]
        private ItemDetailView _itemDetailView;

        [Header("库存 ID 配置 (发送 Request 区分)")]
        [SerializeField]
        [Tooltip("装备栏唯一标识 ID")]
        private string _equipmentInventoryId = "PlayerEquipment";

        [SerializeField]
        [Tooltip("主背包唯一标识 ID")]
        private string _mainInventoryId = "MainInventory";
        #endregion

        #region Private Fields
        private IReadOnlyInventoryContainer _equipmentContainer; // 装备容器
        private IReadOnlyInventoryContainer _mainContainer;      // 主背包容器
        private bool _isSubMenuOpen = false;                     // 当前是否在二级菜单中导航
        private ItemSlot _selectedEquipmentSlot;                 // 当前选中的装备槽
        private readonly List<ItemSlot> _availableEquipmentsBuffer = new();
        #endregion

        #region Public Properties & Events
        /// <summary>
        /// 当二级选择菜单开启时触发的 Action。
        /// </summary>
        public event Action OnSubMenuOpened;

        /// <summary>
        /// 当二级选择菜单关闭时触发的 Action。
        /// </summary>
        public event Action OnSubMenuClosed;

        /// <summary>
        /// 当二级选择菜单显隐状态改变时触发的 Action：(visible, canvasGroup)
        /// </summary>
        public event Action<bool, CanvasGroup> OnSubMenuVisibilityChanged;

        /// <summary>
        /// 二级菜单 CanvasGroup 组件引用。
        /// </summary>
        public CanvasGroup SelectorCanvasGroup => _selectorCanvasGroup;

        /// <summary>
        /// 当前是否处于二级装备选择菜单中。
        /// </summary>
        public bool IsSubMenuOpen => _isSubMenuOpen;

        /// <summary>
        /// 装备列表控制器引用。
        /// </summary>
        public InventoryListUIController EquipmentListController => _equipmentListController;

        /// <summary>
        /// 绑定的装备栏 InventoryID。
        /// </summary>
        public string EquipmentInventoryId => _equipmentInventoryId;

        /// <summary>
        /// 绑定的主背包 InventoryID。
        /// </summary>
        public string MainInventoryId => _mainInventoryId;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            InitializeSelectorCanvasGroup();
            SetSubMenuVisibility(false);
        }

        private void OnEnable()
        {
            InventoryEventPipeline.OnEvent += HandlePipelineEventForView;
            InventoryRegistry.OnRegistered += HandleInventoryRegistered;

            if (_equipmentListController != null)
            {
                _equipmentListController.OnSelectionChanged += HandleEquipmentSelectionChanged;
                _equipmentListController.OnItemSubmitted += HandleEquipmentSlotSubmitted;
            }

            if (_selectorListController != null)
            {
                _selectorListController.OnSelectionChanged += HandleSelectorSelectionChanged;
                _selectorListController.OnItemSubmitted += HandleSelectorItemSubmitted;
                _selectorListController.OnContentUpdated += UpdateSelectorPageText;
            }

            // 在组件激活时，若尚未绑定容器，尝试从全局注册中心自动检索并绑定
            TryAutoBindFromRegistry();
        }

        private void OnDisable()
        {
            InventoryEventPipeline.OnEvent -= HandlePipelineEventForView;
            InventoryRegistry.OnRegistered -= HandleInventoryRegistered;

            if (_equipmentListController != null)
            {
                _equipmentListController.OnSelectionChanged -= HandleEquipmentSelectionChanged;
                _equipmentListController.OnItemSubmitted -= HandleEquipmentSlotSubmitted;
            }

            if (_selectorListController != null)
            {
                _selectorListController.OnSelectionChanged -= HandleSelectorSelectionChanged;
                _selectorListController.OnItemSubmitted -= HandleSelectorItemSubmitted;
                _selectorListController.OnContentUpdated -= UpdateSelectorPageText;
            }
        }

        private void HandlePipelineEventForView(InventoryEvent evt)
        {
            if (evt.Container == null) return;

            IReadOnlyInventoryContainer eqComp = _equipmentContainer;
            IReadOnlyInventoryContainer mainComp = _mainContainer;

            if (eqComp == null && !string.IsNullOrEmpty(_equipmentInventoryId) &&
                string.Equals(evt.InventoryId, _equipmentInventoryId, StringComparison.OrdinalIgnoreCase))
            {
                eqComp = evt.Container;
            }

            if (mainComp == null && !string.IsNullOrEmpty(_mainInventoryId) &&
                string.Equals(evt.InventoryId, _mainInventoryId, StringComparison.OrdinalIgnoreCase))
            {
                mainComp = evt.Container;
            }

            if (eqComp != _equipmentContainer || mainComp != _mainContainer)
            {
                BindContainers(eqComp, mainComp, _equipmentInventoryId, _mainInventoryId);
            }
        }

        private void HandleInventoryRegistered(string inventoryId, InventoryContainer container)
        {
            bool needBind = false;
            IReadOnlyInventoryContainer eqComp = _equipmentContainer;
            IReadOnlyInventoryContainer mainComp = _mainContainer;

            if (eqComp == null && string.Equals(inventoryId, _equipmentInventoryId, StringComparison.OrdinalIgnoreCase))
            {
                eqComp = container;
                needBind = true;
            }

            if (mainComp == null && string.Equals(inventoryId, _mainInventoryId, StringComparison.OrdinalIgnoreCase))
            {
                mainComp = container;
                needBind = true;
            }

            if (needBind)
            {
                Debug.Log($"[EquipmentInventoryView] 监听到 InventoryRegistry 注册事件 '{inventoryId}'，自动执行绑定。");
                BindContainers(eqComp, mainComp, _equipmentInventoryId, _mainInventoryId);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 尝试从全局 InventoryRegistry 注册中心检索并自动绑定容器。
        /// </summary>
        public virtual bool TryAutoBindFromRegistry()
        {
            if (_equipmentContainer != null && _mainContainer != null) return true;

            IReadOnlyInventoryContainer foundEquip = _equipmentContainer;
            IReadOnlyInventoryContainer foundMain = _mainContainer;

            if (foundEquip == null && !string.IsNullOrEmpty(_equipmentInventoryId))
            {
                if (InventoryRegistry.TryGetContainer(_equipmentInventoryId, out var eqContainer))
                {
                    foundEquip = eqContainer;
                }
            }

            if (foundMain == null && !string.IsNullOrEmpty(_mainInventoryId))
            {
                if (InventoryRegistry.TryGetContainer(_mainInventoryId, out var mainContainer))
                {
                    foundMain = mainContainer;
                }
            }

            if (foundEquip != _equipmentContainer || foundMain != _mainContainer)
            {
                Debug.Log($"[EquipmentInventoryView] 通过 InventoryRegistry 主动检索成功！装备容器='{foundEquip != null}', 主背包容器='{foundMain != null}'");
                BindContainers(foundEquip, foundMain, _equipmentInventoryId, _mainInventoryId);
            }

            return _equipmentContainer != null;
        }

        /// <summary>
        /// 初始化绑定装备容器与主背包容器。
        /// </summary>
        /// <param name="equipmentContainer">装备栏容器</param>
        /// <param name="mainContainer">主背包容器</param>
        /// <param name="equipmentInventoryId">装备栏唯一标识 ID (可选)</param>
        /// <param name="mainInventoryId">主背包唯一标识 ID (可选)</param>
        public virtual void BindContainers(IReadOnlyInventoryContainer equipmentContainer, IReadOnlyInventoryContainer mainContainer, string equipmentInventoryId = null, string mainInventoryId = null)
        {
            if (!string.IsNullOrEmpty(equipmentInventoryId)) _equipmentInventoryId = equipmentInventoryId;
            if (!string.IsNullOrEmpty(mainInventoryId)) _mainInventoryId = mainInventoryId;

            Debug.Log($"[EquipmentInventoryView] 开始绑定容器: TargetEquipmentID='{_equipmentInventoryId}', TargetMainID='{_mainInventoryId}'");

            if (_equipmentContainer != null)
            {
                _equipmentContainer.OnSlotUpdated -= HandleContainerUpdated;
                _equipmentContainer.OnBatchCompleted -= RefreshView;
            }

            if (_mainContainer != null)
            {
                _mainContainer.OnSlotUpdated -= HandleContainerUpdated;
                _mainContainer.OnBatchCompleted -= RefreshView;
            }

            _equipmentContainer = equipmentContainer;
            _mainContainer = mainContainer;

            if (_equipmentContainer != null)
            {
                _equipmentContainer.OnSlotUpdated += HandleContainerUpdated;
                _equipmentContainer.OnBatchCompleted += RefreshView;
                Debug.Log($"[EquipmentInventoryView] 装备容器 '{_equipmentInventoryId}' 绑定成功！槽位数: {_equipmentContainer.Slots?.Length ?? 0}");
            }
            else
            {
                Debug.LogWarning($"[EquipmentInventoryView] 装备容器 '{_equipmentInventoryId}' 绑定失败或尚为空！请检查场景中该 Inventory 是否初始化并注册。");
            }

            if (_mainContainer != null)
            {
                _mainContainer.OnSlotUpdated += HandleContainerUpdated;
                _mainContainer.OnBatchCompleted += RefreshView;
                Debug.Log($"[EquipmentInventoryView] 主背包容器 '{_mainInventoryId}' 绑定成功！槽位数: {_mainContainer.Slots?.Length ?? 0}");
            }
            else
            {
                Debug.LogWarning($"[EquipmentInventoryView] 主背包容器 '{_mainInventoryId}' 绑定失败或尚为空！请检查 ID 是否匹配。");
            }

            CloseSubMenu();
            RefreshView();
        }

        /// <summary>
        /// 便捷绑定 Inventory 组件实例。
        /// </summary>
        public virtual void BindInventories(Inventory equipmentInventory, Inventory mainInventory)
        {
            IReadOnlyInventoryContainer equipContainer = equipmentInventory != null ? equipmentInventory.Container : null;
            IReadOnlyInventoryContainer mainContainer = mainInventory != null ? mainInventory.Container : null;
            string equipId = equipmentInventory != null ? equipmentInventory.InventoryId : null;
            string mainId = mainInventory != null ? mainInventory.InventoryId : null;

            BindContainers(equipContainer, mainContainer, equipId, mainId);
        }

        /// <summary>
        /// 响应状态机离散输入。
        /// </summary>
        /// <param name="inputData">输入状态数据</param>
        public virtual void ProcessInput(InventoryInputData inputData)
        {
            if (!IsActive) return;

            if (_isSubMenuOpen)
            {
                // 二级菜单状态下的输入路由
                if (inputData.Cancel)
                {
                    // 按取消键，关闭二级菜单，焦点切回装备槽列表
                    CloseSubMenu();
                    return;
                }

                if (inputData.PagePrev)
                {
                    PreviousSelectorPage();
                    return;
                }

                if (inputData.PageNext)
                {
                    NextSelectorPage();
                    return;
                }

                if (_selectorListController != null && inputData.MoveDirection != Vector2Int.zero)
                {
                    _selectorListController.Navigate(inputData.MoveDirection);
                }

                if (_selectorListController != null && inputData.Submit)
                {
                    _selectorListController.SubmitCurrentSelection();
                }
            }
            else
            {
                // 一级装备槽列表状态下的输入路由
                if (inputData.Unequip)
                {
                    RequestUnequipSelected();
                    return;
                }

                if (_equipmentListController != null && inputData.MoveDirection != Vector2Int.zero)
                {
                    _equipmentListController.Navigate(inputData.MoveDirection);
                }

                if (_equipmentListController != null && inputData.Submit)
                {
                    _equipmentListController.SubmitCurrentSelection();
                }
            }
        }

        /// <summary>
        /// 触发当前选中的装备槽位快捷卸下请求 (移动到主背包的可用空栏/堆叠中)。
        /// </summary>
        public virtual void RequestUnequipSelected()
        {
            if (_equipmentListController == null) return;
            if (_equipmentListController.SelectedDataObject is ItemSlot slot && !slot.IsEmpty)
            {
                InventoryRequestPipeline.Send(InventoryMoveRequest.ToAnySlot(
                    sourceInventoryId: _equipmentInventoryId,
                    sourceSlotIndex: slot.SlotIndex,
                    targetInventoryId: _mainInventoryId
                ));
            }
        }

        /// <summary>
        /// 关闭二级装备选择弹窗菜单，焦点切回装备槽列表。
        /// </summary>
        public virtual void CloseSubMenu()
        {
            _isSubMenuOpen = false;
            SetSubMenuVisibility(false);

            if (_selectorListController != null)
            {
                _selectorListController.ClearFilter();
            }

            // 清空二级备选物品详情面板
            if (_selectorItemDetailView != null)
            {
                _selectorItemDetailView.RenderItem(null);
            }

            // 重新把主装备栏详情面板驱动切回选中的装备槽
            if (_equipmentListController != null)
            {
                RenderEquippedDetail(_equipmentListController.SelectedDataObject);
            }
        }

        /// <summary>
        /// 切换二级装备选择菜单列表到下一页（一级装备栏列表固定无需翻页）。
        /// </summary>
        public virtual void NextSelectorPage()
        {
            if (_isSubMenuOpen && _selectorListController != null)
            {
                _selectorListController.NextPage();
            }
        }

        /// <summary>
        /// 切换二级装备选择菜单列表到上一页（一级装备栏列表固定无需翻页）。
        /// </summary>
        public virtual void PreviousSelectorPage()
        {
            if (_isSubMenuOpen && _selectorListController != null)
            {
                _selectorListController.PreviousPage();
            }
        }

        /// <summary>
        /// 通用下一页接口（仅在二级选择菜单打开时生效）。
        /// </summary>
        public virtual void NextPage() => NextSelectorPage();

        /// <summary>
        /// 通用上一页接口（仅在二级选择菜单打开时生效）。
        /// </summary>
        public virtual void PreviousPage() => PreviousSelectorPage();

        /// <summary>
        /// 刷新二级选择菜单的页码显示文本（如 "1 / 2"）。
        /// </summary>
        public virtual void UpdateSelectorPageText()
        {
            if (_selectorPageText != null && _selectorListController != null)
            {
                _selectorPageText.text = $"{_selectorListController.CurrentPageNumber} / {_selectorListController.TotalPages}";
            }
        }
        #endregion

        #region Private Event Handlers & SubMenu Logic
        protected virtual void RefreshView()
        {
            if (!IsActive) return;
            ForceRefreshView();
        }

        /// <summary>
        /// 强制刷新装备视图界面 (忽略 IsActive 状态门禁，常用于 BindContainers 或 OnActivated 瞬间)。
        /// </summary>
        public virtual void ForceRefreshView()
        {
            if (_equipmentContainer == null || _equipmentContainer.Slots == null)
            {
                Debug.LogWarning($"[EquipmentInventoryView] 无法刷新装备界面：_equipmentContainer 未绑定或为空！目标装备 ID='{_equipmentInventoryId}'");
                return;
            }

            // 1. 刷出一级装备槽位列表 (包含所有定义的装备槽，不管空还是满)
            if (_equipmentListController != null)
            {
                _equipmentListController.SetDataSource(_equipmentContainer.Slots, resetSelection: false);
            }

            // 2. 若二级菜单当前开着，更新二级列表数据
            if (_isSubMenuOpen && _selectedEquipmentSlot != null)
            {
                OpenSubMenuForSlot(_selectedEquipmentSlot);
            }
            else
            {
                if (_equipmentListController != null)
                {
                    RenderEquippedDetail(_equipmentListController.SelectedDataObject);
                }
            }
        }

        protected virtual void HandleContainerUpdated(int slotIndex, ItemSlot slot)
        {
            if (!IsActive) return;
            RefreshView();
        }

        protected virtual void HandleEquipmentSelectionChanged(int dataIndex, object dataObject)
        {
            if (!IsActive) return;
            if (!_isSubMenuOpen)
            {
                RenderEquippedDetail(dataObject);
            }
        }

        /// <summary>
        /// 当在装备槽位上按下确认键时，唤起二级选择菜单。
        /// </summary>
        protected virtual void HandleEquipmentSlotSubmitted(int dataIndex, object dataObject)
        {
            if (!IsActive) return;
            if (dataObject is ItemSlot slot)
            {
                _selectedEquipmentSlot = slot;
                OpenSubMenuForSlot(slot);
            }
        }

        protected virtual void OpenSubMenuForSlot(ItemSlot targetEquipmentSlot)
        {
            if (targetEquipmentSlot == null || _mainContainer == null || _equipmentContainer == null) return;

            _isSubMenuOpen = true;
            _selectedEquipmentSlot = targetEquipmentSlot;

            SetSubMenuVisibility(true);

            if (_selectorListController != null)
            {
                // 绑定主背包容器实体
                _selectorListController.BindContainer(_mainContainer);

                // 策略下沉：直接将 targetEquipmentSlot.CanAccept 装备穿戴校验规则设为 Selector 的筛选策略！
                ItemSlot targetSlot = targetEquipmentSlot;
                _selectorListController.SetFilter(mainSlot => !mainSlot.IsEmpty && targetSlot.CanAccept(_equipmentContainer, mainSlot.Item));
            }

            // 1. 锁定已穿戴面板：渲染当前正在被替换的目标装备槽详情
            RenderEquippedDetail(targetEquipmentSlot);

            // 2. 渲染二级备选面板：渲染二级列表中选中的备选物品详情
            if (_selectorListController != null)
            {
                RenderSelectorDetail(_selectorListController.SelectedDataObject);
            }
        }

        /// <summary>
        /// 统一控制二级选择菜单的 CanvasGroup 显隐状态并引发动效绑定事件。
        /// </summary>
        /// <param name="visible">是否显示二级菜单</param>
        protected virtual void SetSubMenuVisibility(bool visible)
        {
            InitializeSelectorCanvasGroup();

            if (_selectorCanvasGroup != null)
            {
                _selectorCanvasGroup.alpha = visible ? 1f : 0f;
                _selectorCanvasGroup.interactable = visible;
                _selectorCanvasGroup.blocksRaycasts = visible;
            }

            OnSubMenuVisibilityChanged?.Invoke(visible, _selectorCanvasGroup);
            _onSubMenuVisibilityChangedEvent?.Invoke(visible, _selectorCanvasGroup);

            if (visible)
            {
                OnSubMenuOpened?.Invoke();
                _onSubMenuOpenedEvent?.Invoke();
            }
            else
            {
                OnSubMenuClosed?.Invoke();
                _onSubMenuClosedEvent?.Invoke();
            }
        }

        private void InitializeSelectorCanvasGroup()
        {
            if (_selectorCanvasGroup == null && _selectorMenuContainer != null)
            {
                if (!_selectorMenuContainer.TryGetComponent<CanvasGroup>(out _selectorCanvasGroup))
                {
                    _selectorCanvasGroup = _selectorMenuContainer.AddComponent<CanvasGroup>();
                }
            }
        }

        protected virtual void HandleSelectorSelectionChanged(int dataIndex, object dataObject)
        {
            if (!IsActive) return;
            if (_isSubMenuOpen)
            {
                RenderSelectorDetail(dataObject);
            }
        }

        /// <summary>
        /// 当在二级列表中确认选中某件新装备时，触发装备穿戴/替换请求。
        /// </summary>
        protected virtual void HandleSelectorItemSubmitted(int dataIndex, object dataObject)
        {
            if (!IsActive) return;
            if (!_isSubMenuOpen || _selectedEquipmentSlot == null || _equipmentContainer == null || _mainContainer == null) return;

            if (dataObject is ItemSlot selectedMainBagSlot && !selectedMainBagSlot.IsEmpty)
            {
                // 通过统一的 InventoryRequestPipeline 主动请求管道发送装备穿戴/转移请求
                InventoryRequestPipeline.Send(new InventoryMoveRequest(
                    sourceInventoryId: _mainInventoryId,
                    sourceSlotIndex: selectedMainBagSlot.SlotIndex,
                    targetInventoryId: _equipmentInventoryId,
                    targetSlotIndex: _selectedEquipmentSlot.SlotIndex
                ));
            }

            // 穿戴完成，关闭二级菜单切回装备槽列表
            CloseSubMenu();
        }

        /// <summary>
        /// 渲染已穿戴装备栏的详情显示 (优先使用 _equippedItemDetailView，若未配置则退回后备 _itemDetailView)。
        /// </summary>
        protected virtual void RenderEquippedDetail(object dataObject)
        {
            var targetView = _equippedItemDetailView != null ? _equippedItemDetailView : _itemDetailView;
            if (targetView != null)
            {
                targetView.RenderItem(dataObject);
            }
        }

        /// <summary>
        /// 渲染二级可替换备选物品的详情显示 (优先使用 _selectorItemDetailView，若未配置则退回后备 _itemDetailView)。
        /// </summary>
        protected virtual void RenderSelectorDetail(object dataObject)
        {
            var targetView = _selectorItemDetailView != null ? _selectorItemDetailView : _itemDetailView;
            if (targetView != null)
            {
                targetView.RenderItem(dataObject);
            }
        }

        /// <summary>
        /// [已废弃后备 API] 渲染选中的详情显示。
        /// </summary>
        protected virtual void RenderSelectedDetail(object dataObject)
        {
            RenderEquippedDetail(dataObject);
        }

        /// <summary>
        /// 当视图由停用切换为激活时触发的虚回调。
        /// 全量拉取并刷新最新的装备和背包数据。
        /// </summary>
        protected override void OnActivated()
        {
            base.OnActivated();
            Debug.Log($"[EquipmentInventoryView] 视图被激活 (OnActivated / Show)。当前 _equipmentContainer 状态: {(_equipmentContainer != null ? "已绑定" : "未绑定")}。");
            if (_equipmentContainer == null || _mainContainer == null)
            {
                TryAutoBindFromRegistry();
            }
            RefreshView();
        }

        /// <summary>
        /// 当视图由激活切换为停用时触发的虚回调。
        /// 关掉二级装备选择菜单并清理临时交互状态。
        /// </summary>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            CloseSubMenu();
        }
        #endregion
    }
}
