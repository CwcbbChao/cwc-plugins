using UnityEngine;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// Demo 药水运行时组件 (Runtime Potion Component)。
    /// 继承泛型基类 ItemComponentBase<DemoPotionComponentDefinition>，持有一份配对的静态定义引用。
    /// 实现 IUsable 接口，使用后为使用者恢复生命值，并消耗 1 个堆叠数量。
    /// </summary>
    public class DemoPotionComponent : ItemComponentBase<DemoPotionComponentDefinition>, IUsable
    {
        #region Private Fields
        private InventoryContainer _currentContainer;
        private int _currentSlotIndex = -1;
        #endregion

        #region Public Properties
        /// <summary>
        /// 恢复生命值数量（读取定义中的 HealAmount 配置）。
        /// </summary>
        public int HealAmount => Definition != null ? Definition.HealAmount : 5;
        #endregion

        #region Constructors
        /// <summary>
        /// 构造函数，由 DemoPotionComponentDefinition 传递自身引用初始化。
        /// </summary>
        public DemoPotionComponent(DemoPotionComponentDefinition definition) : base(definition)
        {
        }
        #endregion

        #region Lifecycle Overrides
        public override void OnAddedToContainer(InventoryContainer container, int slotIndex)
        {
            _currentContainer = container;
            _currentSlotIndex = slotIndex;
        }

        public override void OnRemovedFromContainer(InventoryContainer container, int slotIndex)
        {
            _currentContainer = null;
            _currentSlotIndex = -1;
        }
        #endregion

        #region IUsable Implementation
        /// <summary>
        /// 判定当前药水是否可使用：需使用者拥有 DemoCharacter 且生命值未满。
        /// </summary>
        public bool CanUse(GameObject user)
        {
            if (user == null) return false;

            if (user.TryGetComponent<DemoCharacter>(out var character))
            {
                return character.CurrentHp < character.MaxHp;
            }

            return false;
        }

        /// <summary>
        /// 执行使用药水：恢复 5 点生命，并扣除 1 个药水堆叠。
        /// </summary>
        public bool OnUse(GameObject user)
        {
            if (!CanUse(user))
            {
                Debug.LogWarning("[DemoPotionComponent] 无法使用药水：使用者无效或生命值已满！");
                return false;
            }

            if (user.TryGetComponent<DemoCharacter>(out var character))
            {
                bool healed = character.Heal(HealAmount);
                if (healed)
                {
                    // 若药水处于容器中，通过容器事务扣除 1 个堆叠数量
                    if (_currentContainer != null && _currentSlotIndex >= 0)
                    {
                        _currentContainer.RemoveItemFromSlot(_currentSlotIndex, 1);
                        Debug.Log("[DemoPotionComponent] 已成功通过容器消耗 1 瓶药水！");
                    }
                    else
                    {
                        Debug.Log("[DemoPotionComponent] 已使用药水 (未在容器中挂载)。");
                    }

                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
