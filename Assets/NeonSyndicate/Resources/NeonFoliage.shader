// Foliage shading for Neon Syndicate's leaves, fronds, needles, cactus skins and weed cards (Built-in render pipeline).
// Standard physically based lighting plus the two terms thin leaves add: wrap lighting (light carries a little past
// the terminator) and a back-light translucency tint (a light behind the leaf glows through toward the viewer),
// both per light so street lamps and headlights work too. Alpha tested, so the same shader draws the cutout weed
// tufts and the solid leaves (which have alpha 1). Mats.Foliage picks it from Medium up; URP keeps its Lit material.
Shader "NeonSyndicate/Foliage"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo (A alpha)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 0.6
        _Glossiness ("Smoothness", Range(0, 1)) = 0.1
        _Translucency ("Translucency (back light tint)", Color) = (0.45, 0.65, 0.2, 1)
        _TransStrength ("Translucency Strength", Range(0, 2)) = 0.8
        _Wrap ("Wrap Lighting", Range(0, 1)) = 0.35
        _Cull ("Cull (0 off, 1 front, 2 back)", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
        LOD 300
        Cull [_Cull]

        CGPROGRAM
        #pragma surface surf Foliage fullforwardshadows alphatest:_Cutoff addshadow exclude_path:deferred exclude_path:prepass
        #pragma target 3.5
        #include "UnityPBSLighting.cginc"
        #include "UnityStandardUtils.cginc"

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        half _BumpScale;
        half _Glossiness;
        fixed4 _Translucency;
        half _TransStrength;
        half _Wrap;

        struct Input
        {
            float2 uv_MainTex;
        };

        struct SurfaceOutputFoliage
        {
            fixed3 Albedo;
            float3 Normal;
            half3 Emission;
            half Metallic;
            half Smoothness;
            half Occlusion;
            fixed Alpha;
            half3 Trans;
        };

        inline SurfaceOutputStandard ToStandard(SurfaceOutputFoliage s)
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

        inline half4 LightingFoliage(SurfaceOutputFoliage s, float3 viewDir, UnityGI gi)
        {
            half4 c = LightingStandard(ToStandard(s), viewDir, gi);
            half3 light = gi.light.color;
            half ndl = dot(s.Normal, gi.light.dir);
            // Wrap: an energy-conserving wrapped diffuse, only the part past plain Lambert is added.
            half wrapped = saturate((ndl + _Wrap) / ((1.0 + _Wrap) * (1.0 + _Wrap)));
            half extra = max(0.0, wrapped - saturate(ndl));
            c.rgb += s.Albedo * s.Trans * light * extra;
            // Back light: the light direction bent by the normal, seen from the opposite side.
            half3 h = normalize(gi.light.dir + s.Normal * 0.2);
            half through = pow(saturate(dot(viewDir, -h)), 3.0);
            c.rgb += light * through * s.Trans * _TransStrength;
            return c;
        }

        inline void LightingFoliage_GI(SurfaceOutputFoliage s, UnityGIInput data, inout UnityGI gi)
        {
            LightingStandard_GI(ToStandard(s), data, gi);
        }

        void surf (Input IN, inout SurfaceOutputFoliage o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_MainTex), _BumpScale);
            o.Metallic = 0.0;
            o.Smoothness = _Glossiness;
            o.Occlusion = 1.0;
            o.Emission = half3(0.0, 0.0, 0.0);
            o.Trans = _Translucency.rgb;
        }
        ENDCG
    }
    FallBack "Transparent/Cutout/Diffuse"
}
