// Neon Syndicate procedural sky for the Built-in render pipeline (driven by Atmosphere.Sky.cs; the same model lives in
// SkyMath.cs for the CPU side). Single scattering through a spherical atmosphere: Rayleigh and Mie with the sun's
// transmittance from an approximate Chapman function, so the sky turns from blue to a red horizon as the sun sinks and
// the sun disc reddens with it. Adds a limb-darkened sun disc, a phased moon, a hashed star field and a faint galaxy
// band that turn with the clock, a layered cloud deck from value noise, horizon haze that pulls toward the scene fog
// colour, the ground hemisphere in the ground's bounce colour (what the ambient probe sees below the horizon), the
// city's neon glow at night, lightning flashes, and the Veil override that follows InnerWorld.Blend.
Shader "Skybox/NeonSky"
{
    Properties
    {
        _SunDir ("Sun Direction", Vector) = (0, 1, 0, 0)
        _SunIrradiance ("Sun Irradiance", Float) = 4.5
        _SunDisc ("Sun Disc Radiance", Float) = 40
        _SunSize ("Sun Angular Radius", Float) = 0.009
        _Haze ("Haze", Float) = 1
        _MieTint ("Mie Tint", Color) = (1, 1, 1, 1)
        _CamHeight ("Camera Height", Float) = 2
        _MoonDir ("Moon Direction", Vector) = (0, -1, 0, 0)
        _MoonPhase ("Moon Phase", Float) = 0.5
        _MoonDisc ("Moon Disc Radiance", Float) = 2
        _Night ("Night Glow", Float) = 0
        _Dark ("Astronomical Night", Float) = 0
        _NightZenith ("Night Zenith", Color) = (0.02, 0.02, 0.05, 1)
        _NightHorizon ("Night Horizon Glow", Color) = (0.13, 0.12, 0.24, 1)
        _Stars ("Stars", Float) = 1
        _StarRotation ("Star Rotation", Float) = 0
        _Cloud ("Cloud Cover", Float) = 0
        _CloudColor ("Cloud Lit", Color) = (0.8, 0.8, 0.8, 1)
        _CloudShade ("Cloud Shade", Color) = (0.3, 0.3, 0.32, 1)
        _CloudOctaves ("Cloud Octaves", Float) = 4
        _Wind ("Wind", Float) = 1
        _GroundColor ("Ground Radiance", Color) = (0.3, 0.26, 0.2, 1)
        _HazeBlend ("Horizon Fog Blend", Float) = 0.6
        _Veil ("Veil", Float) = 0
        _VeilTint ("Veil Tint", Color) = (0.62, 0.12, 0.4, 1)
        _VeilHorizon ("Veil Horizon", Color) = (0.3, 0.07, 0.3, 1)
        _VeilGround ("Veil Ground", Color) = (0.12, 0.02, 0.1, 1)
        _Flash ("Lightning", Float) = 0
        _Exposure ("Exposure", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            float4 _SunDir;
            float _SunIrradiance;
            float _SunDisc;
            float _SunSize;
            float _Haze;
            float4 _MieTint;
            float _CamHeight;
            float4 _MoonDir;
            float _MoonPhase;
            float _MoonDisc;
            float _Night;
            float _Dark;
            float4 _NightZenith;
            float4 _NightHorizon;
            float _Stars;
            float _StarRotation;
            float _Cloud;
            float4 _CloudColor;
            float4 _CloudShade;
            float _CloudOctaves;
            float _Wind;
            float4 _GroundColor;
            float _HazeBlend;
            float _Veil;
            float4 _VeilTint;
            float4 _VeilHorizon;
            float4 _VeilGround;
            float _Flash;
            float _Exposure;

            // Atmosphere constants (SkyMath.cs keeps the same numbers).
            static const float PlanetRadius = 6371000.0;
            static const float AtmosphereTop = 6451000.0;
            static const float RayleighScaleHeight = 8000.0;
            static const float MieScaleHeight = 1200.0;
            static const float3 RayleighBeta = float3(5.8e-6, 13.5e-6, 33.1e-6);
            static const float MieBeta = 2.1e-5;
            static const float MieG = 0.76;
            static const float MultiScatterGain = 3.0;
            static const int ViewSamples = 8;
            static const float Blocked = 1e9;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = mul((float3x3)unity_ObjectToWorld, v.vertex.xyz);
                return o;
            }

            // ---------------------------------------------------------------- scattering

            float Chapman(float X, float h, float cosChi)
            {
                float c = sqrt(1.5707963 * (X + h));
                if (cosChi >= 0.0) return c / (c * cosChi + 1.0) * exp(-h);
                float x0 = sqrt(max(0.0, 1.0 - cosChi * cosChi)) * (X + h);
                if (x0 < X) return Blocked;
                float c0 = sqrt(1.5707963 * x0);
                return 2.0 * c0 * exp(X - x0) - c / (1.0 - c * cosChi) * exp(-h);
            }

            float RayleighPhase(float cosT)
            {
                return 3.0 / (16.0 * UNITY_PI) * (1.0 + cosT * cosT);
            }

            float MiePhase(float cosT, float g)
            {
                float g2 = g * g;
                float d = 1.0 + g2 - 2.0 * g * cosT;
                return 3.0 / (8.0 * UNITY_PI) * (1.0 - g2) * (1.0 + cosT * cosT) / ((2.0 + g2) * d * sqrt(max(1e-6, d)));
            }

            // Transmittance from the camera toward a direction (used for the sun and moon discs).
            float3 Transmittance(float cosChi)
            {
                float h = max(0.0, _CamHeight);
                float tR = RayleighScaleHeight * Chapman(PlanetRadius / RayleighScaleHeight, h / RayleighScaleHeight, cosChi);
                float tM = MieScaleHeight * Chapman(PlanetRadius / MieScaleHeight, h / MieScaleHeight, cosChi);
                if (tR >= Blocked || tM >= Blocked) return 0.0;
                return exp(-(RayleighBeta * tR + MieBeta * _Haze * tM));
            }

            // Sky radiance per unit of sun irradiance along a view ray above the horizon.
            float3 Scatter(float3 view, float3 sun)
            {
                float r0 = PlanetRadius + max(1.0, _CamHeight);
                float b = r0 * view.y;
                float disc = b * b - (r0 * r0 - AtmosphereTop * AtmosphereTop);
                float tMax = -b + sqrt(max(0.0, disc));
                float ds = tMax / ViewSamples;
                float XR = PlanetRadius / RayleighScaleHeight, XM = PlanetRadius / MieScaleHeight;
                float mie = MieBeta * _Haze;
                float tvR = 0.0, tvM = 0.0;
                float3 sumR = 0.0, sumM = 0.0;
                [loop]
                for (int i = 0; i < ViewSamples; i++)
                {
                    float t = (i + 0.5) * ds;
                    float3 p = float3(view.x * t, r0 + view.y * t, view.z * t);
                    float r = length(p);
                    float h = max(0.0, r - PlanetRadius);
                    float rhoR = exp(-h / RayleighScaleHeight), rhoM = exp(-h / MieScaleHeight);
                    tvR += rhoR * ds * 0.5;
                    tvM += rhoM * ds * 0.5;
                    float cs = dot(p, sun) / r;
                    float tsR = RayleighScaleHeight * Chapman(XR, h / RayleighScaleHeight, cs);
                    float tsM = MieScaleHeight * Chapman(XM, h / MieScaleHeight, cs);
                    if (tsR < Blocked && tsM < Blocked)
                    {
                        float3 T = exp(-(RayleighBeta * (tvR + tsR) + mie * (tvM + tsM)));
                        sumR += T * (rhoR * ds);
                        sumM += T * (rhoM * ds);
                    }
                    tvR += rhoR * ds * 0.5;
                    tvM += rhoM * ds * 0.5;
                }
                float cosT = dot(view, sun);
                return (RayleighBeta * RayleighPhase(cosT) * sumR + mie * MiePhase(cosT, MieG) * sumM * _MieTint.rgb) * MultiScatterGain;
            }

            // ---------------------------------------------------------------- hashes and noise

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Hash31(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float3 Hash33(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i), b = Hash21(i + float2(1, 0)), c = Hash21(i + float2(0, 1)), d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Fbm(float2 p, int octaves)
            {
                float v = 0.0, a = 0.5, sum = 0.0;
                [loop]
                for (int i = 0; i < octaves; i++)
                {
                    v += ValueNoise(p) * a;
                    sum += a;
                    p = p * 2.03 + float2(17.3, 9.1);
                    a *= 0.5;
                }
                return v / max(sum, 1e-4);
            }

            float3 RotateAxis(float3 v, float3 axis, float angle)
            {
                float s = sin(angle), c = cos(angle);
                return v * c + cross(axis, v) * s + axis * dot(axis, v) * (1.0 - c);
            }

            // ---------------------------------------------------------------- night sky

            float3 Stars(float3 d, float visibility)
            {
                if (visibility <= 0.001) return 0.0;
                float3 axis = normalize(float3(0.0, 0.64, 0.77));
                float3 sd = RotateAxis(d, axis, _StarRotation);
                float3 p = sd * 70.0;
                float3 cell = floor(p);
                float3 f = frac(p);
                float h = Hash31(cell);
                float3 sp = Hash33(cell + 7.0) * 0.6 + 0.2;
                float dist = length(f - sp);
                float present = step(0.78, h);
                float mag = frac(h * 57.0);
                float bright = lerp(0.25, 1.0, mag * mag);
                float core = (1.0 - smoothstep(0.0, 0.10, dist)) * bright;
                float twinkle = 0.8 + 0.2 * sin(_Time.y * (2.0 + mag * 4.0) + h * 40.0);
                float3 tint = lerp(float3(0.75, 0.85, 1.0), float3(1.0, 0.92, 0.8), frac(h * 13.0));
                float3 stars = core * present * twinkle * tint * 1.6;
                // The galaxy band: a soft great circle tilted against the pole, broken up by noise.
                float3 gN = normalize(float3(0.3, 0.55, -0.78));
                float along = dot(sd, gN);
                float band = exp(-along * along * 40.0);
                float grain = Fbm(float2(atan2(sd.x, sd.z) * 3.0, sd.y * 6.0), 3);
                float3 galaxy = band * (0.35 + 0.65 * grain) * float3(0.22, 0.2, 0.36) * 0.09;
                return (stars + galaxy) * visibility;
            }

            float3 MoonDisc(float3 d, float3 moon, float3 sun)
            {
                float cosA = dot(d, moon);
                float size = 0.0095;
                float ang = acos(clamp(cosA, -1.0, 1.0));
                if (ang > size * 1.5) return 0.0;
                float rr = saturate(ang / size);
                float disc = 1.0 - smoothstep(0.92, 1.0, rr);
                // The lit side faces the sun; a phase near 0 or 1 leaves a thin crescent.
                float3 offset = d - moon * cosA;
                float lit = dot(normalize(offset + 1e-5), normalize(sun - moon * dot(sun, moon) + 1e-5));
                float phase = cos(_MoonPhase * 6.2831853);
                float shade = saturate((lit * sqrt(max(0.0, 1.0 - rr * rr)) * 1.3 + phase) * 0.5 + 0.5);
                float limb = 1.0 - 0.35 * (1.0 - sqrt(max(0.0, 1.0 - rr * rr)));
                float3 face = float3(0.92, 0.93, 0.98) * limb * lerp(0.04, 1.0, shade);
                float halo = exp(-(ang - size) * 180.0) * 0.4 * step(size, ang);
                return (face * disc + float3(0.8, 0.85, 1.0) * halo) * _MoonDisc;
            }

            // ---------------------------------------------------------------- clouds

            float4 Clouds(float3 d, float3 sun)
            {
                if (_Cloud <= 0.002 || d.y <= 0.0) return 0.0;
                float horizonFade = smoothstep(0.0, 0.14, d.y);
                if (horizonFade <= 0.001) return 0.0;
                float t = 1500.0 / max(d.y, 0.03);
                float2 uv = d.xz * t * 0.00035 + float2(0.013, 0.007) * _Time.y * _Wind;
                int oct = (int)clamp(_CloudOctaves, 1.0, 5.0);
                float n = Fbm(uv, oct);
                float cover = saturate(_Cloud);
                float dens = saturate((n - (1.0 - cover * 1.15)) / 0.36);
                dens = dens * dens * (3.0 - 2.0 * dens);
                float alpha = dens * horizonFade * lerp(0.85, 1.0, cover);
                // Thick cores are dark underneath, the edges stay lit; sun behind the deck rims the edges.
                float thick = saturate(dens * 1.4);
                float3 col = lerp(_CloudColor.rgb, _CloudShade.rgb, thick * 0.85);
                float cosT = dot(d, sun);
                float rim = MiePhase(cosT, 0.6) * (1.0 - thick) * saturate(sun.y * 4.0 + 0.3) * 0.12;
                col += _CloudColor.rgb * rim;
                return float4(col, alpha);
            }

            // ---------------------------------------------------------------- main

            float4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float3 sun = normalize(_SunDir.xyz);
                float3 moon = normalize(_MoonDir.xyz);
                float3 lum = float3(0.2126, 0.7152, 0.0722);

                // The sky above the horizon (with a short band below it so the haze can wrap the seam).
                float3 skyDir = d;
                skyDir.y = max(d.y, 0.002);
                skyDir = normalize(skyDir);
                float3 sky = Scatter(skyDir, sun) * _SunIrradiance;

                // Night: the city's glow on the air near the horizon and a floor at the zenith, never pitch black.
                float horizonness = pow(1.0 - saturate(d.y), 3.0);
                float3 night = lerp(_NightZenith.rgb, _NightHorizon.rgb, horizonness) * (1.0 + _Cloud * 0.6);
                sky += night * _Night;

                // Stars and the galaxy show where the sky is dark.
                float skyLum = dot(sky, lum);
                float starVis = _Stars * _Dark * saturate(1.0 - skyLum * 6.0) * (1.0 - _Cloud) * smoothstep(-0.02, 0.1, d.y);
                sky += Stars(d, starVis);

                // The sun disc with limb darkening, reddened by the same transmittance as the sky, and a moon with phases.
                float cosSun = dot(d, sun);
                float angSun = acos(clamp(cosSun, -1.0, 1.0));
                if (angSun < _SunSize && sun.y > -0.02)
                {
                    float rr = angSun / _SunSize;
                    float mu = sqrt(max(0.0, 1.0 - rr * rr));
                    float limb = 1.0 - 0.6 * (1.0 - mu);
                    float edge = 1.0 - smoothstep(0.9, 1.0, rr);
                    sky += Transmittance(sun.y) * _SunDisc * limb * edge;
                }
                if (moon.y > -0.05) sky += MoonDisc(d, moon, sun) * Transmittance(max(moon.y, 0.0)) * _Dark;

                // Clouds.
                float4 cloud = Clouds(d, sun);
                sky = lerp(sky, cloud.rgb, cloud.a * (1.0 - _Veil));

                // Horizon haze toward the scene fog colour so the buildings' fog meets the sky without a seam.
                float haze = pow(1.0 - saturate(d.y * 1.2), 6.0) * _HazeBlend;
                sky = lerp(sky, unity_FogColor.rgb, haze);

                // The ground hemisphere: the ground's bounce radiance, meeting the haze at the horizon.
                float3 ground = lerp(_GroundColor.rgb, unity_FogColor.rgb, 0.5 * _HazeBlend);
                float below = smoothstep(0.0, 0.08, -d.y);
                float3 col = lerp(sky, ground, below);

                // The Veil: a red-violet sky with a dark plum horizon and ground, lit by the low crimson sun.
                float3 veilSky = dot(col, lum) * _VeilTint.rgb * 2.6 + horizonness * _VeilHorizon.rgb;
                if (angSun < _SunSize * 1.6) veilSky += float3(1.0, 0.2, 0.26) * _SunDisc * 0.5 * (1.0 - smoothstep(0.7, 1.0, angSun / (_SunSize * 1.6)));
                float3 veilGround = _VeilGround.rgb;
                float3 veil = lerp(veilSky, veilGround, below);
                col = lerp(col, veil, _Veil);

                // Lightning.
                col += float3(0.6, 0.65, 0.8) * _Flash;
                col *= _Exposure;
                return float4(max(col, 0.0), 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
