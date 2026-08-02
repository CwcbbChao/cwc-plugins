using UnityEngine;

namespace Cwcbb.Tools.NewRoomBuilder
{
    /// <summary>
    /// 插槽类型定义配置（SocketSO）。
    /// 作为一个纯标识性的 ScriptableObject，用于在 Inspector 中作为插槽类型的唯一标识。
    /// 同时自身携带其默认天然的挂载方向属性。
    /// </summary>
    [CreateAssetMenu(fileName = "NewSlotType", menuName = "Cwcbb/NewRoomBuilder/Slot Type", order = 10)]
    public class SlotType : ScriptableObject
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Header("插槽默认物理性质")]
        [Tooltip("此插槽类型自带的默认挂载方向属性")]
        [SerializeField]
        private SlotDirection _defaultDirection = SlotDirection.Up;

        #endregion

        #region 3. 非序列化私有字段
        // 当前类无非序列化私有字段
        #endregion

        #region 4. 公共属性 (Properties)

        /// <summary>
        /// 获取此插槽的默认挂载方向。
        /// </summary>
        public SlotDirection DefaultDirection => _defaultDirection;

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
