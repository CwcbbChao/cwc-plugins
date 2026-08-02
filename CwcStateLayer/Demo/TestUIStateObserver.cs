using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer.Demo
{
    /// <summary>
    /// 测试用的强类型 UI 状态观察者组件（继承自 MonoBehaviour 桥接类 StateObserverComponent）
    /// </summary>
    [AddComponentMenu("Cwcbb/StateLayer/Demo/Test UI State Observer")]
    public class TestUIStateObserver : StateObserverComponent<DemoUIStatePayload>
    {
        // 继承自 StateObserverComponent<DemoUIStatePayload> 即可！
        // 内部自动持有纯 C# StateObserver<DemoUIStatePayload> 核心实例。
        // Inspector 中规则不再包含单独的 UnityEvent，整个组件只需统一监听或使用 CoreObserver.OnMatched。
    }
}
