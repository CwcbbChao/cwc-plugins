using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.NewRoomBuilder
{
    /// <summary>
    /// 结构元素组配置资源（ScriptableObject）。
    /// 包含一系列结构变体元素与此组支持的体素暴露面对齐方向（使用 Flags 位掩码表示），便于在不同的预设中进行复用与组合。
    /// </summary>
    [CreateAssetMenu(fileName = "NewStructureGroup", menuName = "Cwcbb/NewRoomBuilder/Structure Group", order = 20)]
    public class StructureGroup : ScriptableObject
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Header("组物理层级设置")]
        [Tooltip("此结构元素组在生成时，其下所有实例化对象递归设置的目标 Layer")]
        [SerializeField]
        private LayerMask _layer;

        [Header("暴露方向匹配")]
        [Tooltip("此组所支持与适用的体素暴露方向位掩码。支持在下拉列表中多选。侧向墙壁可以直接勾选 Horizontal，地板勾选 Down，天花板勾选 Up")]
        [SerializeField]
        private VoxelFaceDirection _supportedDirections = VoxelFaceDirection.All;

        [Header("结构瓦片资产列表")]
        [Tooltip("此组包含的全部结构件摆放规则列表")]
        [SerializeField]
        private List<StructureElement> _elements = new List<StructureElement>();

        [Header("自动插槽设置")]
        [Tooltip("此结构组生成的瓦片所提供的虚拟插槽类型，若为空则不提供")]
        [SerializeField]
        private SlotType _providedSlotType;

        #endregion

        #region 3. 非序列化私有字段
        // 当前类无非序列化私有字段
        #endregion

        #region 4. 公共属性 (Properties)

        /// <summary>
        /// 获取此结构组统一递归应用的目标 Layer 掩码。
        /// </summary>
        public LayerMask Layer => _layer;

        /// <summary>
        /// 获取此组所支持的暴露方向位掩码。
        /// </summary>
        public VoxelFaceDirection SupportedDirections => _supportedDirections;

        /// <summary>
        /// 获取此组包含的结构件规则列表。
        /// </summary>
        public List<StructureElement> Elements => _elements;

        /// <summary>
        /// 获取此结构组生成的瓦片所提供的虚拟插槽类型。
        /// </summary>
        public SlotType ProvidedSlotType => _providedSlotType;

        #endregion

        #region 5. 生命周期方法 (Unity Lifecycle)
        // 当前类无生命周期方法
        #endregion

        #region 6. 公共方法 (Public Methods)
        // 当前类无公共方法
        #endregion

        #region 7. 私有方法 (Private Methods)
        // 当前类无私有方法
        #endregion
    }
}
