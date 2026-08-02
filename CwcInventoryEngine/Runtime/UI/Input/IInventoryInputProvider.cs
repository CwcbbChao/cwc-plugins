using UnityEngine;

namespace Cwc.InventoryEngine.UI
{
    /// <summary>
    /// 库存 UI 输入动作数据结构。
    /// 包装单帧内接收到的离散控制输入状态。
    /// </summary>
    public struct InventoryInputData
    {
        /// <summary>
        /// 导航移动方向向量 (X: -1/0/1, Y: -1/0/1)。
        /// </summary>
        public Vector2Int MoveDirection;

        /// <summary>
        /// 是否触发确认/选择动作。
        /// </summary>
        public bool Submit;

        /// <summary>
        /// 是否触发取消/返回/关闭动作。
        /// </summary>
        public bool Cancel;

        /// <summary>
        /// 是否触发上一个页签/分类动作 (例如 Q 键)。
        /// </summary>
        public bool TabPrev;

        /// <summary>
        /// 是否触发下一个页签/分类动作 (例如 E 键)。
        /// </summary>
        public bool TabNext;

        /// <summary>
        /// 是否触发列表上一页翻页动作 (例如 LT / PageUp)。
        /// </summary>
        public bool PagePrev;

        /// <summary>
        /// 是否触发列表下一页翻页动作 (例如 RT / PageDown)。
        /// </summary>
        public bool PageNext;

        /// <summary>
        /// 是否触发主库存与装备界面切换动作 (例如 Tab 键)。
        /// </summary>
        public bool ToggleEquipment;

        /// <summary>
        /// 是否触发物品使用动作 (例如 U 键)。
        /// </summary>
        public bool Use;

        /// <summary>
        /// 是否触发物品丢弃动作 (例如 G 键)。
        /// </summary>
        public bool Drop;

        /// <summary>
        /// 是否触发装备快捷卸下动作 (例如 X 键)。
        /// </summary>
        public bool Unequip;
    }

    /// <summary>
    /// 库存 UI 输入提供者接口。
    /// 解耦具体的输入框架（无论是 Legacy Input、New Input System 还是 Custom Manager）。
    /// </summary>
    public interface IInventoryInputProvider
    {
        /// <summary>
        /// 获取当前帧的输入数据状态。
        /// </summary>
        /// <returns>离散输入数据</returns>
        InventoryInputData GetInputData();
    }
}
