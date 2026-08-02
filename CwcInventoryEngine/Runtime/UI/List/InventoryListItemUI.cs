using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Cwc.InventoryEngine.Query;

namespace Cwc.InventoryEngine.UI
{
    /// <summary>
    /// 开箱即用的通用列表单元 UI 项组件。
    /// 挂载在列表每一个 Slot / Cell GameObject 节点上（对应 Content 根节点的子对象）。
    /// 实现 IInventoryListItem 接口与 IPointerClickHandler 接口，
    /// 自动管理图标、TextMeshProUGUI 文本、堆叠数渲染、选中高亮框状态以及鼠标指针点击选择。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/UI/Inventory List Item UI")]
    public class InventoryListItemUI : MonoBehaviour, IInventoryListItem, IPointerClickHandler
    {
        #region Serialized Fields
        [Header("基础 UI 控件组件")]
        [SerializeField]
        [Tooltip("物品图标显示 Image (可选)")]
        private Image _iconImage;

        [SerializeField]
        [Tooltip("物品名称/槽位名称 TextMeshProUGUI (可选)")]
        private TextMeshProUGUI _nameText;

        [SerializeField]
        [Tooltip("堆叠数量文本 TextMeshProUGUI (可选)")]
        private TextMeshProUGUI _stackCountText;

        [Header("视觉状态控制器")]
        [SerializeField]
        [Tooltip("内容容器节点 GameObject (可选，配置后无物品时直接隐藏该容器，完美保留外框背景底图)")]
        private GameObject _contentContainer;

        [SerializeField]
        [Tooltip("选中高亮高光背景/边框 GameObject (可选)")]
        private GameObject _selectionHighlight;

        [SerializeField]
        [Tooltip("禁用/空槽遮罩 GameObject (可选)")]
        private GameObject _disabledMask;

        [Header("文本格式化选项")]
        [SerializeField]
        [Tooltip("是否自动将显示文本中的换行符 ('\\r\\n', '\\n', '\\r') 替换为空格 (列表项中不允许多行换行时启用)")]
        private bool _replaceLineBreaksWithSpace = true;
        #endregion

        #region Private Fields
        private object _currentData;
        private int _dataIndex;
        private bool _isSelected;
        private CanvasGroup _canvasGroup;
        #endregion

        #region Public Properties
        /// <summary>
        /// 当前绑定的数据对象 (ItemSlot 或 ItemInstance)。
        /// </summary>
        public object CurrentData => _currentData;

        /// <summary>
        /// 当前在全局数据源中的绝对索引。
        /// </summary>
        public int DataIndex => _dataIndex;

        /// <summary>
        /// 当前是否处于选中焦点状态。
        /// </summary>
        public bool IsSelected => _isSelected;

        /// <summary>
        /// 是否自动将显示文本中的换行符 ('\r\n', '\n', '\r') 替换为空格。
        /// </summary>
        public bool ReplaceLineBreaksWithSpace
        {
            get => _replaceLineBreaksWithSpace;
            set => _replaceLineBreaksWithSpace = value;
        }
        #endregion

        #region IInventoryListItem Implementation
        /// <summary>
        /// 当列表滚动平移并绑定新数据时回调。
        /// </summary>
        public virtual void OnBindData(object data, int dataIndex)
        {
            _currentData = data;
            _dataIndex = dataIndex;

            ItemInstance item = null;

            if (data is ItemSlot slot)
            {
                item = slot.Item;
            }
            else if (data is ItemInstance instance)
            {
                item = instance;
            }

            if (item == null)
            {
                RenderEmptySlot(data);
                return;
            }

            RenderItem(item);
        }

        /// <summary>
        /// 当该 UI 单元的选择/焦点状态改变时回调。
        /// </summary>
        public virtual void OnSelectionChanged(bool isSelected)
        {
            _isSelected = isSelected;
            SetElementVisibility(_selectionHighlight, isSelected);
        }

        /// <summary>
        /// 设置 UI 单元整体显隐状态 (采用 CanvasGroup 控频，绝对不使用 SetActive，规避顶点 Rebuild).
        /// </summary>
        public virtual void SetVisible(bool visible)
        {
            EnsureCanvasGroup();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
        #endregion

        #region Event System Implementation
        /// <summary>
        /// 当鼠标指针点击当前 UI 单元项时回调。
        /// </summary>
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            var controller = GetComponentInParent<InventoryListUIController>();
            if (controller != null)
            {
                // 若未选中则选中；若已经选中，再次点击相当于 Submit 提交操作
                bool submit = _isSelected;
                controller.NotifyItemClicked(this, submitImmediately: submit);
            }
        }
        #endregion

        #region Private Helper Methods
        protected virtual void RenderEmptySlot(object rawData)
        {
            SetElementVisibility(_disabledMask, false);

            // 若配置了内容容器，使用 CanvasGroup 隐藏内容容器，极速且干净，绝不调用 SetActive
            if (_contentContainer != null)
            {
                SetElementVisibility(_contentContainer, false);
            }
            else
            {
                // 备用机制：隐藏物品图标组件 (关掉 Image.enabled，避免残留 Default 图像)
                if (_iconImage != null)
                {
                    _iconImage.enabled = false;
                    _iconImage.sprite = null;
                }

                // 清空名称文本
                if (_nameText != null)
                {
                    _nameText.text = string.Empty;
                }

                // 清空堆叠数量文本
                if (_stackCountText != null)
                {
                    _stackCountText.text = string.Empty;
                }
            }
        }

        protected virtual void RenderItem(ItemInstance item)
        {
            SetElementVisibility(_disabledMask, false);

            if (_contentContainer != null)
            {
                SetElementVisibility(_contentContainer, true);
            }

            // 1. 渲染名称
            if (_nameText != null)
            {
                string rawName;
                if (ItemPropertyEvaluator.TryGetPropertyValue(item, "Name", out ItemPropertyValue nameVal) && !nameVal.IsEmpty)
                {
                    rawName = nameVal.StringValue;
                }
                else
                {
                    rawName = item.Definition.name;
                }

                _nameText.text = FormatText(rawName);
            }

            // 2. 渲染堆叠数量
            if (_stackCountText != null)
            {
                _stackCountText.text = item.Definition.IsStackable ? $"x{item.StackCount}" : string.Empty;
            }

            // 3. 渲染图标
            if (_iconImage != null)
            {
                Sprite iconSprite = InventorySortComparers.GetItemIcon(item);
                if (iconSprite != null)
                {
                    _iconImage.sprite = iconSprite;
                    _iconImage.enabled = true;
                }
                else
                {
                    _iconImage.enabled = false;
                }
            }
        }

        /// <summary>
        /// 格式化字符串：若开启 _replaceLineBreaksWithSpace，则将回车换行符 ('\r\n', '\n', '\r') 替换为普通空格。
        /// </summary>
        /// <param name="rawText">原始输入的字符串文本</param>
        /// <returns>格式化后的文本</returns>
        protected virtual string FormatText(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return string.Empty;
            if (_replaceLineBreaksWithSpace)
            {
                return rawText.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
            }
            return rawText;
        }

        private void EnsureCanvasGroup()
        {
            if (_canvasGroup == null)
            {
                if (!TryGetComponent<CanvasGroup>(out _canvasGroup))
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        /// <summary>
        /// 使用 CanvasGroup 控制子节点的 Alpha/Raycast 显隐，绝不触发 GameObject.SetActive。
        /// </summary>
        protected virtual void SetElementVisibility(GameObject element, bool visible)
        {
            if (element == null) return;

            if (!element.TryGetComponent<CanvasGroup>(out var cg))
            {
                cg = element.AddComponent<CanvasGroup>();
            }

            cg.alpha = visible ? 1f : 0f;
            cg.interactable = visible;
            cg.blocksRaycasts = visible;
        }
        #endregion
    }
}
