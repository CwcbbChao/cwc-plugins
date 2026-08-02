using UnityEngine;
using Cwc.InventoryEngine.UI;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// Demo 背包与 UI 综合测试控制器组件。
    /// 一站式集成道具动态生成、存档读盘测试以及全局 InventoryUIController 视图绑定的配合逻辑。
    /// 自动在 Game 视图渲染 OnGUI 测试操控小面板。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/Demo/Demo Inventory Test Controller")]
    public class DemoInventoryTestController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("核心 UI 控制器与面板绑定")]
        [SerializeField]
        [Tooltip("UI 顶层统一控制器组件")]
        private InventoryUIController _uiController;

        [Header("目标库存组件")]
        [SerializeField]
        [Tooltip("主背包 Inventory")]
        private Inventory _mainInventoryComponent;

        [SerializeField]
        [Tooltip("装备栏 Inventory")]
        private Inventory _equipmentInventoryComponent;

        [Header("存读盘测试管理器")]
        [SerializeField]
        [Tooltip("DemoSaveLoadManager 组件")]
        private DemoSaveLoadManager _saveLoadManager;

        [Header("测试用的物品静态资产定义 (ItemDefinition)")]
        [SerializeField]
        [Tooltip("测试装备：大剑 (带装备与耐久组件)")]
        private ItemDefinition _swordDefinition;

        [SerializeField]
        [Tooltip("测试装备：头盔 (带装备组件)")]
        private ItemDefinition _helmetDefinition;

        [SerializeField]
        [Tooltip("测试装备：胸甲/护甲 (带装备组件)")]
        private ItemDefinition _armorDefinition;

        [SerializeField]
        [Tooltip("测试消耗品：生命药水 (可堆叠 99)")]
        private ItemDefinition _potionDefinition;

        [SerializeField]
        [Tooltip("测试材料：宝石 (可堆叠 20)")]
        private ItemDefinition _gemDefinition;

        [Header("OnGUI 调试面板配置")]
        [SerializeField]
        [Tooltip("是否在 Game 视图左上角绘制 GUI 测试小面板")]
        private bool _showDebugGUI = true;

        [SerializeField]
        [Tooltip("调试窗口位置与尺寸")]
        private Rect _guiWindowRect = new Rect(20, 20, 360, 480);
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            AutoFindComponents();
        }

        private void Start()
        {
            InitializeDemo();
        }

        private void OnGUI()
        {
            if (_showDebugGUI)
            {
                _guiWindowRect = GUI.Window(9991, _guiWindowRect, DrawDebugWindow, "Cwc Inventory Engine - 测试小面板");
            }
        }
        #endregion

        #region Public Demo Actions
        /// <summary>
        /// 生成测试装备：大剑 x1
        /// </summary>
        public void SpawnSword()
        {
            SpawnItem(_swordDefinition, 1, "大剑");
        }

        /// <summary>
        /// 生成测试装备：头盔 x1
        /// </summary>
        public void SpawnHelmet()
        {
            SpawnItem(_helmetDefinition, 1, "头盔");
        }

        /// <summary>
        /// 生成测试装备：护甲 x1
        /// </summary>
        public void SpawnArmor()
        {
            SpawnItem(_armorDefinition, 1, "胸甲护甲");
        }

        /// <summary>
        /// 生成测试消耗品：生命药水 x5
        /// </summary>
        public void SpawnPotion()
        {
            SpawnItem(_potionDefinition, 5, "生命药水 x5");
        }

        /// <summary>
        /// 生成测试材料：宝石 x10
        /// </summary>
        public void SpawnGem()
        {
            SpawnItem(_gemDefinition, 10, "宝石 x10");
        }

        /// <summary>
        /// 触发存盘
        /// </summary>
        public void TriggerSave()
        {
            if (_saveLoadManager != null)
            {
                _saveLoadManager.SaveInventory();
            }
        }

        /// <summary>
        /// 触发读盘还原
        /// </summary>
        public void TriggerLoad()
        {
            if (_saveLoadManager != null)
            {
                _saveLoadManager.LoadInventory();
            }
        }

        /// <summary>
        /// 仅清空内存背包（不抹煞磁盘存档记录，专用于测试“Save -> 清空背包 -> Load 还原”流程）
        /// </summary>
        public void TriggerClearInventoryOnly()
        {
            if (_mainInventoryComponent != null && _mainInventoryComponent.Container != null)
            {
                _mainInventoryComponent.Container.ClearContainer();
            }
            if (_equipmentInventoryComponent != null && _equipmentInventoryComponent.Container != null)
            {
                _equipmentInventoryComponent.Container.ClearContainer();
            }
            Debug.Log("[DemoInventoryTestController] 内存背包已清空（磁盘 Save 存档依然保留，可点击 Load 还原）！");
        }

        /// <summary>
        /// 彻底清空内存背包与磁盘 Save 存档
        /// </summary>
        public void TriggerClearAll()
        {
            TriggerClearInventoryOnly();
            if (_saveLoadManager != null)
            {
                _saveLoadManager.ClearSaveData();
            }
            Debug.Log("[DemoInventoryTestController] 磁盘 Save 存档也已彻底清空！");
        }
        #endregion

        #region Private Methods
        private void AutoFindComponents()
        {
            if (_uiController == null) _uiController = GetComponentInChildren<InventoryUIController>(true);
            if (_saveLoadManager == null) _saveLoadManager = GetComponent<DemoSaveLoadManager>();

            var comps = GetComponents<Inventory>();
            if (comps.Length >= 2)
            {
                if (_mainInventoryComponent == null) _mainInventoryComponent = comps[0];
                if (_equipmentInventoryComponent == null) _equipmentInventoryComponent = comps[1];
            }
            else if (comps.Length == 1 && _mainInventoryComponent == null)
            {
                _mainInventoryComponent = comps[0];
            }
        }

        private void InitializeDemo()
        {
            if (_uiController != null && _mainInventoryComponent != null)
            {
                _uiController.Initialize(_mainInventoryComponent, _equipmentInventoryComponent);
            }
        }

        private void SpawnItem(ItemDefinition def, int count, string itemName)
        {
            if (def == null)
            {
                Debug.LogWarning($"[DemoInventoryTestController] 生成失败：未分配 {itemName} 的 ItemDefinition 资产！");
                return;
            }

            if (_mainInventoryComponent == null || _mainInventoryComponent.Container == null)
            {
                Debug.LogError("[DemoInventoryTestController] 生成失败：未指定主背包 Inventory！");
                return;
            }

            ItemInstance instance = def.CreateInstance(count);
            if (_mainInventoryComponent.Container.TryAddItem(instance, out var remainder))
            {
                Debug.Log($"[DemoInventoryTestController] 成功添加 {itemName} 到主背包！");
            }
            else
            {
                Debug.LogWarning($"[DemoInventoryTestController] 添加 {itemName} 部分或全部失败（背包已满），剩余数量: {remainder?.StackCount ?? 0}");
            }
        }

        private void DrawDebugWindow(int windowID)
        {
            GUILayout.Space(10);
            GUILayout.Label("【快捷刷物品测试】", GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 大剑")) SpawnSword();
            if (GUILayout.Button("+ 头盔")) SpawnHelmet();
            if (GUILayout.Button("+ 护甲")) SpawnArmor();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 药水 x5")) SpawnPotion();
            if (GUILayout.Button("+ 宝石 x10")) SpawnGem();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("【存读盘与数据测试】", GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save (存盘)")) TriggerSave();
            if (GUILayout.Button("Load (读盘)")) TriggerLoad();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("仅清空内存背包")) TriggerClearInventoryOnly();
            if (GUILayout.Button("彻底删存档")) TriggerClearAll();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("【界面切换与导航测试】", GUI.skin.box);
            if (_uiController != null)
            {
                string stateText = _uiController.IsShowingEquipmentView ? "当前界面: [装备栏界面]" : "当前界面: [主背包界面]";
                GUILayout.Label(stateText);

                if (GUILayout.Button("切换界面 (Tab)"))
                {
                    _uiController.ToggleView();
                }
            }

            GUILayout.Space(15);
            GUILayout.Label("⌨️ 按键指引：\n• W/S/A/D 或 方向键：选择列表项\n• Q / E 键：切换主背包分类\n• Tab 键：切换主背包 / 装备界面\n• Space / Enter 键：确认/唤起二级菜单/装备替换\n• Esc / X 键：取消 / 关闭二级菜单", GUI.skin.box);

            GUI.DragWindow();
        }
        #endregion
    }
}
