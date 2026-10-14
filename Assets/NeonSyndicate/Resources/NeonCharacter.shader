// Character shading for Neon Syndicate's enhanced humans (Built-in render pipeline, physically based).
// Palette albedo (UV0), per-cell metallic, smoothness and fabric-detail strength (_MGTex), glowing seams and
// lit irises (_EmTex), a tiling fabric and tech-weave detail normal map on the second UV set, a soft rim light
// that separates characters from busy neon backgrounds, plus per-renderer hit flash and glow pulse.
Shader "NeonSyndicate/Character"
{
    Properties
    {
        _MainTex ("Palette", 2D) = "white" {}
        _MGTex ("Metal (R) Detail (G) Smooth (A)", 2D) = "black" {}
        _EmTex ("Emission Palette", 2D) = "black" {}
        _DetailTex ("Detail Normal", 2D) = "bump" {}
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
            float3 dn = UnpackNormal(tex2D(_DetailTex, IN.uv2_DetailTex));
            float k = mg.g * _DetailStrength;
            o.Normal = normalize(float3(dn.xy * k, 1.0));
            o.Albedo = c.rgb * (1.0 - _Wet * 0.25);
            o.Metallic = mg.r;
            o.Smoothness = saturate(mg.a + _Wet * 0.35);
            half rim = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), 3.0);
            o.Emission = em * _EmissionBoost * _Glow + _RimColor.rgb * (rim * _RimColor.a) + _HitFlash * half3(1.0, 0.85, 0.7);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
