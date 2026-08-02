using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Cwcbb.Tools
{
    /// <summary>
    /// UI 模板缓冲修改器，允许在 UI 元素上动态添加 Stencil 状态，且不破坏原本材质（如 UIEffect 材质）
    /// </summary>
    [AddComponentMenu("Cwc/UI/CwcUIStencilModifier")]
    [RequireComponent(typeof(Graphic))]
    [ExecuteAlways]
    public class CwcUIStencilModifier : MonoBehaviour, IMaterialModifier
    {
        #region 序列化属性与字段

        [Header("Stencil 核心配置")]
        [Tooltip("模板缓冲参考值 (0-255)")]
        [Range(0, 255)]
        [SerializeField] private int _stencilRef = 128;

        [Tooltip("模板测试比较函数")]
        [SerializeField] private CompareFunction _compareFunction = CompareFunction.Equal;

        [Tooltip("模板测试通过时的操作")]
        [SerializeField] private StencilOp _passOperation = StencilOp.Keep;

        [Header("遮罩与高级参数")]
        [Tooltip("读取掩码 (0-255)")]
        [Range(0, 255)]
        [SerializeField] private int _readMask = 255;

        [Tooltip("写入掩码 (0-255)")]
        [Range(0, 255)]
        [SerializeField] private int _writeMask = 255;

        [Tooltip("颜色写入通道")]
        [SerializeField] private ColorWriteMask _colorWriteMask = ColorWriteMask.All;

        #endregion

        #region 非序列化私有字段

        private Graphic _graphic;
        private Material _customMaterial;

        #endregion

        #region 公共属性 (Properties)

        /// <summary>
        /// 模板缓冲参考值
        /// </summary>
        public int StencilRef
        {
            get => _stencilRef;
            set
            {
                if (_stencilRef != value)
                {
                    _stencilRef = Mathf.Clamp(value, 0, 255);
                    NotifyMaterialDirty();
                }
            }
        }

        /// <summary>
        /// 模板测试比较函数
        /// </summary>
        public CompareFunction CompareFunc
        {
            get => _compareFunction;
            set
            {
                if (_compareFunction != value)
                {
                    _compareFunction = value;
                    NotifyMaterialDirty();
                }
            }
        }

        /// <summary>
        /// 模板测试通过时的操作
        /// </summary>
        public StencilOp PassOp
        {
            get => _passOperation;
            set
            {
                if (_passOperation != value)
                {
                    _passOperation = value;
                    NotifyMaterialDirty();
                }
            }
        }

        #endregion

        #region 生命周期方法 (Unity Lifecycle)

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            if (_graphic == null)
            {
                Debug.LogError("[CwcUIStencilModifier] 未能在同一物体上找到 Graphic 组件！", this);
            }
        }

        private void OnEnable()
        {
            NotifyMaterialDirty();
        }

        private void OnDisable()
        {
            CleanupCustomMaterial();
            NotifyMaterialDirty();
        }

        private void OnDestroy()
        {
            CleanupCustomMaterial();
        }

        private void OnValidate()
        {
            NotifyMaterialDirty();
        }

        #endregion

        #region 公共方法 (Public Methods)

        /// <summary>
        /// 修改并返回带有模板参数的新材质
        /// </summary>
        public Material GetModifiedMaterial(Material baseMaterial)
        {
            // 如果未激活或缺少 Graphic，直接返回基底材质
            if (!isActiveAndEnabled || _graphic == null)
            {
                return baseMaterial;
            }

            // 清理上一次生成的自定义材质引用，防止内存泄漏
            if (_customMaterial != null)
            {
                StencilMaterial.Remove(_customMaterial);
                _customMaterial = null;
            }

            // 使用 Unity 的 StencilMaterial.Add 获取/复用共享的模板材质
            _customMaterial = StencilMaterial.Add(
                baseMaterial,
                _stencilRef,
                _passOperation,
                _compareFunction,
                _colorWriteMask,
                _readMask,
                _writeMask
            );

            return _customMaterial;
        }

        #endregion

        #region 私有方法 (Private Methods)

        /// <summary>
        /// 通知 Graphic 重新构建材质
        /// </summary>
        private void NotifyMaterialDirty()
        {
            if (_graphic == null)
            {
                _graphic = GetComponent<Graphic>();
            }

            if (_graphic != null)
            {
                _graphic.SetMaterialDirty();
            }
        }

        /// <summary>
        /// 释放自定义材质池引用
        /// </summary>
        private void CleanupCustomMaterial()
        {
            if (_customMaterial != null)
            {
                StencilMaterial.Remove(_customMaterial);
                _customMaterial = null;
            }
        }

        #endregion
    }
}
