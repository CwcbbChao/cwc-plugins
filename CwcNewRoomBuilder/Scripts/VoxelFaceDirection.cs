using System;

namespace Cwcbb.Tools.NewRoomBuilder
{
    /// <summary>
    /// 体素表面暴露朝向位掩码。
    /// 标记为 System.Flags，支持在 Inspector 中进行多选，并为侧向提供了快捷多选方式。
    /// </summary>
    [Flags]
    public enum VoxelFaceDirection
    {
        /// <summary>
        /// 无朝向
        /// </summary>
        None = 0,

        /// <summary>
        /// 顶面（天花板）
        /// </summary>
        Up = 1 << 0,      // 1

        /// <summary>
        /// 底面（地板）
        /// </summary>
        Down = 1 << 1,    // 2

        /// <summary>
        /// 前面墙壁（北）
        /// </summary>
        Forward = 1 << 2, // 4

        /// <summary>
        /// 后面墙壁（南）
        /// </summary>
        Back = 1 << 3,    // 8

        /// <summary>
        /// 左面墙壁（西）
        /// </summary>
        Left = 1 << 4,    // 16

        /// <summary>
        /// 右面墙壁（东）
        /// </summary>
        Right = 1 << 5,   // 32

        /// <summary>
        /// 所有水平墙壁朝向的快捷位组合（前、后、左、右）
        /// </summary>
        Horizontal = Forward | Back | Left | Right, // 60

        /// <summary>
        /// 所有朝向
        /// </summary>
        All = Up | Down | Horizontal // 63
    }
}
