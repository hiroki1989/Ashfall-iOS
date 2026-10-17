Shader "NeonVault/SculptedArmor"
{
    Properties { _MainTex ("Engraved metal", 2D) = "white" {} _Color ("Alloy", Color) = (1,1,1,1) _EmissionColor ("Emission", Color) = (0,0,0,0) _PaintMix ("Painted detail", Range(0,1)) = 1 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Armor fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half4 _EmissionColor;
        half _PaintMix;
        struct Input { float2 uv_MainTex; };
        half4 LightingArmor(SurfaceOutput s,half3 lightDir,half3 viewDir,half atten)
        {
            half diffuse=.26+.74*smoothstep(-.18,.7,dot(s.Normal,lightDir));
            half spec=pow(saturate(dot(s.Normal,normalize(lightDir+viewDir))),48)*.75;
            half rim=pow(1-saturate(dot(s.Normal,viewDir)),3)*.16;
            half3 alloy=lerp(half3(.7,.83,.9),s.Albedo,.45);
            return half4((s.Albedo*diffuse+alloy*spec+s.Albedo*rim)*_LightColor0.rgb*atten,s.Alpha);
        }
        void surf(Input IN,inout SurfaceOutput o)
        {
            half3 etched=tex2D(_MainTex,IN.uv_MainTex).rgb;
            // Lift dark engravings while retaining their detail: silhouettes stay readable at gameplay zoom.
            o.Albedo=lerp(half3(1,1,1),.16+etched*.95,_PaintMix)*_Color.rgb;
            o.Alpha=1;
            o.Emission=_EmissionColor.rgb;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
