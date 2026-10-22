Shader "NeonVault/SharedArmorTexture"
{
    Properties
    {
        _MainTex ("Painted model atlas", 2D) = "white" {}
        _GradeLUT ("Material paint lookup", 2D) = "white" {}
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
            sampler2D _MainTex; sampler2D _GradeLUT;
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
                        fixed4 frag(Output i):SV_Target {
                float3 color=tex2D(_MainTex,i.uv).rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                color=LinearToGammaSpace(color);
                #endif
                color=saturate(color);float blue=color.b*31;float slice=floor(blue);
                float2 uv0=float2((color.r*31+slice*32+.5)/1024,1-(color.g*31+.5)/32);
                float2 uv1=float2((color.r*31+min(slice+1,31)*32+.5)/1024,uv0.y);
                float3 painted=lerp(tex2D(_GradeLUT,uv0).rgb,tex2D(_GradeLUT,uv1).rgb,frac(blue));
                return fixed4(painted,1)*_Color*i.shade;
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}


