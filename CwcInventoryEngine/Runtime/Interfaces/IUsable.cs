using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 可使用/消耗物品标准接口 (Usable Interface)。
    /// 开发者可在自定义物品组件上实现此接口，用于处理药品、消耗品等物品的使用逻辑。
    /// </summary>
    public interface IUsable
    {
        /// <summary>
        /// 判定当前物品是否允许被目标使用者使用。
        /// </summary>
        /// <param name="user">使用者 GameObject 实体</param>
        /// <returns>若允许使用返回 true，否则返回 false</returns>
        bool CanUse(GameObject user);

        /// <summary>
        /// 执行物品使用逻辑。
        /// </summary>
        /// <param name="user">使用者 GameObject 实体</param>
        /// <returns>若成功使用返回 true，使用失败或条件不满足返回 false</returns>
        bool OnUse(GameObject user);
    }
}
