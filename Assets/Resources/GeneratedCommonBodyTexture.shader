Shader "NeonVault/GeneratedCommonBodyTexture"
{
    Properties { _MainTex ("Body UV atlas", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Back
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            Output vert(Input v) { Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.uv,_MainTex); return o; }
            fixed4 frag(Output i):SV_Target
            {
                // Authored UVs stay attached to the surface while the camera rotates.
                // A profile photograph must never replace the entire visible body.
                fixed4 c=tex2D(_MainTex,i.uv)*_Color;
                c.a=1;
                return c;
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}