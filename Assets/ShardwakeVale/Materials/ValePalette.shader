Shader "Las Veyra/Shardwake Palette"
{
    // Painted vertex colour lit by the scene's sun (with its shadows) and the trilight ambient, a world-space grain
    // tile chosen by the surface code in the vertex alpha, and a warm-to-cool atmospheric haze. Vertex alpha codes:
    // 1 stone and metal (grain R), 0.6 to 0.9 wood and leaves (grain G), 0.2 to 0.6 soil (grain B), below 0.2 partly
    // self-lit (warm panes, the Crown's seam), 0 fully self-lit and outside the haze (the painted sky).
    Properties
    {
        _Lift ("Paint brightness", Range(0,2)) = 1
        [HideInInspector] _Hearth ("Hearth position and heat", Vector) = (0,0,0,0)
        _Tint ("Tint", Color) = (1,1,1,1)
        _Grain ("Surface grain (R stone, G leaf, B soil)", 2D) = "gray" {}
        _GrainScale ("Grain tiles per metre", Float) = 0.45
        _GrainStrength ("Grain strength", Range(0,1)) = 0.34
        _Wrap ("Soft light wrap", Range(0,1)) = 0.3
        _SunScale ("Sun strength", Range(0,2)) = 0.92
        _SkyScale ("Ambient strength", Range(0,2)) = 0.72
        _NearHaze ("Haze toward the sun", Color) = (0.93,0.83,0.69,1)
        _FarHaze ("Haze away from the sun", Color) = (0.64,0.74,0.85,1)
        _HazeStart ("Haze start (m)", Float) = 34
        _HazeDepth ("Haze depth (m)", Float) = 210
        _HazeMax ("Haze strength", Range(0,1)) = 0.7
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            struct app { float4 vertex:POSITION; float3 normal:NORMAL; fixed4 color:COLOR; };
            struct v2f
            {
                float4 pos:SV_POSITION;
                float3 normal:TEXCOORD0;
                float3 world:TEXCOORD1;
                fixed4 paint:COLOR;
                SHADOW_COORDS(2)
            };
            float _Lift, _GrainScale, _GrainStrength, _Wrap, _SunScale, _SkyScale, _HazeStart, _HazeDepth, _HazeMax;
            fixed4 _Tint, _NearHaze, _FarHaze;
            float4 _Hearth;
            sampler2D _Grain;

            v2f vert(app v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.paint = v.color;
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float code = i.paint.a;
                float glow = saturate(1 - code * 5);
                float3 paint = i.paint.rgb * _Tint.rgb;

                // World-space grain from three planar reads blended by the normal; channel by surface code.
                float3 w = n * n;
                w = w * w;
                w /= max(dot(w, float3(1, 1, 1)), 1e-4);
                float3 uvw = i.world * _GrainScale;
                float3 grain = tex2D(_Grain, uvw.zy).rgb * w.x + tex2D(_Grain, uvw.xz).rgb * w.y + tex2D(_Grain, uvw.xy).rgb * w.z;
                float pick = code >= 0.9 ? grain.r : (code >= 0.6 ? grain.g : grain.b);
                paint *= lerp(1, pick * 2, _GrainStrength * (1 - glow));

                float3 toSun = _WorldSpaceLightPos0.xyz;
                toSun = dot(toSun, toSun) > 1e-6 ? normalize(toSun) : float3(0, 1, 0);
                float wrapped = saturate((dot(n, toSun) + _Wrap) / (1 + _Wrap));
                float shadow = SHADOW_ATTENUATION(i);
                float3 sun = _LightColor0.rgb * wrapped * shadow * _SunScale;
                float3 sky = ShadeSH9(float4(n, 1)) * _SkyScale;
                float3 c = paint * (sun + sky) * _Lift;

                float hearth = saturate(1 - distance(i.world, _Hearth.xyz) / 4.5);
                c += float3(.34, .13, .025) * _Hearth.w * hearth * hearth;
                c = lerp(c, paint * _Lift, glow);

                float3 view = i.world - _WorldSpaceCameraPos;
                float dist = length(view);
                float haze = saturate((dist - _HazeStart) / _HazeDepth) * _HazeMax;
                haze *= saturate(1.15 - max(0, i.world.y - 8) / 70);
                float toward = saturate(dot(view / max(dist, 1e-3), toSun) * .5 + .5);
                float3 hazeColor = lerp(_FarHaze.rgb, _NearHaze.rgb, toward * toward);
                c = lerp(c, hazeColor, haze * step(glow, .99));
                #ifndef UNITY_COLORSPACE_GAMMA
                c = GammaToLinearSpace(c);
                #endif
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    // The fallback's shadow caster pass lets the Vale's meshes cast the sun's shadows.
    FallBack "Legacy Shaders/VertexLit"
}
