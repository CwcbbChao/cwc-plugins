using UnityEngine;
using UnityEngine.Events;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 开箱即用的通用场景掉落物/拾取物组件。
    /// 挂载在掉落物预制件根节点上，完全解耦具体的触发方式（碰撞体 Trigger/按键交互）与回收机制（销毁/放入对象池）。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/Item Pickup")]
    public class ItemPickup : MonoBehaviour, IItemPickup
    {
        #region Serialized Fields
        [Header("默认物品配置 (用于场景固定刷新的道具草药)")]
        [SerializeField]
        [Tooltip("静态物品定义 ScriptableObject")]
        private ItemDefinition _defaultDefinition;

        [SerializeField]
        [Tooltip("初始堆叠数量")]
        [Min(1)]
        private int _defaultCount = 1;

        [Header("安全与自动化控制")]
        [SerializeField]
        [Tooltip("拾取完全成功后是否锁定该组件，拒绝后续所有重复拾取请求 (防止连击刷物品漏洞)")]
        private bool _preventMultiplePickups = true;

        [SerializeField]
        [Tooltip("拾取完全成功后是否自动调用 gameObject.SetActive(false) (快捷隐藏/准备入池)")]
        private bool _autoDisableGameObject = false;

        [Header("拾取成功回调事件")]
        [SerializeField]
        [Tooltip("当该掉落物被成功拾取吸收时触发的回调事件 (可在 Inspector 中绑定播音效、播放动画或对象池回收)")]
        private UnityEvent<ItemInstance> _onPickedUpSuccess;
        #endregion

        #region Private Fields
        private ItemInstance _runtimeInstance;
        private bool _isPickedUp = false;
        #endregion

        #region Public Properties
        /// <summary>
        /// 当前持有的运行时物品实例。
        /// </summary>
        public ItemInstance CurrentItem => _runtimeInstance;

        /// <summary>
        /// 当前掉落物是否已经被完整拾取锁定。
        /// </summary>
        public bool IsPickedUp => _isPickedUp;

        /// <summary>
        /// 拾取成功回调事件。
        /// </summary>
        public UnityEvent<ItemInstance> OnPickedUpSuccess => _onPickedUpSuccess;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_runtimeInstance == null && _defaultDefinition != null)
            {
                _runtimeInstance = _defaultDefinition.CreateInstance(_defaultCount);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 动态绑定运行时物品实例 (常用于怪物死亡爆装备或玩家主动丢弃物品)，并自动重置拾取锁定状态。
        /// </summary>
        /// <param name="instance">运行时物品实例</param>
        public virtual void BindInstance(ItemInstance instance)
        {
            _runtimeInstance = instance;
            _isPickedUp = false;
        }

        /// <summary>
        /// 重置拾取锁状态 (用于对象池回收重用或场景资源点定时刷新)。
        /// </summary>
        public virtual void ResetPickupState()
        {
            _isPickedUp = false;
        }

        /// <summary>
        /// 【基础拾取接口】：直接将物品放入指定的目标库存组件中。
        /// </summary>
        /// <param name="targetInventory">目标库存组件</param>
        /// <returns>若成功放入返回 true，否则返回 false</returns>
        public virtual bool TryPickup(Inventory targetInventory)
        {
            // 防重复拾取保护锁校验
            if (_preventMultiplePickups && _isPickedUp) return false;
            if (targetInventory == null || _runtimeInstance == null || _runtimeInstance.StackCount <= 0) return false;

            if (targetInventory.TryAddItem(_runtimeInstance, out var remainder))
            {
                ItemInstance pickedInstance = _runtimeInstance;
                _runtimeInstance = remainder; // 若背包空间不足，余数保留在掉落物中

                // 当所有数量被完全拾取时，加锁防止连击触发
                if (_runtimeInstance == null || _runtimeInstance.StackCount <= 0)
                {
                    if (_preventMultiplePickups)
                    {
                        _isPickedUp = true;
                    }

                    _onPickedUpSuccess?.Invoke(pickedInstance);

                    if (_autoDisableGameObject)
                    {
                        gameObject.SetActive(false);
                    }
                }
                else
                {
                    // 仅部分拾取（背包满剩余部分）：依然触发事件，但不加防重锁，留待下次继续拾取余数
                    _onPickedUpSuccess?.Invoke(pickedInstance);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 【全局路由拾取接口】：传入拾取者 GameObject (可选)，全自动通过全局路由器 (InventoryRouter) 寻找最佳目标库存执行分发。
        /// </summary>
        /// <param name="user">触发拾取的拾取者/玩家 GameObject (可选)</param>
        /// <returns>若成功放入任何目标库存返回 true，否则返回 false</returns>
        public virtual bool TryPickupGlobal(GameObject user = null)
        {
            return InventoryRouter.RouteAndPickupGlobal(this, user);
        }

        /// <summary>
        /// 【兼容拾取接口】：传入拾取者 GameObject，全自动调用全局路由执行智能分发。
        /// </summary>
        /// <param name="picker">拾取者 GameObject (如 Player 实体)</param>
        /// <returns>若成功放入任何目标库存返回 true，否则返回 false</returns>
        public virtual bool TryPickupWithPicker(GameObject picker)
        {
            return InventoryRouter.RouteAndPickupGlobal(this, picker);
        }

        #region UnityEvent / Inspector Event Binding Overrides (void Returns)
        /// <summary>
        /// 【UnityEvent 专属 Inspector 动态绑定入口】：
        /// 专门供 UnityEvent&lt;GameObject&gt; (如 BaseInteractable 的 OnActivationEvent) 动态绑定，无返回值 void。
        /// </summary>
        /// <param name="user">触发拾取的 GameObject (由 UnityEvent 动态传入)</param>
        public void TriggerPickupGlobal(GameObject user)
        {
            TryPickupGlobal(user);
        }

        /// <summary>
        /// 【UnityEvent 专属 Inspector 静态绑定入口】：
        /// 无参数 void 返回值方法，供 UnityEvent 静态无参回调触发。
        /// </summary>
        public void TriggerPickupGlobal()
        {
            TryPickupGlobal(null);
        }
        #endregion

        #endregion
    }
}
