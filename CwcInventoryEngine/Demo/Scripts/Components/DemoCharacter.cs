using UnityEngine;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// Demo 测试角色数值组件 (Demo Character Component)。
    /// 模拟角色的生命值 (HP) 与攻击力 (Attack) 属性，用于验证药水回复生命及装备增加攻击力等效果。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/Demo/Demo Character")]
    public class DemoCharacter : MonoBehaviour
    {
        #region Serialized Fields
        [Header("生命值配置")]
        [SerializeField]
        [Tooltip("当前生命值")]
        private int _currentHp = 15;

        [SerializeField]
        [Tooltip("最大生命值")]
        private int _maxHp = 20;

        [Header("战斗属性")]
        [SerializeField]
        [Tooltip("基础攻击力")]
        private int _baseAttack = 10;

        [SerializeField]
        [Tooltip("装备加成攻击力")]
        private int _equipmentBonusAttack = 0;

        [Header("防御属性")]
        [SerializeField]
        [Tooltip("基础防御力")]
        private int _baseDefense = 5;

        [SerializeField]
        [Tooltip("装备加成防御力")]
        private int _equipmentBonusDefense = 0;
        #endregion

        #region Public Properties
        /// <summary>
        /// 当前生命值。
        /// </summary>
        public int CurrentHp => _currentHp;

        /// <summary>
        /// 最大生命值。
        /// </summary>
        public int MaxHp => _maxHp;

        /// <summary>
        /// 当前总攻击力 (基础 + 装备加成)。
        /// </summary>
        public int TotalAttack => _baseAttack + _equipmentBonusAttack;

        /// <summary>
        /// 当前总防御力 (基础 + 装备加成)。
        /// </summary>
        public int TotalDefense => _baseDefense + _equipmentBonusDefense;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _currentHp = Mathf.Clamp(_currentHp, 0, _maxHp);
        }
        #endregion

        #region Public Methods - Attribute Operations
        /// <summary>
        /// 为角色恢复生命值。
        /// </summary>
        /// <param name="amount">恢复数值</param>
        /// <returns>若成功恢复返回 true，已满血返回 false</returns>
        public bool Heal(int amount)
        {
            if (amount <= 0 || _currentHp >= _maxHp)
            {
                Debug.Log($"[DemoCharacter] 无法恢复生命值！当前生命: {_currentHp}/{_maxHp}");
                return false;
            }

            int oldHp = _currentHp;
            _currentHp = Mathf.Min(_maxHp, _currentHp + amount);
            int actualHeal = _currentHp - oldHp;

            Debug.Log($"[DemoCharacter] 成功恢复 {actualHeal} 点生命！生命值由 {oldHp} 升至 {_currentHp}/{_maxHp}");
            return true;
        }

        /// <summary>
        /// 修改装备加成的攻击力。
        /// </summary>
        /// <param name="delta">攻击力增量 (正数为穿戴加成，负数为脱下扣除)</param>
        public void ModifyAttack(int delta)
        {
            int oldTotal = TotalAttack;
            _equipmentBonusAttack += delta;
            Debug.Log($"[DemoCharacter] 装备攻击力变更: {(delta >= 0 ? "+" + delta : delta.ToString())}！总攻击力由 {oldTotal} 变为 {TotalAttack} (基础: {_baseAttack}, 装备: {_equipmentBonusAttack})");
        }

        /// <summary>
        /// 修改装备加成的防御力。
        /// </summary>
        /// <param name="delta">防御力增量 (正数为穿戴加成，负数为脱下扣除)</param>
        public void ModifyDefense(int delta)
        {
            int oldTotal = TotalDefense;
            _equipmentBonusDefense += delta;
            Debug.Log($"[DemoCharacter] 装备防御力变更: {(delta >= 0 ? "+" + delta : delta.ToString())}！总防御力由 {oldTotal} 变为 {TotalDefense} (基础: {_baseDefense}, 装备: {_equipmentBonusDefense})");
        }
        #endregion
    }
}
