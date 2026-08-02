using UnityEngine;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// Demo 存盘与读盘管理器组件。
    /// 演示如何使用 InventorySaveData 与 IItemAssetResolver 进行真正的底层序列化与反序列化测试。
    /// 数据默认持久化存储至 PlayerPrefs 键值对中。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/Demo/Demo Save Load Manager")]
    public class DemoSaveLoadManager : MonoBehaviour
    {
        #region Serialized Fields
        [Header("需要保存的目标库存组件")]
        [SerializeField]
        [Tooltip("主背包 Inventory")]
        private Inventory _mainInventoryComponent;

        [SerializeField]
        [Tooltip("角色装备栏 Inventory")]
        private Inventory _equipmentInventoryComponent;

        [SerializeField]
        [Tooltip("用于资产还原的 DemoItemAssetResolver（若未拖拽，将在 Awake 时自动查找）")]
        private DemoItemAssetResolver _assetResolver;

        [Header("存储配置")]
        [SerializeField]
        [Tooltip("PlayerPrefs 保存主背包 JSON 的 Key 名称")]
        private string _mainSaveKey = "Cwc_Demo_Main_Inventory_Save";

        [SerializeField]
        [Tooltip("PlayerPrefs 保存装备栏 JSON 的 Key 名称")]
        private string _equipmentSaveKey = "Cwc_Demo_Equipment_Inventory_Save";
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            AutoFindComponents();
        }
        #endregion

        #region Public Methods - Save & Load APIs
        /// <summary>
        /// 执行存盘序列化：导出主背包与装备栏当前物品并写入 PlayerPrefs。
        /// </summary>
        public bool SaveInventory()
        {
            if (_assetResolver == null)
            {
                Debug.LogError("[DemoSaveLoadManager] 存盘失败：未找到 DemoItemAssetResolver！");
                return false;
            }

            bool mainSuccess = SaveContainer(_mainInventoryComponent, _mainSaveKey, "主背包");
            bool equipSuccess = SaveContainer(_equipmentInventoryComponent, _equipmentSaveKey, "装备栏");

            return mainSuccess && equipSuccess;
        }

        /// <summary>
        /// 执行读盘反序列化：从 PlayerPrefs 读取 JSON 并还原主背包与装备栏。
        /// </summary>
        public bool LoadInventory()
        {
            if (_assetResolver == null)
            {
                Debug.LogError("[DemoSaveLoadManager] 读盘失败：未找到 DemoItemAssetResolver！");
                return false;
            }

            bool mainSuccess = LoadContainer(_mainInventoryComponent, _mainSaveKey, "主背包");
            bool equipSuccess = LoadContainer(_equipmentInventoryComponent, _equipmentSaveKey, "装备栏");

            return mainSuccess || equipSuccess;
        }

        /// <summary>
        /// 清除本地所有存档记录。
        /// </summary>
        public void ClearSaveData()
        {
            DeleteKeyIfExists(_mainSaveKey, "主背包");
            DeleteKeyIfExists(_equipmentSaveKey, "装备栏");
        }
        #endregion

        #region Private Methods
        private void AutoFindComponents()
        {
            if (_assetResolver == null)
            {
                _assetResolver = GetComponent<DemoItemAssetResolver>();
                if (_assetResolver == null)
                {
                    _assetResolver = GetComponentInChildren<DemoItemAssetResolver>(true);
                }
            }

            if (_mainInventoryComponent == null || _equipmentInventoryComponent == null)
            {
                var comps = GetComponents<Inventory>();
                if (comps.Length >= 2)
                {
                    _mainInventoryComponent = comps[0];
                    _equipmentInventoryComponent = comps[1];
                }
                else if (comps.Length == 1)
                {
                    _mainInventoryComponent = comps[0];
                }
            }
        }

        private bool SaveContainer(Inventory comp, string saveKey, string labelName)
        {
            if (comp == null || !comp.IsInitialized) return false;

            InventorySaveData saveData = InventorySaveData.Export(comp.Container, _assetResolver);
            if (saveData == null) return false;

            string json = JsonUtility.ToJson(saveData, true);
            PlayerPrefs.SetString(saveKey, json);
            PlayerPrefs.Save();

            Debug.Log($"[DemoSaveLoadManager] {labelName} 存盘成功！SaveKey: '{saveKey}', 长度: {json.Length} 字节。");
            return true;
        }

        private bool LoadContainer(Inventory comp, string saveKey, string labelName)
        {
            if (comp == null || !comp.IsInitialized || !PlayerPrefs.HasKey(saveKey)) return false;

            string json = PlayerPrefs.GetString(saveKey);
            if (string.IsNullOrEmpty(json)) return false;

            InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);
            if (saveData == null) return false;

            saveData.RestoreToContainer(comp.Container, _assetResolver);
            Debug.Log($"[DemoSaveLoadManager] {labelName} 读盘还原成功！SaveKey: '{saveKey}', 还原了 {saveData.Slots?.Count ?? 0} 个槽位。");
            return true;
        }

        private void DeleteKeyIfExists(string saveKey, string labelName)
        {
            if (PlayerPrefs.HasKey(saveKey))
            {
                PlayerPrefs.DeleteKey(saveKey);
                PlayerPrefs.Save();
                Debug.Log($"[DemoSaveLoadManager] 已删除 {labelName} 存档 Key: '{saveKey}'");
            }
        }
        #endregion
    }
}
