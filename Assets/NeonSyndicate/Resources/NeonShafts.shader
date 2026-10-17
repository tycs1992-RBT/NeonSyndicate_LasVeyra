// Neon Syndicate volumetric light for the Built-in render pipeline (driven by NeonPost.Volumetrics.cs, called before
// the bloom pass). Radial light shafts from the sun by day and the moon by night, occlusion aware through the depth
// texture (only sky pixels seed the rays, so towers and people cut dark bands into them), plus analytic height fog
// (denser air near the ground, a ground fog band at dawn and in the Frostline) that the per-material fog hook of the
// Standard shader cannot do. Passes: 0 sky mask at quarter resolution, 1 radial blur, 2 composite.
Shader "Hidden/NeonSyndicate/Shafts"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _ShaftTex;
    sampler2D_float _CameraDepthTexture;

    float4 _LightUV;        // xy light position in viewport space, z on-screen weight, w radius of the seed in uv
    float4 _ShaftColor;     // rgb tint times intensity
    float4 _Radial;         // x density, y decay, z weight per tap, w samples
    float4 _FogParams;      // x density at the base height, y base height, z falloff per metre, w sky distance
    float4 _FogColor;
    float4 _FogBand;        // x ground band strength, y band top height, z band thickness, w unused
    float4 _CamPos;
    float4 _Proj;           // x P00, y P11
    float4x4 _ViewToWorld;
    float _MaxSeed;

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float2 uvDepth : TEXCOORD1;
    };

    v2f vert(appdata_img v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord;
        o.uvDepth = v.texcoord;
        #if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0) o.uvDepth.y = 1.0 - o.uvDepth.y;
        #endif
        return o;
    }

    float EyeDepth(float2 uv)
    {
        return LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv, 0, 0)));
    }

    float Depth01(float2 uv)
    {
        return Linear01Depth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv, 0, 0)));
    }

    float3 ViewPos(float2 uv, float eye)
    {
        return float3((uv * 2.0 - 1.0) / _Proj.xy * eye, -eye);
    }

    // Pass 0: the seed. Sky pixels near the light, in the source's own HDR colour so the sun disc drives the rays.
    float4 fragMask(v2f i) : SV_Target
    {
        float sky = step(0.9995, Depth01(i.uvDepth));
        float3 c = tex2D(_MainTex, i.uv).rgb;
        c = min(c, _MaxSeed);
        float2 d = (i.uvDepth - _LightUV.xy);
        d.x *= _ScreenParams.x / _ScreenParams.y;
        float w = saturate(1.0 - length(d) / _LightUV.w);
        w *= w;
        return float4(c * sky * w * _LightUV.z, 1.0);
    }

    // Pass 1: radial blur toward the light.
    float4 fragRadial(v2f i) : SV_Target
    {
        int n = (int)_Radial.w;
        float2 toLight = (_LightUV.xy - i.uvDepth) * _Radial.x / n;
        float2 uv = i.uv;
        float illum = 1.0;
        float3 acc = 0.0;
        [loop]
        for (int k = 0; k < n; k++)
        {
            acc += tex2D(_MainTex, uv).rgb * illum;
            uv += toLight;
            illum *= _Radial.y;
        }
        return float4(acc * _Radial.z, 1.0);
    }

    // Pass 2: add the shafts and the height fog to the scene.
    float4 fragComposite(v2f i) : SV_Target
    {
        float3 c = tex2D(_MainTex, i.uv).rgb;
        float3 shafts = tex2D(_ShaftTex, i.uvDepth).rgb;
        c += shafts * _ShaftColor.rgb;
        if (_FogParams.x > 0.0 || _FogBand.x > 0.0)
        {
            float d01 = Depth01(i.uvDepth);
            float eye = EyeDepth(i.uvDepth);
            float3 vp = ViewPos(i.uvDepth, eye);
            float3 world = mul(_ViewToWorld, float4(vp, 1.0)).xyz;
            float3 ray = world - _CamPos.xyz;
            float dist = length(ray);
            float3 dir = ray / max(dist, 1e-4);
            if (d01 > 0.9995)
            {
                dist = _FogParams.w;
                ray = dir * dist;
            }
            // Exponential height fog, integrated along the ray in closed form.
            float falloff = _FogParams.z;
            float startD = _FogParams.x * exp(-(_CamPos.y - _FogParams.y) * falloff);
            float t = ray.y * falloff;
            float integral = abs(t) > 1e-3 ? (1.0 - exp(-t)) / t : 1.0;
            float fogAmount = startD * integral * dist;
            // A ground fog band: a slab of thicker air below the band top (dawn, the Frostline).
            if (_FogBand.x > 0.0)
            {
                float top = _FogBand.y, thick = max(_FogBand.z, 0.5);
                float bandD = _FogBand.x * exp(-max(0.0, _CamPos.y - top) / thick);
                float tb = ray.y / thick;
                float integralB = abs(tb) > 1e-3 ? (1.0 - exp(-tb)) / tb : 1.0;
                fogAmount += bandD * integralB * dist;
            }
            float f = 1.0 - exp(-fogAmount);
            c = lerp(c, _FogColor.rgb, saturate(f));
        }
        return float4(c, 1.0);
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragMask
            #pragma target 3.0
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragRadial
            #pragma target 3.0
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragComposite
            #pragma target 3.0
            ENDCG }
    }
    Fallback Off
}
