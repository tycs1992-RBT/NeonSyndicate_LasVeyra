// Opaque crystal stone. Lit facets, restrained fracture emission and a view-dependent mineral layer approximation.
// No refraction, scene color sampling, grab passes or screen-space distortion. Retained through Resources.
Shader "NeonSyndicate/District Crystal"
{
    Properties
    {
        _Color ("Crystal Body", Color) = (0.105, 0.25, 0.34, 1)
        _DepthColor ("Mineral Interior", Color) = (0.12, 0.47, 0.56, 1)
        _MainTex ("R Fractures G Inclusions B Strata", 2D) = "white" {}
        [HDR] _RimColor ("Resonance Color", Color) = (0.21, 0.83, 1, 1)
        _EmissionStrength ("Emission Strength", Range(0, 4)) = 1.1
        _InternalStrength ("Interior Layer Strength", Range(0, 1)) = 0.45
        _RimPower ("Edge Falloff", Range(1, 8)) = 4.2
        _PulseAmplitude ("Pulse Amplitude", Range(0, 0.3)) = 0.045
        _PulseSpeed ("Pulse Speed", Range(0, 2)) = 0.55
        _Glossiness ("Smoothness", Range(0, 1)) = 0.88
        _Metallic ("Metallic", Range(0, 1)) = 0.08
    }

    SubShader
    {
        PackageRequirements { "com.unity.render-pipelines.universal": "17.3.0" }
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" "UniversalMaterialType"="Lit" }
        LOD 250

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            half4 _DepthColor;
            half4 _RimColor;
            half _EmissionStrength;
            half _InternalStrength;
            half _RimPower;
            half _PulseAmplitude;
            half _PulseSpeed;
            half _Glossiness;
            half _Metallic;
        CBUFFER_END
        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        ENDHLSL

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CrystalVertex
            #pragma fragment CrystalFragment
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
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half4 fogAndVertexLight : TEXCOORD3;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord : TEXCOORD4;
            #endif
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings CrystalVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogAndVertexLight.x = ComputeFogFactor(positions.positionCS.z);
                output.fogAndVertexLight.yzw = VertexLighting(positions.positionWS, output.normalWS);
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(positions);
            #endif
                half4 probeOcclusion = 0;
                OUTPUT_SH4(positions.positionWS, output.normalWS, GetWorldSpaceNormalizeViewDir(positions.positionWS), output.vertexSH, probeOcclusion);
                return output;
            }

            half4 CrystalFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                half3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half facing = saturate(dot(normalWS, viewWS));
                half3 mineral = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
                // The second sample suggests inclusions under a facet; it does not transmit scene color.
                float2 interiorUV = input.uv * 1.71 + float2(0.13, 0.29) + (1.0h - facing) * float2(0.04, -0.025);
                half interior = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, interiorUV).b * mineral.g;
                half veins = smoothstep(0.24h, 0.87h, mineral.r);
                half rim = pow(1.0h - facing, _RimPower);
                half pulse = 1.0h + sin(_Time.y * _PulseSpeed + input.positionWS.y * 0.7) * _PulseAmplitude;

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = _Color.rgb * (0.74h + mineral.g * 0.40h) + _DepthColor.rgb * interior * _InternalStrength * 0.25h;
                surface.metallic = _Metallic;
                surface.smoothness = saturate(_Glossiness - mineral.r * 0.10h - (1.0h - mineral.g) * 0.06h);
                surface.normalTS = half3(0, 0, 1);
                surface.occlusion = 0.93h + mineral.g * 0.07h;
                surface.alpha = 1;
                surface.emission = (_RimColor.rgb * (veins * 0.28h + rim * 0.24h) + _DepthColor.rgb * interior * _InternalStrength * 0.20h) * _EmissionStrength * pulse;

                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.positionCS = input.positionCS;
                lighting.normalWS = normalWS;
                lighting.viewDirectionWS = viewWS;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                lighting.shadowCoord = input.shadowCoord;
            #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            #endif
                lighting.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1), input.fogAndVertexLight.x);
                lighting.vertexLighting = input.fogAndVertexLight.yzw;
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, normalWS);
                lighting.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
                half4 color = UniversalFragmentPBR(lighting, surface);
                color.rgb = MixFog(color.rgb, lighting.fogCoord);
                color.a = 1;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
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
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
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
        Pass
        {
            Name "MotionVectors"
            Tags { "LightMode"="MotionVectors" }
            ColorMask RG
            HLSLPROGRAM
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ObjectMotionVectors.hlsl"
            ENDHLSL
        }
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 250
        Cull Back
        ZWrite On
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        #pragma multi_compile_instancing
        sampler2D _MainTex;
        fixed4 _Color;
        half4 _DepthColor;
        half4 _RimColor;
        half _EmissionStrength, _InternalStrength, _RimPower, _PulseAmplitude, _PulseSpeed, _Glossiness, _Metallic;
        struct Input { float2 uv_MainTex; float3 viewDir; float3 worldPos; };
        void surf(Input input, inout SurfaceOutputStandard output)
        {
            half facing = saturate(normalize(input.viewDir).z);
            half3 mineral = tex2D(_MainTex, input.uv_MainTex).rgb;
            float2 interiorUV = input.uv_MainTex * 1.71 + float2(0.13, 0.29) + (1.0h - facing) * float2(0.04, -0.025);
            half interior = tex2D(_MainTex, interiorUV).b * mineral.g;
            half veins = smoothstep(0.24h, 0.87h, mineral.r);
            half rim = pow(1.0h - facing, _RimPower);
            half pulse = 1.0h + sin(_Time.y * _PulseSpeed + input.worldPos.y * 0.7) * _PulseAmplitude;
            output.Albedo = _Color.rgb * (0.74h + mineral.g * 0.40h) + _DepthColor.rgb * interior * _InternalStrength * 0.25h;
            output.Metallic = _Metallic;
            output.Smoothness = saturate(_Glossiness - mineral.r * 0.10h - (1.0h - mineral.g) * 0.06h);
            output.Occlusion = 0.93h + mineral.g * 0.07h;
            output.Emission = (_RimColor.rgb * (veins * 0.28h + rim * 0.24h) + _DepthColor.rgb * interior * _InternalStrength * 0.20h) * _EmissionStrength * pulse;
            output.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
