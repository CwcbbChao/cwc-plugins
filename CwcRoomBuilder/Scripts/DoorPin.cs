using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 范围门销系统组件。
    /// 挂载此组件的游戏对象在 3D 场景中表现为一个包围盒，能自动裁减掉生成时重合的墙体节点以开辟通道，并可在原地生成门预制件。
    /// </summary>
    public class DoorPin : MonoBehaviour
    {
        #region 序列化字段与属性
        
        [Header("关联与物理配置")]
        [Tooltip("匹配的房间生成器标识 ID。只有与 RoomGenerator 的 generatorId 一致的 DoorPin 才会生效。")]
        public int generatorId = 0;

        [Tooltip("门销的影响范围大小（长、宽、高）。")]
        public Vector3 boundsSize = new Vector3(3f, 3f, 3f);

        [Tooltip("门销的本地坐标位置偏移（锚点偏置）")]
        public Vector3 positionOffset = Vector3.zero;

        #endregion

        #region 生命周期与绘制 (Unity Lifecycle & Gizmos)

        /// <summary>
        /// 当游戏对象被加载或在编辑器中绘制 Gizmos 时被调用，以锚点为一角向 XYZ 正方向绘制蓝色包围盒。
        /// </summary>
        protected virtual void OnDrawGizmos()
        {
            // 保存原有的 Gizmos 矩阵
            Matrix4x4 oldMatrix = Gizmos.matrix;
            
            // 将 Gizmos 矩阵设置为当前 Transform 的 local-to-world 矩阵，从而完美支持物体的旋转与缩放
            Gizmos.matrix = transform.localToWorldMatrix;

            // 1. 计算以 positionOffset 为 Min Corner (一角) 的包围盒中心点
            Vector3 center = positionOffset + boundsSize * 0.5f;

            // 2. 绘制检测包围盒
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.DrawCube(center, boundsSize);
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.6f);
            Gizmos.DrawWireCube(center, boundsSize);

            // 3. 绘制锚点 (在本地坐标系的 positionOffset 处)
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(positionOffset, 0.12f);

            // 如果存在相对原点(0,0,0)的偏移量，绘制一条引导连线
            if (positionOffset != Vector3.zero)
            {
                Gizmos.DrawLine(Vector3.zero, positionOffset);
            }

            // 恢复原有的 Gizmos 矩阵
            Gizmos.matrix = oldMatrix;
        }

        #endregion
    }
}
