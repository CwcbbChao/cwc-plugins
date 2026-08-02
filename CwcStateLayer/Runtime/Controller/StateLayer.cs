using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 权威状态层控制器（纯 C# 运行时类），接收静态配置数据完成构建与全路径状态管控。
    /// </summary>
    [Serializable]
    public class StateLayer
    {
        #region 序列化属性与字段

        [Tooltip("当前激活的状态完整路径（调试检查）")]
        [SerializeField] private string _currentFullPath = string.Empty;

        [Tooltip("上一次激活的状态完整路径（调试检查）")]
        [SerializeField] private string _previousFullPath = string.Empty;

        [Tooltip("权威状态变更历史日志")]
        [SerializeField] private StateChangeHistory _historyLog = new StateChangeHistory();

        [Tooltip("状态层实例标识 ID（可选，用于区分相同 Config 的多实例）")]
        [SerializeField] private string _layerId = string.Empty;

        #endregion

        #region 非序列化私有字段

        private StateLayerConfig _config;
        private int _depth;
        private StateRuntimeNode _currentNode;
        private StateRuntimeNode _previousNode;
        private readonly Dictionary<int, StateRuntimeNode> _nodesByLocalIdHash = new Dictionary<int, StateRuntimeNode>();
        private readonly Dictionary<int, StateRuntimeNode> _nodesByFullPathHash = new Dictionary<int, StateRuntimeNode>();

        #endregion

        #region 事件定义

        /// <summary>
        /// 当状态发生变更时同步广播的全域事件
        /// </summary>
        public event Action<StateLayer, StateChangeContext> OnStateChanged;

        #endregion

        #region 公共属性

        /// <summary>
        /// 控制器引用的静态配置资产
        /// </summary>
        public StateLayerConfig Config => _config;

        /// <summary>
        /// 状态层实例标识 ID（可选）
        /// </summary>
        public string LayerId
        {
            get => _layerId;
            set => _layerId = value ?? string.Empty;
        }

        /// <summary>
        /// 当前状态层控制器所在的深度（1-based）
        /// </summary>
        public int Depth => _depth;

        /// <summary>
        /// 当前激活的状态节点
        /// </summary>
        public StateRuntimeNode CurrentNode => _currentNode;

        /// <summary>
        /// 当前激活的状态 ID
        /// </summary>
        public string CurrentStateId => _currentNode != null ? _currentNode.StateId : string.Empty;

        /// <summary>
        /// 当前激活的状态完整路径
        /// </summary>
        public string CurrentFullPath => _currentFullPath;

        /// <summary>
        /// 当前激活的状态完整路径哈希值
        /// </summary>
        public int CurrentFullPathHash => _currentNode != null ? _currentNode.FullPathHash : 0;

        /// <summary>
        /// 上一个激活的状态节点
        /// </summary>
        public StateRuntimeNode PreviousNode => _previousNode;

        /// <summary>
        /// 上一个激活的状态 ID
        /// </summary>
        public string PreviousStateId => _previousNode != null ? _previousNode.StateId : string.Empty;

        /// <summary>
        /// 上一个激活的状态完整路径
        /// </summary>
        public string PreviousFullPath => _previousFullPath;

        /// <summary>
        /// 上一个激活的状态完整路径哈希值
        /// </summary>
        public int PreviousFullPathHash => _previousNode != null ? _previousNode.FullPathHash : 0;

        /// <summary>
        /// 权威状态变更历史日志记录器
        /// </summary>
        public StateChangeHistory HistoryLog => _historyLog;

        #endregion

        #region 公共初始化与控制接口

        /// <summary>
        /// 接收配置资产并初始化运行时状态层
        /// </summary>
        /// <param name="config">静态配置资产</param>
        /// <param name="layerId">状态层实例标识 ID（可选）</param>
        public void Initialize(StateLayerConfig config, string layerId)
        {
            Initialize(config, layerId, 1, null);
        }

        /// <summary>
        /// 接收配置资产并初始化运行时状态层
        /// </summary>
        /// <param name="config">静态配置资产</param>
        /// <param name="currentDepth">当前递归深度</param>
        /// <param name="visitedConfigs">访问记录防循环引用</param>
        public void Initialize(StateLayerConfig config, int currentDepth = 1, HashSet<StateLayerConfig> visitedConfigs = null)
        {
            Initialize(config, _layerId, currentDepth, visitedConfigs);
        }

        /// <summary>
        /// 接收配置资产与实例 ID 并初始化运行时状态层
        /// </summary>
        /// <param name="config">静态配置资产</param>
        /// <param name="layerId">实例标识 ID</param>
        /// <param name="currentDepth">当前递归深度</param>
        /// <param name="visitedConfigs">访问记录防循环引用</param>
        public void Initialize(StateLayerConfig config, string layerId, int currentDepth, HashSet<StateLayerConfig> visitedConfigs)
        {
            if (config == null)
            {
                Debug.LogError("[CwcStateLayer] 初始化失败：传入的 StateLayerConfig 资产为空！");
                return;
            }

            if (currentDepth > StateLayerConfig.MaxDepthLimit)
            {
                throw new InvalidOperationException($"[CwcStateLayer] 初始化中止！StateLayerConfig [{config.name}] 深度达到 {currentDepth} 层，超过了硬编码最大限制 ({StateLayerConfig.MaxDepthLimit} 层)！");
            }

            visitedConfigs ??= new HashSet<StateLayerConfig>();
            if (visitedConfigs.Contains(config))
            {
                throw new InvalidOperationException($"[CwcStateLayer] 发生循环递归引用！Config: [{config.name}] 已在上下文调用链中。");
            }

            visitedConfigs.Add(config);

            UnsubscribePipeline();

            _config = config;
            if (!string.IsNullOrEmpty(layerId))
            {
                _layerId = layerId;
            }

            _depth = currentDepth;
            _currentNode = null;
            _previousNode = null;
            _currentFullPath = string.Empty;
            _previousFullPath = string.Empty;
            _nodesByLocalIdHash.Clear();
            _nodesByFullPathHash.Clear();

            BuildRuntimeNodesRecursive(_config, string.Empty, _depth, null, visitedConfigs);

            visitedConfigs.Remove(config);

            SubscribePipeline();
            Debug.Log($"[CwcStateLayer] 状态控制器初始化完成！Config: [{_config.name}], LayerId: [{_layerId}], 节点数: {_nodesByFullPathHash.Count}");
        }

        /// <summary>
        /// 释放或销毁 StateLayer 时取消全局管线订阅
        /// </summary>
        public void Dispose()
        {
            UnsubscribePipeline();
            Debug.Log($"[CwcStateLayer] 状态控制器销毁并取消管线订阅。Config: [{_config?.name}], LayerId: [{_layerId}]");
        }

        #endregion

        #region 全局事件管线对接

        private void SubscribePipeline()
        {
            if (_config == null) return;
            StateEventPipeline.OnSyncRequesting += HandlePipelineSyncRequest;
            StateEventPipeline.OnChangeStateRequesting += HandlePipelineChangeStateRequest;
        }

        private void UnsubscribePipeline()
        {
            StateEventPipeline.OnSyncRequesting -= HandlePipelineSyncRequest;
            StateEventPipeline.OnChangeStateRequesting -= HandlePipelineChangeStateRequest;
        }

        private void HandlePipelineSyncRequest(StateSyncRequest req)
        {
            if (_config == null || req.Config != _config) return;
            if (_layerId != req.LayerId) return;

            string currentPath = _currentFullPath;
            StateChangeContext context = new StateChangeContext(
                currentPath,
                currentPath,
                StateChangeReason.Sync);

            StateEventPipeline.PublishStateChanged(_config, _layerId, context);
        }

        private void HandlePipelineChangeStateRequest(StateChangeRequest req)
        {
            if (_config == null || req.Config != _config) return;
            if (_layerId != req.LayerId) return;

            ChangeState(req.TargetPathOrId);
        }

        private void BroadcastStateChanged(in StateChangeContext context)
        {
            _historyLog?.Record(in context);
            OnStateChanged?.Invoke(this, context);
            if (_config != null)
            {
                StateEventPipeline.PublishStateChanged(_config, _layerId, context);
            }
        }

        /// <summary>
        /// 权威切换状态方法（通过路径或 ID 字符串匹配）
        /// </summary>
        /// <param name="targetStateIdOrPath">目标状态 ID 或完整路径</param>
        /// <returns>切换是否成功</returns>
        public bool ChangeState(string targetStateIdOrPath)
        {
            if (string.IsNullOrEmpty(targetStateIdOrPath))
            {
                Debug.LogWarning("[CwcStateLayer] 切换状态失败：目标状态标识为空。");
                return false;
            }

            int pathHash = StatePathUtility.StringToHash(targetStateIdOrPath);
            return ChangeStateInternal(pathHash, targetStateIdOrPath);
        }

        /// <summary>
        /// 零 GC 权威切换状态方法（通过预计算的哈希值匹配）
        /// </summary>
        /// <param name="targetPathHash">目标状态路径或 ID 的整数哈希值</param>
        /// <returns>切换是否成功</returns>
        public bool ChangeState(int targetPathHash)
        {
            if (targetPathHash == 0)
            {
                Debug.LogWarning("[CwcStateLayer] 切换状态失败：目标哈希值为 0。");
                return false;
            }

            return ChangeStateInternal(targetPathHash, null);
        }

        /// <summary>
        /// 零 GC 权威切换状态方法（通过 StatePathInfo 标识匹配）
        /// </summary>
        /// <param name="pathInfo">状态路径标识</param>
        /// <returns>切换是否成功</returns>
        public bool ChangeState(in StatePathInfo pathInfo)
        {
            if (pathInfo.Hash == 0)
            {
                Debug.LogWarning("[CwcStateLayer] 切换状态失败：目标状态标识 Hash 为 0。");
                return false;
            }

            return ChangeStateInternal(pathInfo.Hash, pathInfo.Path);
        }

        /// <summary>
        /// 根据 State ID 或 FullPath 查找运行时节点
        /// </summary>
        public StateRuntimeNode FindNode(string idOrPath)
        {
            if (string.IsNullOrEmpty(idOrPath))
            {
                return null;
            }

            int hash = StatePathUtility.StringToHash(idOrPath);
            return FindNode(hash);
        }

        /// <summary>
        /// 根据路径或 ID 哈希值查找运行时节点
        /// </summary>
        public StateRuntimeNode FindNode(int pathOrIdHash)
        {
            if (pathOrIdHash == 0)
            {
                return null;
            }

            if (_nodesByFullPathHash.TryGetValue(pathOrIdHash, out StateRuntimeNode nodeByPath))
            {
                return nodeByPath;
            }

            if (_nodesByLocalIdHash.TryGetValue(pathOrIdHash, out StateRuntimeNode nodeById))
            {
                return nodeById;
            }

            return null;
        }

        /// <summary>
        /// 全量重置当前 Layer 及其所有状态为 null（全量状态对齐与复位）
        /// </summary>
        public void ResetState()
        {
            if (_currentNode == null)
            {
                return;
            }

            _previousNode = _currentNode;
            _previousFullPath = _currentNode.FullPath;

            string oldPath = _previousFullPath;

            _currentNode = null;
            _currentFullPath = string.Empty;

            StateChangeContext context = new StateChangeContext(
                oldPath,
                string.Empty);

            BroadcastStateChanged(in context);
            Debug.Log($"[CwcStateLayer] 状态已全量复位为 Null (旧路径: [{oldPath}])");
        }

        #endregion

        #region 私有内部逻辑

        /// <summary>
        /// 核心状态切换内部实现
        /// </summary>
        private bool ChangeStateInternal(int targetHash, string debugPathString)
        {
            StateRuntimeNode targetNode = FindNode(targetHash);

            if (targetNode == null)
            {
                string pathName = string.IsNullOrEmpty(debugPathString) ? targetHash.ToString() : debugPathString;
                Debug.LogWarning($"[CwcStateLayer] 切换状态失败：未找到标识为 [{pathName}] 的节点。");
                return false;
            }

            if (_currentNode == targetNode)
            {
                return true;
            }

            _previousNode = _currentNode;
            _previousFullPath = _currentNode != null ? _currentNode.FullPath : string.Empty;

            string oldPath = _previousFullPath;

            _currentNode = targetNode;
            _currentFullPath = _currentNode.FullPath;

            StateChangeContext context = new StateChangeContext(
                oldPath,
                _currentFullPath);

            BroadcastStateChanged(in context);
            Debug.Log($"[CwcStateLayer] 状态成功切换: [{oldPath}] -> [{_currentFullPath}] (Config: [{_config?.name}], LayerId: [{_layerId}])");
            return true;
        }

        /// <summary>
        /// 递归构建运行时节点树结构，并校验同一层级 StateId 的唯一性
        /// </summary>
        private void BuildRuntimeNodesRecursive(
            StateLayerConfig layerConfig,
            string parentPath,
            int depth,
            StateRuntimeNode parentNode,
            HashSet<StateLayerConfig> visitedConfigs)
        {
            if (layerConfig == null || layerConfig.Nodes == null)
            {
                return;
            }

            HashSet<string> localStateIds = new HashSet<string>();

            foreach (StateNodeConfig nodeConfig in layerConfig.Nodes)
            {
                if (nodeConfig == null || string.IsNullOrEmpty(nodeConfig.StateId))
                {
                    continue;
                }

                // 规则防护：同一直接父节点（同一配置层级）下绝对不能包含重名的 StateId！
                if (localStateIds.Contains(nodeConfig.StateId))
                {
                    Debug.LogError($"[CwcStateLayer] 配置错误！在 StateLayerConfig [{layerConfig.name}] 中检测到重名的 StateId: [{nodeConfig.StateId}]！同一层级下的 StateId 必须具备唯一性！");
                    continue;
                }

                localStateIds.Add(nodeConfig.StateId);

                string fullPath = string.IsNullOrEmpty(parentPath)
                    ? nodeConfig.StateId
                    : $"{parentPath}/{nodeConfig.StateId}";

                StateRuntimeNode runtimeNode = new StateRuntimeNode(
                    nodeConfig,
                    fullPath,
                    depth,
                    parentNode);

                // 本层级的相对 ID 哈希映射
                _nodesByLocalIdHash[runtimeNode.StateIdHash] = runtimeNode;

                // 注册全局完整路径哈希映射
                _nodesByFullPathHash[runtimeNode.FullPathHash] = runtimeNode;

                // 若包含嵌套子 Layer 配置，递归解析其子节点加入全局全路径字典
                if (nodeConfig.HasSubLayer && nodeConfig.SubLayerConfig != null)
                {
                    if (visitedConfigs.Contains(nodeConfig.SubLayerConfig))
                    {
                        Debug.LogError($"[CwcStateLayer] 发生循环递归引用！SubLayerConfig: [{nodeConfig.SubLayerConfig.name}] 已在调用链中。");
                        continue;
                    }

                    visitedConfigs.Add(nodeConfig.SubLayerConfig);
                    BuildRuntimeNodesRecursive(nodeConfig.SubLayerConfig, fullPath, depth + 1, runtimeNode, visitedConfigs);
                    visitedConfigs.Remove(nodeConfig.SubLayerConfig);
                }
            }
        }

        #endregion
    }
}
