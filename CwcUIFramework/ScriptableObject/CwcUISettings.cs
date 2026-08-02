using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// 全局 UI 配置文件，统一持有并管理所有的 UIEntrySO 列表
    /// </summary>
    [CreateAssetMenu(fileName = "CwcUISettings", menuName = "CwcUIFramework/UI Settings")]
    public class CwcUISettings : ScriptableObject
    {
        #region 序列化属性与字段

        [Header("全部 UI 配置条目")]
        [SerializeField] private List<UIEntrySO> uiEntries = new List<UIEntrySO>();

        [Header("默认 UI 适配缩放配置")]
        [SerializeField] private CanvasScalerConfig defaultScalerConfig;

        #endregion

        #region 私有字段

        private readonly Dictionary<string, UIEntrySO> _entryCache = new Dictionary<string, UIEntrySO>();

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取所有配置的 UI 条目只读列表
        /// </summary>
        public IReadOnlyList<UIEntrySO> UIEntries => uiEntries;

        /// <summary>
        /// 获取默认的 UI 适配缩放配置
        /// </summary>
        public CanvasScalerConfig DefaultScalerConfig => defaultScalerConfig;

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化缓存字典
        /// </summary>
        public void Initialize()
        {
            _entryCache.Clear();

            if (uiEntries == null) return;

            foreach (var entry in uiEntries)
            {
                if (entry == null) continue;

                string id = entry.ScreenId;
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"[CwcUIFramework] 发现未配置 ScreenId 的 UIEntrySO: {entry.name}");
                    continue;
                }

                if (_entryCache.ContainsKey(id))
                {
                    Debug.LogError($"[CwcUIFramework] 存在重复的 UI ScreenId: '{id}'，请检查配置！");
                    continue;
                }

                _entryCache.Add(id, entry);
            }
        }

        /// <summary>
        /// 根据 ScreenId 获取对应的 UIEntrySO
        /// </summary>
        public UIEntrySO GetEntry(string screenId)
        {
            if (_entryCache.Count == 0)
            {
                Initialize();
            }

            if (_entryCache.TryGetValue(screenId, out var entry))
            {
                return entry;
            }

            Debug.LogError($"[CwcUIFramework] 未找到 ScreenId 为 '{screenId}' 的 UI 配置条目！");
            return null;
        }

        #endregion
    }
}
