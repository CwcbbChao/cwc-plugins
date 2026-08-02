using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 强类型数据载荷状态观察者 MonoBehaviour 桥接抽象基类。
    /// 内聚封装 StateObserver<TData> 核心对象，极简管理生命周期与事件通知。
    /// </summary>
    /// <typeparam name="TData">强类型数据载荷类型</typeparam>
    public abstract class StateObserverComponent<TData> : MonoBehaviour
    {
        #region 序列化字段

        [Tooltip("内聚的强类型核心状态观察者")]
        [SerializeField] protected StateObserver<TData> _observer = new StateObserver<TData>();

        [Tooltip("统一组件回调事件（列表中任意规则匹配成功时触发并传出 TData）")]
        [SerializeField] private UnityEvent<StateBindingRule<TData>, StateChangeContext> _onMatched = new UnityEvent<StateBindingRule<TData>, StateChangeContext>();

        #endregion

        #region 公共属性

        /// <summary>
        /// 关联的配置资产
        /// </summary>
        public StateLayerConfig LayerConfig
        {
            get => _observer?.LayerConfig;
            set
            {
                if (_observer != null)
                {
                    _observer.LayerConfig = value;
                }
            }
        }

        /// <summary>
        /// 规则列表
        /// </summary>
        public List<StateBindingRule<TData>> Rules => _observer?.Rules;

        /// <summary>
        /// 内部持有的纯 C# 核心观察者对象
        /// </summary>
        public StateObserver<TData> CoreObserver => _observer;

        /// <summary>
        /// 组件统一匹配事件
        /// </summary>
        public UnityEvent<StateBindingRule<TData>, StateChangeContext> OnMatched => _onMatched;

        #endregion

        #region Unity生命周期方法

        protected virtual void Awake()
        {
            _observer ??= new StateObserver<TData>();
            _observer.OnMatched += HandleCoreObserverMatched;
        }

        protected virtual void OnEnable()
        {
            if (_observer != null)
            {
                _observer.Bind(autoSyncCurrentState: true);
            }
        }

        protected virtual void OnDisable()
        {
            _observer?.Unbind();
        }

        protected virtual void OnDestroy()
        {
            if (_observer != null)
            {
                _observer.OnMatched -= HandleCoreObserverMatched;
                _observer.Unbind();
            }
        }

        #endregion

        #region 公共绑定接口

        /// <summary>
        /// 绑定到目标 StateLayer 实例
        /// </summary>
        /// <param name="targetLayer">目标状态层</param>
        /// <param name="autoSyncCurrentState">是否在绑定后自动同步当前状态（默认 false）</param>
        public void BindStateLayer(StateLayer targetLayer, bool autoSyncCurrentState = false)
        {
            _observer ??= new StateObserver<TData>();
            _observer.Bind(targetLayer, autoSyncCurrentState);
        }

        /// <summary>
        /// 解除绑定
        /// </summary>
        public void UnbindStateLayer()
        {
            _observer?.Unbind();
        }

        /// <summary>
        /// 手动针对当前绑定的 StateLayer 实例的状态重新触发一次静态数据刷新同步（无参，类型标记为 Sync）
        /// </summary>
        public void SyncCurrentState()
        {
            _observer?.SyncCurrentState();
        }

        #endregion

        #region 私有回调桥接

        private void HandleCoreObserverMatched(StateBindingRule<TData> rule, StateChangeContext context)
        {
            _onMatched?.Invoke(rule, context);
        }

        #endregion
    }
}
