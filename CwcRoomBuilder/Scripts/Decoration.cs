using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 装饰品与怪点配置数据模型，定义了在房间各表面（地板、墙壁、天花板）生成摆件的物理偏移、间距防重叠和数量范围。
    /// </summary>
    [System.Serializable]
    public class Decoration
    {
        #region 序列化字段与属性

        [Tooltip("装饰品的预制件对象")]
        public GameObject prefab;

        [Tooltip("相对装点位置的额外偏移量")]
        public Vector3 positionOffset = Vector3.zero;

        [Tooltip("相对装点位置的额外旋转角偏量")]
        public Vector3 rotationOffset = Vector3.zero;

        [Tooltip("在物理空间中的基础缩放覆盖值")]
        public LockableVector3 scaleOverride = new LockableVector3(Vector3.one, true);

        [Tooltip("自身占用半径，防止同类道具重叠生成。在此距离范围内的其他同类型点将被跳过")]
        public float spacing = 1.0f;

        [Tooltip("安全物理半径，防止在此半径内生成其他任何类型的装饰道具")]
        public float safeArea = 1.0f;

        [Tooltip("随机缩放的上下限区间")]
        public Vector2 scaleRange = new Vector2(1f, 1f);

        [Tooltip("随机旋转的最大偏角 (0-360)")]
        [Range(0f, 360f)]
        public float randomRotation = 0f;

        [Tooltip("每间房间生成的数量上限与下限区间")]
        public Vector2Int amountRange = new Vector2Int(1, 3);

        [Tooltip("生成高度限制范围（相对于当前层地板的最小和最大 Y 轴高度偏移量）")]
        public Vector2 verticalRange = new Vector2(0f, 5f);

        public Decoration()
        {
            Reset();
        }

        /// <summary>
        /// 重置所有装饰品属性为默认值
        /// </summary>
        public void Reset()
        {
            positionOffset = Vector3.zero;
            rotationOffset = Vector3.zero;
            scaleOverride = new LockableVector3(Vector3.one, true);
            spacing = 1.0f;
            safeArea = 1.0f;
            scaleRange = new Vector2(1f, 1f);
            randomRotation = 0f;
            amountRange = new Vector2Int(1, 3);
            verticalRange = new Vector2(0f, 5f);
        }

        #endregion
    }
}
