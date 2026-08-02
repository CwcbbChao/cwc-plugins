using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cwcbb.Tools
{
    /// <summary>
    /// 【可选】3D 渲染器模板缓冲控制器，用于在运行时动态调整渲染器材质的 Stencil 写入参数
    /// </summary>
    [AddComponentMenu("Cwc/VFX/CwcRendererStencilController")]
    [RequireComponent(typeof(Renderer))]
    public class CwcRendererStencilController : MonoBehaviour
    {
        #region 序列化属性与字段

        [Header("Stencil 动态配置")]
        [Tooltip("模板缓冲参考值 (0-255)")]
        [Range(0, 255)]
        [SerializeField] private int _stencilRef = 128;

        [Tooltip("模板测试比较函数（默认写入为 Always）")]
        [SerializeField] private CompareFunction _compareFunction = CompareFunction.Always;

        [Tooltip("模板测试通过时的操作（默认写入为 Replace）")]
        [SerializeField] private StencilOp _passOperation = StencilOp.Replace;

        [Tooltip("模板测试失败时的操作")]
        [SerializeField] private StencilOp _failOperation = StencilOp.Keep;

        [Tooltip("深度测试失败时的操作")]
        [SerializeField] private StencilOp _zFailOperation = StencilOp.Keep;

        #endregion

        #region 非序列化私有字段

        private Renderer _renderer;
        private List<Material> _instancedMaterials = new List<Material>();

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
                    ApplyStencilSettings();
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
                    ApplyStencilSettings();
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
                    ApplyStencilSettings();
                }
            }
        }

        #endregion

        #region 生命周期方法 (Unity Lifecycle)

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer == null)
            {
                Debug.LogError("[CwcRendererStencilController] 未能在同一物体上找到 Renderer 组件！", this);
            }
        }

        private void Start()
        {
            InitializeAndApply();
        }

        private void OnDestroy()
        {
            CleanupMaterials();
        }

        private void OnValidate()
        {
            // 在编辑器非运行状态下如果需要预览，可直接通过 sharedMaterials 修改
            // 但为避免材质资源被永久修改，建议仅在运行时或者通过材质面板直接调整
            if (Application.isPlaying)
            {
                ApplyStencilSettings();
            }
        }

        #endregion

        #region 私有方法 (Private Methods)

        /// <summary>
        /// 初始化并应用模板缓冲设置，克隆材质实例以防污染资源文件
        /// </summary>
        private void InitializeAndApply()
        {
            if (_renderer == null) return;

            // 获取材质数组会触发实例化克隆
            Material[] mats = _renderer.materials;
            _instancedMaterials.Clear();
            _instancedMaterials.AddRange(mats);

            ApplyStencilSettings();
        }

        /// <summary>
        /// 将 Stencil 属性应用到所有已实例化的材质上
        /// </summary>
        private void ApplyStencilSettings()
        {
            if (_instancedMaterials == null || _instancedMaterials.Count == 0)
            {
                if (_renderer != null && Application.isPlaying)
                {
                    // 如果尚未初始化，则在运行时触发初始化
                    InitializeAndApply();
                    return;
                }
                return;
            }

            foreach (var mat in _instancedMaterials)
            {
                if (mat == null) continue;

                // 仅在 Shader 包含对应属性时才进行设置，提高兼容性
                if (mat.HasProperty("_StencilRef"))
                {
                    mat.SetInt("_StencilRef", _stencilRef);
                }
                if (mat.HasProperty("_StencilComp"))
                {
                    mat.SetInt("_StencilComp", (int)_compareFunction);
                }
                if (mat.HasProperty("_StencilPass"))
                {
                    mat.SetInt("_StencilPass", (int)_passOperation);
                }
                if (mat.HasProperty("_StencilFail"))
                {
                    mat.SetInt("_StencilFail", (int)_failOperation);
                }
                if (mat.HasProperty("_StencilZFail"))
                {
                    mat.SetInt("_StencilZFail", (int)_zFailOperation);
                }
            }
        }

        /// <summary>
        /// 清理并在销毁时主动 Destroy 克隆出的材质实例，防止内存泄漏
        /// </summary>
        private void CleanupMaterials()
        {
            if (_instancedMaterials != null && _instancedMaterials.Count > 0)
            {
                foreach (var mat in _instancedMaterials)
                {
                    if (mat != null)
                    {
                        Destroy(mat);
                    }
                }
                _instancedMaterials.Clear();
            }
        }

        #endregion
    }
}
