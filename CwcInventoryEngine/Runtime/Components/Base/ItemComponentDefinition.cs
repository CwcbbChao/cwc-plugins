using System;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 静态物品组件定义抽象基类 (纯 C# 多态序列化类)。
    /// 配置在 ItemDefinition 的 ComponentDefinitions 列表中 (通过 [SerializeReference] 序列化)。
    /// 负责工厂创建对应的 ItemComponentBase 以及实现存盘自治 (Save/Load Autonomy)。
    /// 
    /// ⚠️ 【架构设计原则与约束】：
    /// 1. 严格分离“定义”与“组件”：本定义类纯粹用于 Inspector 静态配置与工厂创建，【严禁实现任何业务操作/查询接口】。
    /// 2. 外部业务逻辑（UI 渲染、属性评估、数据修改等）绝对禁止绕过组件直接操作定义，所有逻辑若想生效，必须由工厂 <see cref="CreateRuntime"/> 创建实际的动态组件 <see cref="ItemComponentBase"/> 在组件中执行。
    /// </summary>
    [Serializable]
    public abstract class ItemComponentDefinition
    {
        #region Factory Method
        /// <summary>
        /// 对应的运行时组件类型。
        /// </summary>
        public abstract Type ComponentType { get; }

        /// <summary>
        /// 创建与之对应的运行时可变组件实例。
        /// </summary>
        /// <returns>运行时组件实例</returns>
        public abstract ItemComponentBase CreateRuntime();
        #endregion

        #region Save / Load Autonomy
        /// <summary>
        /// 存盘自治：从运行时实例中提取需要持久化的状态数据。
        /// </summary>
        /// <param name="instance">物品实例</param>
        /// <param name="jsonData">导出导出的 JSON 状态文本</param>
        /// <returns>若该组件有需要持久化的动态数据返回 true，否则返回 false</returns>
        public virtual bool TryExportState(ItemInstance instance, out string jsonData)
        {
            jsonData = null;
            return false;
        }

        /// <summary>
        /// 读盘自治：将持久化的 JSON 状态还原到运行时组件中。
        /// </summary>
        /// <param name="instance">物品实例</param>
        /// <param name="runtimeComp">目标运行时组件</param>
        /// <param name="jsonData">持久化的 JSON 状态文本</param>
        public virtual void ImportState(ItemInstance instance, ItemComponentBase runtimeComp, string jsonData) { }
        #endregion
    }
}
