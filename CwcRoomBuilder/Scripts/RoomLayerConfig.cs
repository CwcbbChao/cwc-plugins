using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 全局关卡图层配置文件，规范和统一物理检测及装饰品投放时的射线碰撞图层。
    /// </summary>
    [CreateAssetMenu(fileName = "RoomLayerConfig", menuName = "Cwcbb/RoomBuilder/Room Layer Config", order = 2)]
    public class RoomLayerConfig : ScriptableObject
    {
        #region 序列化字段与属性

        [Tooltip("地板检测所在的物理碰撞图层")]
        public LayerMask floorLayer;

        [Tooltip("墙壁检测所在的物理碰撞图层")]
        public LayerMask wallLayer;

        [Tooltip("装饰物检测所在的物理碰撞图层")]
        public LayerMask decorLayer;

        #endregion
    }
}
