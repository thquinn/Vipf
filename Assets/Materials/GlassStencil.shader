Shader "thquinn/GlassStencil"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry-1" }
        Pass
        {
            ColorMask 0
            ZWrite Off
            ZTest LEqual      // only mark pixels where the glass is actually visible
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 vert(float4 pos : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(pos.xyz);
            }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}