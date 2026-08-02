using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 可装备物品标准接口 (Equippable Interface)。
    /// 开发者可在自定义物品组件上实现此接口，用于处理武器、防具等装备的穿戴与脱下逻辑。
    /// </summary>
    public interface IEquippable
    {
        /// <summary>
        /// 判定当前装备是否允许被目标角色穿戴。
        /// </summary>
        /// <param name="user">角色/使用者 GameObject 实体</param>
        /// <returns>若符合穿戴条件返回 true，否则返回 false</returns>
        bool CanEquip(GameObject user);

        /// <summary>
        /// 执行装备穿戴逻辑（如增加面板属性、播放装备音效、挂载特效）。
        /// </summary>
        /// <param name="user">角色/使用者 GameObject 实体</param>
        void OnEquip(GameObject user);

        /// <summary>
        /// 执行装备脱下逻辑（如扣除面板属性、移除装备特效）。
        /// </summary>
        /// <param name="user">角色/使用者 GameObject 实体</param>
        void OnUnequip(GameObject user);
    }
}
