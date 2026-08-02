using UnityEngine;

namespace Cwcbb.Tools.NewRoomBuilder
{
    /// <summary>
    /// 结构元素变体配置。
    /// 继承自 RoomElement 基类，特化了用于体素网格无缝拼接对齐的物理参数。
    /// 去除了单独的对齐方向字段，改由所属 StructureGroup 统一管理。
    /// </summary>
    [System.Serializable]
    public class StructureElement : RoomElement
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Header("结构瓦片特有对齐配置")]
        [Tooltip("确定的轴向缩放覆盖，在生成时硬性应用于逻辑轴点父物体上。确保拼接时严丝合缝")]
        [SerializeField]
        private Vector3 _scaleOverride = Vector3.one;

        [Tooltip("是否允许离散网格旋转。开启后会在生成时随机应用 0, 90, 180, 270 度的旋转（专用于地板/天花板，打乱纹理规律性且保持网格严密对齐）")]
        [SerializeField]
        private bool _random90DegreeRotation = false;

        [Tooltip("加权生成的概率权重。权重越高，被随机选中的概率越大")]
        [SerializeField]
        [Range(0, 1000)]
        private int _weight = 100;

        #endregion

        #region 3. 非序列化私有字段
        // 当前类无非序列化私有字段
        #endregion

        #region 4. 公共属性 (Properties)

        /// <summary>
        /// 获取强制缩放覆盖值。
        /// </summary>
        public Vector3 ScaleOverride => _scaleOverride;

        /// <summary>
        /// 获取是否允许 90 度步长的离散旋转。
        /// </summary>
        public bool Random90DegreeRotation => _random90DegreeRotation;

        /// <summary>
        /// 获取生成的加权随机权重。
        /// </summary>
        public int Weight => _weight;

        #endregion

        #region 5. 生命周期方法 (Unity Lifecycle)
        // 当前类无生命周期方法
        #endregion

        #region 6. 公共方法 (Public Methods)

        /// <summary>
        /// 重置此结构件的所有字段（包含基类字段）为初始默认配置。
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _weight = 100;
            _scaleOverride = Vector3.one;
            _random90DegreeRotation = false;
        }

        #endregion

        #region 7. 私有方法 (Private Methods)
        // 当前类无私有方法
        #endregion
    }
}
