using UnityEngine;

namespace Cwcbb.Tools
{
    /// <summary>
    /// 背景相机标记组件。
    /// 挂载在负责渲染背景 UI 的专用相机上（无论是 Base 相机还是特定的 Overlay 相机），
    /// 用于向全局登记自身相机指针，避免在渲染循环中进行耗时的字符串或 Tag 查找。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Cwc/VFX/CwcBackgroundCameraMarker")]
    public class CwcBackgroundCameraMarker : MonoBehaviour
    {
        #region 静态属性与字段

        /// <summary>
        /// 全局注册的背景相机指针
        /// </summary>
        public static Camera RegisteredCamera { get; private set; }

        #endregion

        #region 非序列化私有字段

        /// <summary>
        /// 缓存的相机组件
        /// </summary>
        private Camera _camera;

        #endregion

        #region 生命周期方法 (Unity Lifecycle)

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                Debug.LogError("[CwcBackgroundCameraMarker] 未能在同一物体上找到 Camera 组件！", this);
            }
        }

        private void OnEnable()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            if (_camera != null)
            {
                RegisteredCamera = _camera;
            }
        }

        private void OnDisable()
        {
            if (RegisteredCamera == _camera)
            {
                RegisteredCamera = null;
            }
        }

        #endregion
    }
}
