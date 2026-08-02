using UnityEngine;

namespace Cwc.InventoryEngine.UI
{
    /// <summary>
    /// 开箱即用的默认键盘输入适配器组件。
    /// 基于 Unity 传统 Input，支持防连按延迟 (Repeat Rate) 机制。
    /// 可挂载在 GameObject 上，或由 InventoryUIController 自动创建。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/UI/Default Inventory Input Provider")]
    public class DefaultInventoryInputProvider : MonoBehaviour, IInventoryInputProvider
    {
        #region Serialized Fields
        [Header("方向导航按键配置")]
        [SerializeField]
        [Tooltip("向上移动按键")]
        private KeyCode _moveUpKey = KeyCode.W;

        [SerializeField]
        [Tooltip("备用向上移动按键")]
        private KeyCode _moveUpAltKey = KeyCode.UpArrow;

        [SerializeField]
        [Tooltip("向下移动按键")]
        private KeyCode _moveDownKey = KeyCode.S;

        [SerializeField]
        [Tooltip("备用向下移动按键")]
        private KeyCode _moveDownAltKey = KeyCode.DownArrow;

        [SerializeField]
        [Tooltip("向左移动按键")]
        private KeyCode _moveLeftKey = KeyCode.A;

        [SerializeField]
        [Tooltip("备用向左移动按键")]
        private KeyCode _moveLeftAltKey = KeyCode.LeftArrow;

        [SerializeField]
        [Tooltip("向右移动按键")]
        private KeyCode _moveRightKey = KeyCode.D;

        [SerializeField]
        [Tooltip("备用向右移动按键")]
        private KeyCode _moveRightAltKey = KeyCode.RightArrow;

        [Header("动作按键映射配置")]
        [SerializeField]
        [Tooltip("确认/选择按键")]
        private KeyCode _submitKey = KeyCode.Space;

        [SerializeField]
        [Tooltip("备用确认按键 (如 Keypad Enter)")]
        private KeyCode _submitAltKey = KeyCode.Return;

        [SerializeField]
        [Tooltip("取消/返回按键")]
        private KeyCode _cancelKey = KeyCode.Escape;

        [SerializeField]
        [Tooltip("备用取消按键")]
        private KeyCode _cancelAltKey = KeyCode.Backspace;

        [SerializeField]
        [Tooltip("切换上一个页签按键")]
        private KeyCode _tabPrevKey = KeyCode.Q;

        [SerializeField]
        [Tooltip("切换下一个页签按键")]
        private KeyCode _tabNextKey = KeyCode.E;

        [SerializeField]
        [Tooltip("切换主库存/装备界面按键")]
        private KeyCode _toggleEquipmentKey = KeyCode.Tab;

        [SerializeField]
        [Tooltip("物品使用按键")]
        private KeyCode _useKey = KeyCode.U;

        [SerializeField]
        [Tooltip("物品丢弃按键")]
        private KeyCode _dropKey = KeyCode.G;

        [SerializeField]
        [Tooltip("装备快捷卸下按键")]
        private KeyCode _unequipKey = KeyCode.X;

        [Header("导航防连按参数")]
        [SerializeField]
        [Tooltip("按住按键触发首次连续移动前的延迟时间 (秒)")]
        private float _initialRepeatDelay = 0.25f;

        [SerializeField]
        [Tooltip("连续移动时的触发间隔时间 (秒)")]
        private float _repeatRate = 0.08f;
        #endregion

        #region Private Fields
        private Vector2Int _lastMoveDir;
        private float _nextMoveTime;
        #endregion

        #region Public Methods
        public InventoryInputData GetInputData()
        {
            InventoryInputData data = new InventoryInputData();

            // 1. 离散动作判定 (GetKeyDown)
            data.Submit = Input.GetKeyDown(_submitKey) || Input.GetKeyDown(_submitAltKey);
            data.Cancel = Input.GetKeyDown(_cancelKey) || Input.GetKeyDown(_cancelAltKey);
            data.TabPrev = Input.GetKeyDown(_tabPrevKey);
            data.TabNext = Input.GetKeyDown(_tabNextKey);
            data.ToggleEquipment = Input.GetKeyDown(_toggleEquipmentKey);
            data.Use = Input.GetKeyDown(_useKey);
            data.Drop = Input.GetKeyDown(_dropKey);
            data.Unequip = Input.GetKeyDown(_unequipKey);

            // 2. 方向导航判定 (支持防连按逻辑)
            Vector2Int currentRawDir = Vector2Int.zero;
            if (Input.GetKey(_moveUpKey) || Input.GetKey(_moveUpAltKey)) currentRawDir.y = 1;
            else if (Input.GetKey(_moveDownKey) || Input.GetKey(_moveDownAltKey)) currentRawDir.y = -1;

            if (Input.GetKey(_moveRightKey) || Input.GetKey(_moveRightAltKey)) currentRawDir.x = 1;
            else if (Input.GetKey(_moveLeftKey) || Input.GetKey(_moveLeftAltKey)) currentRawDir.x = -1;

            float now = Time.unscaledTime;

            if (currentRawDir == Vector2Int.zero)
            {
                _lastMoveDir = Vector2Int.zero;
            }
            else if (currentRawDir != _lastMoveDir)
            {
                // 方向发生改变，立即触发一次
                _lastMoveDir = currentRawDir;
                data.MoveDirection = currentRawDir;
                _nextMoveTime = now + _initialRepeatDelay;
            }
            else if (now >= _nextMoveTime)
            {
                // 按住持续移动
                data.MoveDirection = currentRawDir;
                _nextMoveTime = now + _repeatRate;
            }

            return data;
        }
        #endregion
    }
}
