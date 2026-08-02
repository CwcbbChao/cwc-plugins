using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.NewRoomBuilder
{
    /// <summary>
    /// 房间美术预设资源配置文件（ScriptableObject）。
    /// 用于集中拼装一个关卡主题。不直接引用具体 Prefab，而是通过引用一组或多组结构组和装饰组来实现高度模块化的混搭复用。
    /// </summary>
    [CreateAssetMenu(fileName = "NewRoomPreset", menuName = "Cwcbb/NewRoomBuilder/Room Preset", order = 40)]
    public class RoomPreset : ScriptableObject
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Header("绑定的结构元素组")]
        [Tooltip("此预设所引用的多组结构件库列表（用于生成墙壁、地板、天花板）")]
        [SerializeField]
        private List<StructureGroup> _structureGroups = new List<StructureGroup>();

        [Header("绑定的装饰品组")]
        [Tooltip("此预设所引用的多组装饰件/家具库列表")]
        [SerializeField]
        private List<DecorationGroup> _decorationGroups = new List<DecorationGroup>();

        #endregion

        #region 3. 非序列化私有字段
        // 当前类无非序列化私有字段
        #endregion

        #region 4. 公共属性 (Properties)

        /// <summary>
        /// 获取此预设中引用的结构组列表。
        /// </summary>
        public List<StructureGroup> StructureGroups => _structureGroups;

        /// <summary>
        /// 获取此预设中引用的装饰组列表。
        /// </summary>
        public List<DecorationGroup> DecorationGroups => _decorationGroups;

        #endregion

        #region 5. 生命周期方法 (Unity Lifecycle)
        // 当前类无生命周期方法
        #endregion

        #region 6. 公共方法 (Public Methods)
        // 当前类无公共方法
        #endregion

        #region 7. 私有方法 (Private Methods)
        // 当前类无私有方法
        #endregion
    }
}
