Shader "thquinn/Window"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.8, 0.9, 1, 1)
        _TintStrength ("Tint Strength", Range(0, 1)) = 0.15
        _Mix ("Mix", Color) = (1, 1, 1, 1)
        _BlurRadius ("Blur Radius (pixels)", Range(0, 32)) = 8
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            Blend One Zero   // output replaces what's behind, since we already sampled it

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _TintStrength;
                half4 _Mix;
                half _BlurRadius;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings  { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = GetNormalizedScreenSpaceUV(IN.positionCS);
                float2 px = _ScreenParams.zw - 1.0;   // 1 / screen size

                // per-pixel rotation (interleaved gradient noise) turns banding into fine grain
                float rot = frac(52.9829189 * frac(dot(IN.positionCS.xy, float2(0.06711056, 0.00583715)))) * 6.2831853;

                const int SAMPLES = 12;
                half3 col = 0;
                for (int i = 0; i < SAMPLES; i++)
                {
                    float r = sqrt((i + 0.5) / SAMPLES);
                    float a = i * 2.39996323 + rot;   // golden-angle spiral covers the disc evenly
                    float2 offset = float2(cos(a), sin(a)) * r * _BlurRadius * px;
                    col += SampleSceneColor(uv + offset);
                }
                col /= SAMPLES;

                col = lerp(col, _Tint.rgb, _TintStrength);
                col.rgb *= _Mix;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}