using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util; // 导入新版 RenderGraph 辅助工具命名空间
using UnityEngine.Rendering.Universal;

namespace Cwcbb.Tools
{
    /// <summary>
    /// 自定义渲染特性。
    /// 用于在 URP 渲染管线的指定节点（AfterRenderingTransparents）精准插队，
    /// 克隆当前相机的颜色缓冲区并输出为全局 Shader 贴图（兼容 Unity 6 Render Graph）。
    /// </summary>
    public class CwcBackgroundGrabFeature : ScriptableRendererFeature
    {
        #region 内部类 (Inner Class)

        /// <summary>
        /// 自定义渲染 Pass，负责物理拷贝和全局映射
        /// </summary>
        private class CwcBackgroundGrabPass : ScriptableRenderPass
        {
            #region 非序列化私有字段

            /// <summary>
            /// 抓取的背景纹理句柄
            /// </summary>
            private RTHandle _grabTextureHandle;

            #endregion

            #region 构造函数

            public CwcBackgroundGrabPass()
            {
                // 时机设置：在半透明渲染完成后插队抓取
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            }

            #endregion

            #region 公共方法

            /// <summary>
            /// 新版 Render Graph 录制入口
            /// </summary>
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                // 1. 获取全局注册 of 背景相机指针，如果未注册则直接返回
                Camera registeredCamera = CwcBackgroundCameraMarker.RegisteredCamera;
                if (registeredCamera == null)
                {
                    return;
                }

                // 2. 获取当前相机的渲染数据
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                // 3. 指针比对：如果不是注册的背景相机，直接放行
                if (cameraData.camera != registeredCamera)
                {
                    return;
                }

                // 4. 获取相机的活动颜色渲染目标
                TextureHandle cameraColor = resourceData.activeColorTexture;
                if (!cameraColor.IsValid())
                {
                    return;
                }

                // 5. 安全内存复用：使用 ReAllocateHandleIfNeeded 动态分配匹配当前相机的 RTHandle
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0; // 克隆颜色图无需深度
                desc.msaaSamples = 1;     // 拷贝的全局纹理不需要开启多重采样抗锯齿

                RenderingUtils.ReAllocateHandleIfNeeded(
                    ref _grabTextureHandle, 
                    desc, 
                    FilterMode.Bilinear, 
                    TextureWrapMode.Clamp, 
                    name: "_CwcGrabBackgroundTex"
                );

                if (_grabTextureHandle == null)
                {
                    return;
                }

                // 6. 将外部持久化的 RTHandle 导入到 Render Graph 系统中
                TextureHandle dstTexture = renderGraph.ImportTexture(_grabTextureHandle);

                // 7. 使用 Render Graph 极其高效的内置 CopyPass 进行屏幕物理拷贝
                renderGraph.AddCopyPass(cameraColor, dstTexture);

                // 8. 将更新后的纹理注册到全局 Shader 中，供后续相机中的物体材质使用
                Shader.SetGlobalTexture("_CwcGrabBackgroundTex", _grabTextureHandle);
            }

            /// <summary>
            /// 销毁并释放 RTHandle 显存
            /// </summary>
            public void Cleanup()
            {
                if (_grabTextureHandle != null)
                {
                    _grabTextureHandle.Release();
                    _grabTextureHandle = null;
                }
            }

            #endregion
        }

        #endregion

        #region 非序列化私有字段

        /// <summary>
        /// 缓存的自定义渲染 Pass 实例
        /// </summary>
        private CwcBackgroundGrabPass _grabPass;

        #endregion

        #region 生命周期方法 (Unity Lifecycle)

        /// <summary>
        /// 创建并初始化 Pass
        /// </summary>
        public override void Create()
        {
            _grabPass = new CwcBackgroundGrabPass();
        }

        /// <summary>
        /// 添加 Pass 到渲染队列中
        /// </summary>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // 如果全局相机标记未就绪，直接拦截，零额外开销
            if (CwcBackgroundCameraMarker.RegisteredCamera == null)
            {
                return;
            }

            // 由于在 RecordRenderGraph 中也会过滤相机，这里同样做一次快速的 CPU 侧过滤以提升效率
            if (renderingData.cameraData.camera == CwcBackgroundCameraMarker.RegisteredCamera)
            {
                renderer.EnqueuePass(_grabPass);
            }
        }

        /// <summary>
        /// 特性销毁时清理资源，防止内存泄漏
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _grabPass != null)
            {
                _grabPass.Cleanup();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
