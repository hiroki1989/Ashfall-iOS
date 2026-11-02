Shader "NeonVault/BuildingGhost"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,0.50) _UseTextureAlpha ("Texture alpha", Float) = 0 _WorldAtlas ("World atlas", Float) = 0 _Tile ("Atlas quadrant", Vector) = (0,0,0,0) _Scale ("World texture scale", Float) = 0.8 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float3 worldPos : TEXCOORD1; float3 worldNormal : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; float _UseTextureAlpha; float _WorldAtlas; float4 _Tile; float _Scale;
            v2f vert(appdata v) {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.uv,_MainTex); o.worldPos=mul(unity_ObjectToWorld,v.vertex).xyz; o.worldNormal=UnityObjectToWorldNormal(v.normal); return o;
            }
            float3 atlas(float2 uv) { return tex2D(_MainTex,_Tile.xy+0.003+(1-abs(frac(uv*.5)*2-1))*0.494).rgb; }
            fixed4 frag(v2f i) : SV_Target {
                fixed4 tex=tex2D(_MainTex,i.uv);
                if(_WorldAtlas>0.5) {
                    float3 w=pow(abs(normalize(i.worldNormal)),4); w/=max(dot(w,float3(1,1,1)),0.0001);
                    float3 p=i.worldPos*_Scale;
                    tex.rgb=atlas(p.zy)*w.x+atlas(p.xz)*w.y+atlas(p.xy)*w.z;
                }
                fixed4 c=fixed4(tex.rgb*_Color.rgb,_Color.a*lerp(1,tex.a,_UseTextureAlpha)); clip(c.a-0.01); return c;
            }
            ENDCG
        }
    }
}
