Shader "NeonVault/Trail"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) _EmissionColor ("Emission", Color) = (0,0,0,0) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct Input { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct Output { float4 vertex : SV_POSITION; fixed4 color : COLOR; };
            Output vert(Input v) { Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color; return o; }
            fixed4 frag(Output i) : SV_Target { return _Color*i.color; }
            ENDCG
        }
    }
}
