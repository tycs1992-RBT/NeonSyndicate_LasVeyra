Shader "Las Veyra/Shardwake Signal"
{
    Properties { _Color ("Signal", Color) = (1,.68,.2,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct app { float4 vertex:POSITION; fixed4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; fixed4 color:COLOR; };
            v2f vert(app i) { v2f o; o.pos=UnityObjectToClipPos(i.vertex);o.color=i.color;return o; }
            fixed4 frag(v2f i):SV_Target {return _Color*i.color;}
            ENDCG
        }
    }
}
