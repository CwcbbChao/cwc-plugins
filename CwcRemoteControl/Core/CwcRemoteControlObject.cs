namespace Cwcbb.Tools
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// 挂载在遥控预制件的最外层根节点上，是遥控对象被控端的唯一交互入口。
    /// 仅通过事件派发机制，与具体的控制器（RemoteController）进行信号解耦。
    /// </summary>
    [DisallowMultipleComponent]
    public class CwcRemoteControlObject : MonoBehaviour
    {
        #region 内部定义结构
        [Serializable]
        public struct SignalMapping
        {
            [Tooltip("整型信号 ID（例如：1为入场，2为点击互动，3为退场）")]
            [SerializeField] private int _signalId;

            [Tooltip("当信号触发时，派发执行的 UnityEvent 事件，支持在 Inspector 中可视化配置")]
            [SerializeField] private UnityEvent _onSignalTriggered;

            public int SignalId => _signalId;
            public UnityEvent OnSignalTriggered => _onSignalTriggered;
        }
        #endregion

        #region 序列化字段
        [Header("信号与事件映射列表")]
        [SerializeField] private List<SignalMapping> _signalMappings = new List<SignalMapping>();

        [Header("视觉容器")]
        [Tooltip("相关的视觉效果和模型等存放在此节点下，显示/隐藏时仅控制该节点的激活状态")]
        [SerializeField] private GameObject _visualContainer;
        #endregion

        #region 非序列化私有字段
        private CwcRemoteControlObjectConfig _config;
        private readonly Dictionary<int, UnityEvent> _signalCache = new Dictionary<int, UnityEvent>();
        private readonly List<int> _pendingSignals = new List<int>();
        #endregion

        #region 公共属性
        public CwcRemoteControlObjectConfig Config => _config;

        /// <summary>
        /// 视觉容器当前是否处于显示状态
        /// </summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// 当前是否被控制器占用（对象池状态管理）
        /// </summary>
        public bool IsOccupied { get; set; }
        #endregion

        #region 生命周期方法
        private void Awake()
        {
            _signalCache.Clear();
            if (_signalMappings != null)
            {
                for (int i = 0; i < _signalMappings.Count; i++)
                {
                    var mapping = _signalMappings[i];
                    if (!_signalCache.ContainsKey(mapping.SignalId))
                    {
                        _signalCache.Add(mapping.SignalId, mapping.OnSignalTriggered);
                    }
                    else
                    {
                        Debug.LogWarning($"[{nameof(CwcRemoteControlObject)}] 预制件 {gameObject.name} 上配置了重复的 SignalId: {mapping.SignalId}，已自动忽略重复的映射配置。", this);
                    }
                }
            }

            // 初始化视觉容器的可见性状态
            IsVisible = _visualContainer != null ? _visualContainer.activeSelf : true;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 初始化遥控对象，关联配置源
        /// </summary>
        public void Init(CwcRemoteControlObjectConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// 控制视觉容器的显示与隐藏，并在显示时触发缓存的信号
        /// </summary>
        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (_visualContainer != null)
            {
                _visualContainer.SetActive(visible);
            }
            else
            {
                Debug.LogWarning($"[{nameof(CwcRemoteControlObject)}] 预制件 {gameObject.name} 未关联 VisualContainer 视觉容器，无法控制其显隐状态。", this);
            }

            if (visible)
            {
                TriggerPendingSignals();
            }
        }

        /// <summary>
        /// 接收来自遥控器的整型信号。
        /// 若当前可见，则立即触发对应事件；若当前处于隐藏状态，则将信号缓存起来，待显示时触发。
        /// </summary>
        public void SendSignal(int signalId)
        {
            if (IsVisible)
            {
                TriggerSignal(signalId);
            }
            else
            {
                if (!_pendingSignals.Contains(signalId))
                {
                    _pendingSignals.Add(signalId);
                }
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 触发并清理缓存的所有待处理信号
        /// </summary>
        private void TriggerPendingSignals()
        {
            if (_pendingSignals.Count > 0)
            {
                var tempSignals = new List<int>(_pendingSignals);
                _pendingSignals.Clear();

                for (int i = 0; i < tempSignals.Count; i++)
                {
                    TriggerSignal(tempSignals[i]);
                }
            }
        }

        /// <summary>
        /// 触发特定信号所映射的事件
        /// </summary>
        private void TriggerSignal(int signalId)
        {
            if (_signalCache.TryGetValue(signalId, out UnityEvent evt))
            {
                evt?.Invoke();
            }
        }
        #endregion
    }
}
