using System;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 匹配模式类型
    /// </summary>
    public enum PathMatchType
    {
        /// <summary>
        /// 任意全匹配（Any）
        /// </summary>
        Any,

        /// <summary>
        /// 绝对路径或 ID 精确哈希匹配
        /// </summary>
        ExactPathOrId,

        /// <summary>
        /// 父级前缀通配符匹配（如 MainLayer/InGame/Any）
        /// </summary>
        ParentWildcard
    }

    /// <summary>
    /// 结构化预解析路径匹配模式，避免运行期重复的字符串切割与比较 GC。
    /// </summary>
    public readonly struct CompiledStatePathPattern
    {
        #region 公共属性

        /// <summary>
        /// 原始规则字符串
        /// </summary>
        public string RawPattern { get; }

        /// <summary>
        /// 匹配模式类型
        /// </summary>
        public PathMatchType MatchType { get; }

        /// <summary>
        /// 预计算的模式哈希值（用于 Exact 匹配或 Parent 前缀哈希）
        /// </summary>
        public int PatternHash { get; }

        /// <summary>
        /// 父级通配符时的父路径前缀（仅在 ParentWildcard 模式下有效）
        /// </summary>
        public string ParentPrefix { get; }

        /// <summary>
        /// 父级通配符时的带有斜杠的父路径前缀（仅在 ParentWildcard 模式下有效，预拼接以消除运行时匹配的 GC 开销）
        /// </summary>
        public string ParentPrefixWithSlash { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化并解析路径匹配模式
        /// </summary>
        /// <param name="pattern">规则模式字符串</param>
        public CompiledStatePathPattern(string pattern)
        {
            RawPattern = pattern ?? string.Empty;

            if (string.IsNullOrEmpty(pattern) ||
                pattern.Equals(StatePathMatcher.AnyWildcard, StringComparison.OrdinalIgnoreCase))
            {
                MatchType = PathMatchType.Any;
                PatternHash = 0;
                ParentPrefix = string.Empty;
                ParentPrefixWithSlash = string.Empty;
            }
            else if (pattern.EndsWith("/Any", StringComparison.OrdinalIgnoreCase))
            {
                MatchType = PathMatchType.ParentWildcard;
                ParentPrefix = pattern.Substring(0, pattern.Length - 4);
                ParentPrefixWithSlash = ParentPrefix + "/";
                PatternHash = StatePathUtility.StringToHash(ParentPrefix);
            }
            else
            {
                MatchType = PathMatchType.ExactPathOrId;
                ParentPrefix = string.Empty;
                ParentPrefixWithSlash = string.Empty;
                PatternHash = StatePathUtility.StringToHash(pattern);
            }
        }

        #endregion

        #region 公共评估方法

        /// <summary>
        /// 评估实际路径与 ID 是否符合当前解析模式
        /// </summary>
        public bool IsMatched(string actualPath, int actualPathHash, string actualId = null, int actualIdHash = 0)
        {
            if (MatchType == PathMatchType.Any)
            {
                return true;
            }

            if (MatchType == PathMatchType.ExactPathOrId)
            {
                if (actualPathHash != 0 && actualPathHash == PatternHash)
                {
                    return true;
                }

                if (actualIdHash != 0 && actualIdHash == PatternHash)
                {
                    return true;
                }

                return false;
            }

            if (MatchType == PathMatchType.ParentWildcard)
            {
                if (string.IsNullOrEmpty(actualPath))
                {
                    return false;
                }

                return actualPath.Equals(ParentPrefix, StringComparison.OrdinalIgnoreCase) ||
                       actualPath.StartsWith(ParentPrefixWithSlash, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        #endregion
    }

    /// <summary>
    /// 状态路径匹配器，负责处理精准路径哈希匹配、Any 通配符匹配以及父状态层级通配符匹配。
    /// </summary>
    public static class StatePathMatcher
    {
        #region 常量定义

        /// <summary>
        /// Any 通配符常数（全框架唯一支持的通配符标准）
        /// </summary>
        public const string AnyWildcard = "Any";

        #endregion

        #region 公共静态匹配接口

        /// <summary>
        /// 判断单个路径规则模式与实际路径/ID是否匹配
        /// </summary>
        public static bool IsPathMatched(string pattern, string actualPath, string actualId = null)
        {
            CompiledStatePathPattern compiled = new CompiledStatePathPattern(pattern);
            int pathHash = StatePathUtility.StringToHash(actualPath);
            int idHash = StatePathUtility.StringToHash(actualId);
            return compiled.IsMatched(actualPath, pathHash, actualId, idHash);
        }

        /// <summary>
        /// 校验完整的上下文过渡匹配（从 FromPath 到 ToPath）
        /// </summary>
        public static bool IsContextMatched(string fromPattern, string toPattern, in StateChangeContext context)
        {
            return IsPathMatched(fromPattern, context.OldFullPath, context.OldStateId) &&
                   IsPathMatched(toPattern, context.NewFullPath, context.NewStateId);
        }

        /// <summary>
        /// 使用预解析 CompiledStatePathPattern 进行零堆分配上下文过渡匹配
        /// </summary>
        public static bool IsContextMatched(
            in CompiledStatePathPattern compiledFrom,
            in CompiledStatePathPattern compiledTo,
            in StateChangeContext context)
        {
            bool isFromMatch = compiledFrom.IsMatched(
                context.OldFullPath,
                context.OldFullPathHash,
                context.OldStateId,
                context.OldStateIdHash);

            bool isToMatch = compiledTo.IsMatched(
                context.NewFullPath,
                context.NewFullPathHash,
                context.NewStateId,
                context.NewStateIdHash);

            return isFromMatch && isToMatch;
        }

        #endregion
    }
}
