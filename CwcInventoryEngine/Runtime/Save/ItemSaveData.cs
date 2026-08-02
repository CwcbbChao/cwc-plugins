using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 单个物品实例的序列化持久化传输 DTO。
    /// 完全解耦具体加载框架，仅包含 Token 化的 AssetKey、Guid、堆叠数以及组件增量数据。
    /// </summary>
    [Serializable]
    public class ItemSaveData
    {
        #region Serialized DTO Fields
        /// <summary>
        /// 物品实例的 Guid 字符串。
        /// </summary>
        public string InstanceGuid;

        /// <summary>
        /// 由 IItemAssetResolver 解析导出的静态资产标识 Token。
        /// </summary>
        public string AssetKey;

        /// <summary>
        /// 当前堆叠数量。
        /// </summary>
        public int StackCount;

        /// <summary>
        /// 组件自治序列化导出的动态增量状态列表。
        /// </summary>
        public List<ComponentSaveEntry> ComponentStates = new();
        #endregion

        #region Export Logic
        /// <summary>
        /// 将运行时 ItemInstance 导出为安全的标准 DTO 存盘数据。
        /// </summary>
        public static ItemSaveData Export(ItemInstance instance, IItemAssetResolver resolver)
        {
            if (instance == null || resolver == null) return null;

            string assetKey = resolver.GetAssetKey(instance.Definition);
            if (string.IsNullOrEmpty(assetKey))
            {
                Debug.LogWarning($"[CwcInventoryEngine] 无法为物品资产 '{instance.Definition.name}' 获取 AssetKey！");
                return null;
            }

            ItemSaveData saveData = new ItemSaveData
            {
                InstanceGuid = instance.InstanceID.ToString(),
                AssetKey = assetKey,
                StackCount = instance.StackCount,
                ComponentStates = new List<ComponentSaveEntry>()
            };

            // 存盘自治：遍历所有的组件定义，提取对应的动态组件状态
            var components = instance.Components;
            int compCount = components.Count;

            for (int i = 0; i < compCount; i++)
            {
                var runtimeComp = components[i];
                if (runtimeComp == null) continue;

                // 查找静态定义层
                if (instance.Definition.TryGetComponentDefinition(runtimeComp.GetType(), out var compDef))
                {
                    if (compDef.TryExportState(instance, out string jsonData) && !string.IsNullOrEmpty(jsonData))
                    {
                        saveData.ComponentStates.Add(new ComponentSaveEntry
                        {
                            ComponentType = runtimeComp.GetType().FullName,
                            JsonData = jsonData
                        });
                    }
                }
            }

            return saveData;
        }
        #endregion

        #region Import Logic
        /// <summary>
        /// 从 DTO 存盘数据还原 ItemInstance，并具备完善的读盘容错机制。
        /// </summary>
        public ItemInstance Restore(IItemAssetResolver resolver)
        {
            if (resolver == null)
            {
                Debug.LogError("[CwcInventoryEngine] 还原物品失败：未提供 IItemAssetResolver！");
                return null;
            }

            ItemDefinition def = resolver.ResolveDefinition(AssetKey);
            if (def == null)
            {
                Debug.LogWarning($"[CwcInventoryEngine] 无法根据 AssetKey '{AssetKey}' 解析对应的 ItemDefinition 资产！");
                return null;
            }

            ItemId parsedId = ItemId.Parse(InstanceGuid);
            if (parsedId.IsEmpty)
            {
                parsedId = ItemId.NewId();
            }

            // 创建带指定 ID 的实例
            ItemInstance instance = def.CreateInstanceWithId(parsedId, StackCount);

            // 读盘自治还原组件状态
            if (ComponentStates != null && ComponentStates.Count > 0)
            {
                int stateCount = ComponentStates.Count;
                for (int i = 0; i < stateCount; i++)
                {
                    var entry = ComponentStates[i];
                    if (entry == null || string.IsNullOrEmpty(entry.ComponentType)) continue;

                    // 匹配运行时组件
                    if (TryFindRuntimeComponentByType(instance, entry.ComponentType, out var runtimeComp))
                    {
                        if (def.TryGetComponentDefinition(runtimeComp.GetType(), out var compDef))
                        {
                            compDef.ImportState(instance, runtimeComp, entry.JsonData);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[CwcInventoryEngine] 存档中含有类型为 '{entry.ComponentType}' 的组件，但在该物品定义 '{def.name}' 中未找到对应的匹配项，已自动跳过。");
                    }
                }
            }

            // 触发读盘完成生命周期
            instance.TriggerLoadedLifecycle();
            return instance;
        }

        private bool TryFindRuntimeComponentByType(ItemInstance instance, string typeFullName, out ItemComponentBase runtimeComp)
        {
            var components = instance.Components;
            int count = components.Count;
            for (int i = 0; i < count; i++)
            {
                if (components[i].GetType().FullName == typeFullName)
                {
                    runtimeComp = components[i];
                    return true;
                }
            }
            runtimeComp = null;
            return false;
        }
        #endregion
    }
}
