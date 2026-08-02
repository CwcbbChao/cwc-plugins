using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 房间预设资源配置文件，用于集中定义某个主题或样式的关卡美术资产，支持在生成器中一键切换。
    /// </summary>
    [CreateAssetMenu(fileName = "NewRoomPreset", menuName = "Cwcbb/RoomBuilder/Room Preset", order = 1)]
    public class RoomPreset : ScriptableObject
    {
        #region 序列化字段与属性 (瓦片与结构配置)

        [Header("结构瓦片资产")]
        [Tooltip("可选的地板瓦片列表（加权选择）")]
        public List<Floor> floorTiles = new List<Floor>();

        [Tooltip("可选的墙体瓦片列表（加权选择）")]
        public List<Wall> wallTiles = new List<Wall>();

        [Tooltip("可选的墙角瓦片列表（加权选择）")]
        public List<Wall> wallCorners = new List<Wall>();

        [Tooltip("可选的屋顶瓦片列表（加权选择）")]
        public List<Roof> roofTiles = new List<Roof>();
        
        [Tooltip("可选的门通道瓦片列表（加权选择）")]
        public List<Door> doorTiles = new List<Door>();

        #endregion

        #region 序列化字段与属性 (装饰配置)

        [Header("室内与外部装饰品资产")]
        [Tooltip("在地板上生成的普通装饰摆件")]
        public List<Decoration> floorDecorations = new List<Decoration>();

        [Tooltip("在墙面上挂载的装饰摆件")]
        public List<Decoration> wallDecorations = new List<Decoration>();

        [Tooltip("在天花板上悬挂的装饰摆件")]
        public List<Decoration> roofDecorations = new List<Decoration>();

        [Tooltip("角色与高价值产物，如怪点、NPC点、交互宝箱等")]
        public List<Decoration> characters = new List<Decoration>();

        #endregion
    }
}
