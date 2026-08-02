using System;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 静态物品丢弃配置组件。
    /// 可通过 [SerializeReference] 挂载在 ItemDefinition 的 ComponentDefinitions 列表中，用于控制物品的丢弃行为与特定 Prefab。
    /// </summary>
    [Serializable]
    [ItemComponentPath("通用/物品掉落 Drop")]
    public class ItemDropComponentDefinition : ItemComponentDefinition
    {
        #region 序列化字段
        [SerializeField]
        [Tooltip("该物品是否允许丢弃。若设为 false，丢弃请求将被直接拒绝 (例如任务道具、绑定装备)。")]
        private bool _canDrop = true;

        [SerializeField]
        [Tooltip("该物品专用的场景掉落物 Prefab (需挂载 ItemPickup 组件)。若为空，将回退使用项目的默认掉落物 Prefab。")]
        private GameObject _customDropPrefab;

        [SerializeField]
        [Tooltip("丢弃该物品时播放的自定义音效 (可选)。")]
        private AudioClip _dropSound;
        #endregion

        #region 公共属性
        public override Type ComponentType => typeof(ItemDropComponent);

        /// <summary>
        /// 是否允许丢弃。
        /// </summary>
        public bool CanDrop => _canDrop;

        /// <summary>
        /// 专属掉落物预制件 (可为 null)。
        /// </summary>
        public GameObject CustomDropPrefab => _customDropPrefab;

        /// <summary>
        /// 丢弃音效 (可为 null)。
        /// </summary>
        public AudioClip DropSound => _dropSound;
        #endregion

        #region 工厂方法
        public override ItemComponentBase CreateRuntime()
        {
            return new ItemDropComponent(this);
        }
        #endregion
    }

    /// <summary>
    /// 运行时物品掉落控制组件。
    /// 持有一份静态 ItemDropComponentDefinition 的引用，并支持运行时动态覆盖丢弃状态或专属 Prefab。
    /// </summary>
    public class ItemDropComponent : ItemComponentBase<ItemDropComponentDefinition>
    {
        #region 动态覆盖字段
        private bool? _customCanDropOverride;
        private GameObject _customDropPrefabOverride;
        #endregion

        #region 公共属性
        /// <summary>
        /// 当前是否允许丢弃 (若有动态覆盖则使用覆盖值，否则使用静态定义)。
        /// </summary>
        public bool CanDrop => _customCanDropOverride ?? Definition.CanDrop;

        /// <summary>
        /// 当前专属掉落物 Prefab (若有动态覆盖则使用覆盖值，否则使用静态定义)。
        /// </summary>
        public GameObject CustomDropPrefab => _customDropPrefabOverride != null ? _customDropPrefabOverride : Definition.CustomDropPrefab;

        /// <summary>
        /// 丢弃音效。
        /// </summary>
        public AudioClip DropSound => Definition.DropSound;
        #endregion

        #region 构造函数
        public ItemDropComponent(ItemDropComponentDefinition definition) : base(definition)
        {
        }
        #endregion

        #region 动态控制方法
        /// <summary>
        /// 运行时动态设置/锁定该物品实例的丢弃状态 (例如装备绑定后禁止丢弃)。
        /// </summary>
        public void SetCanDrop(bool canDrop)
        {
            _customCanDropOverride = canDrop;
        }

        /// <summary>
        /// 运行时动态重置丢弃状态锁定。
        /// </summary>
        public void ClearCanDropOverride()
        {
            _customCanDropOverride = null;
        }

        /// <summary>
        /// 运行时动态覆盖掉落 Prefab。
        /// </summary>
        public void SetCustomDropPrefab(GameObject dropPrefab)
        {
            _customDropPrefabOverride = dropPrefab;
        }
        #endregion
    }
}
