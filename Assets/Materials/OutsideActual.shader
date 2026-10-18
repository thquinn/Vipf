Shader "thquinn/OutsideActual"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [HDR]         _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)

        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.5

        [Header(World Y Fade)]
        _YMin("Min World Y", Float) = 0
        _YMax("Max World Y", Float) = 10
        _FadeDistance("Fade Distance", Float) = 1
        [KeywordEnum(Smooth, Exponential, Power)] _Falloff("Falloff Mode", Float) = 0
        _FadeSharpness("Exponential Sharpness", Float) = 3
        _FadeExponent("Power Exponent", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        // Same CBUFFER in every pass => SRP Batcher compatible.
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half4  _EmissionColor;
            half   _Metallic;
            half   _Smoothness;
            float  _YMin;
            float  _YMax;
            float  _FadeDistance;
            float  _FadeSharpness;
            float  _FadeExponent;
        CBUFFER_END

        // Returns 1 inside [_YMin, _YMax] and falls toward 0 outside it.
        // The curve is chosen by the "Falloff Mode" dropdown on the material.
        half YRangeFade(float worldY)
        {
            float fd = max(_FadeDistance, 1e-4);

            // Distance outside the range; 0 anywhere inside it.
            float d = max(max(_YMin - worldY, worldY - _YMax), 0.0);

        #if defined(_FALLOFF_EXPONENTIAL)
            // exp(-k * d / fd): 1 at the edge, never quite reaching 0.
            // Higher sharpness = faster fade; fd acts as the distance scale.
            return (half)exp(-_FadeSharpness * d / fd);

        #elif defined(_FALLOFF_POWER)
            // (1 - d/fd)^p: reaches exactly 0 at fd. p > 1 drops faster near the edge,
            // p < 1 holds brightness longer and then drops off near fd.
            float t = saturate(d / fd);
            return (half)pow(1.0 - t, max(_FadeExponent, 1e-4));

        #else
            // Smooth (default): smoothstep ramp reaching exactly 0 at fd.
            float t = saturate(d / fd);
            return (half)smoothstep(0.0, 1.0, 1.0 - t);
        #endif
        }
        ENDHLSL

        // ------------------------------------------------------------------
        // Main lit pass
        // ------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _FALLOFF_SMOOTH _FALLOFF_EXPONENTIAL _FALLOFF_POWER

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrm = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = nrm.normalWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor  = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                SurfaceData surface = (SurfaceData)0;
                surface.albedo     = baseSample.rgb;
                surface.alpha      = 1;
                surface.metallic   = _Metallic;
                surface.smoothness = _Smoothness;
                surface.occlusion  = 1;
                surface.emission   = _EmissionColor.rgb;

                InputData input = (InputData)0;
                input.positionWS                = IN.positionWS;
                input.positionCS                = IN.positionCS;
                input.normalWS                  = NormalizeNormalPerPixel(IN.normalWS);
                input.viewDirectionWS           = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                input.shadowCoord               = TransformWorldToShadowCoord(IN.positionWS);
                input.fogCoord                  = IN.fogFactor;
                input.vertexLighting            = half3(0, 0, 0);
                input.bakedGI                   = SampleSH(input.normalWS);
                input.normalizedScreenSpaceUV   = GetNormalizedScreenSpaceUV(IN.positionCS);
                input.shadowMask                = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentPBR(input, surface);

                // Fade lit result AND emission to black outside the Y range.
                color.rgb *= YRangeFade(IN.positionWS.y);

                color.rgb = MixFog(color.rgb, IN.fogFactor);
                color.a = 1;
                return color;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Shadow caster
        // ------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Depth only (for depth prepass / camera depth texture)
        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthVert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half DepthFrag(Varyings IN) : SV_Target
            {
                return IN.positionCS.z;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
