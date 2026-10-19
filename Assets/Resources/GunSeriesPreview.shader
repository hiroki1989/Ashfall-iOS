Shader "NeonVault/GunSeriesPreview"
{
    Properties { _MainTex("Item art",2D)="white"{} _Crop("Crop",Vector)=(0,0,1,1) _AccentColor("Panel accent",Color)=(0.1,0.65,0.9,1) _AccentStrength("Panel accent strength",Range(0,1))=0 }
    SubShader { Cull Off ZWrite Off ZTest Always
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _Crop;float4 _MainTex_TexelSize;
            float4 _AccentColor;float _AccentStrength;
            float3 SmoothPaint(float2 uv,float4 center)
            {
                float2 stepUV=max(_MainTex_TexelSize.xy*1.35,fwidth(uv)*.55);
                float3 total=center.rgb*2;float weight=2;
                float baseLight=dot(center.rgb,float3(.2126,.7152,.0722));
                for(int sampleIndex=0;sampleIndex<4;sampleIndex++)
                {
                    float2 offset=sampleIndex==0?float2(stepUV.x,0):sampleIndex==1?float2(-stepUV.x,0):sampleIndex==2?float2(0,stepUV.y):float2(0,-stepUV.y);
                    float4 neighbor=tex2D(_MainTex,uv+offset);
                    float difference=abs(dot(neighbor.rgb,float3(.2126,.7152,.0722))-baseLight);
                    float contribution=smoothstep(.18,.65,neighbor.a)*(1-smoothstep(.10,.30,difference))*(1-smoothstep(.20,.55,length(neighbor.rgb-center.rgb)));
                    total+=neighbor.rgb*contribution;weight+=contribution;
                }
                return total/weight;
            }
            float3 ToonPanels(float3 paint)
            {
                float light=dot(paint,float3(.2126,.7152,.0722));
                // Preserve the artist's separate metal, cloth, leather, paint
                // and glass colors. Only organize luminance, never replace hue.
                paint=lerp(light.xxx,paint,1.08);
                float bands=min(1,.16+floor(saturate(light)*4)*.21);
                float3 panels=saturate(paint/max(.055,light)*lerp(light,bands,.35));
                return lerp(panels,saturate(paint*.65),1-smoothstep(.025,.07,light));
            }
            int _ClipCount;float4 _ClipPoints[64];int _ExcludeCount;float4 _ExcludeRects[32];
            float Alpha(float2 uv)
            {
                if(_ClipCount>2)
                {
                    float smallest=1,largest=-1;
                    for(int edge=0;edge<_ClipCount;edge++){float2 a=_ClipPoints[edge].xy,b=_ClipPoints[(edge+1)%_ClipCount].xy,d=b-a,p=uv-a;float side=d.x*p.y-d.y*p.x;smallest=min(smallest,side);largest=max(largest,side);}
                    if(smallest<-.000002&&largest>.000002)return 0;
                }
                for(int hole=0;hole<_ExcludeCount;hole++){float4 r=_ExcludeRects[hole];if(uv.x>=r.x&&uv.x<=r.x+r.z&&uv.y>=r.y&&uv.y<=r.y+r.w)return 0;}return tex2D(_MainTex,uv).a;
            }
            fixed4 frag(v2f_img i):SV_Target
            {
                float2 local=(i.uv-.08)/.84;
                float2 uv=_Crop.xy+local*_Crop.zw;
                fixed4 color=tex2D(_MainTex,uv);
                color.a=Alpha(uv);
                if(any(local<0)||any(local>1))color.a=0;
                float2 stepUV=max(_MainTex_TexelSize.xy*8,fwidth(uv)*1.8);
                float around=max(max(Alpha(uv+float2(stepUV.x,0)),Alpha(uv-float2(stepUV.x,0))),max(Alpha(uv+float2(0,stepUV.y)),Alpha(uv-float2(0,stepUV.y))));
                if(color.a<.12){if(around>.3&&all(local>-.025)&&all(local<1.025))return fixed4(.012,.014,.024,1);return 0;}
                // Match the native paint used by the equipped sprite without blurring its ink.
                return color;
            }
            ENDCG
        }
    }
}


