Shader "NeonVault/ManufacturerLogoCutout" {
 Properties { _MainTex ("Image", 2D) = "white" {} }
 SubShader { Cull Off ZWrite Off ZTest Always
  Pass { CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   fixed4 frag(v2f_img i) : SV_Target {
    fixed4 c=tex2D(_MainTex,i.uv);
    float strength=max(c.r,max(c.g,c.b));
    c.a*=smoothstep(.10,.22,strength); return c;
   }
  ENDCG }
 }
}
