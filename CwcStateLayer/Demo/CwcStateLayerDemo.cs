using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer.Demo
{
    /// <summary>
    /// 分层响应式状态公告板系统 Demo 运行与效果测试控制器
    /// </summary>
    [AddComponentMenu("Cwcbb/StateLayer/Demo/Cwc State Layer Demo")]
    public class CwcStateLayerDemo : MonoBehaviour
    {
        #region 序列化字段

        [Tooltip("引用的主状态层配置资产")]
        [SerializeField] private StateLayerConfig _mainLayerConfig;

        [Tooltip("场景中的 UI 测试观察者组件")]
        [SerializeField] private TestUIStateObserver _uiStateObserver;

        #endregion

        #region 非序列化私有字段

        private StateLayer _runtimeStateLayer;

        #endregion

        #region Unity生命周期方法

        private void Start()
        {
            if (_mainLayerConfig == null)
            {
                Debug.LogWarning("[StateLayerDemo] 请先在 Inspector 中创建并赋值 StateLayerConfig 资产！");
                return;
            }

            // 1. 初始化纯 C# 运行时 StateLayer 控制器
            _runtimeStateLayer = new StateLayer();
            _runtimeStateLayer.Initialize(_mainLayerConfig);

            // 2. 绑定 UI 观察者组件，并注册观察者【单一统一匹配回调】！
            if (_uiStateObserver != null)
            {
                _uiStateObserver.BindStateLayer(_runtimeStateLayer);

                // 无论是匹配到哪一条规则，整个组件都通过这一个统一事件通知 UI 管理器，带出配置 Data 与上下文！
                _uiStateObserver.CoreObserver.OnMatched += OnUIStateMatched;
            }

            // 3. 注册底层全局广播事件
            _runtimeStateLayer.OnStateChanged += OnLayerStateChanged;

            Debug.Log("[StateLayerDemo] 状态公告板 Demo 初始化成功！在运行模式下按下键盘 [Space] 键可模拟切换状态。");
        }

        private void Update()
        {
            if (_runtimeStateLayer == null || _mainLayerConfig == null || _mainLayerConfig.Nodes == null)
            {
                return;
            }

            // 键盘空格键触发模拟状态切换
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TriggerNextStateTest();
            }
        }

        private void OnDestroy()
        {
            if (_uiStateObserver != null && _uiStateObserver.CoreObserver != null)
            {
                _uiStateObserver.CoreObserver.OnMatched -= OnUIStateMatched;
            }

            if (_runtimeStateLayer != null)
            {
                _runtimeStateLayer.OnStateChanged -= OnLayerStateChanged;
            }
        }

        #endregion

        #region 私有测试方法

        /// <summary>
        /// 观察者组件内任意规则匹配成功时的【单一统一入口】
        /// </summary>
        private void OnUIStateMatched(StateBindingRule<DemoUIStatePayload> matchedRule, StateChangeContext context)
        {
            DemoUIStatePayload data = matchedRule.Data;
            string pageTitle = data != null ? data.pageTitle : "无配置";

            Debug.Log($"<color=cyan>[UI Manager 统一回调响应]</color> 匹配到规则 [{matchedRule.FromPath} -> {matchedRule.ToPath}] (类型: {context.Reason})！" +
                      $"\n当前界面标题: {pageTitle}，上下文: {context.NewFullPath}");
        }

        private void OnLayerStateChanged(StateLayer layer, StateChangeContext context)
        {
            Debug.Log($"[StateLayer 权威广播] 状态层切换: [{context.OldFullPath}] -> [{context.NewFullPath}]");
        }

        private void TriggerNextStateTest()
        {
            if (_mainLayerConfig.Nodes.Count == 0)
            {
                Debug.LogWarning("[StateLayerDemo] 配置资产中没有 Node 节点，无法切换。");
                return;
            }

            // 计算下一个切换的状态 ID
            int currentIndex = -1;
            for (int i = 0; i < _mainLayerConfig.Nodes.Count; i++)
            {
                if (_mainLayerConfig.Nodes[i].StateId == _runtimeStateLayer.CurrentStateId)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = (currentIndex + 1) % _mainLayerConfig.Nodes.Count;
            string targetStateId = _mainLayerConfig.Nodes[nextIndex].StateId;

            Debug.Log($"[StateLayerDemo] 发起状态切换 -> 目标 StateID: {targetStateId}");
            _runtimeStateLayer.ChangeState(targetStateId);
        }

        #endregion
    }
}
