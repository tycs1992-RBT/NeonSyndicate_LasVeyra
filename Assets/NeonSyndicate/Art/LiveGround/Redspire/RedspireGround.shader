Shader "NeonSyndicate/Redspire Ground L3"
{
    Properties
    {
        _LegacyTex ("Original ground", 2D) = "white" {}
        _LegacyColor ("Original tint", Color) = (1,1,1,1)
        _LegacySmoothness ("Original smoothness", Range(0,1)) = 0.1
        [Normal] _LegacyNormal ("Original normal", 2D) = "bump" {}
        _LegacyPacked ("Original packed mask", 2D) = "white" {}
        _LegacyDetail ("Original detail color", 2D) = "gray" {}
        [Normal] _LegacyDetailNormal ("Original detail normal", 2D) = "bump" {}
        _LegacyHasMask ("Original mask present", Float) = 0
        _LegacyMaskScale ("Original mask scale", Float) = 1
        _LegacyHasNormal ("Original normal present", Float) = 0
        _LegacyBumpScale ("Original normal scale", Float) = 1
        _LegacyDetailEnabled ("Original detail enabled", Float) = 0
        _LegacyDetailBump ("Original detail normal scale", Float) = 0
        _LegacyOcclusion ("Original occlusion", Float) = 0
        _EarthColor ("Earth color", 2D) = "white" {}
        [Normal] _EarthNormal ("Earth normal", 2D) = "bump" {}
        _EarthMask ("Earth metallic smoothness", 2D) = "black" {}
        _SandColor ("Stony sand color", 2D) = "white" {}
        [Normal] _SandNormal ("Stony sand normal", 2D) = "bump" {}
        _SandMask ("Stony sand metallic smoothness", 2D) = "black" {}
        _RegionMask ("Existing biome weight", 2D) = "black" {}
        _MapHalf ("Map half metres", Float) = 1600
        _TileMetres ("Repeat metres", Float) = 4
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.5
        #include "UnityCG.cginc"
        UNITY_DECLARE_TEX2D(_LegacyTex);
        UNITY_DECLARE_TEX2D_NOSAMPLER(_LegacyNormal);
        UNITY_DECLARE_TEX2D_NOSAMPLER(_LegacyPacked);
        UNITY_DECLARE_TEX2D_NOSAMPLER(_LegacyDetail);
        UNITY_DECLARE_TEX2D_NOSAMPLER(_LegacyDetailNormal);
        UNITY_DECLARE_TEX2D(_EarthColor);
        UNITY_DECLARE_TEX2D_NOSAMPLER(_EarthNormal);
        UNITY_DECLARE_TEX2D_NOSAMPLER(_EarthMask);
        UNITY_DECLARE_TEX2D_NOSAMPLER(_SandColor);
        UNITY_DECLARE_TEX2D_NOSAMPLER(_SandNormal);
        UNITY_DECLARE_TEX2D_NOSAMPLER(_SandMask);
        sampler2D _RegionMask;
        float4 _LegacyDetail_ST;
        float _LegacyHasMask,_LegacyMaskScale,_LegacyHasNormal,_LegacyBumpScale,_LegacyDetailEnabled,_LegacyDetailBump,_LegacyOcclusion;
        fixed4 _LegacyColor;
        float _MapHalf, _TileMetres, _LegacySmoothness;
        struct Input { float2 uv_LegacyTex; float3 worldPos; float3 worldNormal; INTERNAL_DATA };
        void surf(Input i, inout SurfaceOutputStandard o)
        {
            float2 region = tex2D(_RegionMask, i.worldPos.xz / (_MapHalf * 2) + .5).rg;
            float3 up = normalize(WorldNormalVector(i, float3(0,0,1)));
            float mix = saturate(region.g + (1 - saturate(up.y)) * 1.8);
            float2 uv = i.worldPos.xz / _TileMetres;
            float3 a = UNITY_SAMPLE_TEX2D_SAMPLER(_EarthColor,_EarthColor,uv).rgb;
            float3 b = UNITY_SAMPLE_TEX2D_SAMPLER(_SandColor,_EarthColor,uv).rgb;
            float3 legacy = UNITY_SAMPLE_TEX2D(_LegacyTex,i.uv_LegacyTex).rgb * _LegacyColor.rgb;
            float2 duv=i.uv_LegacyTex*_LegacyDetail_ST.xy+_LegacyDetail_ST.zw;
            legacy*=lerp(float3(1,1,1),UNITY_SAMPLE_TEX2D_SAMPLER(_LegacyDetail,_LegacyTex,duv).rgb*unity_ColorSpaceDouble.rgb,_LegacyDetailEnabled);
            float4 oldMask=UNITY_SAMPLE_TEX2D_SAMPLER(_LegacyPacked,_LegacyTex,i.uv_LegacyTex);
            float3 oldNormal=UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_LegacyNormal,_LegacyTex,i.uv_LegacyTex));
            oldNormal.xy*=_LegacyBumpScale;
            oldNormal=normalize(lerp(float3(0,0,1),oldNormal,_LegacyHasNormal));
            float3 detail=UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_LegacyDetailNormal,_LegacyTex,duv));
            oldNormal=normalize(oldNormal+float3(detail.xy*_LegacyDetailBump*_LegacyDetailEnabled,0));
            o.Albedo = lerp(legacy, lerp(a,b,mix), region.r);
            o.Smoothness = lerp(lerp(_LegacySmoothness,oldMask.a*_LegacyMaskScale,_LegacyHasMask), lerp(UNITY_SAMPLE_TEX2D_SAMPLER(_EarthMask,_EarthColor,uv).a,UNITY_SAMPLE_TEX2D_SAMPLER(_SandMask,_EarthColor,uv).a,mix),region.r);
            float3 n = normalize(lerp(UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_EarthNormal,_EarthColor,uv)),UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_SandNormal,_EarthColor,uv)),mix));
            float3 east = normalize(float3(1,0,0) - up * up.x);
            float3 north = normalize(cross(east,up));
            float3 perturbed = normalize(east*n.x + north*n.y + up*n.z);
            float3 tangentNormal = float3(dot(perturbed,WorldNormalVector(i,float3(1,0,0))),dot(perturbed,WorldNormalVector(i,float3(0,1,0))),dot(perturbed,up));
            o.Normal = normalize(lerp(oldNormal,tangentNormal,region.r));
            o.Metallic = oldMask.r*_LegacyHasMask*(1-region.r); o.Occlusion = lerp(lerp(1,oldMask.g,_LegacyOcclusion*_LegacyHasMask),1,region.r); o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
