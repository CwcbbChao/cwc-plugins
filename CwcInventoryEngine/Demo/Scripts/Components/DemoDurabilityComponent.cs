using System;
using UnityEngine;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// Demo 动态耐久度运行时组件 (Runtime Item Component)。
    /// 存在于动态内存中，跟随 ItemInstance 生命周期，负责记录与修改动态耐久度。
    /// 演示动态组件干预堆叠逻辑（如不同耐久度的武器不可堆叠）以及生命周期 Hook 回调。
    /// </summary>
    public class DemoDurabilityComponent : ItemComponentBase, IItemDisplay, IUsable, IEquippable
    {
        #region Private Fields
        private int _maxDurability;
        private int _currentDurability;
        #endregion

        #region Public Properties
        /// <summary>
        /// 最大耐久度。
        /// </summary>
        public int MaxDurability => _maxDurability;

        /// <summary>
        /// 当前耐久度。
        /// </summary>
        public int CurrentDurability => _currentDurability;

        /// <summary>
        /// 耐久度比例 (0.0 ~ 1.0)。
        /// </summary>
        public float DurabilityPercent => _maxDurability > 0 ? (float)_currentDurability / _maxDurability : 0f;

        /// <summary>
        /// 是否已经损坏（耐久为 0）。
        /// </summary>
        public bool IsBroken => _currentDurability <= 0;
        #endregion

        #region Public Events
        /// <summary>
        /// 当耐久度发生变化时触发的回调 (current, max)。
        /// </summary>
        public event Action<int, int> OnDurabilityChanged;
        #endregion

        #region Constructors
        /// <summary>
        /// 构造函数。
        /// </summary>
        public DemoDurabilityComponent(int maxDurability)
        {
            _maxDurability = Mathf.Max(1, maxDurability);
            _currentDurability = _maxDurability;
        }
        #endregion

        #region Stack Logic & Lifecycle Overrides
        /// <summary>
        /// 堆叠兼容性逻辑重写：
        /// 只有当两个物品的当前耐久度完全相等时，才允许堆叠；否则拒绝堆叠。
        /// </summary>
        public override bool IsStackCompatible(ItemComponentBase other)
        {
            if (other is DemoDurabilityComponent otherDurability)
            {
                return _currentDurability == otherDurability.CurrentDurability && _maxDurability == otherDurability.MaxDurability;
            }
            return false;
        }

        public override void OnInstanceCreated(ItemInstance instance)
        {
            // 首次创建时默认满耐久
        }

        public override void OnInstanceLoaded(ItemInstance instance)
        {
            // 读盘恢复完成后触发
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 扣除/消耗指定数量的耐久度。
        /// </summary>
        /// <param name="amount">扣除值</param>
        /// <returns>若已被扣损坏返回 true</returns>
        public bool ConsumeDurability(int amount)
        {
            if (amount <= 0 || _currentDurability <= 0) return _currentDurability <= 0;

            _currentDurability = Mathf.Max(0, _currentDurability - amount);
            OnDurabilityChanged?.Invoke(_currentDurability, _maxDurability);
            return _currentDurability <= 0;
        }

        /// <summary>
        /// 修复指定数量的耐久度。
        /// </summary>
        public void Repair(int amount)
        {
            if (amount <= 0) return;

            _currentDurability = Mathf.Min(_maxDurability, _currentDurability + amount);
            OnDurabilityChanged?.Invoke(_currentDurability, _maxDurability);
        }

        /// <summary>
        /// 内部/读盘还原使用的直接设置接口。
        /// </summary>
        public void SetCurrentDurability(int currentDurability)
        {
            _currentDurability = Mathf.Clamp(currentDurability, 0, _maxDurability);
            OnDurabilityChanged?.Invoke(_currentDurability, _maxDurability);
        }
        #endregion

        #region IUsable Implementation
        /// <summary>
        /// 判定大剑是否可使用：未损坏（当前耐久 > 0）即可使用。
        /// </summary>
        public bool CanUse(GameObject user)
        {
            return !IsBroken;
        }

        /// <summary>
        /// 执行大剑使用逻辑：挥舞大剑，消耗 1 点耐久。
        /// </summary>
        public bool OnUse(GameObject user)
        {
            if (IsBroken)
            {
                Debug.LogWarning("[DemoDurabilityComponent] 无法挥舞大剑：大剑已彻底损坏！");
                return false;
            }

            bool isBroken = ConsumeDurability(1);
            Debug.Log($"[DemoDurabilityComponent] 挥舞大剑！扣除 1 点耐久，当前剩余耐久: {_currentDurability}/{_maxDurability}" + (isBroken ? " (警告: 大剑已被消耗损坏！)" : ""));
            return true;
        }
        #endregion

        #region IEquippable Implementation
        /// <summary>
        /// 判定物品是否满足穿戴耐久要求（未损坏）。
        /// </summary>
        public bool CanEquip(GameObject user)
        {
            return !IsBroken;
        }

        public void OnEquip(GameObject user)
        {
            // 耐久度组件仅做装配校验，不介入攻击力数值修改
        }

        public void OnUnequip(GameObject user)
        {
            // 耐久度组件仅做装配校验，不介入攻击力数值修改
        }
        #endregion

        #region IItemDisplay Implementation (Demo Interface Extension)
        /// <summary>
        /// 示例：业务组件可根据需要实现 IItemDisplay 接口。
        /// 当设置更高 Priority 时，UI 会优先读取该组件提供的动态数值信息。
        /// </summary>
        public string DisplayName => null;
        public Sprite Icon => null;
        public string Description => $"当前耐久度: {_currentDurability} / {_maxDurability}";
        #endregion
    }
}
