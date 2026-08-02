using System;

namespace Cwc.InventoryEngine.Query
{
    /// <summary>
    /// 物品/组件动态属性提取提供者接口。
    /// 【定位】专用于支持 UI 与查询引擎的动态条件筛选 (Filtering) 与 Key 数值/文本排序 (Sorting)。
    /// （例如暴露: "RequiredLevel", "AttackBonus", "SlotType", "AffixCount" 等可计算或可比较的属性）。
    /// 
    /// ⚠️ 避坑提示：
    /// 对于 Icon (Sprite)、DisplayName、Description 等固定的基础视觉渲染，请优先使用强类型的 <see cref="IItemDisplay"/> 接口，
    /// 避免将 Sprite 资源滥用为弱类型 Key，以保障最佳的编译期类型安全与渲染性能。
    /// </summary>
    public interface IItemPropertyProvider
    {
        #region Public Methods
        /// <summary>
        /// 尝试提取指定 Key 的关键属性或元数据。
        /// </summary>
        /// <param name="key">属性名称标识符（区分大小写或忽略大小写取决于评估器配置）</param>
        /// <param name="value">提取到的轻量属性值 (ItemPropertyValue)</param>
        /// <returns>若成功找到该属性则返回 true，否则返回 false</returns>
        bool TryGetProperty(string key, out ItemPropertyValue value);
        #endregion
    }
}
