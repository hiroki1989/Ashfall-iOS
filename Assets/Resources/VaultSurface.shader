Shader "NeonVault/Surface"
{
    Properties { _MainTex ("Painted surface", 2D) = "white" {} _Color ("Color", Color) = (1,1,1,1) _EmissionColor ("Emission", Color) = (0,0,0,0) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Vault fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half4 _EmissionColor;
        struct Input { float2 uv_MainTex; };
        half4 LightingVault(SurfaceOutput s,half3 lightDir,half3 viewDir,half atten)
        {
            half nl=dot(s.Normal,lightDir);
            half smoothLight=saturate(nl*.8+.2);
            half band=.24+.76*lerp(smoothLight,floor(smoothLight*3+.5)/3,.60);
            half rim=pow(1-saturate(dot(s.Normal,viewDir)),3)*.1;
            half4 c;c.rgb=(s.Albedo*band+rim*s.Albedo)*_LightColor0.rgb*atten;c.a=s.Alpha;return c;
        }
        void surf(Input IN,inout SurfaceOutput o)
        {
            fixed4 painted=tex2D(_MainTex,IN.uv_MainTex);
            half light=dot(painted.rgb,half3(.2126,.7152,.0722));
            half3 material=lerp(light.xxx,painted.rgb,.70);
            material=lerp(half3(.62,.64,.65),material,.48);
            o.Albedo=material*_Color.rgb;o.Alpha=painted.a*_Color.a;o.Emission=_EmissionColor.rgb;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
