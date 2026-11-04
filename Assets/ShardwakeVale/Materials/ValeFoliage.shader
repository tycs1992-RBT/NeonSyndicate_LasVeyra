Shader "Las Veyra/Shardwake Foliage"
{
    // Leaf, needle, grass and flower cards from the painted atlas: alpha cutout, both sides drawn, soft wrapped sun
    // light with its shadows, the trilight ambient, light through the leaves when looking toward the sun, a gentle
    // wind sway weighted by the vertex alpha, and the same haze as the Vale palette.
    Properties
    {
        _MainTex ("Foliage atlas (RGBA)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.45
        _Wrap ("Soft light wrap", Range(0,1)) = 0.55
        _SunScale ("Sun strength", Range(0,2)) = 0.95
        _SkyScale ("Ambient strength", Range(0,2)) = 0.8
        _Translucency ("Light through leaves", Range(0,2)) = 0.7
        _TransColor ("Light through leaves colour", Color) = (0.92,1,0.5,1)
        _Wind ("Wind sway (m)", Range(0,0.3)) = 0.05
        _NearHaze ("Haze toward the sun", Color) = (0.93,0.83,0.69,1)
        _FarHaze ("Haze away from the sun", Color) = (0.64,0.74,0.85,1)
        _HazeStart ("Haze start (m)", Float) = 34
        _HazeDepth ("Haze depth (m)", Float) = 210
        _HazeMax ("Haze strength", Range(0,1)) = 0.7
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "IgnoreProjector"="True" }
        Cull Off
        CGINCLUDE
        #include "UnityCG.cginc"
        float _Wind;
        float3 Sway(float3 world, float weight)
        {
            float t = _Time.y;
            float a = sin(t * 1.7 + world.x * 0.31 + world.z * 0.23);
            float b = cos(t * 1.3 + world.x * 0.19 - world.z * 0.27);
            return float3(a, 0.25 * a * b, b) * _Wind * weight;
        }
        ENDCG
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            struct app { float4 vertex:POSITION; float3 normal:NORMAL; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f
            {
                float4 pos:SV_POSITION;
                float3 normal:TEXCOORD0;
                float3 world:TEXCOORD1;
                float2 uv:TEXCOORD2;
                fixed4 tint:COLOR;
                SHADOW_COORDS(3)
            };
            sampler2D _MainTex;
            fixed4 _Color, _TransColor, _NearHaze, _FarHaze;
            float _Cutoff, _Wrap, _SunScale, _SkyScale, _Translucency, _HazeStart, _HazeDepth, _HazeMax;

            v2f vert(app v)
            {
                v2f o;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                world += Sway(world, v.color.a);
                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1));
                o.world = world;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                o.tint = v.color;
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                clip(tex.a - _Cutoff);
                float3 albedo = tex.rgb * i.tint.rgb * _Color.rgb;
                float3 n = normalize(i.normal);
                float3 toSun = _WorldSpaceLightPos0.xyz;
                toSun = dot(toSun, toSun) > 1e-6 ? normalize(toSun) : float3(0, 1, 0);
                float wrapped = saturate((dot(n, toSun) + _Wrap) / (1 + _Wrap));
                float shadow = SHADOW_ATTENUATION(i);
                float3 view = i.world - _WorldSpaceCameraPos;
                float dist = length(view);
                float3 dir = view / max(dist, 1e-3);
                float through = pow(saturate(dot(dir, toSun)), 3) * _Translucency;
                float3 sun = _LightColor0.rgb * shadow * _SunScale * (wrapped + through * _TransColor.rgb);
                float3 sky = ShadeSH9(float4(n, 1)) * _SkyScale;
                float3 c = albedo * (sun + sky);
                float haze = saturate((dist - _HazeStart) / _HazeDepth) * _HazeMax;
                haze *= saturate(1.15 - max(0, i.world.y - 8) / 70);
                float toward = saturate(dot(dir, toSun) * .5 + .5);
                c = lerp(c, lerp(_FarHaze.rgb, _NearHaze.rgb, toward * toward), haze);
                #ifndef UNITY_COLORSPACE_GAMMA
                c = GammaToLinearSpace(c);
                #endif
                return fixed4(c, 1);
            }
            ENDCG
        }
        Pass
        {
            Tags { "LightMode"="ShadowCaster" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            sampler2D _MainTex;
            float _Cutoff;
            struct app { float4 vertex:POSITION; float3 normal:NORMAL; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { V2F_SHADOW_CASTER; float2 uv:TEXCOORD1; };
            v2f vert(app v)
            {
                v2f o;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                world += Sway(world, v.color.a);
                v.vertex = mul(unity_WorldToObject, float4(world, 1));
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = v.uv;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                clip(tex2D(_MainTex, i.uv).a - _Cutoff);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
