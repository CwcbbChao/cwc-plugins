using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 可自定义的拾取路由策略接口。
    /// 允许项目层扩展特定的路由算法。
    /// </summary>
    public interface IInventoryRoutePolicy
    {
        /// <summary>
        /// 针对给定的物品实例与使用者实体，寻找最佳的目标库存。
        /// </summary>
        /// <param name="item">待路由的物品实例</param>
        /// <param name="user">使用者 GameObject 实体 (可为 null)</param>
        /// <returns>目标库存，若无合适库存返回 null</returns>
        Inventory ResolveTargetInventory(ItemInstance item, GameObject user);
    }

    /// <summary>
    /// 全局背包拾取智能路由器。
    /// 完全脱离对物理节点 (如玩家 GameObject) 的硬绑定，100% 依托 InventoryRegistry 全局注册中心与库存收纳自查能力 (IsRoutable & CanAcceptItem) 进行解耦交互。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/Inventory Router")]
    public class InventoryRouter : MonoBehaviour
    {
        #region Serialized Fields
        [Header("默认路由目标配置")]
        [SerializeField]
        [Tooltip("兜底使用的主背包 InventoryId (默认 MainInventory)")]
        private string _mainInventoryId = "MainInventory";
        #endregion

        #region Private Static Fields
        private static readonly List<Inventory> _globalInventoryBuffer = new();
        private static IInventoryRoutePolicy _customPolicy;
        private static string _defaultMainInventoryId = "MainInventory";
        #endregion

        #region Private Fields
        private readonly List<Inventory> _cachedInventories = new();
        #endregion

        #region Public Static Properties
        /// <summary>
        /// 全局默认兜底主背包 ID。
        /// </summary>
        public static string DefaultMainInventoryId
        {
            get => _defaultMainInventoryId;
            set => _defaultMainInventoryId = value;
        }

        /// <summary>
        /// 自定义全局路由策略实现。
        /// </summary>
        public static IInventoryRoutePolicy CustomPolicy
        {
            get => _customPolicy;
            set => _customPolicy = value;
        }
        #endregion

        #region Public Instance Properties
        /// <summary>
        /// 实例组件配置的兜底主背包 ID。
        /// </summary>
        public string MainInventoryId
        {
            get => _mainInventoryId;
            set => _mainInventoryId = value;
        }
        #endregion

        #region Global Static Routing APIs
        /// <summary>
        /// 全局静态路由入口：为掉落物拾取寻找最合适的目标库存并执行拾取。
        /// 完全依托全局注册中心，不受玩家 GameObject 销毁/重建影响。
        /// </summary>
        /// <param name="pickup">掉落物拾取组件/接口</param>
        /// <param name="user">触发拾取的角色/实体 GameObject (可选)</param>
        /// <returns>若成功放入合适的目标库存返回 true，否则返回 false</returns>
        public static bool RouteAndPickupGlobal(IItemPickup pickup, GameObject user = null)
        {
            if (pickup == null || pickup.CurrentItem == null) return false;

            ItemInstance item = pickup.CurrentItem;
            Inventory targetInv = ResolveTargetInventoryGlobal(item, user);

            if (targetInv != null)
            {
                return pickup.TryPickup(targetInv);
            }

            return false;
        }

        /// <summary>
        /// 全局静态解析入口：智能计算给定的物品应该放入哪个目标库存。
        /// 100% 依托 InventoryRegistry 全局注册中心中的可路由 (IsRoutable == true) 库存集。
        /// </summary>
        /// <param name="item">待路由的物品实例</param>
        /// <param name="user">使用者 GameObject 实体 (可选，用于自定义扩展筛选)</param>
        /// <returns>最佳目标库存，若无法放入则返回 null</returns>
        public static Inventory ResolveTargetInventoryGlobal(ItemInstance item, GameObject user = null)
        {
            if (item == null) return null;

            // 1. 如果配置了自定义路由策略，优先交给项目扩展判定
            if (_customPolicy != null)
            {
                Inventory customTarget = _customPolicy.ResolveTargetInventory(item, user);
                if (customTarget != null && customTarget.IsRoutable && customTarget.CanAcceptItem(item))
                {
                    return customTarget;
                }
            }

            // 2. 从 InventoryRegistry 全局注册中心提取所有声明允许路由 (IsRoutable == true) 的已注册库存
            _globalInventoryBuffer.Clear();
            InventoryRegistry.GetRoutableComponents(_globalInventoryBuffer);

            if (_globalInventoryBuffer.Count == 0) return null;

            Inventory mainFallbackInv = null;

            // 3. 第一优先级：寻找带有专用槽位分类限制的路由库存 (HasCategoryRestriction && CanAcceptItem)，实现拾取自动穿戴/入栏
            int count = _globalInventoryBuffer.Count;
            for (int i = 0; i < count; i++)
            {
                var inv = _globalInventoryBuffer[i];
                if (inv == null || !inv.IsInitialized || !inv.IsRoutable) continue;

                if (string.Equals(inv.InventoryId, _defaultMainInventoryId, StringComparison.OrdinalIgnoreCase))
                {
                    mainFallbackInv = inv;
                }

                // 若库存声明了专用分类限制且自查能够收纳，直接路由
                if (inv.HasCategoryRestriction && inv.CanAcceptItem(item))
                {
                    return inv;
                }
            }

            // 4. 第二优先级：放入允许路由的主背包
            if (mainFallbackInv != null && mainFallbackInv.IsRoutable && mainFallbackInv.CanAcceptItem(item))
            {
                return mainFallbackInv;
            }

            return null;
        }
        #endregion

        #region Component Instance Routing Methods
        /// <summary>
        /// 【组件局部路由 API】针对全局注册中心执行路由与拾取。
        /// </summary>
        /// <param name="pickup">目标掉落物组件/接口</param>
        /// <returns>若成功放入目标库存返回 true</returns>
        public virtual bool RouteAndPickup(IItemPickup pickup)
        {
            if (pickup == null || pickup.CurrentItem == null) return false;

            ItemInstance item = pickup.CurrentItem;
            Inventory targetInv = ResolveTargetInventory(item);

            if (targetInv != null)
            {
                return pickup.TryPickup(targetInv);
            }

            return false;
        }

        /// <summary>
        /// 【组件局部解析 API】选择目标库存。
        /// </summary>
        /// <param name="item">待路由的物品实例</param>
        /// <returns>目标库存组件</returns>
        public virtual Inventory ResolveTargetInventory(ItemInstance item)
        {
            if (item == null) return null;
            return ResolveTargetInventoryGlobal(item, gameObject);
        }
        #endregion
    }
}
