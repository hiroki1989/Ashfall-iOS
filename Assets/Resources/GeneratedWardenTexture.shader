Shader "NeonVault/GeneratedWardenTexture"
{
    Properties
    {
        _MainTex ("Painted model atlas", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Off
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; float2 neckShade:TEXCOORD1; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float shade:TEXCOORD1; };
            Output vert(Input v)
            {
                Output o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=TRANSFORM_TEX(v.uv,_MainTex);
                o.shade=lerp(1.0,v.neckShade.x,v.neckShade.y);
                return o;
            }
            fixed4 frag(Output i):SV_Target { return tex2D(_MainTex,i.uv)*_Color*i.shade; }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
