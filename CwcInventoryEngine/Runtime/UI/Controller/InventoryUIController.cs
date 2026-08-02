using System;
using UnityEngine;

namespace Cwc.InventoryEngine.UI
{
    /// <summary>
    /// 库存 UI 顶层统一控制器。
    /// 充当 UI 系统的 Mediator（协调者）。
    /// 统一管理输入适配路由、主库存界面与装备界面切换，以及焦点传递。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/UI/Inventory UI Controller")]
    public class InventoryUIController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("子视图组件绑定")]
        [SerializeField]
        [Tooltip("主库存视图面板")]
        private MainInventoryView _mainInventoryView;

        [SerializeField]
        [Tooltip("装备库存视图面板 (含二级选择菜单)")]
        private EquipmentInventoryView _equipmentInventoryView;

        [Header("绑定的 Inventory (也可在代码中动态初始化)")]
        [SerializeField]
        [Tooltip("主背包 Component")]
        private Inventory _mainInventoryComponent;

        [SerializeField]
        [Tooltip("装备栏 Component")]
        private Inventory _equipmentInventoryComponent;

        [Header("输入提供者 (可选，不配置时自动使用默认键盘适配器)")]
        [SerializeField]
        [Tooltip("自定义输入提供者 MonoBehaviour 引用")]
        private MonoBehaviour _customInputProvider;
        #endregion

        #region Private Fields
        private IInventoryInputProvider _inputProvider;
        private bool _isShowingEquipmentView = false;
        private bool _isInitialized = false;
        #endregion

        #region Public Properties
        /// <summary>
        /// 当前是否处于装备视图界面。
        /// </summary>
        public bool IsShowingEquipmentView => _isShowingEquipmentView;

        /// <summary>
        /// 主库存视图组件。
        /// </summary>
        public MainInventoryView MainView => _mainInventoryView;

        /// <summary>
        /// 装备库存视图组件。
        /// </summary>
        public EquipmentInventoryView EquipmentView => _equipmentInventoryView;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            SetupInputProvider();
        }

        private void Start()
        {
            if (!_isInitialized && _mainInventoryComponent != null)
            {
                Initialize(_mainInventoryComponent, _equipmentInventoryComponent);
            }
        }

        private void Update()
        {
            HandleInputUpdate();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 初始化绑定主背包与装备栏组件。
        /// </summary>
        /// <param name="mainInventory">主背包封装组件</param>
        /// <param name="equipmentInventory">装备栏封装组件 (可选)</param>
        public virtual void Initialize(Inventory mainInventory, Inventory equipmentInventory = null)
        {
            _mainInventoryComponent = mainInventory;
            _equipmentInventoryComponent = equipmentInventory;

            IReadOnlyInventoryContainer mainContainer = mainInventory != null ? mainInventory.Container : null;
            IReadOnlyInventoryContainer equipContainer = equipmentInventory != null ? equipmentInventory.Container : null;

            if (_mainInventoryView != null && mainContainer != null)
            {
                _mainInventoryView.BindContainer(mainContainer);
            }

            if (_equipmentInventoryView != null && equipContainer != null && mainContainer != null)
            {
                _equipmentInventoryView.BindInventories(equipmentInventory, mainInventory);
            }

            _isInitialized = true;

            // 默认打开主库存视图
            ShowMainInventoryView();
        }

        /// <summary>
        /// 切换显示主库存界面。
        /// </summary>
        public virtual void ShowMainInventoryView()
        {
            _isShowingEquipmentView = false;

            if (_mainInventoryView != null)
            {
                _mainInventoryView.gameObject.SetActive(true);
            }

            if (_equipmentInventoryView != null)
            {
                _equipmentInventoryView.CloseSubMenu();
                _equipmentInventoryView.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 切换显示装备库存界面。
        /// </summary>
        public virtual void ShowEquipmentInventoryView()
        {
            if (_equipmentInventoryComponent == null && _equipmentInventoryView == null) return;

            _isShowingEquipmentView = true;

            if (_mainInventoryView != null)
            {
                _mainInventoryView.gameObject.SetActive(false);
            }

            if (_equipmentInventoryView != null)
            {
                _equipmentInventoryView.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 在主库存界面与装备界面之间进行无缝切换。
        /// </summary>
        public virtual void ToggleView()
        {
            if (_isShowingEquipmentView)
            {
                ShowMainInventoryView();
            }
            else
            {
                ShowEquipmentInventoryView();
            }
        }

        /// <summary>
        /// 动态替换输入提供者。
        /// </summary>
        /// <param name="inputProvider">符合 IInventoryInputProvider 接口的提供者实例</param>
        public virtual void SetInputProvider(IInventoryInputProvider inputProvider)
        {
            _inputProvider = inputProvider;
        }
        #endregion

        #region Private Helper Methods
        protected virtual void SetupInputProvider()
        {
            if (_customInputProvider is IInventoryInputProvider customProvider)
            {
                _inputProvider = customProvider;
                return;
            }

            if (TryGetComponent<IInventoryInputProvider>(out var componentProvider))
            {
                _inputProvider = componentProvider;
                return;
            }

            // 自动挂载默认键盘适配器组件
            _inputProvider = gameObject.AddComponent<DefaultInventoryInputProvider>();
        }

        protected virtual void HandleInputUpdate()
        {
            if (_inputProvider == null) return;

            InventoryInputData inputData = _inputProvider.GetInputData();

            // 1. 响应界面切换快捷键 (Tab 键)
            if (inputData.ToggleEquipment)
            {
                // 如果装备界面的二级菜单开着，按切换键优先关二级菜单，否则切换大界面
                if (_isShowingEquipmentView && _equipmentInventoryView != null && _equipmentInventoryView.IsSubMenuOpen)
                {
                    _equipmentInventoryView.CloseSubMenu();
                }
                else
                {
                    ToggleView();
                }
                return;
            }

            // 2. 将离散输入路由派发给当前处于激活状态的子视图
            if (_isShowingEquipmentView)
            {
                if (_equipmentInventoryView != null && _equipmentInventoryView.gameObject.activeSelf)
                {
                    // 在装备界面若按取消/返回键且未在二级菜单中，切回主背包界面
                    if (inputData.Cancel && !_equipmentInventoryView.IsSubMenuOpen)
                    {
                        ShowMainInventoryView();
                        return;
                    }

                    _equipmentInventoryView.ProcessInput(inputData);
                }
            }
            else
            {
                if (_mainInventoryView != null && _mainInventoryView.gameObject.activeSelf)
                {
                    _mainInventoryView.ProcessInput(inputData);
                }
            }
        }
        #endregion
    }
}
