using System;
using UnityEngine;
using UnityEngine.UI;

namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// UI 适配缩放配置，用于统一或独立配置 CanvasScaler 的缩放策略
    /// </summary>
    [Serializable]
    public class CanvasScalerConfig
    {
        [Header("参考分辨率")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);

        [Header("屏幕缩放匹配模式")]
        [SerializeField] private CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        [Header("宽高度适配权重(0: 宽, 1: 高)")]
        [Range(0f, 1f)]
        [SerializeField] private float matchWidthOrHeight = 0.5f;

        /// <summary>
        /// 获取参考分辨率
        /// </summary>
        public Vector2 ReferenceResolution => referenceResolution;

        /// <summary>
        /// 获取屏幕缩放匹配模式
        /// </summary>
        public CanvasScaler.ScreenMatchMode ScreenMatchMode => screenMatchMode;

        /// <summary>
        /// 获取宽高度适配权重
        /// </summary>
        public float MatchWidthOrHeight => matchWidthOrHeight;

        /// <summary>
        /// 应用当前缩放配置至目标 CanvasScaler 组件
        /// </summary>
        /// <param name="scaler">CanvasScaler 组件实例</param>
        public void ApplyTo(CanvasScaler scaler)
        {
            if (scaler == null) return;
            
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = screenMatchMode;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
        }
    }
}
