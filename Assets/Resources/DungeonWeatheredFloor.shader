Shader "NeonVault/DungeonWeatheredFloor" {
 Properties { _MainTex ("Floor", 2D) = "white" {} _Color ("Color", Color) = (1,1,1,1) _Grime ("Grime", Range(0,1)) = 0.58 }
 SubShader {
  Tags { "RenderType"="Opaque" }
  LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  sampler2D _MainTex;
  fixed4 _Color;
  half _Grime;
  struct Input { float2 uv_MainTex; float3 worldPos; };
  float hash2(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
  float noise2(float2 p) { float2 i=floor(p),f=frac(p); f=f*f*(3-2*f); return lerp(lerp(hash2(i),hash2(i+float2(1,0)),f.x),lerp(hash2(i+float2(0,1)),hash2(i+1),f.x),f.y); }
  void surf(Input IN, inout SurfaceOutputStandard o) {
   fixed4 tex=tex2D(_MainTex,IN.uv_MainTex)*_Color;
   float n=noise2(IN.worldPos.xz*.32); float detail=noise2(IN.worldPos.xz*1.8);
   float dirt=smoothstep(.40,.78,n*.75+detail*.25)*_Grime;
   o.Albedo=lerp(tex.rgb,tex.rgb*float3(.38,.35,.29),dirt); o.Metallic=0; o.Smoothness=.08; o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard"
}
