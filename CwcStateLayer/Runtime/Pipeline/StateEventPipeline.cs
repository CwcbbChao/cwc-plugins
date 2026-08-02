using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 状态管线广播事件载荷（结构体传输，0 GC 分配）
    /// </summary>
    public readonly struct StatePipelineEvent
    {
        /// <summary>
        /// 状态层配置资产（权威身份标识）
        /// </summary>
        public readonly StateLayerConfig Config;

        /// <summary>
        /// 状态层实例标识 ID（可选，用于区分相同 Config 的多实例）
        /// </summary>
        public readonly string LayerId;

        /// <summary>
        /// 状态变更/同步上下文
        /// </summary>
        public readonly StateChangeContext Context;

        public StatePipelineEvent(StateLayerConfig config, string layerId, in StateChangeContext context)
        {
            Config = config;
            LayerId = layerId ?? string.Empty;
            Context = context;
        }
    }

    /// <summary>
    /// 状态刷新请求事件载荷
    /// </summary>
    public readonly struct StateSyncRequest
    {
        /// <summary>
        /// 目标状态层配置资产
        /// </summary>
        public readonly StateLayerConfig Config;

        /// <summary>
        /// 目标状态层实例标识 ID（可选）
        /// </summary>
        public readonly string LayerId;

        public StateSyncRequest(StateLayerConfig config, string layerId)
        {
            Config = config;
            LayerId = layerId ?? string.Empty;
        }
    }

    /// <summary>
    /// 状态变更请求事件载荷
    /// </summary>
    public readonly struct StateChangeRequest
    {
        /// <summary>
        /// 目标状态层配置资产
        /// </summary>
        public readonly StateLayerConfig Config;

        /// <summary>
        /// 目标状态层实例标识 ID（可选）
        /// </summary>
        public readonly string LayerId;

        /// <summary>
        /// 目标状态路径或节点 ID
        /// </summary>
        public readonly string TargetPathOrId;

        /// <summary>
        /// 变更原因
        /// </summary>
        public readonly StateChangeReason Reason;

        public StateChangeRequest(StateLayerConfig config, string layerId, string targetPathOrId, StateChangeReason reason)
        {
            Config = config;
            LayerId = layerId ?? string.Empty;
            TargetPathOrId = targetPathOrId ?? string.Empty;
            Reason = reason;
        }
    }

    /// <summary>
    /// 全局状态事件管线，实现 StateObserver 与 StateLayer 之间的零引用解耦广播通信。
    /// </summary>
    public static class StateEventPipeline
    {
        #region 事件定义

        /// <summary>
        /// 全局状态变更/同步广播事件
        /// </summary>
        public static event Action<StatePipelineEvent> OnStateBroadcasting;

        /// <summary>
        /// 全局状态刷新请求事件
        /// </summary>
        public static event Action<StateSyncRequest> OnSyncRequesting;

        /// <summary>
        /// 全局状态变更请求事件
        /// </summary>
        public static event Action<StateChangeRequest> OnChangeStateRequesting;

        #endregion

        #region 公共广播方法

        /// <summary>
        /// 发布状态改变或同步广播
        /// </summary>
        /// <param name="config">状态层配置资产</param>
        /// <param name="layerId">状态层实例 ID</param>
        /// <param name="context">状态上下文</param>
        public static void PublishStateChanged(StateLayerConfig config, string layerId, in StateChangeContext context)
        {
            if (config == null) return;
            OnStateBroadcasting?.Invoke(new StatePipelineEvent(config, layerId, context));
        }

        /// <summary>
        /// 观察者或外部请求刷新当前状态
        /// </summary>
        /// <param name="config">目标配置资产</param>
        /// <param name="layerId">目标实例 ID</param>
        public static void PublishSyncRequest(StateLayerConfig config, string layerId)
        {
            if (config == null) return;
            OnSyncRequesting?.Invoke(new StateSyncRequest(config, layerId));
        }

        /// <summary>
        /// 观察者或外部请求状态切换
        /// </summary>
        /// <param name="config">目标配置资产</param>
        /// <param name="layerId">目标实例 ID</param>
        /// <param name="targetPathOrId">目标状态路径或 ID</param>
        /// <param name="reason">变更原因</param>
        public static void PublishChangeStateRequest(StateLayerConfig config, string layerId, string targetPathOrId, StateChangeReason reason = StateChangeReason.Transition)
        {
            if (config == null) return;
            OnChangeStateRequesting?.Invoke(new StateChangeRequest(config, layerId, targetPathOrId, reason));
        }

        #endregion
    }
}
