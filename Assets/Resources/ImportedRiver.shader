Shader "NeonVault/ImportedRiver"
{
    Properties
    {
        _Color("River water",Color)=(0.12,0.24,0.25,0.82)
        _ShallowColor("Shore water",Color)=(0.26,0.36,0.30,1)
        _FoamColor("Restrained shore foam",Color)=(0.63,0.68,0.57,1)
        _BumpMap("Optional wave normal",2D)="bump"{}
        _HasNormal("Wave normal available",Float)=0
        _WorldScale("World repeats per metre",Float)=0.14
        _Flow("World flow",Vector)=(0.018,-0.06,0,0)
        _RiverShape("Centre, amplitude A, amplitude B, half width",Vector)=(420,430,140,110)
        _RiverFrequency("Centre frequencies and phases",Vector)=(0.00067,0.00161,0,0.7)
        _RiverWidth("Half width and amplitudes",Vector)=(110,15,5,0)
        _RiverWidthFrequency("Width frequencies and phases",Vector)=(0.00091,0.0023,1.2,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            // Built-in/mobile approximation. WaterWorks SSR, scene refraction,
            // URP renderer features and depth textures are deliberately not used.
            sampler2D _BumpMap;
            float4 _Color, _ShallowColor, _FoamColor, _Flow, _RiverShape, _RiverFrequency,_RiverWidth,_RiverWidthFrequency;
            float _HasNormal, _WorldScale;
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 vertex:SV_POSITION; float3 world:TEXCOORD0; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex); return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                // World coordinates keep phase/scale continuous across streamed chunks.
                float2 uv=i.world.xz*_WorldScale;
                float2 drift=_Time.y*_Flow.xy;
                float a=dot(uv,float2(1.8,2.3))+_Time.y*1.1;
                float b=dot(uv,float2(-2.5,1.4))-_Time.y*.8;
                float2 ripple=float2(cos(a)*.075,cos(b)*.065);
                if(_HasNormal>.5)
                {
                    float3 n1=UnpackNormal(tex2D(_BumpMap,uv+drift));
                    float3 n2=UnpackNormal(tex2D(_BumpMap,uv*.71-drift*.63));
                    ripple+=(n1.xy+n2.xy)*.12;
                }
                float3 normal=normalize(float3(ripple.x,1,ripple.y));
                float3 view=normalize(_WorldSpaceCameraPos.xyz-i.world);
                float fresnel=pow(1-saturate(dot(normal,view)),4);
                float centre=_RiverShape.x+_RiverShape.y*sin(i.world.z*_RiverFrequency.x+_RiverFrequency.z)
                    +_RiverShape.z*sin(i.world.z*_RiverFrequency.y+_RiverFrequency.w);
                float halfWidth=_RiverWidth.x+_RiverWidth.y*sin(i.world.z*_RiverWidthFrequency.x+_RiverWidthFrequency.z)
                    +_RiverWidth.z*sin(i.world.z*_RiverWidthFrequency.y+_RiverWidthFrequency.w);
                float shoreDistance=max(0,halfWidth-abs(i.world.x-centre));
                float shore=1-smoothstep(.1,3.8,shoreDistance);
                float crest=sin(a)*sin(b)*.5+.5;
                float foam=shore*smoothstep(.48,.8,crest)*.46;
                float3 color=lerp(_Color.rgb,_ShallowColor.rgb,shore*.65);
                // A broad sky tint and a small sun glint avoid screen-space buffers.
                color+=float3(.14,.21,.23)*fresnel+crest*.018;
                float glint=pow(saturate(dot(reflect(-normalize(float3(.4,1,.25)),normal),view)),48);
                color+=float3(.35,.32,.23)*glint*.3;
                color=lerp(color,_FoamColor.rgb,foam);
                fixed4 result=fixed4(color,saturate(_Color.a+foam*.12));
                UNITY_APPLY_FOG(i.fogCoord,result); return result;
            }
            ENDCG
        }
    }
    Fallback Off
}
