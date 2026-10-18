Shader "thquinn/OutsideActual"
{
    Properties
    {
        // ---- World Y fade -------------------------------------------------
        _WorldYFadeMin("Fade Min Y", Float) = 0.0
        _WorldYFadeMax("Fade Max Y", Float) = 10.0
        _WorldYFadeDistance("Fade Distance", Float) = 2.0

        // ---- Stock URP Lit properties --------------------------------------
        _WorkflowMode("WorkflowMode", Float) = 1.0

        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        _SpecGlossMap("Specular", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax("Scale", Range(0.005, 0.08)) = 0.005
        _ParallaxMap("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMapScale("Scale", Range(0.0, 2.0)) = 1.0
        _DetailAlbedoMap("Detail Albedo x2", 2D) = "linearGrey" {}
        _DetailNormalMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}

        // SRP batching compatibility for Clear Coat (not used in Lit)
        [HideInInspector] _ClearCoatMask("_ClearCoatMask", Float) = 0.0
        [HideInInspector] _ClearCoatSmoothness("_ClearCoatSmoothness", Float) = 0.0

        // Blending state
        _Surface("__surface", Float) = 0.0
        _Blend("__blend", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _BlendOp("__blendop", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0

        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        _QueueOffset("Queue offset", Float) = 0.0

        // Obsolete properties (kept so old materials upgrade cleanly)
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _GlossMapScale("Smoothness", Float) = 0.0
        [HideInInspector] _Glossiness("Smoothness", Float) = 0.0
        [HideInInspector] _GlossyReflections("EnvironmentReflections", Float) = 0.0

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        // ---------------------------------------------------------------------
        // Shared by every pass: an inlined copy of URP's LitInput.hlsl with the
        // three fade properties added to the UnityPerMaterial CBUFFER.
        // (The SRP Batcher needs all passes to share one identical CBUFFER, which
        // is why this can't simply be declared in the forward pass alone.)
        // It reuses URP's include guard, so any later include of the package's
        // LitInput.hlsl becomes a no-op.
        // ---------------------------------------------------------------------
        HLSLINCLUDE
        #ifndef UNIVERSAL_LIT_INPUT_INCLUDED
        #define UNIVERSAL_LIT_INPUT_INCLUDED

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ParallaxMapping.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

        #if defined(_DETAIL_MULX2) || defined(_DETAIL_SCALED)
        #define _DETAIL
        #endif

        // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        float4 _DetailAlbedoMap_ST;
        half4 _BaseColor;
        half4 _SpecColor;
        half4 _EmissionColor;
        half _Cutoff;
        half _Smoothness;
        half _Metallic;
        half _BumpScale;
        half _Parallax;
        half _OcclusionStrength;
        half _ClearCoatMask;
        half _ClearCoatSmoothness;
        half _DetailAlbedoMapScale;
        half _DetailNormalMapScale;
        half _Surface;

        // --- World Y fade (added) ---
        float _WorldYFadeMin;
        float _WorldYFadeMax;
        float _WorldYFadeDistance;
        CBUFFER_END

        TEXTURE2D(_ParallaxMap);        SAMPLER(sampler_ParallaxMap);
        TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
        TEXTURE2D(_DetailMask);         SAMPLER(sampler_DetailMask);
        TEXTURE2D(_DetailAlbedoMap);    SAMPLER(sampler_DetailAlbedoMap);
        TEXTURE2D(_DetailNormalMap);    SAMPLER(sampler_DetailNormalMap);
        TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
        TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);
        TEXTURE2D(_ClearCoatMap);       SAMPLER(sampler_ClearCoatMap);

        #ifdef _SPECULAR_SETUP
            #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv)
        #else
            #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv)
        #endif

        // World Y fade: 1 inside [min, max]; outside, smoothstep down to 0 over
        // _WorldYFadeDistance world units (0 distance = hard cut).
        half WorldYFadeFactor(float worldY)
        {
            float lo = min(_WorldYFadeMin, _WorldYFadeMax);
            float hi = max(_WorldYFadeMin, _WorldYFadeMax);

            // Distance outside the band (0 when inside).
            float outside = max(max(lo - worldY, worldY - hi), 0.0);

            float t = saturate(outside / max(_WorldYFadeDistance, 1e-4));
            return (half)(1.0 - smoothstep(0.0, 1.0, t));
        }

        // ---- Stock LitInput.hlsl functions below ----
        half4 SampleMetallicSpecGloss(float2 uv, half albedoAlpha)
        {
            half4 specGloss;

        #ifdef _METALLICSPECGLOSSMAP
            specGloss = half4(SAMPLE_METALLICSPECULAR(uv));
            #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
                specGloss.a = albedoAlpha * _Smoothness;
            #else
                specGloss.a *= _Smoothness;
            #endif
        #else
            #if _SPECULAR_SETUP
                specGloss.rgb = _SpecColor.rgb;
            #else
                specGloss.rgb = _Metallic.rrr;
            #endif

            #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
                specGloss.a = albedoAlpha * _Smoothness;
            #else
                specGloss.a = _Smoothness;
            #endif
        #endif

            return specGloss;
        }

        half SampleOcclusion(float2 uv)
        {
        #ifdef _OCCLUSIONMAP
            half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
            return LerpWhiteTo(occ, _OcclusionStrength);
        #else
            return half(1.0);
        #endif
        }

        half2 SampleClearCoat(float2 uv)
        {
        #if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
            half2 clearCoatMaskSmoothness = half2(_ClearCoatMask, _ClearCoatSmoothness);

        #if defined(_CLEARCOATMAP)
            clearCoatMaskSmoothness *= SAMPLE_TEXTURE2D(_ClearCoatMap, sampler_ClearCoatMap, uv).rg;
        #endif

            return clearCoatMaskSmoothness;
        #else
            return half2(0.0, 1.0);
        #endif
        }

        void ApplyPerPixelDisplacement(half3 viewDirTS, inout float2 uv)
        {
        #if defined(_PARALLAXMAP)
            uv += ParallaxMapping(TEXTURE2D_ARGS(_ParallaxMap, sampler_ParallaxMap), viewDirTS, _Parallax, uv);
        #endif
        }

        half3 ScaleDetailAlbedo(half3 detailAlbedo, half scale)
        {
            return half(2.0) * detailAlbedo * scale - scale + half(1.0);
        }

        half3 ApplyDetailAlbedo(float2 detailUv, half3 albedo, half detailMask)
        {
        #if defined(_DETAIL)
            half3 detailAlbedo = SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, detailUv).rgb;

            #if defined(_DETAIL_SCALED)
                detailAlbedo = ScaleDetailAlbedo(detailAlbedo, _DetailAlbedoMapScale);
            #else
                detailAlbedo = half(2.0) * detailAlbedo;
            #endif

            return albedo * LerpWhiteTo(detailAlbedo, detailMask);
        #else
            return albedo;
        #endif
        }

        half3 ApplyDetailNormal(float2 detailUv, half3 normalTS, half detailMask)
        {
        #if defined(_DETAIL)
            #if BUMP_SCALE_NOT_SUPPORTED
                half3 detailNormalTS = UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUv));
            #else
                half3 detailNormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUv), _DetailNormalMapScale);
            #endif

            detailNormalTS = normalize(detailNormalTS);

            return lerp(normalTS, BlendNormalRNM(normalTS, detailNormalTS), detailMask);
        #else
            return normalTS;
        #endif
        }

        inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
        {
            half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
            outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

            half4 specGloss = SampleMetallicSpecGloss(uv, albedoAlpha.a);
            outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
            outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);

        #if _SPECULAR_SETUP
            outSurfaceData.metallic = half(1.0);
            outSurfaceData.specular = specGloss.rgb;
        #else
            outSurfaceData.metallic = specGloss.r;
            outSurfaceData.specular = half3(0.0, 0.0, 0.0);
        #endif

            outSurfaceData.smoothness = specGloss.a;
            outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
            outSurfaceData.occlusion = SampleOcclusion(uv);
            outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

        #if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
            half2 clearCoat = SampleClearCoat(uv);
            outSurfaceData.clearCoatMask       = clearCoat.r;
            outSurfaceData.clearCoatSmoothness = clearCoat.g;
        #else
            outSurfaceData.clearCoatMask       = half(0.0);
            outSurfaceData.clearCoatSmoothness = half(0.0);
        #endif

        #if defined(_DETAIL)
            half detailMask = SAMPLE_TEXTURE2D(_DetailMask, sampler_DetailMask, uv).a;
            float2 detailUv = uv * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
            outSurfaceData.albedo = ApplyDetailAlbedo(detailUv, outSurfaceData.albedo, detailMask);
            outSurfaceData.normalTS = ApplyDetailNormal(detailUv, outSurfaceData.normalTS, detailMask);
        #endif
        }

        #endif // UNIVERSAL_LIT_INPUT_INCLUDED
        ENDHLSL

        // ---------------------------------------------------------------------
        // Forward lit: wraps the stock fragment function and darkens its output.
        // (No GBuffer pass: under the Deferred renderer URP draws this shader
        //  with this forward pass.)
        // ---------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            ZWrite[_ZWrite]
            Cull[_Cull]
            AlphaToMask[_AlphaToMask]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex LitPassVertex
            #pragma fragment LitFadeFragment

            // ---- Material keywords ----
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP

            // ---- Universal Pipeline keywords ----
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            // Forward+ was renamed in newer URP versions; listing both is harmless.
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            // ---- Unity defined keywords ----
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            // ---- GPU instancing ----
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            // The Varyings struct only carries positionWS when this is defined.
            // We need it for the per-pixel world Y, so make sure it's on.
            #ifndef REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
            #define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
            #endif

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

            // Wrapper around the stock fragment function. Signature matches
            // URP 14+ (output via out params, optional rendering-layers target).
            void LitFadeFragment(
                Varyings input
                , out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out float4 outRenderingLayers : SV_Target1
            #endif
            )
            {
                LitPassFragment(input, outColor
            #ifdef _WRITE_RENDERING_LAYERS
                    , outRenderingLayers
            #endif
                );

                // Scales lighting, specular, GI, fog and emission together.
                // Alpha is left alone so transparent materials keep their shape.
                outColor.rgb *= WorldYFadeFactor(input.positionWS.y);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // Remaining passes are the stock Lit passes (they pick up the shared
        // input block above, so their CBUFFER matches the forward pass).
        // ---------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }

        // Baked GI only. Baked emission is a static bake and is NOT affected
        // by the world-Y fade.
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaLit

            #pragma shader_feature EDITOR_VISUALIZATION
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SPECGLOSSMAP

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
