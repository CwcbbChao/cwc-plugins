using System;

namespace Cwcbb.Tools.NewRoomBuilder
{
    /// <summary>
    /// 插槽在物理空间中的挂载/插入方向位域枚举。
    /// 用于对齐和过滤合适的装饰品挂载。
    /// </summary>
    [Flags]
    public enum SlotDirection
    {
        /// <summary>
        /// 无方向约束。
        /// </summary>
        None = 0,

        /// <summary>
        /// 垂直朝上，通常用于地板、桌面等物体的上表面。
        /// </summary>
        Up = 1 << 0,

        /// <summary>
        /// 垂直朝下，通常用于天花板底面、吊顶、柜子底面等悬挂处。
        /// </summary>
        Down = 1 << 1,

        /// <summary>
        /// 水平方向，通常用于墙面挂载、垂直立面等。
        /// </summary>
        Horizontal = 1 << 2,

        /// <summary>
        /// 任意方向。
        /// </summary>
        Any = Up | Down | Horizontal
    }
}
