using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 单条状态变更日志记录
    /// </summary>
    [Serializable]
    public struct StateChangeLogEntry
    {
        #region 序列化字段

        [Tooltip("日志生成时间戳")]
        [SerializeField] private string _timestamp;

        [Tooltip("变更前的状态完整路径")]
        [SerializeField] private string _oldFullPath;

        [Tooltip("变更后的状态完整路径")]
        [SerializeField] private string _newFullPath;

        [Tooltip("状态变更原因")]
        [SerializeField] private StateChangeReason _reason;

        [Tooltip("触发规则或附加信息描述")]
        [SerializeField] private string _ruleDescription;

        #endregion

        #region 公共属性

        /// <summary>
        /// 时间戳 (HH:mm:ss.fff)
        /// </summary>
        public string Timestamp => _timestamp;

        /// <summary>
        /// 变更前的状态完整路径
        /// </summary>
        public string OldFullPath => _oldFullPath;

        /// <summary>
        /// 变更后的状态完整路径
        /// </summary>
        public string NewFullPath => _newFullPath;

        /// <summary>
        /// 状态变更原因
        /// </summary>
        public StateChangeReason Reason => _reason;

        /// <summary>
        /// 触发规则或附加信息描述
        /// </summary>
        public string RuleDescription => _ruleDescription;

        #endregion

        #region 构造函数

        public StateChangeLogEntry(string oldPath, string newPath, StateChangeReason reason, string ruleDescription = "")
        {
            _timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            _oldFullPath = string.IsNullOrEmpty(oldPath) ? "(None)" : oldPath;
            _newFullPath = string.IsNullOrEmpty(newPath) ? "(None)" : newPath;
            _reason = reason;
            _ruleDescription = ruleDescription ?? string.Empty;
        }

        #endregion
    }

    /// <summary>
    /// 可序列化的状态变更历史日志记录器，管理日志条目与上限保护。
    /// </summary>
    [Serializable]
    public class StateChangeHistory
    {
        #region 序列化字段

        [Tooltip("是否开启日志记录（仅在调试模式时启用以提升性能）")]
        [SerializeField] private bool _enableLog = true;

        [Tooltip("历史日志最大保留容量")]
        [SerializeField] private int _maxCapacity = 20;

        [Tooltip("已记录的状态变更历史条目列表")]
        [SerializeField] private List<StateChangeLogEntry> _entries = new List<StateChangeLogEntry>();

        #endregion

        #region 公共属性

        /// <summary>
        /// 日志功能使能控制
        /// </summary>
        public bool EnableLog
        {
            get => _enableLog;
            set => _enableLog = value;
        }

        /// <summary>
        /// 最大容量上限（最小值为 1）
        /// </summary>
        public int MaxCapacity
        {
            get => _maxCapacity;
            set => _maxCapacity = Mathf.Max(1, value);
        }

        /// <summary>
        /// 当前记录的所有日志条目只读列表
        /// </summary>
        public IReadOnlyList<StateChangeLogEntry> Entries => _entries;

        #endregion

        #region 构造函数

        public StateChangeHistory()
        {
        }

        public StateChangeHistory(int maxCapacity, bool enableLog = true)
        {
            _maxCapacity = Mathf.Max(1, maxCapacity);
            _enableLog = enableLog;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 根据状态变更上下文记录一条新的变更日志
        /// </summary>
        /// <param name="context">状态变更上下文对象</param>
        /// <param name="ruleDescription">触发规则或匹配描述（可选）</param>
        public void Record(in StateChangeContext context, string ruleDescription = "")
        {
            if (!_enableLog)
            {
                return;
            }

            Record(context.OldFullPath, context.NewFullPath, context.Reason, ruleDescription);
        }

        /// <summary>
        /// 显式记录一条新的变更日志
        /// </summary>
        /// <param name="oldPath">变更前路径</param>
        /// <param name="newPath">变更后路径</param>
        /// <param name="reason">变更原因</param>
        /// <param name="ruleDescription">触发规则或匹配描述（可选）</param>
        public void Record(string oldPath, string newPath, StateChangeReason reason, string ruleDescription = "")
        {
            if (!_enableLog)
            {
                return;
            }

            if (_entries == null)
            {
                _entries = new List<StateChangeLogEntry>();
            }

            int limit = MaxCapacity;
            while (_entries.Count >= limit && _entries.Count > 0)
            {
                _entries.RemoveAt(0);
            }

            _entries.Add(new StateChangeLogEntry(oldPath, newPath, reason, ruleDescription));
        }

        /// <summary>
        /// 清空所有历史日志条目
        /// </summary>
        public void Clear()
        {
            _entries?.Clear();
        }

        #endregion
    }
}
