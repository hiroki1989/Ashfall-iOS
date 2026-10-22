Shader "Hidden/NeonVault/Post"
{
    Properties { _MainTex ("Source",2D)="white"{} _Bloom("Bloom",2D)="black"{} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex,_Bloom,_CameraDepthNormalsTexture;
        float4 _MainTex_TexelSize;
        float4 _Direction;
        half4 bright(v2f_img i):SV_Target {half3 c=tex2D(_MainTex,i.uv).rgb;return half4(max(c-.85,0),1);}
        half4 blur(v2f_img i):SV_Target
        {return tex2D(_MainTex,i.uv)*.4+(tex2D(_MainTex,i.uv+_Direction.xy)+tex2D(_MainTex,i.uv-_Direction.xy))*.24+(tex2D(_MainTex,i.uv+_Direction.xy*2)+tex2D(_MainTex,i.uv-_Direction.xy*2))*.06;}
        half4 finish(v2f_img i):SV_Target
        {
            half3 c=tex2D(_MainTex,i.uv).rgb+tex2D(_Bloom,i.uv).rgb*.28;
            c=c/(1+c*.28);c=pow(max(c,0),.95);
            float centerDepth;float3 centerNormal;DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,i.uv),centerDepth,centerNormal);
            float ink=0;
            if(centerDepth>.0001&&centerDepth<.999)
            {
                for(int sample=0;sample<4;sample++)
                {
                    float2 offset=sample==0?float2(1.15,0):sample==1?float2(-1.15,0):sample==2?float2(0,1.15):float2(0,-1.15);
                    float depth;float3 normal;DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,i.uv+offset*_MainTex_TexelSize.xy),depth,normal);
                    // Painted scans already contain ink. Facet-normal edges turn their
                    // fine surface into dark speckles; use geometric depth boundaries only.
                    float separation=smoothstep(.008,.05,abs(depth-centerDepth)/max(.004,centerDepth));
                    ink=max(ink,separation);
                }
            }
            c=lerp(c,half3(.009,.014,.02),ink*.52);
            float2 p=i.uv-.5;float vignette=1-smoothstep(.2,.75,dot(p,p)*1.7)*.4;
            c*=vignette;return half4(c,1);
        }
        ENDCG
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment bright
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment blur
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment finish
            ENDCG }
    }
}
