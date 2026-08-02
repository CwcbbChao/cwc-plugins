using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 抽象物品 UI 显示结构接口。
    /// 按照名称 + 图标 + 描述文本精简结构划分，解耦具体组件与 UI 视图渲染。
    /// 任何物品组件实现此接口后，均可被 UI 列表、详情面板与排序工具自动读取。
    /// </summary>
    public interface IItemDisplay
    {
        /// <summary>
        /// 物品显示名称。
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 物品显示图标。
        /// </summary>
        Sprite Icon { get; }

        /// <summary>
        /// 物品详细描述或动态格式化文本。
        /// </summary>
        string Description { get; }
    }
}
