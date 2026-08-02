namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 状态变更的原因/类型
    /// </summary>
    public enum StateChangeReason
    {
        /// <summary>
        /// 真实的状态流转/切换（需触发 Enter/Exit 动效或状态流转逻辑）
        /// </summary>
        Transition = 0,

        /// <summary>
        /// 界面初始化或主动反向同步（仅刷新静态数据/显示，不应触发重入状态动作）
        /// </summary>
        Sync = 1
    }

    /// <summary>
    /// 状态变更信号上下文，记录状态变更前后的 ID、完整层级路径、预计算哈希值以及变更原因。
    /// </summary>
    public readonly struct StateChangeContext
    {
        #region 公共属性

        /// <summary>
        /// 状态变更的原因/类型
        /// </summary>
        public StateChangeReason Reason { get; }

        /// <summary>
        /// 是否为真实的状态流转/切换
        /// </summary>
        public bool IsTransition => Reason == StateChangeReason.Transition;

        /// <summary>
        /// 是否为静态刷新同步
        /// </summary>
        public bool IsSync => Reason == StateChangeReason.Sync;

        /// <summary>
        /// 变更前的状态唯一标识 ID
        /// </summary>
        public string OldStateId { get; }

        /// <summary>
        /// 变更前的状态唯一标识 ID 哈希值
        /// </summary>
        public int OldStateIdHash { get; }

        /// <summary>
        /// 变更后的状态唯一标识 ID
        /// </summary>
        public string NewStateId { get; }

        /// <summary>
        /// 变更后的状态唯一标识 ID 哈希值
        /// </summary>
        public int NewStateIdHash { get; }

        /// <summary>
        /// 变更前的状态完整路径（如 MainLayer/InGame/Combat）
        /// </summary>
        public string OldFullPath { get; }

        /// <summary>
        /// 变更前的状态完整路径哈希值
        /// </summary>
        public int OldFullPathHash { get; }

        /// <summary>
        /// 变更后的状态完整路径（如 MainLayer/InGame/Pause）
        /// </summary>
        public string NewFullPath { get; }

        /// <summary>
        /// 变更后的状态完整路径哈希值
        /// </summary>
        public int NewFullPathHash { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 接收状态完整路径初始化信号上下文（自动从全路径导出最后一级节点 StateId）
        /// </summary>
        /// <param name="oldFullPath">变更前的状态完整路径</param>
        /// <param name="newFullPath">变更后的状态完整路径</param>
        /// <param name="reason">状态变更原因</param>
        public StateChangeContext(string oldFullPath, string newFullPath, StateChangeReason reason = StateChangeReason.Transition)
            : this(ExtractStateId(oldFullPath), ExtractStateId(newFullPath), oldFullPath, newFullPath, reason)
        {
        }

        /// <summary>
        /// 初始化状态变更信号上下文（默认 Reason 为 Transition）
        /// </summary>
        /// <param name="oldStateId">变更前的状态 ID</param>
        /// <param name="newStateId">变更后的状态 ID</param>
        /// <param name="oldFullPath">变更前的状态完整路径</param>
        /// <param name="newFullPath">变更后的状态完整路径</param>
        public StateChangeContext(string oldStateId, string newStateId, string oldFullPath, string newFullPath)
            : this(oldStateId, newStateId, oldFullPath, newFullPath, StateChangeReason.Transition)
        {
        }

        /// <summary>
        /// 初始化状态变更信号上下文
        /// </summary>
        /// <param name="oldStateId">变更前的状态 ID</param>
        /// <param name="newStateId">变更后的状态 ID</param>
        /// <param name="oldFullPath">变更前的状态完整路径</param>
        /// <param name="newFullPath">变更后的状态完整路径</param>
        /// <param name="reason">状态变更原因</param>
        public StateChangeContext(string oldStateId, string newStateId, string oldFullPath, string newFullPath, StateChangeReason reason)
        {
            Reason = reason;

            OldStateId = oldStateId ?? string.Empty;
            OldStateIdHash = StatePathUtility.StringToHash(OldStateId);

            NewStateId = newStateId ?? string.Empty;
            NewStateIdHash = StatePathUtility.StringToHash(NewStateId);

            OldFullPath = oldFullPath ?? string.Empty;
            OldFullPathHash = StatePathUtility.StringToHash(OldFullPath);

            NewFullPath = newFullPath ?? string.Empty;
            NewFullPathHash = StatePathUtility.StringToHash(NewFullPath);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 格式化字符串输出
        /// </summary>
        public override string ToString()
        {
            return $"StateChangeContext ({Reason}): [{OldFullPath}] -> [{NewFullPath}] (IDs: {OldStateId} -> {NewStateId})";
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 从完整状态路径中提取最后一级的短状态 ID
        /// </summary>
        private static string ExtractStateId(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return string.Empty;
            }

            int lastSlashIndex = fullPath.LastIndexOf('/');
            if (lastSlashIndex < 0 || lastSlashIndex >= fullPath.Length - 1)
            {
                return fullPath;
            }

            return fullPath.Substring(lastSlashIndex + 1);
        }

        #endregion
    }
}
