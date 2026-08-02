using System;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 组件数据持久化传输 DTO。
    /// 仅基于 C# 类型全名或稳定 TypeID 进行组件数据识别，严禁基于数组下标索引。
    /// </summary>
    [Serializable]
    public class ComponentSaveEntry
    {
        /// <summary>
        /// 组件类型全名 (AssemblyQualifiedName 或 FullName)。
        /// </summary>
        public string ComponentType;

        /// <summary>
        /// 由组件自治 TryExportState 导出的 JSON 状态字符串。
        /// </summary>
        public string JsonData;
    }
}
