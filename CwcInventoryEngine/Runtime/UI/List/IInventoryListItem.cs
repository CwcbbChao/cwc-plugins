using System;

namespace Cwc.InventoryEngine.UI
{
    /// <summary>
    /// 库存列表可管理 UI 单元项接口。
    /// 所有要在 InventoryListUIController 中展示和管理的 UI 单元组件需实现此接口。
    /// </summary>
    public interface IInventoryListItem
    {
        /// <summary>
        /// 当该 UI 单元被绑定的数据对象改变时回调。
        /// </summary>
        /// <param name="data">绑定的数据源对象（可能是 ItemSlot、ItemInstance 或其他视图数据）</param>
        /// <param name="dataIndex">在全局数据源中的绝对索引</param>
        void OnBindData(object data, int dataIndex);

        /// <summary>
        /// 当该 UI 单元的选择/焦点状态发生改变时回调。
        /// </summary>
        /// <param name="isSelected">是否处于选中焦点状态</param>
        void OnSelectionChanged(bool isSelected);

        /// <summary>
        /// 设置单元的显示/隐藏状态。
        /// </summary>
        /// <param name="visible">是否可见</param>
        void SetVisible(bool visible);
    }
}
