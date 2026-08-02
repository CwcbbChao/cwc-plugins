using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 全局库存注册中心。
    /// 维护基于 InventoryID (如 "MainInventory", "PlayerEquipment", "Chest_01") 的全局映射。
    /// 允许 UI 和业务逻辑通过 ID 解耦查找与绑定库存，无需拖拽直接引用。
    /// </summary>
    public static class InventoryRegistry
    {
        #region Private Static Fields
        private static readonly Dictionary<string, InventoryContainer> _containers = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Inventory> _components = new(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region Public Static Events
        /// <summary>
        /// 当新的 InventoryID 被注册时触发。
        /// </summary>
        public static event Action<string, InventoryContainer> OnRegistered;

        /// <summary>
        /// 当 InventoryID 被注销时触发。
        /// </summary>
        public static event Action<string> OnUnregistered;
        #endregion

        #region Public Static Methods
        /// <summary>
        /// 注册一个指定 ID 的库存容器。
        /// </summary>
        /// <param name="inventoryId">库存唯一标识 ID</param>
        /// <param name="container">容器领域模型实体</param>
        /// <param name="ownerComponent">所属的 Mono 组件（可选）</param>
        /// <returns>若注册成功返回 true，若 ID 已存在或参数为空返回 false</returns>
        public static bool Register(string inventoryId, InventoryContainer container, Inventory ownerComponent = null)
        {
            if (string.IsNullOrEmpty(inventoryId) || container == null) return false;

            if (_containers.ContainsKey(inventoryId))
            {
                Debug.LogWarning($"[InventoryRegistry] 尝试注册重复的 InventoryID: '{inventoryId}'。已覆盖之前的注册。");
                _containers[inventoryId] = container;
            }
            else
            {
                _containers.Add(inventoryId, container);
            }

            if (ownerComponent != null)
            {
                _components[inventoryId] = ownerComponent;
            }

            OnRegistered?.Invoke(inventoryId, container);
            return true;
        }

        /// <summary>
        /// 注销指定 ID 的库存容器。
        /// </summary>
        public static bool Unregister(string inventoryId)
        {
            if (string.IsNullOrEmpty(inventoryId)) return false;

            bool removed = _containers.Remove(inventoryId);
            _components.Remove(inventoryId);

            if (removed)
            {
                OnUnregistered?.Invoke(inventoryId);
            }
            return removed;
        }

        /// <summary>
        /// 尝试根据 InventoryID 获取容器实体。
        /// </summary>
        public static bool TryGetContainer(string inventoryId, out InventoryContainer container)
        {
            if (string.IsNullOrEmpty(inventoryId))
            {
                container = null;
                return false;
            }
            return _containers.TryGetValue(inventoryId, out container);
        }

        /// <summary>
        /// 尝试根据 InventoryID 获取所属的 Mono 组件。
        /// </summary>
        public static bool TryGetComponent(string inventoryId, out Inventory ownerComponent)
        {
            if (string.IsNullOrEmpty(inventoryId))
            {
                ownerComponent = null;
                return false;
            }
            return _components.TryGetValue(inventoryId, out ownerComponent);
        }

        /// <summary>
        /// 检查指定 InventoryID 是否已注册。
        /// </summary>
        public static bool IsRegistered(string inventoryId)
        {
            if (string.IsNullOrEmpty(inventoryId)) return false;
            return _containers.ContainsKey(inventoryId);
        }

        /// <summary>
        /// 零 GC 填充获取全局注册中心中所有已注册且允许拾取路由 (IsRoutable == true) 的 Inventory 组件。
        /// </summary>
        /// <param name="results">输出接收列表</param>
        public static void GetRoutableComponents(List<Inventory> results)
        {
            if (results == null) return;
            results.Clear();

            foreach (var kvp in _components)
            {
                var inv = kvp.Value;
                if (inv != null && inv.IsInitialized && inv.IsRoutable)
                {
                    results.Add(inv);
                }
            }
        }

        /// <summary>
        /// 零 GC 填充获取全局注册中心中所有已注册的 Inventory 组件。
        /// </summary>
        /// <param name="results">输出接收列表</param>
        public static void GetAllComponents(List<Inventory> results)
        {
            if (results == null) return;
            results.Clear();

            foreach (var kvp in _components)
            {
                if (kvp.Value != null)
                {
                    results.Add(kvp.Value);
                }
            }
        }

        /// <summary>
        /// 清空全部注册记录（通常用于关卡切换或游戏重置）。
        /// </summary>
        public static void ClearAll()
        {
            _containers.Clear();
            _components.Clear();
        }
        #endregion
    }
}
