// Crystal Drake 01B. The URP package guard and passes are preserved from NeonCharacterURP.
// Private scale maps affect this material only. Hit flash, attack glow, fog and shadows keep their existing meaning.
Shader "NeonSyndicate/Crystal Drake URP"
{
    Properties
    {
        _MainTex ("Palette", 2D) = "white" {}
        _MGTex ("Metal (R) Detail (G) Smooth (A)", 2D) = "black" {}
        _EmTex ("Emission Palette", 2D) = "black" {}
        _DetailTex ("RGB Scale Normal", 2D) = "bump" {}
        _ScaleSurface ("Scale AO (R), Polish (G), Pearl (B)", 2D) = "white" {}
        _DetailStrength ("Detail Strength", Range(0, 2)) = 0.75
        _EmissionBoost ("Emission Boost", Float) = 2.4
        _RimColor ("Rim Color (A = strength)", Color) = (0.6, 0.78, 1, 0.14)
        _HitFlash ("Hit Flash", Float) = 0
        _Glow ("Glow Pulse", Float) = 1
        _Wet ("Wetness", Range(0, 1)) = 0
    }

    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal": "17.3.0"
        }
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // One layout for every pass (an SRP Batcher requirement).
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _DetailTex_ST;
            half _DetailStrength;
            half _EmissionBoost;
            half4 _RimColor;
            half _HitFlash;
            half _Glow;
            half _Wet;
        CBUFFER_END

        TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
        TEXTURE2D(_MGTex);      SAMPLER(sampler_MGTex);
        TEXTURE2D(_EmTex);      SAMPLER(sampler_EmTex);
        TEXTURE2D(_DetailTex);  SAMPLER(sampler_DetailTex);
        TEXTURE2D(_ScaleSurface); SAMPLER(sampler_ScaleSurface);
        ENDHLSL

        // ------------------------------------------------------------------ forward lighting
        // LightMode UniversalForwardOnly, as URP's own forward-only shaders use: drawn by the Forward and Forward+
        // renderers like UniversalForward, and still drawn if the renderer is switched to Deferred.
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CharacterVertex
            #pragma fragment CharacterFragment

            // URP lighting keywords (the same set URP Lit compiles for forward rendering).
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 uv : TEXCOORD0;            // xy palette (_MainTex_ST), zw detail (_DetailTex_ST)
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;      // xyz tangent, w bitangent sign
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                half4 fogFactorAndVertexLight : TEXCOORD4;
            #else
                half fogFactor : TEXCOORD4;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord : TEXCOORD5;
            #endif
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings CharacterVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.uv.xy = TRANSFORM_TEX(input.uv0, _MainTex);
                output.uv.zw = TRANSFORM_TEX(input.uv1, _DetailTex);
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                real sign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

                half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
                output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
            #else
                output.fogFactor = fogFactor;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(vertexInput);
            #endif
                half4 probeOcclusion = 0;
                OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, probeOcclusion);
                output.positionCS = vertexInput.positionCS;
                return output;
            }

            half4 CharacterFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // ---- private scale surface; UV0 still selects the original palette cells
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv.xy);
                half4 mg = SAMPLE_TEXTURE2D(_MGTex, sampler_MGTex, input.uv.xy);
                half3 em = SAMPLE_TEXTURE2D(_EmTex, sampler_EmTex, input.uv.xy).rgb;
                // These runtime normals are explicitly RGB encoded on every graphics API.
                half3 dn = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, input.uv.zw).xyz * 2.0 - 1.0;
                half3 scale = SAMPLE_TEXTURE2D(_ScaleSurface, sampler_ScaleSurface, input.uv.zw).rgb;
                half mask = mg.b;
                half k = mg.g * _DetailStrength;
                half3 normalTS = normalize(half3(dn.xy * k, 1.0));

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = c.rgb * lerp(1.0, scale.r, mask) * (1.0 - _Wet * 0.25);
                surface.metallic = mg.r;
                surface.specular = half3(0.0, 0.0, 0.0);
                surface.smoothness = saturate(mg.a + (scale.g - 0.5) * mask * 0.28 + _Wet * 0.35);
                surface.normalTS = normalTS;
                surface.occlusion = lerp(1.0, scale.r, mask * 0.6);
                surface.alpha = 1.0;

                // ---- lighting inputs (as URP Lit's InitializeInputData)
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float sgn = input.tangentWS.w;
                float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
                inputData.tangentToWorld = tangentToWorld;
                inputData.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));
                inputData.viewDirectionWS = viewDirWS;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                inputData.shadowCoord = input.shadowCoord;
            #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
            #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
            #endif
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
                inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
            #else
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
            #endif
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

                // ---- glow, rim light and hit flash (emission, on top of the lighting like the surface shader's)
                half facing = saturate(dot(viewDirWS, inputData.normalWS));
                half pearl = mask * scale.b * pow(1.0 - facing, 4.0) * 0.18;
                surface.albedo *= lerp(half3(1, 1, 1), half3(0.80, 0.93, 1.0), pearl);
                half rim = pow(1.0 - facing, 3.0);
                surface.emission = em * _EmissionBoost * _Glow + _RimColor.rgb * (rim * _RimColor.a) + _HitFlash * half3(1.0, 0.85, 0.7);

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1.0;
                return color;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------ shadows (URP's own shadow caster)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            // Directional and punctual light shadows apply the normal bias differently.
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // ------------------------------------------------------------------ depth prepass and depth texture
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // ------------------------------------------------------------------ depth and normals (SSAO): vertex normals,
        // so the fabric detail does not speckle the ambient occlusion
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormalsOnly" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }

        // ------------------------------------------------------------------ per-object motion vectors (TAA, motion
        // blur in Camera and Objects mode); skinned bodies supply their previous positions (skinnedMotionVectors)
        Pass
        {
            Name "MotionVectors"
            Tags { "LightMode" = "MotionVectors" }
            ColorMask RG

            HLSLPROGRAM
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ObjectMotionVectors.hlsl"
            ENDHLSL
        }
    }

    // Without the URP package the SubShader above is skipped; this placeholder keeps the shader valid. The game never
    // uses this shader on the Built-in pipeline (it uses CrystalDrake.shader there).
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
    FallBack Off
}
