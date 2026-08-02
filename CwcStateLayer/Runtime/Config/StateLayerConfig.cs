using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 状态层静态配置资产（ScriptableObject），仅定义层级与节点结构，不包含任何运行时逻辑。
    /// </summary>
    [CreateAssetMenu(fileName = "NewStateLayerConfig", menuName = "Cwcbb/StateLayer/State Layer Config")]
    public class StateLayerConfig : ScriptableObject
    {
        #region 常量与静态字段

        /// <summary>
        /// 规定子 Layer 嵌套的最大硬编码深度限制（防止循环引用）
        /// </summary>
        public const int MaxDepthLimit = 3;

        #endregion

        #region 序列化字段

        [Tooltip("该状态层包含的节点配置列表")]
        [SerializeField] private List<StateNodeConfig> _nodes = new List<StateNodeConfig>();

        #endregion

        #region 公共属性

        /// <summary>
        /// 节点配置只读列表
        /// </summary>
        public IReadOnlyList<StateNodeConfig> Nodes => _nodes;

        #endregion

        #region 公共方法

        /// <summary>
        /// 递归收集当前配置资产下所有状态节点的完整路径列表（最多解析 MaxDepthLimit 层，仅对含子节点的容器生成 /Any 通配符）
        /// </summary>
        /// <returns>完整路径列表</returns>
        public List<string> CollectAllFullPaths()
        {
            List<string> pathList = new List<string>();
            HashSet<StateLayerConfig> visitedConfigs = new HashSet<StateLayerConfig>();
            CollectPathsRecursive(this, string.Empty, 1, visitedConfigs, pathList);
            return pathList;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 递归收集路径的核心辅助实现
        /// </summary>
        private static void CollectPathsRecursive(
            StateLayerConfig currentConfig,
            string currentParentPath,
            int currentDepth,
            HashSet<StateLayerConfig> visitedConfigs,
            List<string> resultList)
        {
            if (currentConfig == null || currentConfig.Nodes == null)
            {
                return;
            }

            if (currentDepth > MaxDepthLimit)
            {
                Debug.LogWarning($"[CwcStateLayer] 配置 [{currentConfig.name}] 嵌套深度超过最大限制 {MaxDepthLimit} 层，已停止递归。");
                return;
            }

            if (visitedConfigs.Contains(currentConfig))
            {
                Debug.LogError($"[CwcStateLayer] 检测到循环引用的 StateLayerConfig 资产: [{currentConfig.name}]！已拦截递归。");
                return;
            }

            visitedConfigs.Add(currentConfig);

            foreach (StateNodeConfig node in currentConfig.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.StateId))
                {
                    continue;
                }

                string nodePath = string.IsNullOrEmpty(currentParentPath)
                    ? node.StateId
                    : $"{currentParentPath}/{node.StateId}";

                resultList.Add(nodePath);

                // 仅当节点真正配置了子 Layer 且包含子节点时，才添加 /Any 通配选项与深入递归
                bool hasChildNodes = node.HasSubLayer &&
                                     node.SubLayerConfig != null &&
                                     node.SubLayerConfig.Nodes != null &&
                                     node.SubLayerConfig.Nodes.Count > 0;

                if (hasChildNodes)
                {
                    string wildcardPath = $"{nodePath}/Any";
                    if (!resultList.Contains(wildcardPath))
                    {
                        resultList.Add(wildcardPath);
                    }

                    CollectPathsRecursive(node.SubLayerConfig, nodePath, currentDepth + 1, visitedConfigs, resultList);
                }
            }

            visitedConfigs.Remove(currentConfig);
        }

        #endregion
    }
}
