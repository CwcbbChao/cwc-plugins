using UnityEngine;

namespace Cwcbb.Tools.NewRoomBuilder
{
    /// <summary>
    /// 所有可生成物体（包含结构瓦片和装饰摆件）的基础房间元素数据模型基类。
    /// 抽取了三维生成和定位所需的公共物理参数，便于进行统一的工厂化实例化及偏移对齐。
    /// </summary>
    [System.Serializable]
    public class RoomElement
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Tooltip("瓦片或摆件的预制件对象")]
        [SerializeField]
        private GameObject _prefab;



        [Tooltip("在本地坐标系中的位置偏移偏置值")]
        [SerializeField]
        private Vector3 _positionOffset = Vector3.zero;

        [Tooltip("在本地坐标系中的旋转角度偏移偏置值")]
        [SerializeField]
        private Vector3 _rotationOffset = Vector3.zero;

        #endregion

        #region 3. 非序列化私有字段
        // 当前类无非序列化私有字段
        #endregion

        #region 4. 公共属性 (Properties)

        /// <summary>
        /// 获取生成的预制件对象。
        /// </summary>
        public GameObject Prefab => _prefab;



        /// <summary>
        /// 获取在本地空间的位置偏差。
        /// </summary>
        public Vector3 PositionOffset => _positionOffset;

        /// <summary>
        /// 获取在本地空间的旋转角度偏差。
        /// </summary>
        public Vector3 RotationOffset => _rotationOffset;

        #endregion

        #region 5. 生命周期方法 (Unity Lifecycle)
        // 当前类无生命周期方法
        #endregion

        #region 6. 公共方法 (Public Methods)
        
        /// <summary>
        /// 重置此元素的所有字段为初始默认配置。
        /// </summary>
        public virtual void Reset()
        {
            _prefab = null;
            _positionOffset = Vector3.zero;
            _rotationOffset = Vector3.zero;
        }

        #endregion

        #region 7. 私有方法 (Private Methods)
        // 当前类无私有方法
        #endregion
    }
}
