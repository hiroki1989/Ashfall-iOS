Shader "NeonVault/EnemyInkPaint3D"
{
    Properties
    {
        _MainTex ("Illustrated enemy atlas", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _InkColor ("Contour ink", Color) = (.055,.040,.032,1)
        _Outline ("Contour width", Range(0,.012)) = .0016
        _ShadowInk ("Painted shadow ink", Range(0,1)) = .27
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Pass
        {
            Name "PAINTED_CONTOUR"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex outlineVertex
            #pragma fragment outlineFragment
            #include "UnityCG.cginc"
            float _Outline;
            fixed4 _InkColor;
            struct ContourVertex { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct ContourOutput { float4 position:SV_POSITION; };
            ContourOutput outlineVertex(ContourVertex input)
            {
                ContourOutput output;
                output.position=UnityObjectToClipPos(input.vertex+float4(input.normal*_Outline,0));
                return output;
            }
            fixed4 outlineFragment(ContourOutput input):SV_Target { clip(_Outline-.00001); return _InkColor; }
            ENDCG
        }
        CGPROGRAM
        #pragma surface surf EnemyPaint fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half _ShadowInk;
        struct Input { float2 uv_MainTex; };
        void surf(Input input,inout SurfaceOutput output)
        {
            fixed4 paint=tex2D(_MainTex,input.uv_MainTex)*_Color;
            output.Albedo=paint.rgb;
            output.Alpha=1;
        }
        half4 LightingEnemyPaint(SurfaceOutput surface,half3 lightDir,half attenuation)
        {
            half lightValue=dot(surface.Normal,lightDir);
            half shade=.30h+.23h*smoothstep(-.10h,.12h,lightValue)
                       +.24h*smoothstep(.24h,.40h,lightValue)
                       +.23h*smoothstep(.58h,.78h,lightValue);
            half ink=(1-smoothstep(-.04h,.15h,lightValue))*_ShadowInk;
            half3 color=surface.Albedo*max(.16h,shade-ink)*attenuation;
            return half4(color,1);
        }
        ENDCG
    }
    Fallback "Diffuse"
}
