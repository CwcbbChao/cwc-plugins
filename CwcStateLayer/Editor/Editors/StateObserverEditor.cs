using UnityEditor;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer.Editor
{
    /// <summary>
    /// 状态观察者组件 CustomEditor 扩展。
    /// </summary>
    [CustomEditor(typeof(ResponsiveStateObserver))]
    public class ResponsiveStateObserverEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
