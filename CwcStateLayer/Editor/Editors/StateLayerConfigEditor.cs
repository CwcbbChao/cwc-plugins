using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer.Editor
{
    /// <summary>
    /// StateLayerConfig 自定义 Inspector 扩展，提供全树路径可视化预览与深度安全检查。
    /// </summary>
    [CustomEditor(typeof(StateLayerConfig))]
    public class StateLayerConfigEditor : UnityEditor.Editor
    {
        #region 私有字段

        private bool _showPathPreview = true;

        #endregion

        #region 重写 Inspector 绘制方法

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            StateLayerConfig config = (StateLayerConfig)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("StateLayer 辅助工具", EditorStyles.boldLabel);

            _showPathPreview = EditorGUILayout.Foldout(_showPathPreview, "预览收集的全层级路径 (最大3层)", true);
            if (_showPathPreview)
            {
                EditorGUI.indentLevel++;
                List<string> paths = config.CollectAllFullPaths();
                if (paths != null && paths.Count > 0)
                {
                    foreach (string path in paths)
                    {
                        EditorGUILayout.SelectableLabel(path, EditorStyles.miniLabel, GUILayout.Height(16f));
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("暂无有效状态路径，请添加 Node 节点。", MessageType.Info);
                }
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}
