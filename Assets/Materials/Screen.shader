Shader "thquinn/Screen"
{
    Properties
    {
        [MainTexture] _BaseMap ("Screen Texture", 2D) = "black" {}
        [MainColor] _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Brightness ("Brightness", Range(0, 4)) = 1
        [PerRendererData] _Glitch ("Glitch", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _Tint;
                half   _Brightness;
                float _Glitch;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            float randomNoise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float displace = randomNoise(float2(uv.y * .00001, _Time.y * .000001));
                displace += sin(uv.y * 10 + _Time.y * 10);
                displace += sin(uv.y * 31 + _Time.y * 53);
                uv.x += displace * 0.1 * _Glitch;
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                col.rgb *= _Tint.rgb * _Brightness;
                col.r *= 1 + _Glitch * 4;
                col.b *= 1 + _Glitch * 4;
                col.rgb += half3(.25, 0, .25) * _Glitch;
                col.a = 1;
                return col;
            }
            ENDHLSL
        }
    }
}
