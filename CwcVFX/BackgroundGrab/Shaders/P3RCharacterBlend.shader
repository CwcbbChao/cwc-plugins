Shader "Cwcbb/P3RCharacterBlend"
{
    Properties
    {
        // --------------------------------------------------
        // 1. 基础卡通渲染设置 (Base Cel Shading)
        // --------------------------------------------------
        [Header(1. Base Cel Shading)]
        [Space(8)]
        _MainTex ("Base Map (Albedo)", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [Toggle(_USE_UNLIT)] _UseUnlit ("Use Unlit (Ignore Lights)", Float) = 0.0

        [Space(15)]
        [Header(3D Shadow Settings)]
        [Space(8)]
        [HDR] _ShadowColor ("3D Shadow Color (RGB Multiply, A Alpha)", Color) = (0.5, 0.5, 0.65, 1)
        _ShadowThreshold ("3D Shadow Threshold", Range(-1.0, 1.0)) = 0.0
        _ShadowFeather ("3D Shadow Feather", Range(0.001, 0.5)) = 0.05

        [Space(15)]
        [Header(Normal Map)]
        [Space(8)]
        [Toggle(_USE_NORMAL_MAP)] _UseNormalMap ("Use Normal Map", Float) = 0.0
        [NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0.0, 2.0)) = 1.0

        [Space(15)]
        [Header(Emission)]
        [Space(8)]
        [Toggle(_USE_EMISSION)] _UseEmission ("Use Emission", Float) = 0.0
        [HDR] _EmissionColor ("Emission Color", Color) = (0.0, 0.0, 0.0, 1.0)
        _EmissionMap ("Emission Map", 2D) = "white" {}

        // --------------------------------------------------
        // 2. 高对比度漫画设置 (High Contrast Comic Mode)
        // --------------------------------------------------
        [Space(25)]
        [Header(2. High Contrast Comic Mode)]
        [Space(8)]
        [Toggle(_USE_HIGH_CONTRAST)] _UseHighContrast ("Enable High Contrast", Float) = 0.0
        [HDR] _ComicBrightColor ("Comic Bright Color (White)", Color) = (1, 1, 1, 1)
        [HDR] _ComicShadowColor ("Comic Shadow Color (Blue)", Color) = (0.0, 0.0, 0.4, 1)
        _LuminanceThreshold ("Luminance Threshold", Range(0.0, 1.0)) = 0.3
        _LuminanceFeather ("Luminance Feather", Range(0.001, 0.5)) = 0.02
        
        [Space(15)]
        [Header(Comic Face Line Protection)]
        [Space(8)]
        [HDR] _ComicLineColor ("Comic Line Color (Black/Dark)", Color) = (0.0, 0.0, 0.15, 1)
        _LineThreshold ("Detail Line Threshold", Range(0.0, 1.0)) = 0.2

        // --------------------------------------------------
        // 3. 屏幕背景抓取与混合 (Screen Background Blend)
        // --------------------------------------------------
        [Space(25)]
        [Header(3. Screen Background Blend)]
        [Space(8)]
        _BlendWeight ("Global Blend Weight", Range(0.0, 1.0)) = 0.5
        _MaskTex ("Blend Mask Tex (R * A)", 2D) = "black" {}
        [Toggle(_DARK_AS_BLEND_MASK)] _DarkAsBlendMask ("Dark Area As Blend Mask", Float) = 0.0

        // --------------------------------------------------
        // 4. 轮廓描边设置 (Outline Settings)
        // --------------------------------------------------
        [Space(25)]
        [Header(4. Outline Settings)]
        [Space(8)]
        [Toggle(_USE_OUTLINE)] _UseOutline ("Enable Outline", Float) = 0.0
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.01
        [HDR] _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        [Toggle(_OUTLINE_USE_BG_BLEND)] _OutlineUseBgBlend ("Outline Use BgBlend", Float) = 0.0
        _OutlineBlendWeight ("Outline Blend Weight", Range(0.0, 1.0)) = 1.0

        // --------------------------------------------------
        // 5. 渲染状态配置 (Render States)
        // --------------------------------------------------
        [Space(25)]
        [Header(5. Render States)]
        [Space(8)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2.0 // Default is Back
        [Toggle(_USE_CLIP)] _UseClip ("Use Alpha Clip", Float) = 0.0
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1.0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0.0
        [Enum(UnityEngine.Rendering.Toggle)] _ZWrite ("ZWrite", Float) = 1.0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "Queue" = "Geometry+10"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // 公共纹理与采样器声明，两个 Pass 共享导入
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        TEXTURE2D(_CwcGrabBackgroundTex);
        SAMPLER(sampler_CwcGrabBackgroundTex);

        TEXTURE2D(_BumpMap);
        SAMPLER(sampler_BumpMap);

        TEXTURE2D(_EmissionMap);
        SAMPLER(sampler_EmissionMap);

        TEXTURE2D(_MaskTex);
        SAMPLER(sampler_MaskTex);

        // 公共常量缓冲区声明，保证 SRP 批处理兼容
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _BaseColor;
            half4 _ShadowColor;
            half _ShadowThreshold;
            half _ShadowFeather;
            half _UseUnlit;
            half _BlendWeight;

            half _Cull;

            half _UseClip;
            half _Cutoff;

            half _UseNormalMap;
            half _BumpScale;

            half _UseEmission;
            half4 _EmissionColor;

            half _SrcBlend;
            half _DstBlend;
            half _ZWrite;
            half _ZTest;

            // 新增属性
            half _UseHighContrast;
            half4 _ComicBrightColor;
            half4 _ComicShadowColor;
            half4 _ComicLineColor;
            half _LuminanceThreshold;
            half _LuminanceFeather;
            half _LineThreshold;

            float4 _MaskTex_ST;
            half _DarkAsBlendMask;

            half _UseOutline;
            half _OutlineWidth;
            half4 _OutlineColor;
            half _OutlineUseBgBlend;
            half _OutlineBlendWeight;
        CBUFFER_END
        ENDHLSL

        // Pass 1: Outline
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual
            Offset 1, 1 // 深度偏移，防止外挤出描边与正面网格产生 Z-fighting
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #pragma shader_feature_local _USE_CLIP
            #pragma shader_feature_local _USE_OUTLINE
            #pragma shader_feature_local _OUTLINE_USE_BG_BLEND

            struct AttributesOutline
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct VaryingsOutline
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 screenPos    : TEXCOORD1;
                float3 positionWS   : TEXCOORD3;
                float3 normalWS     : TEXCOORD4;
            };

            VaryingsOutline vertOutline(AttributesOutline input)
            {
                VaryingsOutline output = (VaryingsOutline)0;
                
                // 性能优化：若未启用描边，在顶点阶段直接剔除，避免产生任何像素片元，保护 Early-Z
                #if !defined(_USE_OUTLINE)
                output.positionCS = float4(0, 0, 0, 0);
                return output;
                #endif

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // 沿法线方向挤出描边
                positionWS += normalWS * _OutlineWidth;
                
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                return output;
            }

            half4 fragOutline(VaryingsOutline input) : SV_Target
            {
                #if defined(_USE_CLIP)
                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(baseTex.a - _Cutoff);
                #endif

                half4 finalColor = _OutlineColor;

                #if defined(_OUTLINE_USE_BG_BLEND)
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                half4 bgColor = SAMPLE_TEXTURE2D(_CwcGrabBackgroundTex, sampler_CwcGrabBackgroundTex, screenUV);
                finalColor = lerp(_OutlineColor, bgColor, _OutlineBlendWeight);
                #endif

                return finalColor;
            }
            ENDHLSL
        }

        // Pass 2: ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest [_ZTest]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _USE_UNLIT
            #pragma shader_feature_local _USE_CLIP
            #pragma shader_feature_local _USE_NORMAL_MAP
            #pragma shader_feature_local _USE_EMISSION
            #pragma shader_feature_local _USE_HIGH_CONTRAST
            #pragma shader_feature_local _DARK_AS_BLEND_MASK
            
            // 多编译指令：用于支持 URP 附加光源数据（点光源、聚光灯）填充
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 screenPos    : TEXCOORD1;
                float3 positionWS   : TEXCOORD3;
                float3 normalWS     : TEXCOORD4;
                float4 tangentWS    : TANGENT;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
                
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. 采样角色固有色贴图并加上基础颜色
                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor;

                // 2. 提前进行 Alpha 裁剪（正面和背面都适用，使得镂空效果在背部也有效）
                #if defined(_USE_CLIP)
                clip(baseTex.a - _Cutoff);
                #endif

                // 计算图像明度（Luminance）与基于亮度的二值化插值系数（用于高对比度漫画色和暗部遮罩）
                half brightness = dot(baseTex.rgb, half3(0.299, 0.587, 0.114));
                half t = smoothstep(_LuminanceThreshold - _LuminanceFeather, _LuminanceThreshold + _LuminanceFeather, brightness);

                // 3. 计算角色本色（Cel-shading / Unlit）
                half4 characterColor;
                half totalLightIntensity = 1.0; // 3D 光照强度因子 (1为亮，0为阴影)
                
                #if defined(_USE_UNLIT)
                characterColor = baseTex;
                #else
                // 计算法线贴图（切线空间转世界空间）
                #if defined(_USE_NORMAL_MAP)
                half3 tangent = normalize(input.tangentWS.xyz);
                half3 normal = normalize(input.normalWS);
                half3 bitangent = cross(normal, tangent) * input.tangentWS.w;
                half3x3 tangentToWorld = half3x3(tangent, bitangent, normal);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv));
                normalTS.xy *= _BumpScale;
                half3 finalNormal = normalize(mul(normalTS, tangentToWorld));
                #else
                half3 finalNormal = normalize(input.normalWS);
                #endif

                // 计算主光源 Cel-shading 光照
                Light mainLight = GetMainLight();
                half ndotl = dot(finalNormal, mainLight.direction);
                half celIntensity = smoothstep(_ShadowThreshold - _ShadowFeather, _ShadowThreshold + _ShadowFeather, ndotl);
                
                // 优化：计算主平行光的实际有效亮度
                half mainLightBrightness = max(mainLight.color.r, max(mainLight.color.g, mainLight.color.b));
                celIntensity *= mainLightBrightness;

                half3 litColor = baseTex.rgb * lerp(_ShadowColor.rgb, half3(1.0, 1.0, 1.0), celIntensity) * mainLight.color;

                // 提取主光源光照强度
                totalLightIntensity = celIntensity * mainLight.shadowAttenuation;

                // 计算附加光源（点光源、聚光灯）的 Cel-shading 累加
                #if defined(_ADDITIONAL_LIGHTS)
                int additionalLightsCount = GetAdditionalLightsCount();
                for (int lightIndex = 0; lightIndex < additionalLightsCount; ++lightIndex)
                {
                    Light addLight = GetAdditionalLight(lightIndex, input.positionWS);
                    half addNdotl = dot(finalNormal, addLight.direction);
                    
                    // 考虑附加光的实际亮度
                    half addLightBrightness = max(addLight.color.r, max(addLight.color.g, addLight.color.b));
                    half addCelIntensity = smoothstep(_ShadowThreshold - _ShadowFeather, _ShadowThreshold + _ShadowFeather, addNdotl) * addLightBrightness;
                    
                    // 计算距离衰减与阴影衰减
                    half3 addLightColor = addLight.color * (addLight.distanceAttenuation * addLight.shadowAttenuation);
                    litColor += baseTex.rgb * lerp(_ShadowColor.rgb, half3(1.0, 1.0, 1.0), addCelIntensity) * addLightColor;
                    
                    // 附加光源累加，正常把物理阴影区点亮
                    totalLightIntensity = saturate(totalLightIntensity + addCelIntensity * addLight.distanceAttenuation * addLight.shadowAttenuation);
                }
                #endif

                characterColor = half4(litColor, baseTex.a);
                #endif

                // 自发光计算
                #if defined(_USE_EMISSION)
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
                characterColor.rgb += emission;
                #endif

                // 统一的混合本色（2D 漫画本色）
                half3 baseCharacterColor = characterColor.rgb;

                #if defined(_USE_HIGH_CONTRAST)
                half3 comicColor = lerp(_ComicShadowColor.rgb, _ComicBrightColor.rgb, t);
                // 引入 lineMask 以防止五官细节线丢失，并染为统一的插画细节线色
                half lineMask = smoothstep(_LineThreshold - 0.05, _LineThreshold + 0.05, brightness);
                baseCharacterColor = lerp(_ComicLineColor.rgb, comicColor, lineMask);
                #endif

                // 4. 背景混合计算（只针对 2D 漫画基础映射）
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                half4 bgColor = SAMPLE_TEXTURE2D(_CwcGrabBackgroundTex, sampler_CwcGrabBackgroundTex, screenUV);

                // 采样遮罩贴图（R通道与Alpha通道相乘，完美兼容带透明的Mask和黑白Mask图）
                half4 maskTexSample = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                half maskVal = maskTexSample.r * maskTexSample.a;
                half finalMask = maskVal;

                // 如果开启了暗部作为映射遮罩
                #if defined(_DARK_AS_BLEND_MASK)
                half darkWeight = 1.0 - t;
                finalMask = max(finalMask, darkWeight);
                #endif

                half lerpFactor = _BlendWeight * characterColor.a * finalMask;
                // 混合背景得到 blendedColor
                half3 blendedColor = lerp(baseCharacterColor, bgColor.rgb, lerpFactor);

                // 5. 3D物理阴影作为最后一层正片叠底（Multiply）染色层叠在最上方
                half3 finalColor = blendedColor;
                #if defined(_USE_HIGH_CONTRAST)
                    #if !defined(_USE_UNLIT)
                    // 乘法混合计算：_ShadowColor.a 控制阴影的强弱权重
                    half3 shadowedColor = lerp(blendedColor, blendedColor * _ShadowColor.rgb, _ShadowColor.a);
                    finalColor = lerp(shadowedColor, blendedColor, totalLightIntensity);
                    #endif
                #endif

                return half4(finalColor, characterColor.a);
            }
            ENDHLSL
        }
    }
}
