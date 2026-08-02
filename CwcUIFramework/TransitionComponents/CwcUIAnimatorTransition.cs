using System;
using System.Collections;
using UnityEngine;

namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// 基于 Animator 状态机的过渡动画组件案例
    /// </summary>
    public class CwcUIAnimatorTransition : CwcUITransitionComponent
    {
        #region 序列化属性与字段

        [Header("Animator 组件")]
        [SerializeField] private Animator animator;

        [Header("动画状态名称(在 Animator 中配置的状态)")]
        [SerializeField] private string stateName;

        [Header("时间更新模式")]
        [SerializeField]
        [Tooltip("是否使用不受时间缩放影响的 unscaledDeltaTime 计时。UI 过渡建议开启。")]
        private bool useUnscaledTime = true;

        #endregion

        #region 非序列化私有字段

        private int _stateHash;
        private float _elapsed;
        private float _waitTime;
        private bool _isPlaying;

        #endregion

        #region 生命周期方法

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                Debug.LogError($"[CwcUIFramework] '{gameObject.name}' 上未找到 Animator 组件，Animator 过渡组件失效！");
                return;
            }

            if (!string.IsNullOrEmpty(stateName))
            {
                // 缓存字符串为 Hash 键，优化性能，防止每帧分配
                _stateHash = Animator.StringToHash(stateName);
            }
            else
            {
                Debug.LogError($"[CwcUIFramework] '{gameObject.name}' 的 Animator 过渡组件 stateName 为空，请配置动画状态名称！");
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 播放过渡动画
        /// </summary>
        public override void Play(Action onComplete)
        {
            base.Play(onComplete);

            if (animator == null || _stateHash == 0)
            {
                // 若配置不完备，直接触发完成
                var callback = currentCallback;
                currentCallback = null;
                callback?.Invoke();
                return;
            }

            if (gameObject.activeInHierarchy)
            {
                // 播放特定动画状态，并从起始处播放
                animator.Play(_stateHash, 0, 0f);
                // 强刷一帧以立即应用状态改变，确保下面能读取到正确的 stateInfo.length
                animator.Update(0f);

                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                _waitTime = stateInfo.length;

                // 倘若当前状态不是我们播的，采用默认保底时间 (例如 0.5s)
                if (stateInfo.shortNameHash != _stateHash)
                {
                    _waitTime = 0.5f;
                }

                _elapsed = 0f;
                _isPlaying = true;
            }
            else
            {
                // 假如物体未处于激活状态，直接对齐状态
                animator.Play(_stateHash, 0, 1f);
                animator.Update(0f);
                
                _isPlaying = false;
                var callback = currentCallback;
                currentCallback = null;
                callback?.Invoke();
            }
        }

        /// <summary>
        /// 强制终止动画，清空回调，并将动画状态立即同步对齐至最后一帧
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            _isPlaying = false;

            if (animator != null && _stateHash != 0)
            {
                // 强制跳到目标状态的结束点并刷渲染
                animator.Play(_stateHash, 0, 1f);
                animator.Update(0f);
            }
        }

        #endregion

        #region 私有方法

        private void Update()
        {
            if (!_isPlaying) return;

            if (animator == null)
            {
                _isPlaying = false;
                return;
            }

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _elapsed += deltaTime;

            if (_elapsed >= _waitTime)
            {
                _isPlaying = false;

                var callback = currentCallback;
                currentCallback = null;
                callback?.Invoke();
            }
        }

        #endregion
    }
}
