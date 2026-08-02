using UnityEngine;

namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// 通用 UI 面板组件。用于纯显示、无需编写专属逻辑的界面（如静态背景图、单纯的遮罩等）。
    /// 可以直接挂载在 GameObject 上，无需再为其新建专用的子类脚本。
    /// </summary>
    [AddComponentMenu("CwcUIFramework/CwcUI Common Element")]
    public class CwcUICommonElement : CwcUIElement
    {
        // 继承基类所有的生命周期、扩展组件收集以及动效控制功能，无需重写任何逻辑。
    }
}
