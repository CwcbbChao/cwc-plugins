using System;
using System.Collections.Generic;
using UnityEngine;
using Cwc.InventoryEngine;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// Demo 物品实例动作静态扩展类 (Demo Item Instance Extensions)。
    /// 提供针对 ItemInstance 的静态扩充 API，无需修改核心库即可直接实现使用、装备、取消装备等通用业务操作。
    /// 内部使用缓存列表遍历模式，做到零堆内存垃圾分配 (Zero GC Alloc)。
    /// </summary>
    public static class DemoItemInstanceExtensions
    {
        #region Private Static Fields
        /// <summary>
        /// IUsable 接口结果临时缓存列表，用以消除 GC Alloc。
        /// </summary>
        private static readonly List<IUsable> s_usableCache = new();

        /// <summary>
        /// IEquippable 接口结果临时缓存列表，用以消除 GC Alloc。
        /// </summary>
        private static readonly List<IEquippable> s_equippableCache = new();
        #endregion

        #region Public Static Methods - Usable Extensions
        /// <summary>
        /// 判断物品是否包含可使用组件且当前允许被目标使用者使用。
        /// </summary>
        /// <param name="item">目标物品实例</param>
        /// <param name="user">使用者 GameObject 实体</param>
        /// <returns>若可以被使用返回 true，否则返回 false</returns>
        public static bool CanUse(this ItemInstance item, GameObject user)
        {
            if (item == null || user == null) return false;

            s_usableCache.Clear();
            item.GetComponents(s_usableCache);

            if (s_usableCache.Count == 0) return false;

            for (int i = 0; i < s_usableCache.Count; i++)
            {
                if (s_usableCache[i].CanUse(user))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 尝试使用物品。遍历并执行所有实现 IUsable 接口的组件逻辑。
        /// </summary>
        /// <param name="item">目标物品实例</param>
        /// <param name="user">使用者 GameObject 实体</param>
        /// <returns>若成功触发至少一个组件的使用逻辑返回 true，否则返回 false</returns>
        public static bool TryUse(this ItemInstance item, GameObject user)
        {
            if (item == null || user == null) return false;

            s_usableCache.Clear();
            item.GetComponents(s_usableCache);

            if (s_usableCache.Count == 0) return false;

            bool anyUsed = false;
            for (int i = 0; i < s_usableCache.Count; i++)
            {
                var usable = s_usableCache[i];
                if (usable.CanUse(user))
                {
                    if (usable.OnUse(user))
                    {
                        anyUsed = true;
                    }
                }
            }

            return anyUsed;
        }
        #endregion

        #region Public Static Methods - Equippable Extensions
        /// <summary>
        /// 判断物品是否包含可装备组件且当前允许被目标角色穿戴。
        /// </summary>
        /// <param name="item">目标物品实例</param>
        /// <param name="user">角色/使用者 GameObject 实体</param>
        /// <returns>若允许穿戴返回 true，否则返回 false</returns>
        public static bool CanEquip(this ItemInstance item, GameObject user)
        {
            if (item == null || user == null) return false;

            s_equippableCache.Clear();
            item.GetComponents(s_equippableCache);

            if (s_equippableCache.Count == 0) return false;

            for (int i = 0; i < s_equippableCache.Count; i++)
            {
                if (s_equippableCache[i].CanEquip(user))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 尝试穿戴装备。遍历并执行所有实现 IEquippable 接口的组件 OnEquip 穿戴逻辑。
        /// </summary>
        /// <param name="item">目标物品实例</param>
        /// <param name="user">角色/使用者 GameObject 实体</param>
        /// <returns>若成功穿戴返回 true，否则返回 false</returns>
        public static bool TryEquip(this ItemInstance item, GameObject user)
        {
            if (item == null || user == null) return false;

            s_equippableCache.Clear();
            item.GetComponents(s_equippableCache);

            if (s_equippableCache.Count == 0) return false;

            for (int i = 0; i < s_equippableCache.Count; i++)
            {
                var equippable = s_equippableCache[i];
                if (equippable.CanEquip(user))
                {
                    equippable.OnEquip(user);
                }
            }

            return true;
        }

        /// <summary>
        /// 尝试脱下/取消装备。遍历并执行所有实现 IEquippable 接口的组件 OnUnequip 脱下逻辑。
        /// </summary>
        /// <param name="item">目标物品实例</param>
        /// <param name="user">角色/使用者 GameObject 实体</param>
        /// <returns>若成功执行脱下逻辑返回 true，否则返回 false</returns>
        public static bool TryUnequip(this ItemInstance item, GameObject user)
        {
            if (item == null || user == null) return false;

            s_equippableCache.Clear();
            item.GetComponents(s_equippableCache);

            if (s_equippableCache.Count == 0) return false;

            for (int i = 0; i < s_equippableCache.Count; i++)
            {
                s_equippableCache[i].OnUnequip(user);
            }

            return true;
        }
        #endregion
    }
}
