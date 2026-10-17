Shader "NeonVault/CombatGlow"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
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
            struct Input { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varying { float4 position:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            Varying vert(Input v) { Varying o;o.position=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o; }
            fixed4 frag(Varying i):SV_Target
            {
                // Rounded cross-section softens trails without a raster texture dependency.
                float edge=abs(i.uv.y*2-1);
                float glow=exp(-edge*edge*5.5)*smoothstep(1, .65, edge);
                return fixed4(i.color.rgb*1.3,i.color.a*glow);
            }
            ENDCG
        }
    }
}

