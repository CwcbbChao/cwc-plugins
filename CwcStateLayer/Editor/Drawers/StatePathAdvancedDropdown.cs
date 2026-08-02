using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer.Editor
{
    /// <summary>
    /// 基于 Unity 官方 AdvancedDropdown 的状态路径高级搜索下拉弹窗，支持关键字模糊搜索与层级树状浏览。
    /// </summary>
    public class StatePathAdvancedDropdown : AdvancedDropdown
    {
        #region 私有字段

        private readonly List<string> _fullPaths;
        private readonly Action<string> _onPathSelected;
        private readonly Dictionary<int, string> _idToFullPathMap = new Dictionary<int, string>();

        #endregion

        #region 构造函数

        public StatePathAdvancedDropdown(
            AdvancedDropdownState state,
            List<string> fullPaths,
            Action<string> onPathSelected) : base(state)
        {
            _fullPaths = fullPaths ?? new List<string>();
            _onPathSelected = onPathSelected;
            minimumSize = new Vector2(280f, 340f);
        }

        #endregion

        #region 重写 AdvancedDropdown 方法

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem("状态路径选择 (支持搜索)");
            _idToFullPathMap.Clear();

            // 1. 添加 Any 全局通配项
            int anyId = -1;
            AdvancedDropdownItem anyItem = new AdvancedDropdownItem("Any")
            {
                id = anyId
            };
            _idToFullPathMap[anyId] = "Any";
            root.AddChild(anyItem);

            // 2. 构建路径节点树
            Dictionary<string, AdvancedDropdownItem> nodeCache = new Dictionary<string, AdvancedDropdownItem>();

            for (int i = 0; i < _fullPaths.Count; i++)
            {
                string path = _fullPaths[i];
                if (string.IsNullOrEmpty(path) || path == "*" || path == "Any")
                {
                    continue;
                }

                string[] parts = path.Split('/');
                AdvancedDropdownItem currentParent = root;
                string accumulatedPath = string.Empty;

                for (int j = 0; j < parts.Length; j++)
                {
                    string part = parts[j];
                    accumulatedPath = string.IsNullOrEmpty(accumulatedPath) ? part : $"{accumulatedPath}/{part}";

                    if (!nodeCache.TryGetValue(accumulatedPath, out AdvancedDropdownItem item))
                    {
                        int itemId = accumulatedPath.GetHashCode();
                        item = new AdvancedDropdownItem(part)
                        {
                            id = itemId
                        };
                        _idToFullPathMap[itemId] = accumulatedPath;
                        currentParent.AddChild(item);
                        nodeCache[accumulatedPath] = item;
                    }

                    currentParent = item;
                }
            }

            return root;
        }

        /// <summary>
        /// Unity AdvancedDropdown 选中项的回调虚方法 (ItemSelected)
        /// </summary>
        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);

            if (item != null && _idToFullPathMap.TryGetValue(item.id, out string selectedPath))
            {
                _onPathSelected?.Invoke(selectedPath);
            }
        }

        #endregion
    }
}
