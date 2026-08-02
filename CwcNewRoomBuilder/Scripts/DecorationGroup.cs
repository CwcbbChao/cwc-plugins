using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.NewRoomBuilder
{
    /// <summary>
    /// 装饰元素组配置资源（ScriptableObject）。
    /// 包含一系列装饰变体元素，便于在不同的主预设中进行复用与组合。
    /// </summary>
    [CreateAssetMenu(fileName = "NewDecorationGroup", menuName = "Cwcbb/NewRoomBuilder/Decoration Group", order = 30)]
    public class DecorationGroup : ScriptableObject
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Header("组物理层级设置")]
        [Tooltip("此组中的全部装饰品在生成时，其下所有实例化对象递归设置的目标 Layer")]
        [SerializeField]
        private LayerMask _layer;

        [Header("装饰摆件资产列表")]
        [Tooltip("此组包含的所有摆件的配置投放规则列表")]
        [SerializeField]
        private List<DecorationElement> _decorations = new List<DecorationElement>();

        [Header("自动插槽设置")]
        [Tooltip("此组内的所有装饰元素统一匹配并挂载的父级插槽类型列表")]
        [SerializeField]
        private List<SlotType> _allowedSlots = new List<SlotType>();

        [Tooltip("此组物品生成后，其上提供的二级虚拟插槽类型，若为空则代表该物品无法再承载任何其他物体")]
        [SerializeField]
        private SlotType _providedSlotType;

        #endregion

        #region 3. 非序列化私有字段
        // 当前类无非序列化私有字段
        #endregion

        #region 4. 公共属性 (Properties)

        /// <summary>
        /// 获取此装饰组统一递归应用的目标 Layer 掩码。
        /// </summary>
        public LayerMask Layer => _layer;

        /// <summary>
        /// 获取此组包含的装饰物规则列表。
        /// </summary>
        public List<DecorationElement> Decorations => _decorations;

        /// <summary>
        /// 获取此装饰组允许匹配的父级插槽类型列表。
        /// </summary>
        public List<SlotType> AllowedSlots => _allowedSlots;

        /// <summary>
        /// 获取此装饰组生成的物品上提供的二级虚拟插槽类型。
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
