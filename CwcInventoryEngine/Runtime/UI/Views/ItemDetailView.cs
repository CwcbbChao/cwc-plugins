using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cwc.InventoryEngine.Query;

namespace Cwc.InventoryEngine.UI
{
    /// <summary>
    /// 物品通用详情展示面板。
    /// 纯粹基于统一的 IItemDisplay 接口解析并展示物品图标、名称、描述以及堆叠数量。
    /// 使用现代化 TextMeshProUGUI 进行文本渲染。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/UI/Item Detail View")]
    public class ItemDetailView : MonoBehaviour
    {
        #region Serialized Fields
        [Header("基础 UI 控件组件")]
        [SerializeField]
        [Tooltip("物品图标显示 Image (可选)")]
        private Image _iconImage;

        [SerializeField]
        [Tooltip("物品名称文本 TextMeshProUGUI")]
        private TextMeshProUGUI _nameText;

        [SerializeField]
        [Tooltip("物品描述文本 TextMeshProUGUI")]
        private TextMeshProUGUI _descriptionText;

        [SerializeField]
        [Tooltip("物品堆叠数量文本 TextMeshProUGUI (可选)")]
        private TextMeshProUGUI _stackCountText;

        [Header("无选择时的空显示")]
        [SerializeField]
        [Tooltip("当选中的物品为空时显示的内容或控制的 GameObject")]
        private GameObject _emptyContainer;

        [SerializeField]
        [Tooltip("当选中的物品非空时显示的内容控制 GameObject")]
        private GameObject _contentContainer;
        #endregion

        #region Private Fields
        private ItemSlot _currentItemSlot;
        #endregion

        #region Public Methods
        /// <summary>
        /// 绑定并渲染指定的物品数据对象（可以是 ItemInstance 或 ItemSlot）。
        /// 仅用于纯只读数据展示。
        /// </summary>
        /// <param name="data">数据源对象</param>
        public virtual void RenderItem(object data)
        {
            ItemInstance item = null;

            if (data is ItemSlot slot)
            {
                _currentItemSlot = slot;
                item = slot.Item;
            }
            else if (data is ItemInstance instance)
            {
                _currentItemSlot = null;
                item = instance;
            }

            if (item == null)
            {
                ShowEmptyState();
                return;
            }

            ShowContentState();

            // 1. 渲染名称与基础描述
            if (_nameText != null)
            {
                _nameText.text = GetItemName(item);
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = GetItemDescription(item);
            }

            // 2. 渲染堆叠数
            if (_stackCountText != null)
            {
                _stackCountText.text = item.Definition.IsStackable ? $"x{item.StackCount}" : string.Empty;
            }

            // 3. 渲染图标
            if (_iconImage != null)
            {
                Sprite icon = GetItemIcon(item);
                if (icon != null)
                {
                    _iconImage.sprite = icon;
                    _iconImage.enabled = true;
                }
                else
                {
                    _iconImage.enabled = false;
                }
            }
        }
        #endregion

        #region Private Helper Methods
        protected virtual void ShowEmptyState()
        {
            if (_emptyContainer != null) _emptyContainer.SetActive(true);
            if (_contentContainer != null) _contentContainer.SetActive(false);

            if (_nameText != null) _nameText.text = string.Empty;
            if (_descriptionText != null) _descriptionText.text = string.Empty;
            if (_stackCountText != null) _stackCountText.text = string.Empty;
            if (_iconImage != null) _iconImage.enabled = false;
        }

        protected virtual void ShowContentState()
        {
            if (_emptyContainer != null) _emptyContainer.SetActive(false);
            if (_contentContainer != null) _contentContainer.SetActive(true);
        }

        /// <summary>
        /// 获取物品关联的 IItemDisplay 抽象显示结构接口组件。
        /// </summary>
        protected virtual IItemDisplay GetItemDisplay(ItemInstance item)
        {
            return item != null ? item.GetComponent<IItemDisplay>() : null;
        }

        /// <summary>
        /// 获取物品在 UI 上显示的名称。
        /// 优先从 IItemDisplay 接口提取；若未配置或为空，回退使用 Definition 资产名称。
        /// </summary>
        protected virtual string GetItemName(ItemInstance item)
        {
            if (item == null) return string.Empty;

            var display = GetItemDisplay(item);
            if (display != null && !string.IsNullOrEmpty(display.DisplayName))
            {
                return display.DisplayName;
            }

            return item.Definition != null ? item.Definition.name : string.Empty;
        }

        /// <summary>
        /// 获取物品在 UI 上显示的描述文本。
        /// 优先从 IItemDisplay 接口提取。
        /// </summary>
        protected virtual string GetItemDescription(ItemInstance item)
        {
            if (item == null) return string.Empty;

            var display = GetItemDisplay(item);
            return display != null && !string.IsNullOrEmpty(display.Description) ? display.Description : string.Empty;
        }

        /// <summary>
        /// 获取物品图标 Sprite。
        /// 优先从 IItemDisplay 接口提取；若为空则通过工具方法自动匹配。
        /// </summary>
        protected virtual Sprite GetItemIcon(ItemInstance item)
        {
            if (item == null) return null;

            var display = GetItemDisplay(item);
            if (display != null && display.Icon != null)
            {
                return display.Icon;
            }

            return InventorySortComparers.GetItemIcon(item);
        }
        #endregion
    }
}
