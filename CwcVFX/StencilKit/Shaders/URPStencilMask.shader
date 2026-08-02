Shader "Cwcbb/Stencil/StencilMask"
{
    Properties
    {
        [Header(Stencil Settings)]
        _StencilRef ("Stencil Ref", Int) = 128
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp", Int) = 8 // Always
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil Pass", Int) = 2 // Replace
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilFail ("Stencil Fail", Int) = 0 // Keep
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilZFail ("Stencil ZFail", Int) = 0 // Keep
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque" 
            "Queue" = "Geometry-1" // 比普通几何体稍早渲染，以便为其写入模板缓冲
        }

        Pass
        {
            Name "StencilMaskPass"
            
            // 核心：不写入任何颜色通道，仅保留深度测试与写入，写入模板缓存
            ColorMask 0
            ZWrite On
            Cull Back

            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                // 转换到裁剪空间
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 片元着色器不输出颜色（已被 ColorMask 0 截断），仅返回哑值
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
