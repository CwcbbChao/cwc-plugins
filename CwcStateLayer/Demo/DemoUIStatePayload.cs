using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer.Demo
{
    /// <summary>
    /// 测试用的自定义 UI 数据载荷配置类
    /// </summary>
    [Serializable]
    public class DemoUIStatePayload
    {
        [Tooltip("界面标题名称")]
        public string pageTitle = "主界面";

        [Tooltip("界面背景颜色")]
        public Color backgroundColor = Color.blue;
    }
}
