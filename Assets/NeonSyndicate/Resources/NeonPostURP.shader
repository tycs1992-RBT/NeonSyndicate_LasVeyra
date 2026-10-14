// Neon Syndicate's own image passes on the Universal Render Pipeline, drawn by NeonPostFeature (a URP renderer
// feature) for the game camera. They are the two parts of NeonPost.shader that URP's volume stack has no exact twin for:
//  - "NeonLuminance": the scene's log luminance into a small 64 x 64 target before post-processing, the same taps and
//    centre weighting as NeonPost.shader's fragLum. Its mip chain averages it to one texel for eye adaptation.
//  - "NeonVignette": NeonPost's vignette, multiplied onto the image after URP's tonemapping and colour grading, which
//    is where NeonPost applies it (URP's own vignette darkens before tonemapping).
// The URP SubShader is only compiled when the URP package is installed (PackageRequirements); the second SubShader
// keeps this file valid in a Built-in-only project, where nothing uses it.
Shader "Hidden/NeonSyndicate/URP Post"
{
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal": "17.3.0"
        }
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        HLSLINCLUDE
        #pragma target 3.5
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _NeonVignette;    // NeonPost's vignette amount times 2.2 (UrpGradeMapping.Result.ExactVignette)

        float NeonLuma(float3 c)
        {
            return dot(c, float3(0.2126, 0.7152, 0.0722));
        }

        float3 NeonSampleLinear(float2 uv)
        {
            float3 c = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0).rgb;
        #if defined(UNITY_COLORSPACE_GAMMA)
            c = SRGBToLinear(c);
        #endif
            return c;
        }

        float NeonLogLuma(float2 uv)
        {
            return clamp(log2(NeonLuma(NeonSampleLinear(uv)) + 1e-4), -13.0, 5.0);
        }

        float4 FragLuminance(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            // Four taps per texel of the 64 x 64 target, a quarter of its texel from the centre (NeonPost fragLum);
            // the offsets follow the source's scale so a dynamically scaled camera target reads the same spots.
            float2 t = (1.0 / 256.0) * _BlitScaleBias.xy;
            float lsum = NeonLogLuma(uv + float2(-t.x, -t.y));
            lsum += NeonLogLuma(uv + float2(t.x, -t.y));
            lsum += NeonLogLuma(uv + float2(-t.x, t.y));
            lsum += NeonLogLuma(uv + float2(t.x, t.y));
            // The centre of the screen counts more than the edges.
            float2 d = DYNAMIC_SCALING_REMOVE_SCALEBIAS(input.texcoord) - 0.5;
            float w = saturate(1.0 - dot(d, d) * 2.4) + 0.15;
            return float4(lsum * 0.25 * w, w, 0.0, 1.0);
        }

        half4 FragVignette(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 d = input.texcoord - 0.5;
            float v = saturate(1.0 - dot(d, d) * _NeonVignette);
        #if defined(UNITY_COLORSPACE_GAMMA)
            // The target holds gamma-encoded colour; this scales it so the linear colour is multiplied by v, as in NeonPost.
            v = pow(v, 1.0 / 2.2);
        #endif
            return half4(v, v, v, 1.0);
        }
        ENDHLSL

        Pass
        {
            Name "NeonLuminance"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragLuminance
            ENDHLSL
        }

        Pass
        {
            Name "NeonVignette"
            // Multiplies the graded image in place: result = vignette x image.
            Blend DstColor Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragVignette
            ENDHLSL
        }
    }

    // Without the URP package the SubShader above is skipped; this placeholder keeps the shader valid.
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 vert(float4 vertex : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex);
            }

            fixed4 frag() : SV_Target
            {
                return fixed4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
