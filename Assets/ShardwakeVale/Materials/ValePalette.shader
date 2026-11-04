Shader "Las Veyra/Shardwake Palette"
{
    Properties { _Lift ("Paint brightness", Range(0,2)) = 1 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct app { float4 vertex:POSITION; float3 normal:NORMAL; fixed4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; fixed3 paint:COLOR; };
            float _Lift;
            v2f vert(app v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal=UnityObjectToWorldNormal(v.normal); o.paint=v.color.rgb; return o; }
            fixed4 frag(v2f i):SV_Target {
                float sun=saturate(dot(normalize(i.normal),normalize(float3(-.52,.85,-.38))));
                float sky=saturate(i.normal.y*.5+.5);
                float3 c=i.paint*(.59+.34*sun+.17*sky)*_Lift;
                float haze=saturate((distance(i.world,_WorldSpaceCameraPos)-52)/165)*.55;
                c=lerp(c,float3(.64,.74,.78),haze);
                #ifndef UNITY_COLORSPACE_GAMMA
                c=GammaToLinearSpace(c);
                #endif
                return fixed4(c,1);
            }
            ENDCG
        }
    }
}
