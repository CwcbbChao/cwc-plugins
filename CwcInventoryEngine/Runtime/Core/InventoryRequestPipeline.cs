using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 主动请求事件管线 (Request Pipeline / Request Bus)。
    /// 用于将 UI 控制器发出的主动操作意图 (如 Drop, Use, Move) 广播分发给具体的内置 Handler 或组合 Behavior 组件。
    /// 完全解耦 UI 视图层与逻辑处理层。
    /// </summary>
    public static class InventoryRequestPipeline
    {
        #region 私有静态字段
        private static readonly Dictionary<Type, Delegate> _requestListeners = new();
        private static readonly List<Action<InventoryRequest>> _untypedListeners = new();
        #endregion

        #region 公共事件
        /// <summary>
        /// 全局通用无类型请求订阅事件 (可用于全局日志监控、审计或通用安全校验)。
        /// </summary>
        public static event Action<InventoryRequest> OnAnyRequest;
        #endregion

        #region 公共泛型订阅 API
        /// <summary>
        /// 订阅特定类型的请求。
        /// </summary>
        /// <typeparam name="T">继承自 InventoryRequest 的请求类型</typeparam>
        /// <param name="listener">处理函数回调</param>
        public static void Subscribe<T>(Action<T> listener) where T : InventoryRequest
        {
            if (listener == null) return;
            Type requestType = typeof(T);

            if (_requestListeners.TryGetValue(requestType, out Delegate existingDelegate))
            {
                _requestListeners[requestType] = Delegate.Combine(existingDelegate, listener);
            }
            else
            {
                _requestListeners[requestType] = listener;
            }
        }

        /// <summary>
        /// 取消订阅特定类型的请求。
        /// </summary>
        /// <typeparam name="T">继承自 InventoryRequest 的请求类型</typeparam>
        /// <param name="listener">处理函数回调</param>
        public static void Unsubscribe<T>(Action<T> listener) where T : InventoryRequest
        {
            if (listener == null) return;
            Type requestType = typeof(T);

            if (_requestListeners.TryGetValue(requestType, out Delegate existingDelegate))
            {
                Delegate current = Delegate.Remove(existingDelegate, listener);
                if (current == null)
                {
                    _requestListeners.Remove(requestType);
                }
                else
                {
                    _requestListeners[requestType] = current;
                }
            }
        }
        #endregion

        #region 公共广播 API
        /// <summary>
        /// 发送请求到请求总线。
        /// 自动寻找所有订阅了该请求类型的 Handler 并触发回调。
        /// </summary>
        /// <typeparam name="T">请求类型</typeparam>
        /// <param name="request">请求实例对象</param>
        public static void Send<T>(T request) where T : InventoryRequest
        {
            if (request == null) return;

            // 1. 触发无类型全局通用监听器
            OnAnyRequest?.Invoke(request);

            // 2. 查找精准类型的强类型监听器
            Type requestType = request.GetType();
            if (_requestListeners.TryGetValue(requestType, out Delegate listenerDelegate))
            {
                if (listenerDelegate is Action<T> typedAction)
                {
                    typedAction.Invoke(request);
                }
                else
                {
                    // 处理通过基类或多态派发的情况
                    listenerDelegate.DynamicInvoke(request);
                }
            }
        }

        /// <summary>
        /// 重置并清空所有请求订阅 (主要用于单元测试或场景重载时重置状态)。
        /// </summary>
        public static void ClearAllListeners()
        {
            _requestListeners.Clear();
            OnAnyRequest = null;
        }
        #endregion
    }
}
