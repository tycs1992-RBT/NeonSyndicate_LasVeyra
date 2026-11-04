Shader "Las Veyra/Shardwake Signal"
{
    // Gameplay signals (wave, focus halo, Sunbolt). _DstBlend 10 is ordinary transparency; 1 adds light (glow).
    // _Soft fades a LineRenderer toward its edges using the across-line texture coordinate (0 keeps it solid).
    Properties
    {
        _Color ("Signal", Color) = (1,.68,.2,1)
        _Boost ("Brightness", Range(0,4)) = 1
        _Soft ("Soft line edges", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha [_DstBlend]
        ZWrite Off
        Cull Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _Boost, _Soft;
            struct app { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            v2f vert(app i) { v2f o; o.pos=UnityObjectToClipPos(i.vertex); o.color=i.color; o.uv=i.uv; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c = _Color * i.color;
                c.rgb *= _Boost;
                float edge = 1 - abs(i.uv.y * 2 - 1);
                c.a *= lerp(1, edge * edge, _Soft);
                return c;
            }
            ENDCG
        }
    }
}
