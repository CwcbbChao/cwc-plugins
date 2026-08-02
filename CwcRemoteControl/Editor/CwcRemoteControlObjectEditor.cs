#if UNITY_EDITOR
namespace Cwcbb.Tools
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// CwcRemoteControlObject 的自定义编辑器，在 Inspector 中提供运行期的快速信号触发调试面板。
    /// </summary>
    [CustomEditor(typeof(CwcRemoteControlObject))]
    public class CwcRemoteControlObjectEditor : Editor
    {
        #region 非序列化私有字段
        private int _debugSignalId = 0;
        #endregion

        #region 生命周期方法
        /// <summary>
        /// 绘制 Inspector GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            // 1. 首先绘制组件默认的属性字段
            DrawDefaultInspector();

            EditorGUILayout.Space(15);

            // 2. 绘制快速调试控制台标题
            EditorGUILayout.LabelField("快速调试控制台", EditorStyles.boldLabel);

            // 检查当前是否在运行模式 (Play Mode)
            bool isPlaying = Application.isPlaying;

            // 提示在非运行模式下的限制，编辑器状态下不运行以防意外修改物体永久状态
            if (!isPlaying)
            {
                EditorGUILayout.HelpBox("提示: 调试功能仅在运行模式 (Play Mode) 下可用，以防在编辑状态下由于触发 UnityEvent 导致物体状态被永久修改。", MessageType.Info);
            }

            // 3. 开始绘制调试输入和触发区域
            EditorGUI.BeginDisabledGroup(!isPlaying);
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    EditorGUILayout.LabelField("手动信号发射器", EditorStyles.miniBoldLabel);
                    
                    EditorGUILayout.BeginHorizontal();
                    {
                        _debugSignalId = EditorGUILayout.IntField("调试信号 ID", _debugSignalId);
                        
                        if (GUILayout.Button("发送信号", GUILayout.Width(100)))
                        {
                            CwcRemoteControlObject myTarget = (CwcRemoteControlObject)target;
                            myTarget.SendSignal(_debugSignalId);
                            Debug.Log($"[{nameof(CwcRemoteControlObjectEditor)}] 手动发送调试信号 ID: {_debugSignalId}", myTarget);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(10);

                    // 4. 读取配置的信号映射列表，并自动生成快捷触发按钮
                    SerializedProperty mappingsProperty = serializedObject.FindProperty("_signalMappings");
                    if (mappingsProperty != null && mappingsProperty.isArray && mappingsProperty.arraySize > 0)
                    {
                        EditorGUILayout.LabelField("已配置的信号快捷触发:", EditorStyles.miniBoldLabel);
                        
                        for (int i = 0; i < mappingsProperty.arraySize; i++)
                        {
                            SerializedProperty element = mappingsProperty.GetArrayElementAtIndex(i);
                            SerializedProperty idProp = element.FindPropertyRelative("_signalId");
                            
                            if (idProp != null)
                            {
                                int signalId = idProp.intValue;

                                EditorGUILayout.BeginHorizontal();
                                {
                                    EditorGUILayout.LabelField($"信号 ID: {signalId}", GUILayout.Width(120));

                                    if (GUILayout.Button($"触发信号 {signalId}", GUILayout.ExpandWidth(true)))
                                    {
                                        CwcRemoteControlObject myTarget = (CwcRemoteControlObject)target;
                                        myTarget.SendSignal(signalId);
                                        Debug.Log($"[{nameof(CwcRemoteControlObjectEditor)}] 快捷触发信号 ID: {signalId}", myTarget);
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("列表中没有配置任何信号映射，请在 [信号与事件映射列表] 中添加配置。", MessageType.None);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUI.EndDisabledGroup();
        }
        #endregion
    }
}
#endif
