using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 纯 C# 核心状态观察者，可序列化，不强依赖 MonoBehaviour。
    /// 通过 StateEventPipeline 事件管线全自动匹配广播与进行双向交互。
    /// </summary>
    [Serializable]
    public class StateObserver
    {
        #region 序列化字段

        [Tooltip("引用的状态层配置资产（权威身份标识）")]
        [SerializeField] private StateLayerConfig _layerConfig;

        [Tooltip("状态层实例标识 ID（可选，用于区分相同 Config 的多实例）")]
        [SerializeField] private string _layerId = string.Empty;

        [Tooltip("状态绑定规则列表")]
        [SerializeField] private List<StateBindingRule> _rules = new List<StateBindingRule>();

        [Tooltip("最新接收到的状态完整路径（调试检查）")]
        [SerializeField] private string _currentFullPath = string.Empty;

        [Tooltip("上一次接收到的状态完整路径（调试检查）")]
        [SerializeField] private string _previousFullPath = string.Empty;

        [Tooltip("观察者接收到的状态变更评估日志")]
        [SerializeField] private StateChangeHistory _historyLog = new StateChangeHistory();

        #endregion

        #region 非序列化私有字段

        [NonSerialized] private StateLayer _boundStateLayer;
        [NonSerialized] private bool _isBound;

        #endregion

        #region 事件定义

        /// <summary>
        /// 当内部规则列表中有任何规则匹配成功时，同步触发的统一事件回调
        /// </summary>
        public event Action<StateBindingRule, StateChangeContext> OnMatched;

        #endregion

        #region 公共属性

        /// <summary>
        /// 关联引用的静态配置资产
        /// </summary>
        public StateLayerConfig LayerConfig
        {
            get => _layerConfig;
            set => _layerConfig = value;
        }

        /// <summary>
        /// 关联的状态层实例标识 ID
        /// </summary>
        public string LayerId
        {
            get => _layerId;
            set => _layerId = value ?? string.Empty;
        }

        /// <summary>
        /// 规则列表
        /// </summary>
        public List<StateBindingRule> Rules
        {
            get => _rules;
            set => _rules = value ?? new List<StateBindingRule>();
        }

        /// <summary>
        /// 是否处于已绑定监听状态
        /// </summary>
        public bool IsBound => _isBound;

        /// <summary>
        /// 当前绑定的 StateLayer 引用（若采用无感绑定则可能为空）
        /// </summary>
        public StateLayer BoundStateLayer => _boundStateLayer;

        /// <summary>
        /// 最新接收到的状态完整路径
        /// </summary>
        public string CurrentFullPath => _currentFullPath;

        /// <summary>
        /// 上一次接收到的状态完整路径
        /// </summary>
        public string PreviousFullPath => _previousFullPath;

        /// <summary>
        /// 观察者评估历史日志记录器
        /// </summary>
        public StateChangeHistory HistoryLog => _historyLog;

        #endregion

        #region 构造函数

        public StateObserver()
        {
        }

        /// <summary>
        /// 纯 C# 动态创建观察者构造函数（在 MonoBehaviour 中建议直接声明 [SerializeField] 并利用 Inspector 配置）
        /// </summary>
        /// <param name="config">引用的 StateLayerConfig 资产</param>
        /// <param name="rules">绑定规则列表</param>
        public StateObserver(StateLayerConfig config, List<StateBindingRule> rules = null)
        {
            _layerConfig = config;
            _rules = rules ?? new List<StateBindingRule>();
        }

        #endregion

        #region 公共绑定接口

        /// <summary>
        /// 全自动绑定：对接 StateEventPipeline 全局事件管线。
        /// 无需依赖外部获取 StateLayer 物理引用，凭内部配置的 LayerConfig 全自动建立监听与同步。
        /// </summary>
        /// <param name="autoSyncCurrentState">绑定成功后是否向管线发送刷新请求以同步当前状态（默认 true）</param>
        public void Bind(bool autoSyncCurrentState = true)
        {
            if (_isBound)
            {
                Unbind();
            }

            if (_layerConfig == null)
            {
                Debug.LogWarning("[CwcStateLayer] 观察者绑定中断：引用的 StateLayerConfig 资产未配置！");
                return;
            }

            StateEventPipeline.OnStateBroadcasting += HandlePipelineBroadcasting;
            _isBound = true;

            Debug.Log($"[CwcStateLayer] 观察者已成功绑定事件管线！Config: [{_layerConfig.name}], LayerId: [{_layerId}], 规则条数: {_rules?.Count ?? 0}");

            if (autoSyncCurrentState)
            {
                SyncCurrentState();
            }
        }

        /// <summary>
        /// 兼容接口：显式指定目标 StateLayer 实例并完成管线绑定
        /// </summary>
        /// <param name="targetLayer">目标状态层</param>
        /// <param name="autoSyncCurrentState">绑定成功后是否自动根据当前状态触发一次刷新同步（默认 false）</param>
        public void Bind(StateLayer targetLayer, bool autoSyncCurrentState = false)
        {
            _boundStateLayer = targetLayer;
            if (targetLayer != null && targetLayer.Config != null)
            {
                _layerConfig = targetLayer.Config;
                _layerId = targetLayer.LayerId;
            }

            Bind(autoSyncCurrentState);
        }

        /// <summary>
        /// 解除全局事件管线绑定
        /// </summary>
        public void Unbind()
        {
            if (!_isBound)
            {
                return;
            }

            StateEventPipeline.OnStateBroadcasting -= HandlePipelineBroadcasting;
            _isBound = false;
            _boundStateLayer = null;
            Debug.Log($"[CwcStateLayer] 观察者已解除管线绑定。Config: [{_layerConfig?.name}], LayerId: [{_layerId}]");
        }

        #endregion

        #region 公共刷新与双向控制接口

        /// <summary>
        /// 向全局事件管线发布刷新请求，促使对应的 StateLayer 广播当前状态
        /// </summary>
        public void SyncCurrentState()
        {
            if (_layerConfig == null)
            {
                return;
            }

            StateEventPipeline.PublishSyncRequest(_layerConfig, _layerId);
        }

        /// <summary>
        /// 向全局事件管线发布状态变更请求，促使对应的 StateLayer 执行状态切换
        /// </summary>
        /// <param name="targetPathOrId">目标状态路径或节点 ID</param>
        /// <param name="reason">变更原因</param>
        public void RequestStateChange(string targetPathOrId, StateChangeReason reason = StateChangeReason.Transition)
        {
            if (_layerConfig == null)
            {
                Debug.LogWarning("[CwcStateLayer] 发起状态变更请求失败：未配置 StateLayerConfig。");
                return;
            }

            StateEventPipeline.PublishChangeStateRequest(_layerConfig, _layerId, targetPathOrId, reason);
        }

        #endregion

        #region 私有评估与事件处理

        /// <summary>
        /// 内部评估给定的状态变更上下文，若匹配成功则同步触发 OnMatched 回调
        /// </summary>
        private void EvaluateInternal(in StateChangeContext context)
        {
            _currentFullPath = context.NewFullPath;
            _previousFullPath = context.OldFullPath;

            if (_rules == null || _rules.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _rules.Count; i++)
            {
                StateBindingRule rule = _rules[i];
                if (rule != null && rule.IsMatched(context))
                {
                    string ruleDesc = $"{rule.FromPath} -> {rule.ToPath}";
                    _historyLog?.Record(in context, ruleDesc);
                    Debug.Log($"[CwcStateLayer] 观察者规则匹配成功！[{ruleDesc}] (Config: [{_layerConfig?.name}])");
                    OnMatched?.Invoke(rule, context);

                    if (rule.StopOnMatch)
                    {
                        Debug.Log($"[CwcStateLayer] 规则配置了 StopOnMatch，已成功中断后续规则的触发评估。");
                        break;
                    }
                }
            }
        }

        private void HandlePipelineBroadcasting(StatePipelineEvent evt)
        {
            if (_layerConfig == null || evt.Config != _layerConfig)
            {
                return;
            }

            if (_layerId != evt.LayerId)
            {
                return;
            }

            EvaluateInternal(in evt.Context);
        }

        #endregion
    }

    /// <summary>
    /// 纯 C# 强类型数据载荷核心状态观察者，匹配时带出强类型配置数据 TData。
    /// 通过 StateEventPipeline 事件管线全自动匹配广播与进行双向交互。
    /// </summary>
    /// <typeparam name="TData">强类型数据载荷类型</typeparam>
    [Serializable]
    public class StateObserver<TData>
    {
        #region 序列化字段

        [Tooltip("引用的状态层配置资产（权威身份标识）")]
        [SerializeField] private StateLayerConfig _layerConfig;

        [Tooltip("状态层实例标识 ID（可选，用于区分相同 Config 的多实例）")]
        [SerializeField] private string _layerId = string.Empty;

        [Tooltip("强类型状态绑定规则列表")]
        [SerializeField] private List<StateBindingRule<TData>> _rules = new List<StateBindingRule<TData>>();

        [Tooltip("最新接收到的状态完整路径（调试检查）")]
        [SerializeField] private string _currentFullPath = string.Empty;

        [Tooltip("上一次接收到的状态完整路径（调试检查）")]
        [SerializeField] private string _previousFullPath = string.Empty;

        [Tooltip("观察者接收到的状态变更评估日志")]
        [SerializeField] private StateChangeHistory _historyLog = new StateChangeHistory();

        #endregion

        #region 非序列化私有字段

        [NonSerialized] private StateLayer _boundStateLayer;
        [NonSerialized] private bool _isBound;

        #endregion

        #region 事件定义

        /// <summary>
        /// 当内部规则列表中有任何规则匹配成功时，同步触发的统一事件回调
        /// </summary>
        public event Action<StateBindingRule<TData>, StateChangeContext> OnMatched;

        #endregion

        #region 公共属性

        /// <summary>
        /// 关联引用的静态配置资产
        /// </summary>
        public StateLayerConfig LayerConfig
        {
            get => _layerConfig;
            set => _layerConfig = value;
        }

        /// <summary>
        /// 关联的状态层实例标识 ID
        /// </summary>
        public string LayerId
        {
            get => _layerId;
            set => _layerId = value ?? string.Empty;
        }

        /// <summary>
        /// 强类型规则列表
        /// </summary>
        public List<StateBindingRule<TData>> Rules
        {
            get => _rules;
            set => _rules = value ?? new List<StateBindingRule<TData>>();
        }

        /// <summary>
        /// 是否处于已绑定监听状态
        /// </summary>
        public bool IsBound => _isBound;

        /// <summary>
        /// 当前绑定的 StateLayer 实例（若采用无感绑定则可能为空）
        /// </summary>
        public StateLayer BoundStateLayer => _boundStateLayer;

        /// <summary>
        /// 最新接收到的状态完整路径
        /// </summary>
        public string CurrentFullPath => _currentFullPath;

        /// <summary>
        /// 上一次接收到的状态完整路径
        /// </summary>
        public string PreviousFullPath => _previousFullPath;

        /// <summary>
        /// 观察者评估历史日志记录器
        /// </summary>
        public StateChangeHistory HistoryLog => _historyLog;

        #endregion

        #region 构造函数

        public StateObserver()
        {
        }

        /// <summary>
        /// 纯 C# 动态创建强类型观察者构造函数（在 MonoBehaviour 中建议直接声明 [SerializeField] 并利用 Inspector 配置）
        /// </summary>
        /// <param name="config">引用的 StateLayerConfig 资产</param>
        /// <param name="rules">强类型绑定规则列表</param>
        public StateObserver(StateLayerConfig config, List<StateBindingRule<TData>> rules = null)
        {
            _layerConfig = config;
            _rules = rules ?? new List<StateBindingRule<TData>>();
        }

        #endregion

        #region 公共绑定接口

        /// <summary>
        /// 全自动绑定：对接 StateEventPipeline 全局事件管线。
        /// 无需依赖外部获取 StateLayer 物理引用，凭内部配置的 LayerConfig 全自动建立监听与同步。
        /// </summary>
        /// <param name="autoSyncCurrentState">绑定成功后是否向管线发送刷新请求以同步当前状态（默认 true）</param>
        public void Bind(bool autoSyncCurrentState = true)
        {
            if (_isBound)
            {
                Unbind();
            }

            if (_layerConfig == null)
            {
                Debug.LogWarning("[CwcStateLayer] 强类型观察者绑定中断：引用的 StateLayerConfig 资产未配置！");
                return;
            }

            StateEventPipeline.OnStateBroadcasting += HandlePipelineBroadcasting;
            _isBound = true;

            Debug.Log($"[CwcStateLayer] 强类型观察者已成功绑定事件管线！Config: [{_layerConfig.name}], LayerId: [{_layerId}], 规则条数: {_rules?.Count ?? 0}");

            if (autoSyncCurrentState)
            {
                SyncCurrentState();
            }
        }

        /// <summary>
        /// 兼容接口：显式指定目标 StateLayer 实例并完成管线绑定
        /// </summary>
        /// <param name="targetLayer">目标状态层</param>
        /// <param name="autoSyncCurrentState">绑定成功后是否自动根据当前状态触发一次刷新同步（默认 false）</param>
        public void Bind(StateLayer targetLayer, bool autoSyncCurrentState = false)
        {
            _boundStateLayer = targetLayer;
            if (targetLayer != null && targetLayer.Config != null)
            {
                _layerConfig = targetLayer.Config;
                _layerId = targetLayer.LayerId;
            }

            Bind(autoSyncCurrentState);
        }

        /// <summary>
        /// 解除全局事件管线绑定
        /// </summary>
        public void Unbind()
        {
            if (!_isBound)
            {
                return;
            }

            StateEventPipeline.OnStateBroadcasting -= HandlePipelineBroadcasting;
            _isBound = false;
            _boundStateLayer = null;
            Debug.Log($"[CwcStateLayer] 强类型观察者已解除管线绑定。Config: [{_layerConfig?.name}], LayerId: [{_layerId}]");
        }

        #endregion

        #region 公共刷新与双向控制接口

        /// <summary>
        /// 向全局事件管线发布刷新请求，促使对应的 StateLayer 广播当前状态
        /// </summary>
        public void SyncCurrentState()
        {
            if (_layerConfig == null)
            {
                return;
            }

            StateEventPipeline.PublishSyncRequest(_layerConfig, _layerId);
        }

        /// <summary>
        /// 向全局事件管线发布状态变更请求，促使对应的 StateLayer 执行状态切换
        /// </summary>
        /// <param name="targetPathOrId">目标状态路径或节点 ID</param>
        /// <param name="reason">变更原因</param>
        public void RequestStateChange(string targetPathOrId, StateChangeReason reason = StateChangeReason.Transition)
        {
            if (_layerConfig == null)
            {
                Debug.LogWarning("[CwcStateLayer] 发起状态变更请求失败：未配置 StateLayerConfig。");
                return;
            }

            StateEventPipeline.PublishChangeStateRequest(_layerConfig, _layerId, targetPathOrId, reason);
        }

        #endregion

        #region 私有评估与事件处理

        /// <summary>
        /// 内部评估给定的状态变更上下文，若匹配成功则同步触发 OnMatched 回调
        /// </summary>
        private void EvaluateInternal(in StateChangeContext context)
        {
            _currentFullPath = context.NewFullPath;
            _previousFullPath = context.OldFullPath;

            if (_rules == null || _rules.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _rules.Count; i++)
            {
                StateBindingRule<TData> rule = _rules[i];
                if (rule != null && rule.IsMatched(context))
                {
                    string ruleDesc = $"{rule.FromPath} -> {rule.ToPath}";
                    _historyLog?.Record(in context, ruleDesc);
                    Debug.Log($"[CwcStateLayer] 强类型观察者规则匹配成功！[{ruleDesc}] (Config: [{_layerConfig?.name}])");
                    OnMatched?.Invoke(rule, context);

                    if (rule.StopOnMatch)
                    {
                        Debug.Log($"[CwcStateLayer] 强类型规则配置了 StopOnMatch，已成功中断后续规则的触发评估。");
                        break;
                    }
                }
            }
        }

        private void HandlePipelineBroadcasting(StatePipelineEvent evt)
        {
            if (_layerConfig == null || evt.Config != _layerConfig)
            {
                return;
            }

            if (_layerId != evt.LayerId)
            {
                return;
            }

            EvaluateInternal(in evt.Context);
        }

        #endregion
    }
}
