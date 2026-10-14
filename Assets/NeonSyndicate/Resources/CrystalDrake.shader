// Crystal Drake surface, 01B. Private palette and scale textures; no changes to the shared character shader.
Shader "NeonSyndicate/Crystal Drake"
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
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.5

        sampler2D _MainTex;
        sampler2D _MGTex;
        sampler2D _EmTex;
        sampler2D _DetailTex;
        sampler2D _ScaleSurface;
        half _DetailStrength;
        half _EmissionBoost;
        fixed4 _RimColor;
        half _HitFlash;
        half _Glow;
        half _Wet;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_DetailTex;
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 mg = tex2D(_MGTex, IN.uv_MainTex);
            fixed3 em = tex2D(_EmTex, IN.uv_MainTex).rgb;
            // Runtime-generated RGB normals are not platform-compressed normal textures.
            float3 dn = tex2D(_DetailTex, IN.uv2_DetailTex).xyz * 2.0 - 1.0;
            half3 scale = tex2D(_ScaleSurface, IN.uv2_DetailTex).rgb;
            float k = mg.g * _DetailStrength;
            o.Normal = normalize(float3(dn.xy * k, 1.0));
            half mask = mg.b;
            half cavity = lerp(1.0, scale.r, mask);
            half facing = saturate(dot(normalize(IN.viewDir), o.Normal));
            // A restrained cool edge tint suggests nacre without adding self illumination.
            half pearl = mask * scale.b * pow(1.0 - facing, 4.0) * 0.18;
            o.Albedo = c.rgb * cavity * lerp(half3(1, 1, 1), half3(0.80, 0.93, 1.0), pearl) * (1.0 - _Wet * 0.25);
            o.Metallic = mg.r;
            o.Smoothness = saturate(mg.a + (scale.g - 0.5) * mask * 0.28 + _Wet * 0.35);
            o.Occlusion = lerp(1.0, scale.r, mask * 0.6);
            half rim = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), 3.0);
            o.Emission = em * _EmissionBoost * _Glow + _RimColor.rgb * (rim * _RimColor.a) + _HitFlash * half3(1.0, 0.85, 0.7);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
