using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.NewRoomBuilder
{
    /// <summary>
    /// 装饰元素变体配置。
    /// 继承自 RoomElement 基类，特化了用于插槽过滤筛选、数量区间控制、防重叠安全距离以及自由随机缩放旋转的物理参数。
    /// 去除了多余的方向配置，直接继承并对齐所挂载插槽自带的方向契约。
    /// </summary>
    [System.Serializable]
    public class DecorationElement : RoomElement
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)



        [Header("投放数量与空间限制")]
        [Tooltip("在当前房间中，此装饰品所允许生成的最大与最小数量区间限制")]
        [SerializeField]
        private Vector2Int _amountRange = new Vector2Int(1, 3);

        [Tooltip("防重叠的安全排除半径。当某处生成此装饰物后，以此为中心的该半径内将禁止生成其他有 Spacing 约束的饰品")]
        [SerializeField]
        private float _spacing = 1.0f;

        [Header("随机变换控制")]
        [Tooltip("在区间内进行随机的缩放大小计算，以增加场景随机表现力")]
        [SerializeField]
        private Vector2 _scaleRange = new Vector2(0.9f, 1.1f);

        [Tooltip("在 Y 轴上旋转的最大随机偏角。0 表示不随机旋转，360 表示允许全方向自转")]
        [SerializeField]
        [Range(0f, 360f)]
        private float _randomRotationY = 360f;

        [Header("物理体积与全局排斥")]
        [Tooltip("是否为实体体积摆件。开启后，该物品将被视为具有体积的障碍物，其他勾选了【避让实体摆件】的物体生成时会避开它。")]
        [SerializeField]
        private bool _isVolumeObject = false;

        [Tooltip("是否避让其他一切实体体积摆件。开启后，该物品在生成时如果靠近任何已生成的【实体体积摆件】或同类物品，将拒绝生成。")]
        [SerializeField]
        private bool _avoidVolumeObjects = false;

        #endregion

        #region 3. 非序列化私有字段
        // 当前类无非序列化私有字段
        #endregion

        #region 4. 公共属性 (Properties)



        /// <summary>
        /// 获取生成数量的上下限区间限制。
        /// </summary>
        public Vector2Int AmountRange => _amountRange;

        /// <summary>
        /// 获取防穿模重叠的安全隔离半径。
        /// </summary>
        public float Spacing => _spacing;

        /// <summary>
        /// 获取随机缩放区间。
        /// </summary>
        public Vector2 ScaleRange => _scaleRange;

        /// <summary>
        /// 获取 Y 轴随机最大旋转角。
        /// </summary>
        public float RandomRotationY => _randomRotationY;

        /// <summary>
        /// 获取当前装饰品是否为实体体积摆件。
        /// </summary>
        public bool IsVolumeObject => _isVolumeObject;

        /// <summary>
        /// 获取是否避让其它一切实体体积摆件。
        /// </summary>
        public bool AvoidVolumeObjects => _avoidVolumeObjects;

        #endregion

        #region 5. 生命周期方法 (Unity Lifecycle)
        // 当前类无生命周期方法
        #endregion

        #region 6. 公共方法 (Public Methods)

        /// <summary>
        /// 重置此装饰件的所有字段（包含基类字段）为初始默认配置。
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            _amountRange = new Vector2Int(1, 3);
            _spacing = 1.0f;
            _scaleRange = new Vector2(0.9f, 1.1f);
            _randomRotationY = 360f;
            _isVolumeObject = false;
            _avoidVolumeObjects = false;
        }

        #endregion

        #region 7. 私有方法 (Private Methods)
        // 当前类无私有方法
        #endregion
    }
}
