// Neon Syndicate post-processing for the Built-in render pipeline (driven by NeonPost.cs through OnRenderImage).
// Passes: 0 bloom prefilter, 1 bloom downsample, 2 bloom upsample, 3 ambient occlusion (horizon based), 4 occlusion
// blur, 5 composite (chromatic aberration, sharpen, motion blur, occlusion, bloom, eye adaptation, ACES tonemap, grade,
// split tone, vignette, grain), 6 FXAA (quality 39 style) with a light sharpen, 7 scene luminance for eye adaptation,
// 8 eye adaptation over time, 9 temporal anti-aliasing resolve, 10 occlusion applied before the temporal resolve,
// 11 depth of field prefilter (half resolution, circle of confusion in alpha), 12 depth of field one-sided gather,
// 13 depth of field hexagon from two gathers, 14 depth of field composite at full resolution.
// Orientation rule: _MainTex is read at uv; every other texture (depth, motion vectors, our own render textures) is
// read at uvDepth, which the vertex shader flips when the camera target is stored upside down (multisampling).
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
    sampler2D_half _CameraMotionVectorsTexture;
    sampler2D _HistoryTex;
    float4 _HistoryTex_TexelSize;
    sampler2D _DofTex;
    sampler2D _DofTexB;

    float4 _Threshold;      // x threshold, y knee, z clamp
    float _Scatter;
    float _BloomIntensity;
    float3 _BloomTint;
    float _Exposure;
    float _PreExposure;     // multiplier before the ACES curve (PostGrade.AcesPreExposure)
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
    float _AOStrength;      // 1 when the composite applies the occlusion, 0 when it was applied before the temporal resolve (or is off)
    float4 _AOParams;       // x radius in metres, y strength exponent, z slices, w steps per side
    float4 _AOParams2;      // x direction phase 0..1, y largest radius in pixels, zw texel size of the occlusion target
    float4 _MotionBlurParams; // x strength, y mode (0 off, 1 camera reprojection, 2 motion vectors), z samples, w longest blur in uv
    float _Sharpen;
    float2 _BlurDir;
    float4 _Proj;           // x P00, y P11
    float4x4 _ViewToWorld;
    float4x4 _PrevViewProj;
    sampler2D _LumTex;
    sampler2D _AdaptTex;
    float4 _AdaptRate;      // x blend when the scene got brighter, y when it got darker, z mip of the 1x1 level
    float4 _AutoExposure;   // x strength, y key (log2 of the target average), z min EV, w max EV
    float4 _TaaParams;      // x history valid, y feedback at rest, z feedback in motion
    float4 _DofParams;      // x focus distance, y range (fraction of the focus distance to full blur), z largest radius in half-res pixels, w taps per gather
    float4 _DofDir;         // xy first gather direction, zw second (hexagon pass)

    #define PI 3.14159265
    #define HALF_PI 1.57079633

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
    float4 Tap4(float2 uv) { return tex2Dlod(_MainTex, float4(uv, 0, 0)); }

    // Kills NaN and infinity: min with a NaN returns the other operand on every desktop GPU.
    float3 Finite(float3 c) { return -min(-c, 0.0); }

    float Noise(float2 pixel)
    {
        return frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
    }

    // ---------------------------------------------------------------- depth helpers

    float EyeDepth(float2 uv)
    {
        return LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv, 0, 0)));
    }

    float3 ViewPos(float2 uv, float eye)
    {
        return float3((uv * 2.0 - 1.0) / _Proj.xy * eye, -eye);
    }

    // Occlusion upsample with depth weights: the four nearest half resolution texels, each trusted only where its
    // depth agrees with the full resolution pixel, so the darkening never bleeds across silhouettes.
    float SampleOcclusion(float2 uv, float eye)
    {
        float2 size = _AOTex_TexelSize.zw;
        float2 p = uv * size - 0.5;
        float2 f = frac(p);
        float2 base = (floor(p) + 0.5) * _AOTex_TexelSize.xy;
        float2 t = _AOTex_TexelSize.xy;
        float4 w = float4((1.0 - f.x) * (1.0 - f.y), f.x * (1.0 - f.y), (1.0 - f.x) * f.y, f.x * f.y);
        float2 s0 = tex2Dlod(_AOTex, float4(base, 0, 0)).rg;
        float2 s1 = tex2Dlod(_AOTex, float4(base + float2(t.x, 0), 0, 0)).rg;
        float2 s2 = tex2Dlod(_AOTex, float4(base + float2(0, t.y), 0, 0)).rg;
        float2 s3 = tex2Dlod(_AOTex, float4(base + t, 0, 0)).rg;
        float tol = eye * 0.06 + 0.05;
        float4 d = float4(s0.g, s1.g, s2.g, s3.g);
        float4 k = w * saturate(1.0 - abs(d - eye) / tol);
        float sum = dot(k, float4(s0.r, s1.r, s2.r, s3.r));
        float wsum = dot(k, 1.0);
        float plain = dot(w, float4(s0.r, s1.r, s2.r, s3.r));
        return wsum > 1e-3 ? sum / wsum : plain;
    }

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
        return float4(Prefilter(Finite(s)), 1.0);
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

    // ---------------------------------------------------------------- ambient occlusion (horizon based)

    float3 ReconstructNormal(float2 uv, float3 p, float2 t)
    {
        // Normal from the neighbour with the smaller depth step on each axis (keeps silhouettes clean).
        float3 pr = ViewPos(uv + float2(t.x, 0), EyeDepth(uv + float2(t.x, 0)));
        float3 pl = ViewPos(uv - float2(t.x, 0), EyeDepth(uv - float2(t.x, 0)));
        float3 pu = ViewPos(uv + float2(0, t.y), EyeDepth(uv + float2(0, t.y)));
        float3 pd = ViewPos(uv - float2(0, t.y), EyeDepth(uv - float2(0, t.y)));
        float3 dx = abs(pr.z - p.z) < abs(p.z - pl.z) ? pr - p : p - pl;
        float3 dy = abs(pu.z - p.z) < abs(p.z - pd.z) ? pu - p : p - pd;
        float3 n = normalize(cross(dy, dx));
        if (n.z < 0) n = -n;
        return n;
    }

    // Highest horizon along one side of a slice: the cosine between the sample vector and the view vector, eased
    // back toward open sky as the sample leaves the world-space radius (so distant geometry cannot darken a surface).
    float HorizonSide(float2 uv, float2 stepUv, float3 p, float3 v, float radius, int steps, float jitter)
    {
        float horizon = -1.0;
        for (int k = 0; k < steps; k++)
        {
            float t = (k + jitter) / steps;
            t = max(t * t, 0.08);
            float2 suv = uv + stepUv * t;
            float3 sp = ViewPos(suv, EyeDepth(suv));
            float3 ds = sp - p;
            float dl = max(length(ds), 1e-4);
            float c = dot(ds, v) / dl;
            float w = saturate(1.0 - dl * dl / (radius * radius));
            horizon = max(horizon, lerp(-1.0, c, w));
        }
        return horizon;
    }

    float4 fragAO(v2f i) : SV_Target
    {
        float2 uv = i.uvDepth;
        float eye = EyeDepth(uv);
        if (eye > 180.0) return float4(1.0, eye, 0, 1);
        float3 p = ViewPos(uv, eye);
        float2 texel = _AOParams2.zw;
        float3 n = ReconstructNormal(uv, p, texel * 2.0);
        float3 v = normalize(-p);
        float radius = _AOParams.x;
        // One metre at this depth in uv, capped in pixels so close-up surfaces stay cheap and coherent.
        float2 radUv = radius * _Proj.xy * 0.5 / eye;
        float rpx = radUv.y / texel.y;
        if (rpx < 1.0) return float4(1.0, eye, 0, 1);
        if (rpx > _AOParams2.y) radUv *= _AOParams2.y / rpx;
        int slices = (int)_AOParams.z;
        int steps = (int)_AOParams.w;
        float phase = _AOParams2.x;
        float noise = Noise(i.pos.xy);
        float jitter = frac(Noise(i.pos.yx * 1.7 + 13.0) + phase * 0.61803);
        float ang0 = (noise + phase) * PI;
        float vis = 0.0;
        for (int s = 0; s < slices; s++)
        {
            float phi = ang0 + s * PI / slices;
            float2 dir = float2(cos(phi), sin(phi));
            float3 d3 = float3(dir, 0.0);
            float3 ortho = normalize(d3 - dot(d3, v) * v);
            float3 axis = cross(ortho, v);
            // The normal projected into the slice plane, and its signed angle from the view vector.
            float3 np = n - axis * dot(n, axis);
            float npLen = length(np);
            if (npLen < 1e-4) continue;
            np /= npLen;
            float cosN = clamp(dot(np, v), -1.0, 1.0);
            float nAng = sign(dot(np, ortho)) * acos(cosN);
            float h1cos = HorizonSide(uv, -dir * radUv, p, v, radius, steps, jitter);
            float h2cos = HorizonSide(uv, dir * radUv, p, v, radius, steps, 1.0 - jitter);
            float h1 = -acos(clamp(h1cos, -1.0, 1.0));
            float h2 = acos(clamp(h2cos, -1.0, 1.0));
            h1 = nAng + max(h1 - nAng, -HALF_PI);
            h2 = nAng + min(h2 - nAng, HALF_PI);
            float sinN = sin(nAng);
            float a1 = 0.25 * (-cos(2.0 * h1 - nAng) + cosN + 2.0 * h1 * sinN);
            float a2 = 0.25 * (-cos(2.0 * h2 - nAng) + cosN + 2.0 * h2 * sinN);
            vis += npLen * (a1 + a2);
        }
        vis = saturate(vis / slices);
        float ao = pow(vis, _AOParams.y);
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

    // Occlusion multiplied onto the HDR image before the temporal resolve (the resolve then settles its noise).
    float4 fragApplyAO(v2f i) : SV_Target
    {
        float4 c = Tap4(i.uv);
        float eye = EyeDepth(i.uvDepth);
        float ao = SampleOcclusion(i.uvDepth, eye);
        c.rgb = ToOutput(ToLinear(c.rgb) * lerp(1.0, ao, 0.85));
        return c;
    }

    // ---------------------------------------------------------------- temporal anti-aliasing

    float3 RGBToYCoCg(float3 c)
    {
        return float3(0.25 * c.r + 0.5 * c.g + 0.25 * c.b, 0.5 * c.r - 0.5 * c.b, -0.25 * c.r + 0.5 * c.g - 0.25 * c.b);
    }

    float3 YCoCgToRGB(float3 c)
    {
        return float3(c.x + c.y - c.z, c.x + c.z, c.x - c.y - c.z);
    }

    float3 ClipToBox(float3 boxMin, float3 boxMax, float3 q)
    {
        float3 centre = 0.5 * (boxMax + boxMin);
        float3 extent = 0.5 * (boxMax - boxMin) + 1e-5;
        float3 v = q - centre;
        float3 unit = abs(v / extent);
        float m = max(unit.x, max(unit.y, unit.z));
        return m > 1.0 ? centre + v / m : q;
    }

    float3 HistoryTap(float2 uv)
    {
        return tex2Dlod(_HistoryTex, float4(uv, 0, 0)).rgb;
    }

    // Catmull-Rom history fetch (nine taps): keeps the accumulated image sharp instead of smearing it a little every frame.
    float3 HistoryCatmullRom(float2 uv)
    {
        float2 size = _HistoryTex_TexelSize.zw, texel = _HistoryTex_TexelSize.xy;
        float2 samplePos = uv * size;
        float2 texPos1 = floor(samplePos - 0.5) + 0.5;
        float2 f = samplePos - texPos1;
        float2 w0 = f * (-0.5 + f * (1.0 - 0.5 * f));
        float2 w1 = 1.0 + f * f * (-2.5 + 1.5 * f);
        float2 w2 = f * (0.5 + f * (2.0 - 1.5 * f));
        float2 w3 = f * f * (-0.5 + 0.5 * f);
        float2 w12 = w1 + w2;
        float2 offset12 = w2 / w12;
        float2 texPos0 = (texPos1 - 1.0) * texel;
        float2 texPos3 = (texPos1 + 2.0) * texel;
        float2 texPos12 = (texPos1 + offset12) * texel;
        float3 r = 0.0;
        r += HistoryTap(float2(texPos0.x, texPos0.y)) * w0.x * w0.y;
        r += HistoryTap(float2(texPos12.x, texPos0.y)) * w12.x * w0.y;
        r += HistoryTap(float2(texPos3.x, texPos0.y)) * w3.x * w0.y;
        r += HistoryTap(float2(texPos0.x, texPos12.y)) * w0.x * w12.y;
        r += HistoryTap(float2(texPos12.x, texPos12.y)) * w12.x * w12.y;
        r += HistoryTap(float2(texPos3.x, texPos12.y)) * w3.x * w12.y;
        r += HistoryTap(float2(texPos0.x, texPos3.y)) * w0.x * w3.y;
        r += HistoryTap(float2(texPos12.x, texPos3.y)) * w12.x * w3.y;
        r += HistoryTap(float2(texPos3.x, texPos3.y)) * w3.x * w3.y;
        return max(r, 0.0);
    }

    float4 fragTaa(v2f i) : SV_Target
    {
        float2 t = abs(_MainTex_TexelSize.xy);
        // Motion vector of the nearest surface in the 3x3 neighbourhood, so edges of moving objects drag their history correctly.
        float2 closest = i.uvDepth;
        float closestDepth = EyeDepth(i.uvDepth);
        [unroll]
        for (int y = -1; y <= 1; y++)
        {
            [unroll]
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0) continue;
                float2 suv = i.uvDepth + float2(x, y) * t;
                float d = EyeDepth(suv);
                if (d < closestDepth) { closestDepth = d; closest = suv; }
            }
        }
        float2 velocity = tex2Dlod(_CameraMotionVectorsTexture, float4(closest, 0, 0)).rg;
        float2 historyUv = i.uvDepth - velocity;

        // The new frame and its neighbourhood in YCoCg: mean and deviation give the box the history must fit.
        float3 centre = RGBToYCoCg(Finite(Tap(i.uv)));
        float3 m1 = centre, m2 = centre * centre;
        float3 boxMin = centre, boxMax = centre;
        [unroll]
        for (int yy = -1; yy <= 1; yy++)
        {
            [unroll]
            for (int xx = -1; xx <= 1; xx++)
            {
                if (xx == 0 && yy == 0) continue;
                float3 c = RGBToYCoCg(Finite(Tap(i.uv + float2(xx, yy) * t)));
                m1 += c;
                m2 += c * c;
                boxMin = min(boxMin, c);
                boxMax = max(boxMax, c);
            }
        }
        float3 mean = m1 / 9.0;
        float3 sigma = sqrt(max(m2 / 9.0 - mean * mean, 0.0));
        float velPixels = length(velocity * _MainTex_TexelSize.zw);
        // A tighter box when moving (less ghosting), a looser one at rest (less flicker).
        float gamma = lerp(1.25, 0.75, saturate(velPixels / 20.0));
        float3 lo = max(boxMin, mean - gamma * sigma);
        float3 hi = min(boxMax, mean + gamma * sigma);

        float3 history = RGBToYCoCg(HistoryCatmullRom(historyUv));
        history = ClipToBox(lo, hi, history);
        bool onScreen = all(historyUv >= 0.0) && all(historyUv <= 1.0);
        float feedback = _TaaParams.x > 0.5 && onScreen ? lerp(_TaaParams.y, _TaaParams.z, saturate(velPixels / 40.0)) : 0.0;
        // Luminance-weighted blend: a bright flickering highlight cannot dominate the average.
        float wc = (1.0 - feedback) / (1.0 + max(centre.x, 0.0));
        float wh = feedback / (1.0 + max(history.x, 0.0));
        float3 result = (centre * wc + history * wh) / max(wc + wh, 1e-5);
        return float4(max(YCoCgToRGB(result), 0.0), 1.0);
    }

    // ---------------------------------------------------------------- depth of field

    float CircleOfConfusion(float eye)
    {
        return clamp((eye - _DofParams.x) / (_DofParams.x * _DofParams.y), -1.0, 1.0);
    }

    float4 fragDofCoc(v2f i) : SV_Target
    {
        // Half resolution: the 2x2 source pixels averaged, with the circle of confusion of the nearest one in alpha
        // (a near sharp edge keeps its sharpness; a near blurred one keeps its spill).
        float2 t = _MainTex_TexelSize.xy * 0.5;
        float2 td = abs(t);
        float3 c = SampleLinear(i.uv + float2(-t.x, -t.y)) + SampleLinear(i.uv + float2(t.x, -t.y)) + SampleLinear(i.uv + float2(-t.x, t.y)) + SampleLinear(i.uv + float2(t.x, t.y));
        float e0 = EyeDepth(i.uvDepth + float2(-td.x, -td.y)), e1 = EyeDepth(i.uvDepth + float2(td.x, -td.y));
        float e2 = EyeDepth(i.uvDepth + float2(-td.x, td.y)), e3 = EyeDepth(i.uvDepth + float2(td.x, td.y));
        float coc = CircleOfConfusion(min(min(e0, e1), min(e2, e3)));
        return float4(Finite(c * 0.25), coc);
    }

    // One-sided gather along a direction: each tap counts only as far as its own circle of confusion reaches this
    // pixel. A tap behind the focus plane can reach no further than this pixel's own blur, so a blurred background
    // never leaks over a sharp foreground, while a tap in front spills freely. Alpha carries how blurred the result is.
    float4 GatherDir(sampler2D tex, float2 uv, float2 dirUv, float cocC, float radiusPx)
    {
        float4 centre = tex2Dlod(tex, float4(uv, 0, 0));
        float3 sum = centre.rgb;
        float wsum = 1.0;
        float blur = abs(cocC);
        int taps = (int)_DofParams.w;
        float ownReach = abs(cocC) * radiusPx;
        for (int k = 1; k <= taps; k++)
        {
            float f = k / (float)taps;
            float distPx = f * radiusPx;
            float4 s = tex2Dlod(tex, float4(uv + dirUv * f, 0, 0));
            float reach = s.a < 0.0 ? abs(s.a) * radiusPx : min(s.a * radiusPx, ownReach + 0.5);
            float w = saturate(reach - distPx + 1.0);
            sum += s.rgb * w;
            wsum += w;
            if (s.a < 0.0) blur = max(blur, w * abs(s.a));
        }
        return float4(sum / wsum, blur);
    }

    float4 fragDofDir(v2f i) : SV_Target
    {
        float radiusPx = _DofParams.z;
        float2 dirUv = _DofDir.xy * radiusPx * _MainTex_TexelSize.xy;
        float cocC = Tap4(i.uv).a;
        float4 g = GatherDir(_MainTex, i.uv, dirUv, cocC, radiusPx);
        // Keep the signed circle for the next gather; the spill measure travels in the hexagon pass instead.
        return float4(g.rgb, cocC);
    }

    float4 fragDofHex(v2f i) : SV_Target
    {
        // _MainTex: the source gathered upward. _DofTexB: the source gathered down-left. The hexagon is the upward
        // gather spread down-left, plus the sum of both spread down-right (three rhombi).
        float radiusPx = _DofParams.z;
        float2 d1 = _DofDir.xy * radiusPx * _MainTex_TexelSize.xy;
        float2 d2 = _DofDir.zw * radiusPx * _MainTex_TexelSize.xy;
        float cocC = Tap4(i.uv).a;
        float4 a = GatherDir(_MainTex, i.uv, d1, cocC, radiusPx);
        float4 b = GatherDir(_MainTex, i.uv, d2, cocC, radiusPx);
        float4 c = GatherDir(_DofTexB, i.uv, d2, cocC, radiusPx);
        float blur = max(a.a, max(b.a, c.a));
        return float4((a.rgb + b.rgb + c.rgb) / 3.0, blur);
    }

    float4 fragDofComposite(v2f i) : SV_Target
    {
        float4 sharp = Tap4(i.uv);
        float4 blurred = tex2Dlod(_DofTex, float4(i.uvDepth, 0, 0));
        float eye = EyeDepth(i.uvDepth);
        float coc = CircleOfConfusion(eye);
        // Fully the bokeh once the blur radius passes about a pixel and a half of the half resolution buffer; the
        // spill measure lets out-of-focus foreground cover a sharp pixel behind it.
        float own = abs(coc) * _DofParams.z;
        float spill = blurred.a * _DofParams.z;
        float t = saturate(max(own, spill) / 1.5);
        // The half resolution chain works in linear colour; the result goes back to the source's colour space.
        return float4(ToOutput(lerp(ToLinear(sharp.rgb), blurred.rgb, t)), sharp.a);
    }

    // ---------------------------------------------------------------- composite

    // The fitted ACES curve (RRT and ODT), sRGB in and out: highlights roll off and bright saturated colours
    // desaturate toward white the way the reference transform does. PostGrade.AcesFitted is the same maths.
    static const float3x3 AcesInputMat = { 0.59719, 0.35458, 0.04823, 0.07600, 0.90834, 0.01566, 0.02840, 0.13383, 0.83777 };
    static const float3x3 AcesOutputMat = { 1.60475, -0.53108, -0.07367, -0.10208, 1.10813, -0.00605, -0.00327, -0.07276, 1.07602 };

    float3 RrtAndOdtFit(float3 v)
    {
        float3 a = v * (v + 0.0245786) - 0.000090537;
        float3 b = v * (0.983729 * v + 0.4329510) + 0.238081;
        return a / b;
    }

    float3 ACES(float3 x)
    {
        x = max(x, 0.0);
        x = mul(AcesInputMat, x);
        x = RrtAndOdtFit(x);
        x = mul(AcesOutputMat, x);
        return saturate(x);
    }

    // Reconstruction weights for the motion vector blur: a sample counts when its own blur reaches this pixel and it
    // is in front, when this pixel's blur reaches it and it is behind, or when both move alike.
    float Cone(float dist, float v) { return saturate(1.0 - dist / max(v, 1e-5)); }
    float Cylinder(float dist, float v) { return 1.0 - smoothstep(0.95 * v, 1.05 * v + 1e-5, dist); }
    float InFront(float za, float zb) { return saturate(1.0 - (za - zb) / 0.5); }

    float3 MotionBlurVectors(float2 uv, float2 uvDepth, float3 c, float2 pixel)
    {
        float2 vel = tex2Dlod(_CameraMotionVectorsTexture, float4(uvDepth, 0, 0)).rg * _MotionBlurParams.x;
        float len = length(vel);
        if (len > _MotionBlurParams.w) { vel *= _MotionBlurParams.w / len; len = _MotionBlurParams.w; }
        if (len < 0.0015) return c;
        float2 velMain = vel;
        #if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0) velMain.y = -velMain.y;
        #endif
        float zc = EyeDepth(uvDepth);
        int n = (int)_MotionBlurParams.z;
        float j = Noise(pixel) - 0.5;
        float3 acc = c;
        float wsum = 1.0;
        for (int k = 1; k < n; k++)
        {
            float tt = (k + j) / (n - 1.0) - 0.5;
            float2 sm = uv - velMain * tt;
            float2 sd = uvDepth - vel * tt;
            float dist = abs(tt) * len;
            float zs = EyeDepth(sd);
            float vs = length(tex2Dlod(_CameraMotionVectorsTexture, float4(sd, 0, 0)).rg * _MotionBlurParams.x);
            vs = min(vs, _MotionBlurParams.w);
            float w = InFront(zs, zc) * Cone(dist, vs) + InFront(zc, zs) * Cone(dist, len) + Cylinder(dist, vs) * Cylinder(dist, len) * 2.0;
            acc += SampleLinear(sm) * w;
            wsum += w;
        }
        return acc / wsum;
    }

    float3 MotionBlurReprojection(float2 uv, float2 uvDepth, float3 c)
    {
        // Camera motion blur by reprojection; near objects (the player) stay sharp.
        float eye = EyeDepth(uvDepth);
        float3 vp = ViewPos(uvDepth, eye);
        float4 world = mul(_ViewToWorld, float4(vp, 1.0));
        float4 prev = mul(_PrevViewProj, world);
        float2 prevUV = prev.xy / max(prev.w, 1e-4) * 0.5 + 0.5;
        float2 vel = (uvDepth - prevUV) * _MotionBlurParams.x * smoothstep(5.0, 12.0, eye);
        float len = length(vel);
        if (len > _MotionBlurParams.w) vel *= _MotionBlurParams.w / len;
        #if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0) vel.y = -vel.y;
        #endif
        if (len <= 0.0015) return c;
        float3 acc = c;
        [unroll]
        for (int k = 1; k < 8; k++) acc += SampleLinear(uv - vel * (k / 7.0 - 0.5));
        return acc / 8.0;
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
        c = Finite(c);
        // Light sharpen (unsharp mask on the HDR image).
        if (_Sharpen > 0.0)
        {
            float2 t = _MainTex_TexelSize.xy;
            float3 blur = (SampleLinear(uv + float2(t.x, 0)) + SampleLinear(uv - float2(t.x, 0)) + SampleLinear(uv + float2(0, t.y)) + SampleLinear(uv - float2(0, t.y))) * 0.25;
            float3 sharp = c + (c - blur) * _Sharpen;
            c = clamp(sharp, c * 0.6, c * 1.6 + 0.02);
        }
        if (_MotionBlurParams.y > 1.5) c = MotionBlurVectors(uv, i.uvDepth, c, i.pos.xy);
        else if (_MotionBlurParams.y > 0.5) c = MotionBlurReprojection(uv, i.uvDepth, c);
        if (_AOStrength > 0.0)
        {
            float ao = SampleOcclusion(i.uvDepth, EyeDepth(i.uvDepth));
            c *= lerp(1.0, ao, 0.85);
        }
        // Bloom, AO and depth are render textures; with MSAA the screen source can be stored the other way up,
        // which uvDepth corrects for.
        c += tex2Dlod(_BloomTex, float4(i.uvDepth, 0, 0)).rgb * _BloomIntensity * _BloomTint;
        // Eye adaptation: dark scenes open up and bright ones settle, like a camera finding its exposure.
        float ev = 0.0;
        if (_AutoExposure.x > 0.0)
        {
            float avgLog = tex2Dlod(_AdaptTex, float4(0.5, 0.5, 0, 0)).r;
            ev = clamp(_AutoExposure.y - avgLog, _AutoExposure.z, _AutoExposure.w) * _AutoExposure.x;
        }
        // Filmic tonemap (one stop of pre-exposure puts middle grey where the game was graded) and grade.
        c = ACES(c * _Exposure * _PreExposure * exp2(ev));
        float l = Luma(c);
        c = lerp(l.xxx, c, _Saturation);
        // Contrast pivots on middle grey with a power curve, so shadows deepen but never clip to black.
        c = saturate(0.18 * pow(max(c, 0.0) / 0.18, _Contrast));
        c = c * _Gain + _Lift * (1.0 - c);
        c = pow(max(c, 0.0), 1.0 / max(_Gamma, 0.01));
        // Split tone: the shadow tint over the dark half, the highlight tint over the bright half.
        float sh = 1.0 - smoothstep(0.0, 0.45, l);
        float hi = smoothstep(0.45, 1.0, l);
        c += _ShadowTint * sh + _HighlightTint * hi;
        c *= saturate(1.0 - dot(fromCenter, fromCenter) * _Vignette * 2.2);
        // Grain weighted by luminance (strongest in the shadows, almost none in highlights); a little dither against banding.
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

    // ---------------------------------------------------------------- FXAA (3.11 quality 39 style)

    float LumaG(float3 c) { return sqrt(dot(c, float3(0.299, 0.587, 0.114))); }
    float LumaAt(float2 uv) { return LumaG(Tap(uv)); }

    static const float FxaaSteps[12] = { 1.0, 1.0, 1.0, 1.0, 1.0, 1.5, 2.0, 2.0, 2.0, 2.0, 4.0, 8.0 };

    float4 fragFXAA(v2f i) : SV_Target
    {
        float2 t = _MainTex_TexelSize.xy;
        float2 uv = i.uv;
        float3 m = Tap(uv);
        float lm = LumaG(m);
        float ln = LumaAt(uv + float2(0, t.y)), ls = LumaAt(uv - float2(0, t.y));
        float le = LumaAt(uv + float2(t.x, 0)), lw = LumaAt(uv - float2(t.x, 0));
        float lmin = min(lm, min(min(ln, ls), min(le, lw)));
        float lmax = max(lm, max(max(ln, ls), max(le, lw)));
        float range = lmax - lmin;
        // Contrast-aware sharpen, applied in every branch so the picture stays crisp after anti-aliasing.
        float3 cross4 = (Tap(uv + float2(t.x, 0)) + Tap(uv - float2(t.x, 0)) + Tap(uv + float2(0, t.y)) + Tap(uv - float2(0, t.y))) * 0.25;
        float edge = saturate(range * 4.0);
        float3 sharpen = (m - cross4) * _Sharpen * (1.0 - edge);
        if (range < max(0.0312, lmax * 0.125)) return float4(saturate(m + sharpen), 1.0);

        float lnw = LumaAt(uv + float2(-t.x, t.y)), lne = LumaAt(uv + float2(t.x, t.y));
        float lsw = LumaAt(uv + float2(-t.x, -t.y)), lse = LumaAt(uv + float2(t.x, -t.y));
        float edgeH = abs(lnw - 2.0 * lw + lsw) + 2.0 * abs(ln - 2.0 * lm + ls) + abs(lne - 2.0 * le + lse);
        float edgeV = abs(lnw - 2.0 * ln + lne) + 2.0 * abs(lw - 2.0 * lm + le) + abs(lsw - 2.0 * ls + lse);
        bool horizontal = edgeH >= edgeV;
        float luma1 = horizontal ? ls : lw;
        float luma2 = horizontal ? ln : le;
        float grad1 = luma1 - lm, grad2 = luma2 - lm;
        bool steep1 = abs(grad1) >= abs(grad2);
        float gradScaled = 0.25 * max(abs(grad1), abs(grad2));
        float stepLength = horizontal ? t.y : t.x;
        float localAvg;
        if (steep1) { stepLength = -stepLength; localAvg = 0.5 * (luma1 + lm); }
        else localAvg = 0.5 * (luma2 + lm);
        float2 cur = uv;
        if (horizontal) cur.y += stepLength * 0.5; else cur.x += stepLength * 0.5;
        float2 offset = horizontal ? float2(t.x, 0) : float2(0, t.y);
        float2 uv1 = cur - offset, uv2 = cur + offset;
        float end1 = LumaAt(uv1) - localAvg, end2 = LumaAt(uv2) - localAvg;
        bool reached1 = abs(end1) >= gradScaled, reached2 = abs(end2) >= gradScaled;
        if (!reached1) uv1 -= offset;
        if (!reached2) uv2 += offset;
        if (!(reached1 && reached2))
        {
            [loop]
            for (int k = 0; k < 12; k++)
            {
                if (!reached1) end1 = LumaAt(uv1) - localAvg;
                if (!reached2) end2 = LumaAt(uv2) - localAvg;
                reached1 = abs(end1) >= gradScaled;
                reached2 = abs(end2) >= gradScaled;
                if (!reached1) uv1 -= offset * FxaaSteps[k];
                if (!reached2) uv2 += offset * FxaaSteps[k];
                if (reached1 && reached2) break;
            }
        }
        float dist1 = horizontal ? uv.x - uv1.x : uv.y - uv1.y;
        float dist2 = horizontal ? uv2.x - uv.x : uv2.y - uv.y;
        bool dir1 = dist1 < dist2;
        float distFinal = min(dist1, dist2);
        float thickness = dist1 + dist2;
        float pixelOffset = -distFinal / max(thickness, 1e-6) + 0.5;
        bool centreSmaller = lm < localAvg;
        bool correct = ((dir1 ? end1 : end2) < 0.0) != centreSmaller;
        float finalOffset = correct ? pixelOffset : 0.0;
        // Sub-pixel aliasing: thin lines and specks blend by the neighbourhood's contrast.
        float avg = (1.0 / 12.0) * (2.0 * (ln + ls + le + lw) + lnw + lne + lsw + lse);
        float sub1 = saturate(abs(avg - lm) / max(range, 1e-6));
        float sub2 = (-2.0 * sub1 + 3.0) * sub1 * sub1;
        float subFinal = sub2 * sub2 * 0.75;
        finalOffset = max(finalOffset, subFinal);
        float2 finalUv = uv;
        if (horizontal) finalUv.y += finalOffset * stepLength; else finalUv.x += finalOffset * stepLength;
        float3 aa = Tap(finalUv);
        return float4(saturate(aa + sharpen), 1.0);
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
            #pragma target 3.5
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
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragTaa
            #pragma target 3.5
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragApplyAO
            #pragma target 3.0
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragDofCoc
            #pragma target 3.0
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragDofDir
            #pragma target 3.5
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragDofHex
            #pragma target 3.5
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragDofComposite
            #pragma target 3.0
            ENDCG }
    }
    Fallback Off
}
