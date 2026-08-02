using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 房间瓷砖的基础数据模型，定义了单个物理瓦片在拼装时的基础属性与摆放规则。
    /// </summary>
    [System.Serializable]
    public class Tile
    {
        #region 序列化字段与属性
        
        [Tooltip("瓦片的预制件对象")]
        public GameObject prefab;

        [Tooltip("加权生成的概率权重。权重越高，该瓦片在随机选择中被选中的概率越大")]
        [Range(0, 1000)]
        public int weight = 100;

        [Tooltip("在物理空间中的位置偏置值")]
        public Vector3 positionOffset = Vector3.zero;

        [Tooltip("在物理空间中的旋转偏置值")]
        public Vector3 rotationOffset = Vector3.zero;

        [Tooltip("是否允许在此瓦片表面随机摆放装饰品或怪点")]
        public bool allowDecor = true;

        [Tooltip("是否通过物理射线向下/向上投射，使装饰品表面贴合该瓦片物理面")]
        public bool alignToSurface = false;

        [Tooltip("贴合射线检测时需要过滤的图层")]
        public LayerMask tileLayer;

        [Tooltip("在物理空间中的缩放覆盖值")]
        public LockableVector3 scaleOverride = new LockableVector3(Vector3.one, true);

        public Tile()
        {
            Reset();
        }

        /// <summary>
        /// 重置所有属性为默认值
        /// </summary>
        public virtual void Reset()
        {
            weight = 100;
            positionOffset = Vector3.zero;
            rotationOffset = Vector3.zero;
            allowDecor = true;
            alignToSurface = false;
            scaleOverride = new LockableVector3(Vector3.one, true);
        }

        #endregion
    }

    /// <summary>
    /// 地板瓦片，支持步长为 90 度的随机旋转偏角。
    /// </summary>
    [System.Serializable]
    public class Floor : Tile
    {
        #region 序列化字段与属性

        [Tooltip("最大随机旋转数（0-3），每个整数单位代表 90 度的旋转（如 1代表 90度，3代表 270度）")]
        [Range(0, 3)]
        public int randomRotation = 0;

        public Floor() : base()
        {
        }

        /// <summary>
        /// 重置地板属性为默认值
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            randomRotation = 0;
        }

        #endregion
    }

    /// <summary>
    /// 墙体瓦片。
    /// </summary>
    [System.Serializable]
    public class Wall : Tile
    {
    }

    /// <summary>
    /// 屋顶瓦片，支持步长为 90 度的随机旋转偏角。
    /// </summary>
    [System.Serializable]
    public class Roof : Tile
    {
        #region 序列化字段与属性

        [Tooltip("最大随机旋转数（0-3），每个整数单位代表 90 度的旋转")]
        [Range(0, 3)]
        public int randomRotation = 0;

        public Roof() : base()
        {
        }

        /// <summary>
        /// 重置屋顶属性为默认值
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            randomRotation = 0;
        }

        #endregion
    }

    /// <summary>
    /// 门瓦片。
    /// </summary>
    [System.Serializable]
    public class Door : Tile
    {
    }

    /// <summary>
    /// 比例锁定的 Vector3 数据结构。
    /// 用于在 Inspector 中提供支持等比锁定的缩放值。
    /// </summary>
    [System.Serializable]
    public struct LockableVector3
    {
        [Tooltip("Vector3 的物理数值")]
        public Vector3 value;

        [Tooltip("是否锁定了三轴的比例")]
        public bool locked;

        public LockableVector3(Vector3 initialValue, bool isLocked = true)
        {
            value = initialValue;
            locked = isLocked;
        }

        /// <summary>
        /// 隐式转换，使得该结构可以直接作为 Vector3 使用
        /// </summary>
        public static implicit operator Vector3(LockableVector3 lockable)
        {
            return lockable.value;
        }
    }
}
