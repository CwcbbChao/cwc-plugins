namespace Cwcbb.Tools
{
    using UnityEngine;

    /// <summary>
    /// 挂载在主控端元素上（如 UI 面板、触发物体），扮演操控遥控对象的“遥控器”。
    /// </summary>
    [DisallowMultipleComponent]
    public class CwcRemoteController : MonoBehaviour
    {
        #region 序列化字段
        [Header("遥控配置")]
        [Tooltip("该遥控器控制的遥控对象配置资产")]
        [SerializeField] private CwcRemoteControlObjectConfig _objectConfig;
        #endregion

        #region 私有字段
        private CwcRemoteControlObject _objectInstance;
        #endregion

        #region 公共属性
        public CwcRemoteControlObject ObjectInstance => _objectInstance;
        #endregion

        #region 生命周期
        private void OnDestroy()
        {
            RecycleObject();
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 显式请求加载并获取该遥控器的操控对象
        /// </summary>
        public void RequestObject()
        {
            if (_objectInstance != null) return;
            if (_objectConfig == null)
            {
                Debug.LogWarning($"[{nameof(CwcRemoteController)}] 绑定的 ObjectConfig 为空，请求对象失败！", this);
                return;
            }

            _objectInstance = CwcRemoteControlManager.Instance.RequestObject(_objectConfig);
            if (_objectInstance != null)
            {
                ApplyInitialTransform();
            }
            
            if (CwcRemoteControlManager.Instance != null)
            {
                CwcRemoteControlManager.Instance.UpdateCameraState();
            }
        }

        /// <summary>
        /// 控制遥控对象的显示与隐藏。
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_objectInstance == null && visible)
            {
                RequestObject();
            }

            if (_objectInstance != null)
            {
                _objectInstance.SetVisible(visible);
                if (visible)
                {
                    ApplyInitialTransform();
                }
            }

            if (CwcRemoteControlManager.Instance != null)
            {
                CwcRemoteControlManager.Instance.UpdateCameraState();
            }
        }

        /// <summary>
        /// 向关联的遥控对象发送控制信号动作
        /// </summary>
        public void SendSignal(int signalId)
        {
            if (_objectInstance == null)
            {
                // 若实例尚未加载，则尝试自动加载
                RequestObject();
            }

            if (_objectInstance != null)
            {
                _objectInstance.SendSignal(signalId);
            }
            else
            {
                Debug.LogWarning($"[{nameof(CwcRemoteController)}] 发送信号 {signalId} 失败！受控对象实例加载失败，请检查是否在 Inspector 中正确关联了 ObjectConfig。", this);
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 初始化应用遥控对象的空间变换
        /// </summary>
        private void ApplyInitialTransform()
        {
            if (_objectInstance == null || _objectConfig == null) return;

            // 应用配置中的局部偏移、旋转与缩放
            _objectInstance.transform.localPosition = _objectConfig.DefaultPositionOffset;
            _objectInstance.transform.localRotation = Quaternion.Euler(_objectConfig.DefaultRotation);
            _objectInstance.transform.localScale = _objectConfig.DefaultScale;
        }

        /// <summary>
        /// 回收遥控对象，归还到池中，保底防泄漏
        /// </summary>
        private void RecycleObject()
        {
            if (_objectInstance != null)
            {
                if (CwcRemoteControlManager.Instance != null)
                {
                    CwcRemoteControlManager.Instance.RecycleObject(_objectInstance);
                }
                _objectInstance = null;
            }
        }
        #endregion
    }
}
