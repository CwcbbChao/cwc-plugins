using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 响应式状态过渡事件绑定规则，定义 FromPath 与 ToPath 过渡，并为每个独立过渡绑定独有的无参 UnityEvent。
    /// </summary>
    [Serializable]
    public class ResponsiveStateBindingRule : StateBindingRule
    {
        #region 序列化字段

        [Tooltip("当本条状态过渡规则匹配成功时独立触发的事件回调（无参，方便 Inspector 直接拖拽如 GameObject.SetActive）")]
        [SerializeField] private UnityEvent _onMatched = new UnityEvent();

        #endregion

        #region 构造函数

        public ResponsiveStateBindingRule()
        {
        }

        public ResponsiveStateBindingRule(string fromPath, string toPath) : base(fromPath, toPath)
        {
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 当本条过渡规则匹配成功时触发的专属独立事件
        /// </summary>
        public UnityEvent OnMatched => _onMatched;

        #endregion
    }

    /// <summary>
    /// 通用无代码状态响应器 MonoBehaviour 桥接组件。
    /// 允许为每一个状态过渡独立配置专有的无参 UnityEvent，真正实现零代码、天然隔离的状态响应。
    /// </summary>
    [AddComponentMenu("Cwcbb/StateLayer/Responsive State Observer")]
    public class ResponsiveStateObserver : MonoBehaviour
    {
        #region 序列化字段

        [Tooltip("引用的状态层配置资产（权威身份标识）")]
        [SerializeField] private StateLayerConfig _layerConfig;

        [Tooltip("状态层实例标识 ID（可选，用于区分相同 Config 的多实例）")]
        [SerializeField] private string _layerId = string.Empty;

        [Tooltip("状态过渡事件规则列表（每个过渡独立拥有专属的 UnityEvent 回调）")]
        [SerializeField] private List<ResponsiveStateBindingRule> _rules = new List<ResponsiveStateBindingRule>();

        #endregion

        #region 非序列化私有字段

        [NonSerialized] private bool _isBound;

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
        /// 过渡事件规则列表
        /// </summary>
        public List<ResponsiveStateBindingRule> Rules
        {
            get => _rules;
            set => _rules = value ?? new List<ResponsiveStateBindingRule>();
        }

        /// <summary>
        /// 是否处于已绑定监听状态
        /// </summary>
        public bool IsBound => _isBound;

        #endregion

        #region Unity生命周期方法

        protected virtual void OnEnable()
        {
            Bind();
        }

        protected virtual void OnDisable()
        {
            Unbind();
        }

        #endregion

        #region 公共绑定接口

        /// <summary>
        /// 绑定全局事件管线
        /// </summary>
        /// <param name="autoSyncCurrentState">是否在绑定后自动同步当前状态（默认 true）</param>
        public void Bind(bool autoSyncCurrentState = true)
        {
            if (_isBound)
            {
                Unbind();
            }

            if (_layerConfig == null)
            {
                Debug.LogWarning($"[CwcStateLayer] ResponsiveStateObserver [{gameObject.name}] 绑定失败：引用的 StateLayerConfig 资产未配置！");
                return;
            }

            StateEventPipeline.OnStateBroadcasting += HandlePipelineBroadcasting;
            _isBound = true;

            Debug.Log($"[CwcStateLayer] ResponsiveStateObserver [{gameObject.name}] 已绑定管线！Config: [{_layerConfig.name}], 独立过渡规则数: {_rules?.Count ?? 0}");

            if (autoSyncCurrentState)
            {
                SyncCurrentState();
            }
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
            Debug.Log($"[CwcStateLayer] ResponsiveStateObserver [{gameObject.name}] 已解除管线绑定。");
        }

        /// <summary>
        /// 主动向管线发起当前状态同步请求
        /// </summary>
        public void SyncCurrentState()
        {
            if (_layerConfig == null) return;
            StateEventPipeline.PublishSyncRequest(_layerConfig, _layerId);
        }

        #endregion

        #region 私有事件处理

        private void HandlePipelineBroadcasting(StatePipelineEvent evt)
        {
            if (_layerConfig == null || evt.Config != _layerConfig) return;
            if (_layerId != evt.LayerId) return;

            EvaluateRules(in evt.Context);
        }

        private void EvaluateRules(in StateChangeContext context)
        {
            if (_rules == null || _rules.Count == 0) return;

            for (int i = 0; i < _rules.Count; i++)
            {
                ResponsiveStateBindingRule rule = _rules[i];
                if (rule != null && rule.IsMatched(context))
                {
                    Debug.Log($"[CwcStateLayer] ResponsiveStateObserver [{gameObject.name}] 规则匹配成功！[{rule.FromPath} -> {rule.ToPath}]，正在触发专属独立 UnityEvent。");
                    
                    // 触发本条过渡规则独有的无参 UnityEvent
                    rule.OnMatched?.Invoke();

                    if (rule.StopOnMatch)
                    {
                        Debug.Log($"[CwcStateLayer] ResponsiveStateObserver 规则配置了 StopOnMatch，已成功中断后续规则的触发评估。");
                        break;
                    }
                }
            }
        }

        #endregion
    }
}
