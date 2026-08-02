using UnityEngine;
using UnityEngine.UI;

namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// 用于将预制件内部或根节点的 Canvas 动态绑定到指定 UI 层级的相机与渲染设置上
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    [AddComponentMenu("CwcUIFramework/CwcUI Layer Canvas Binder")]
    public class CwcUILayerCanvasBinder : MonoBehaviour
    {
        #region 序列化属性与字段

        [Header("目标 UI 层级配置")]
        [SerializeField] private CwcUILayerSO targetLayer;

        #endregion

        #region 非序列化私有字段

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private GraphicRaycaster _raycaster;

        #endregion

        #region 生命周期方法

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }

            _scaler = GetComponent<CanvasScaler>();
            if (_scaler == null)
            {
                _scaler = gameObject.AddComponent<CanvasScaler>();
            }

            _raycaster = GetComponent<GraphicRaycaster>();
            if (_raycaster == null)
            {
                _raycaster = gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void Start()
        {
            Bind();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 执行绑定逻辑，自动向上寻找 UIElement 对齐状态并通过其注入的 UIFrame 应用层级配置
        /// </summary>
        public void Bind()
        {
            if (targetLayer == null)
            {
                Debug.LogWarning($"[CwcUIFramework] GameObject '{gameObject.name}' 上的 CwcUILayerCanvasBinder 未配置 targetLayer！", this);
                return;
            }

            var parentElement = GetComponentInParent<CwcUIElement>();
            if (parentElement != null)
            {
                // 1. 对齐物理显隐状态到父级 UIElement 权威显隐值
                SetCanvasEnabled(parentElement.IsVisible);

                // 2. 通过父级已注入的 UIFrame 引用直达 Frame，应用层级相机配置
                var uiFrame = parentElement.UIFrame;
                if (uiFrame != null)
                {
                    uiFrame.ApplyLayerSettings(_canvas, targetLayer, _scaler);
                }
                else
                {
                    Debug.LogWarning($"[CwcUIFramework] Binder '{gameObject.name}' 找到了父级 UIElement '{parentElement.gameObject.name}'，但该 Element 尚未被 UIFrame 注入管理（UIFrame 为 Null），无法自动应用层级配置！", this);
                }
            }
            else
            {
                Debug.LogWarning($"[CwcUIFramework] GameObject '{gameObject.name}' 在父级中未找到 CwcUIElement 组件，无法应用任何状态与配置！", this);
            }
        }

        /// <summary>
        /// 设置当前绑定的 Canvas 渲染使能状态
        /// </summary>
        /// <param name="isEnabled">是否启用渲染</param>
        public void SetCanvasEnabled(bool isEnabled)
        {
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
            }

            if (_canvas != null)
            {
                _canvas.enabled = isEnabled;
            }
        }

        #endregion
    }
}
