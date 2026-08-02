using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// Demo 专属物品资产解析器 (Item Asset Resolver)。
    /// 实现了 IItemAssetResolver 接口。
    /// 允许在 Inspector 中拖拽配置静态测试道具定义资产，在存盘序列化与反序列化读盘时实现 AssetKey 与 ScriptableObject 的互相解析映射。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/Demo/Demo Item Asset Resolver")]
    public class DemoItemAssetResolver : MonoBehaviour, IItemAssetResolver
    {
        #region Serialized Fields
        [Header("测试道具资产注册表")]
        [SerializeField]
        [Tooltip("注册在此处的测试 ItemDefinition 静态资产，用于存盘读盘时的 Token 映射解析")]
        private List<ItemDefinition> _registeredItems = new();
        #endregion

        #region Public Methods - IItemAssetResolver Implementation
        /// <summary>
        /// 根据物品 ScriptableObject 资产获取存盘 AssetKey Token (此处默认使用 asset 名称)。
        /// </summary>
        public string GetAssetKey(ItemDefinition definition)
        {
            if (definition == null) return string.Empty;
            return definition.name;
        }

        /// <summary>
        /// 根据存盘 Token (AssetKey) 解析并还原物品 ScriptableObject 资产。
        /// </summary>
        public ItemDefinition ResolveDefinition(string assetKey)
        {
            if (string.IsNullOrEmpty(assetKey) || _registeredItems == null) return null;

            int count = _registeredItems.Count;
            for (int i = 0; i < count; i++)
            {
                var itemDef = _registeredItems[i];
                if (itemDef != null && string.Equals(itemDef.name, assetKey, StringComparison.OrdinalIgnoreCase))
                {
                    return itemDef;
                }
            }

            Debug.LogWarning($"[DemoItemAssetResolver] 未能从注册表中找到名为 '{assetKey}' 的 ItemDefinition 资产！");
            return null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 运行时动态注册物品资产定义。
        /// </summary>
        public void RegisterItem(ItemDefinition definition)
        {
            if (definition != null && !_registeredItems.Contains(definition))
            {
                _registeredItems.Add(definition);
            }
        }
        #endregion
    }
}
