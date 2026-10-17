// Character shading for Neon Syndicate's enhanced humans (Built-in render pipeline, physically based).
// Palette albedo (UV0), per-cell metallic, smoothness and fabric-detail strength (_MGTex), glowing seams and
// lit irises (_EmTex), a tiling fabric and tech-weave detail normal map on the second UV set, a soft rim light
// that separates characters from busy neon backgrounds, plus per-renderer hit flash and glow pulse.
// Phase 2 of the visual upgrade adds the shading models the palette rows stand for, on top of the Standard lighting
// and per light: skin (palette row 0) gets wrap-lit subsurface scatter in a per-tone colour and a thin-skin
// transmission term at silhouettes (ears, fingers); cloth (rows 1 and 3) gets a grazing sheen; hair (row 2) gets a
// Kajiya-Kay strand highlight along the loft direction. Property names and the palette pipeline are unchanged.
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
        _SkinScatter ("Skin Scatter (RGB tint, A strength)", Color) = (1.0, 0.32, 0.22, 0.6)
        _ClothSheen ("Cloth Sheen (RGB tint, A strength)", Color) = (1.0, 1.0, 1.0, 0.22)
        _HairAniso ("Hair Highlight (RGB tint, A strength)", Color) = (1.0, 0.92, 0.82, 0.35)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf NeonChar fullforwardshadows exclude_path:deferred exclude_path:prepass
        #pragma target 3.5
        #include "UnityPBSLighting.cginc"

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
        half4 _SkinScatter;
        half4 _ClothSheen;
        half4 _HairAniso;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_DetailTex;
            float3 viewDir;
            float3 worldNormal;
            INTERNAL_DATA
        };

        struct SurfaceOutputNeon
        {
            fixed3 Albedo;
            float3 Normal;
            half3 Emission;
            half Metallic;
            half Smoothness;
            half Occlusion;
            fixed Alpha;
            half Scatter;
            half3 ScatterTint;
            half Sheen;
            half Aniso;
            half3 Strand;
        };

        inline SurfaceOutputStandard ToStandard(SurfaceOutputNeon s)
        {
            SurfaceOutputStandard std;
            UNITY_INITIALIZE_OUTPUT(SurfaceOutputStandard, std);
            std.Albedo = s.Albedo;
            std.Normal = s.Normal;
            std.Emission = s.Emission;
            std.Metallic = s.Metallic;
            std.Smoothness = s.Smoothness;
            std.Occlusion = s.Occlusion;
            std.Alpha = s.Alpha;
            return std;
        }

        inline half4 LightingNeonChar(SurfaceOutputNeon s, float3 viewDir, UnityGI gi)
        {
            half4 c = LightingStandard(ToStandard(s), viewDir, gi);
            half3 light = gi.light.color;
            half3 L = gi.light.dir;
            half ndl = dot(s.Normal, L);
            half ndv = saturate(dot(s.Normal, viewDir));
            // Skin: wrapped diffuse past the terminator in the scatter colour, and light coming through thin parts
            // (the silhouette stands in for ears and fingers until a thickness map exists).
            half wrap = 0.5 * s.Scatter;
            half wrapped = saturate((ndl + wrap) / ((1.0 + wrap) * (1.0 + wrap)));
            half band = max(0.0, wrapped - saturate(ndl));
            half3 skin = s.ScatterTint * s.Albedo * light * band * 1.6;
            half3 h = normalize(L + s.Normal * 0.3);
            half through = pow(saturate(dot(viewDir, -h)), 4.0) * pow(1.0 - ndv, 2.0);
            skin += s.ScatterTint * light * through * s.Scatter * 0.6;
            // Cloth: a soft grazing sheen on the lit half.
            half sheen = pow(1.0 - ndv, 4.0) * saturate(ndl + 0.3) * s.Sheen;
            // Hair: a strand highlight along the loft direction.
            half3 hv = normalize(L + viewDir);
            half tdh = dot(s.Strand, hv);
            half sinTH = sqrt(max(0.0, 1.0 - tdh * tdh));
            half aniso = pow(sinTH, 64.0) * saturate(ndl + 0.2) * s.Aniso;
            c.rgb += skin + light * (sheen * _ClothSheen.rgb + aniso * _HairAniso.rgb);
            return c;
        }

        inline void LightingNeonChar_GI(SurfaceOutputNeon s, UnityGIInput data, inout UnityGI gi)
        {
            LightingStandard_GI(ToStandard(s), data, gi);
        }

        void surf (Input IN, inout SurfaceOutputNeon o)
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
            o.Occlusion = 1.0;
            o.Alpha = 1.0;
            // The palette row says what the surface is: 0 skin, 1 cloth, 2 hair, 3 shoes and uniforms (half sheen).
            half row = floor(IN.uv_MainTex.y * 16.0);
            half isSkin = row < 0.5 ? 1.0 : 0.0;
            half isCloth = (row > 0.5 && row < 1.5) ? 1.0 : ((row > 2.5 && row < 3.5) ? 0.5 : 0.0);
            half isHair = (row > 1.5 && row < 2.5) ? 1.0 : 0.0;
            o.Scatter = isSkin * _SkinScatter.a;
            o.ScatterTint = lerp(_SkinScatter.rgb, c.rgb * _SkinScatter.rgb * 2.0, 0.5);
            o.Sheen = isCloth * _ClothSheen.a;
            o.Aniso = isHair * _HairAniso.a;
            o.Strand = normalize(WorldNormalVector(IN, float3(0.0, 1.0, 0.0)));
            half rim = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), 3.0);
            o.Emission = em * _EmissionBoost * _Glow + _RimColor.rgb * (rim * _RimColor.a) + _HitFlash * half3(1.0, 0.85, 0.7);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
