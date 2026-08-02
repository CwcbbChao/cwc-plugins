using System;
using System.Collections;
using UnityEngine;

namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// 基于 Update 计时的 CanvasGroup.alpha 渐变过渡动画组件案例
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CwcUIFadeTransition : CwcUITransitionComponent
    {
        #region 序列化属性与字段

        [Header("渐变持续时间(秒)")]
        [SerializeField] private float duration = 0.25f;

        [Header("起始透明度")]
        [SerializeField] private float startAlpha = 0f;

        [Header("目标透明度")]
        [SerializeField] private float targetAlpha = 1f;

        [Header("时间更新模式")]
        [SerializeField]
        [Tooltip("是否使用不受时间缩放影响的 unscaledDeltaTime 计时。UI 过渡建议开启。")]
        private bool useUnscaledTime = true;

        #endregion

        #region 非序列化私有字段

        private CanvasGroup _canvasGroup;
        private float _elapsed;
        private bool _isPlaying;

        #endregion

        #region 生命周期方法

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                Debug.LogError($"[CwcUIFramework] '{gameObject.name}' 上未找到 CanvasGroup，Fade 过渡组件无法工作！");
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 开启渐变过渡
        /// </summary>
        public override void Play(Action onComplete)
        {
            base.Play(onComplete);

            _elapsed = 0f;
            _isPlaying = true;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = startAlpha;
            }

            if (!gameObject.activeInHierarchy)
            {
                // 如果物体未激活（极少见情况），直接对齐状态并完成
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = targetAlpha;
                }
                _isPlaying = false;
                var callback = currentCallback;
                currentCallback = null;
                callback?.Invoke();
            }
        }

        /// <summary>
        /// 打断并停止渐变过渡，清空回调并立刻对齐到目标透明度
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            _isPlaying = false;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = targetAlpha;
            }
        }

        #endregion

        #region 私有方法

        private void Update()
        {
            if (!_isPlaying) return;

            if (_canvasGroup == null)
            {
                _isPlaying = false;
                return;
            }

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _elapsed += deltaTime;

            float t = Mathf.Clamp01(_elapsed / duration);
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            if (_elapsed >= duration)
            {
                _canvasGroup.alpha = targetAlpha;
                _isPlaying = false;

                var callback = currentCallback;
                currentCallback = null;
                callback?.Invoke();
            }
        }

        #endregion
    }
}
