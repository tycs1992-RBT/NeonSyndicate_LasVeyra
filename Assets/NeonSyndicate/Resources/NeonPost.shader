// Neon Syndicate post-processing for the Built-in render pipeline (driven by NeonPost.cs through OnRenderImage).
// Passes: 0 bloom prefilter, 1 bloom downsample, 2 bloom upsample, 3 ambient occlusion, 4 occlusion blur,
// 5 composite (camera motion blur, chromatic aberration, occlusion, bloom, ACES tonemap, grade, vignette, grain),
// 6 FXAA with a light sharpen, 7 scene luminance for eye adaptation, 8 eye adaptation over time.
// Works in linear and gamma colour space.
Shader "Hidden/NeonSyndicate/Post"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _BloomBase;
    sampler2D _BloomTex;
    sampler2D _AOTex;
    float4 _AOTex_TexelSize;
    sampler2D_float _CameraDepthTexture;

    float4 _Threshold;      // x threshold, y knee, z clamp
    float _Scatter;
    float _BloomIntensity;
    float3 _BloomTint;
    float _Exposure;
    float _Saturation;
    float _Contrast;
    float3 _Lift;
    float3 _Gamma;
    float3 _Gain;
    float3 _ShadowTint;
    float3 _HighlightTint;
    float _Vignette;
    float _Chromatic;
    float _Grain;
    float _GrainTime;
    float _AOStrength;
    float _AORadius;
    float _MotionBlur;
    float _Sharpen;
    float2 _BlurDir;
    float4 _Proj;           // x P00, y P11
    float4x4 _ViewToWorld;
    float4x4 _PrevViewProj;
    sampler2D _LumTex;
    sampler2D _AdaptTex;
    float4 _AdaptRate;      // x blend when the scene got brighter, y when it got darker, z mip of the 1x1 level
    float4 _AutoExposure;   // x strength, y key (log2 of the target average), z min EV, w max EV

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

    float3 ToLinear(float3 c)
    {
        #ifdef UNITY_COLORSPACE_GAMMA
        return GammaToLinearSpace(c);
        #else
        return c;
        #endif
    }

    float3 ToOutput(float3 c)
    {
        #ifdef UNITY_COLORSPACE_GAMMA
        return LinearToGammaSpace(c);
        #else
        return c;
        #endif
    }

    float Luma(float3 c) { return dot(c, float3(0.2126, 0.7152, 0.0722)); }

    float3 SampleLinear(float2 uv) { return ToLinear(tex2Dlod(_MainTex, float4(uv, 0, 0)).rgb); }
    float3 Tap(float2 uv) { return tex2Dlod(_MainTex, float4(uv, 0, 0)).rgb; }

    // ---------------------------------------------------------------- bloom

    float3 Prefilter(float3 c)
    {
        c = min(c, _Threshold.z);
        float br = max(c.r, max(c.g, c.b));
        float soft = clamp(br - _Threshold.x + _Threshold.y, 0.0, 2.0 * _Threshold.y);
        soft = soft * soft / (4.0 * _Threshold.y + 1e-4);
        float contrib = max(soft, br - _Threshold.x) / max(br, 1e-4);
        return c * contrib;
    }

    float4 fragPrefilter(v2f i) : SV_Target
    {
        // Karis average of four taps: bright single pixels (fireflies) cannot flicker the bloom.
        float2 t = _MainTex_TexelSize.xy;
        float3 a = SampleLinear(i.uv + t * float2(-1, -1));
        float3 b = SampleLinear(i.uv + t * float2(1, -1));
        float3 c = SampleLinear(i.uv + t * float2(-1, 1));
        float3 d = SampleLinear(i.uv + t * float2(1, 1));
        float wa = 1.0 / (1.0 + Luma(a)), wb = 1.0 / (1.0 + Luma(b)), wc = 1.0 / (1.0 + Luma(c)), wd = 1.0 / (1.0 + Luma(d));
        float3 s = (a * wa + b * wb + c * wc + d * wd) / (wa + wb + wc + wd);
        return float4(Prefilter(s), 1.0);
    }

    float4 fragDown(v2f i) : SV_Target
    {
        float2 t = _MainTex_TexelSize.xy;
        float3 s = Tap(i.uv) * 4.0;
        s += Tap(i.uv + t * float2(-1, -1));
        s += Tap(i.uv + t * float2(1, -1));
        s += Tap(i.uv + t * float2(-1, 1));
        s += Tap(i.uv + t * float2(1, 1));
        return float4(s / 8.0, 1.0);
    }

    float4 fragUp(v2f i) : SV_Target
    {
        // Tent filter on the smaller mip, added onto this level.
        float2 t = _MainTex_TexelSize.xy;
        float3 s = Tap(i.uv + t * float2(-1, 0)) * 2.0;
        s += Tap(i.uv + t * float2(1, 0)) * 2.0;
        s += Tap(i.uv + t * float2(0, -1)) * 2.0;
        s += Tap(i.uv + t * float2(0, 1)) * 2.0;
        s += Tap(i.uv + t * float2(-1, -1));
        s += Tap(i.uv + t * float2(1, -1));
        s += Tap(i.uv + t * float2(-1, 1));
        s += Tap(i.uv + t * float2(1, 1));
        s /= 12.0;
        float3 base = tex2Dlod(_BloomBase, float4(i.uv, 0, 0)).rgb;
        return float4(base + s * _Scatter, 1.0);
    }

    // ---------------------------------------------------------------- ambient occlusion

    float EyeDepth(float2 uv)
    {
        return LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv, 0, 0)));
    }

    float3 ViewPos(float2 uv, float eye)
    {
        return float3((uv * 2.0 - 1.0) / _Proj.xy * eye, -eye);
    }

    float Noise(float2 pixel)
    {
        return frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
    }

    float4 fragAO(v2f i) : SV_Target
    {
        // This pass reads only the depth texture and writes a render texture, which share one orientation.
        float2 uv = i.uv;
        float eye = EyeDepth(uv);
        if (eye > 180.0) return 1.0;
        float3 p = ViewPos(uv, eye);
        // Normal from the neighbour with the smaller depth step (keeps silhouettes clean).
        float2 t = abs(_MainTex_TexelSize.xy) * 2.0;
        float3 pr = ViewPos(uv + float2(t.x, 0), EyeDepth(uv + float2(t.x, 0)));
        float3 pl = ViewPos(uv - float2(t.x, 0), EyeDepth(uv - float2(t.x, 0)));
        float3 pu = ViewPos(uv + float2(0, t.y), EyeDepth(uv + float2(0, t.y)));
        float3 pd = ViewPos(uv - float2(0, t.y), EyeDepth(uv - float2(0, t.y)));
        float3 dx = abs(pr.z - p.z) < abs(p.z - pl.z) ? pr - p : p - pl;
        float3 dy = abs(pu.z - p.z) < abs(p.z - pd.z) ? pu - p : p - pd;
        float3 n = normalize(cross(dy, dx));
        if (n.z < 0) n = -n;
        float ang = Noise(i.pos.xy) * 6.2831853;
        float cs = cos(ang), sn = sin(ang);
        float radius = _AORadius * (1.0 + eye * 0.012);
        float occ = 0.0;
        const int SAMPLES = 10;
        [unroll]
        for (int k = 0; k < SAMPLES; k++)
        {
            float fk = (k + 0.5) / SAMPLES;
            float a = fk * 15.0 + ang;
            float r = sqrt(fk);
            float3 d = float3(cos(a) * r, sin(a) * r, sqrt(saturate(1.0 - r * r)));
            // Rotate the hemisphere sample around the normal.
            float3 up = abs(n.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
            float3 tx = normalize(cross(up, n));
            float3 ty = cross(n, tx);
            float3 dir = tx * d.x + ty * d.y + n * d.z;
            float3 s = p + dir * radius * lerp(0.25, 1.0, fk * fk);
            float2 suv = (s.xy / -s.z) * _Proj.xy * 0.5 + 0.5;
            float sceneEye = EyeDepth(suv);
            float diff = (-s.z) - sceneEye;
            float range = saturate(radius / max(abs(eye - sceneEye), 1e-3));
            occ += (diff > 0.03 ? 1.0 : 0.0) * range;
        }
        float ao = saturate(1.0 - occ / SAMPLES * _AOStrength);
        return float4(ao, eye, 0, 1);
    }

    float4 fragAOBlur(v2f i) : SV_Target
    {
        float2 c = tex2Dlod(_MainTex, float4(i.uv, 0, 0)).rg;
        float sum = c.r, wsum = 1.0;
        float2 step = _MainTex_TexelSize.xy * _BlurDir;
        [unroll]
        for (int k = -3; k <= 3; k++)
        {
            if (k == 0) continue;
            float2 s = tex2Dlod(_MainTex, float4(i.uv + step * k, 0, 0)).rg;
            float w = exp(-k * k * 0.18) * saturate(1.0 - abs(s.g - c.g) / max(c.g * 0.05, 0.05));
            sum += s.r * w;
            wsum += w;
        }
        return float4(sum / wsum, c.g, 0, 1);
    }

    // ---------------------------------------------------------------- composite

    float3 ACES(float3 x)
    {
        x = max(x, 0.0);
        return saturate((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14));
    }

    float4 fragComposite(v2f i) : SV_Target
    {
        float2 uv = i.uv;
        float2 fromCenter = uv - 0.5;
        float3 c;
        // Chromatic aberration grows toward the edges.
        if (_Chromatic > 0.0)
        {
            float2 off = fromCenter * dot(fromCenter, fromCenter) * _Chromatic;
            c = float3(SampleLinear(uv - off).r, SampleLinear(uv).g, SampleLinear(uv + off).b);
        }
        else c = SampleLinear(uv);
        // Light sharpen (unsharp mask on the HDR image).
        if (_Sharpen > 0.0)
        {
            float2 t = _MainTex_TexelSize.xy;
            float3 blur = (SampleLinear(uv + float2(t.x, 0)) + SampleLinear(uv - float2(t.x, 0)) + SampleLinear(uv + float2(0, t.y)) + SampleLinear(uv - float2(0, t.y))) * 0.25;
            float3 sharp = c + (c - blur) * _Sharpen;
            c = clamp(sharp, c * 0.6, c * 1.6 + 0.02);
        }
        // Camera motion blur by reprojection; near objects (the player) stay sharp.
        if (_MotionBlur > 0.0)
        {
            float eye = EyeDepth(i.uvDepth);
            float3 vp = ViewPos(i.uvDepth, eye);
            float4 world = mul(_ViewToWorld, float4(vp, 1.0));
            float4 prev = mul(_PrevViewProj, world);
            float2 prevUV = prev.xy / max(prev.w, 1e-4) * 0.5 + 0.5;
            float2 vel = (i.uvDepth - prevUV) * _MotionBlur * smoothstep(5.0, 12.0, eye);
            float len = length(vel);
            if (len > 0.04) vel *= 0.04 / len;
            #if UNITY_UV_STARTS_AT_TOP
            if (_MainTex_TexelSize.y < 0) vel.y = -vel.y;
            #endif
            if (len > 0.0015)
            {
                float3 acc = c;
                [unroll]
                for (int k = 1; k < 8; k++) acc += SampleLinear(uv - vel * (k / 7.0 - 0.5));
                c = acc / 8.0;
            }
        }
        if (_AOStrength > 0.0)
        {
            float ao = tex2Dlod(_AOTex, float4(i.uvDepth, 0, 0)).r;
            c *= lerp(1.0, ao, 0.85);
        }
        // Bloom, AO and depth are render textures; with MSAA the screen source can be stored the other way up,
        // which uvDepth corrects for.
        c += tex2Dlod(_BloomTex, float4(i.uvDepth, 0, 0)).rgb * _BloomIntensity * _BloomTint;
        // Filmic tonemap and grade.
        // Eye adaptation: dark scenes open up and bright ones settle, like a camera finding its exposure.
        float ev = 0.0;
        if (_AutoExposure.x > 0.0)
        {
            float avgLog = tex2Dlod(_AdaptTex, float4(0.5, 0.5, 0, 0)).r;
            ev = clamp(_AutoExposure.y - avgLog, _AutoExposure.z, _AutoExposure.w) * _AutoExposure.x;
        }
        c = ACES(c * _Exposure * exp2(ev));
        float l = Luma(c);
        c = lerp(l.xxx, c, _Saturation);
        // Contrast pivots on middle grey with a power curve, so shadows deepen but never clip to black.
        c = saturate(0.18 * pow(max(c, 0.0) / 0.18, _Contrast));
        c = c * _Gain + _Lift * (1.0 - c);
        c = pow(max(c, 0.0), 1.0 / max(_Gamma, 0.01));
        float sh = 1.0 - smoothstep(0.0, 0.45, l);
        float hi = smoothstep(0.45, 1.0, l);
        c += _ShadowTint * sh + _HighlightTint * hi;
        c *= saturate(1.0 - dot(fromCenter, fromCenter) * _Vignette * 2.2);
        // Grain, stronger in the shadows; a little dither against banding.
        float n = Noise(i.pos.xy + _GrainTime * float2(113.0, 71.0)) - 0.5;
        c += n * _Grain * (1.0 - l * 0.7);
        c += (Noise(i.pos.xy * 1.37) - 0.5) / 255.0;
        return float4(ToOutput(saturate(c)), 1.0);
    }

    // ---------------------------------------------------------------- eye adaptation

    float4 fragLum(v2f i) : SV_Target
    {
        // Four taps per texel of the small luminance target; log2 so the mip chain averages to a geometric mean.
        // The centre of the screen counts more than the edges.
        // The target is 64 x 64, so these taps sit a quarter of a target texel from its centre.
        float2 t = float2(1.0 / 256.0, 1.0 / 256.0);
        float lsum = clamp(log2(Luma(SampleLinear(i.uv + float2(-t.x, -t.y))) + 1e-4), -13.0, 5.0);
        lsum += clamp(log2(Luma(SampleLinear(i.uv + float2(t.x, -t.y))) + 1e-4), -13.0, 5.0);
        lsum += clamp(log2(Luma(SampleLinear(i.uv + float2(-t.x, t.y))) + 1e-4), -13.0, 5.0);
        lsum += clamp(log2(Luma(SampleLinear(i.uv + float2(t.x, t.y))) + 1e-4), -13.0, 5.0);
        float2 d = i.uv - 0.5;
        float w = saturate(1.0 - dot(d, d) * 2.4) + 0.15;
        return float4(lsum * 0.25 * w, w, 0, 1);
    }

    float4 fragAdapt(v2f i) : SV_Target
    {
        float2 s = tex2Dlod(_LumTex, float4(0.5, 0.5, 0, _AdaptRate.z)).rg;
        float target = s.x / max(s.y, 1e-4);
        float prev = tex2Dlod(_MainTex, float4(0.5, 0.5, 0, 0)).r;
        float rate = target > prev ? _AdaptRate.x : _AdaptRate.y;
        return float4(lerp(prev, target, rate), 0, 0, 1);
    }

    // ---------------------------------------------------------------- FXAA

    float LumaG(float3 c) { return sqrt(dot(c, float3(0.299, 0.587, 0.114))); }

    float4 fragFXAA(v2f i) : SV_Target
    {
        float2 t = _MainTex_TexelSize.xy;
        float3 nw = Tap(i.uv + float2(-1, -1) * t);
        float3 ne = Tap(i.uv + float2(1, -1) * t);
        float3 sw = Tap(i.uv + float2(-1, 1) * t);
        float3 se = Tap(i.uv + float2(1, 1) * t);
        float3 m = Tap(i.uv);
        float lnw = LumaG(nw), lne = LumaG(ne), lsw = LumaG(sw), lse = LumaG(se), lm = LumaG(m);
        float lmin = min(lm, min(min(lnw, lne), min(lsw, lse)));
        float lmax = max(lm, max(max(lnw, lne), max(lsw, lse)));
        float2 dir;
        dir.x = -((lnw + lne) - (lsw + lse));
        dir.y = (lnw + lsw) - (lne + lse);
        float reduce = max((lnw + lne + lsw + lse) * 0.03125, 1.0 / 128.0);
        float rcpMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + reduce);
        dir = clamp(dir * rcpMin, -8.0, 8.0) * t;
        float3 a = 0.5 * (Tap(i.uv + dir * (1.0 / 3.0 - 0.5)) + Tap(i.uv + dir * (2.0 / 3.0 - 0.5)));
        float3 b = a * 0.5 + 0.25 * (Tap(i.uv - dir * 0.5) + Tap(i.uv + dir * 0.5));
        float lb = LumaG(b);
        float3 aa = (lb < lmin || lb > lmax) ? a : b;
        // Contrast-aware sharpen on top, so the image stays crisp after anti-aliasing.
        float3 cross4 = (Tap(i.uv + float2(t.x, 0)) + Tap(i.uv - float2(t.x, 0)) + Tap(i.uv + float2(0, t.y)) + Tap(i.uv - float2(0, t.y))) * 0.25;
        float edge = saturate((lmax - lmin) * 4.0);
        aa += (m - cross4) * _Sharpen * (1.0 - edge);
        return float4(saturate(aa), 1.0);
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragPrefilter
            #pragma target 3.0
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragDown
            #pragma target 3.0
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragUp
            #pragma target 3.0
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragAO
            #pragma target 3.5
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragAOBlur
            #pragma target 3.0
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragComposite
            #pragma target 3.5
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragFXAA
            #pragma target 3.0
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragLum
            #pragma target 3.0
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragAdapt
            #pragma target 3.0
            ENDCG }
    }
    Fallback Off
}
