Shader "NeonVault/EnemySprite"
{
    Properties { [PerRendererData] _MainTex ("Painted cutout",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _AccentColor("Panel accent",Color)=(0.1,0.65,0.9,1) _AccentStrength("Panel accent strength",Range(0,1))=0 }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" "DisableBatching"="True" }
        Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct a { float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            struct v { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            sampler2D _MainTex;float4 _MainTex_TexelSize;fixed4 _Color;float _Hit;float _Breath;
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
            float _BodyHeight;float _TorsoBend;float _TorsoTwist;
            int _ClipCount;float4 _ClipPoints[64];
            float _PartClipEnabled;float4 _PartClipRect;
            float _RoundClipEnabled;float4 _RoundRect;
            float4 _LimbRect;float _LimbBend;
            v vert(a i)
            {
                v o;
                // The upper-body sprite is authored with a waist pivot. Keep
                // that seam fixed while the chest breathes, bends and twists.
                float y=saturate(i.vertex.y/max(.001,_BodyHeight));
                // Native SpriteRenderer quad: a waist-fixed trapezoid/shear.
                // Both top vertices respond to breathing; no runtime mesh edit.
                i.vertex.x*=1+_Breath*y-abs(_TorsoTwist)*y;
                i.vertex.x+=_BodyHeight*(_TorsoBend*y+_TorsoTwist*y*.35);
                i.vertex.y+=_BodyHeight*_Breath*y*.2;
                o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.color=i.color*_Color;return o;
            }
            fixed4 frag(v i):SV_Target
            {
                if(_PartClipEnabled>.5){clip(i.uv.y-_PartClipRect.y);clip(_PartClipRect.w-i.uv.y);}
                if(_RoundClipEnabled>.5){float2 radius=(i.uv-_RoundRect.xy)/_RoundRect.zw;clip(1-dot(radius,radius));}
                if(_ClipCount>2)
                {
                    float smallest=1,largest=-1;
                    for(int edge=0;edge<_ClipCount;edge++)
                    {
                        float2 a=_ClipPoints[edge].xy,b=_ClipPoints[(edge+1)%_ClipCount].xy;
                        float2 segment=b-a,offset=i.uv-a;float side=segment.x*offset.y-segment.y*offset.x;
                        smallest=min(smallest,side);largest=max(largest,side);
                    }
                    if(smallest<-.000002&&largest>.000002)clip(-1);
                }
                float2 paintUV=i.uv;
                if(_LimbRect.z>.00001)
                {
                    float2 localUV=(i.uv-_LimbRect.xy)/_LimbRect.zw;
                    paintUV.x-=sin(saturate(localUV.y)*3.14159265)*_LimbBend*_LimbRect.z;
                    float2 warped=(paintUV-_LimbRect.xy)/_LimbRect.zw;
                    if(any(warped<0)||any(warped>1))clip(-1);
                }
                fixed4 source=tex2D(_MainTex,paintUV);fixed4 c=float4(ToonPanels(SmoothPaint(paintUV,source)*i.color.rgb),source.a*i.color.a);
                float2 stroke=max(_MainTex_TexelSize.xy*2.8,fwidth(paintUV)*1.8);
                float nearAlpha=min(min(tex2D(_MainTex,paintUV+float2(stroke.x,0)).a,tex2D(_MainTex,paintUV-float2(stroke.x,0)).a),min(tex2D(_MainTex,paintUV+float2(0,stroke.y)).a,tex2D(_MainTex,paintUV-float2(0,stroke.y)).a));
                if(c.a>.05&&nearAlpha<.06)c.rgb=float3(.012,.014,.022);
                c.rgb=lerp(c.rgb,float3(1,.72,.48),_Hit);clip(c.a-.012);return c;
            }
            ENDCG
        }
    }
}



