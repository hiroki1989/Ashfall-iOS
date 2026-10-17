Shader "NeonVault/LayeredPlayerSprite"
{
    Properties { [PerRendererData] _MainTex ("Painted cutout",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _AccentColor("Panel accent",Color)=(0.1,0.65,0.9,1) _AccentStrength("Panel accent strength",Range(0,1))=0 }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct a { float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            struct v { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 screen:TEXCOORD1;float4 color:COLOR; };
            sampler2D _MainTex;fixed4 _Color;float4 _MainTex_TexelSize;
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
            int _ClipCount;float4 _ClipPoints[64];
            float _PartClipEnabled;float4 _PartClipRect;
            float _RoundClipEnabled;float4 _RoundRect;
            float _StockOcclusionEnabled;float2 _StockUVOrigin;float2 _StockUVAxis;float _StockFraction;
            float4 _StockTorsoA;float4 _StockTorsoB;float _StockTorsoRadius;
            float _TorsoSleeveCutEnabled;float4 _TorsoSleeveA;float4 _TorsoSleeveB;float _TorsoSleeveRadius;
            v vert(a i){v o;o.vertex=UnityObjectToClipPos(i.vertex);o.screen=ComputeScreenPos(o.vertex);o.uv=i.uv;o.color=i.color*_Color;return o;}
            fixed4 frag(v i):SV_Target
            {
                float sleeveEdge=100;
                if(_TorsoSleeveCutEnabled>.5)
                {
                    float2 pixel=(i.screen.xy/i.screen.w)*_ScreenParams.xy;
                    float2 a=_TorsoSleeveA.xy*_ScreenParams.xy,b=_TorsoSleeveB.xy*_ScreenParams.xy,axis=b-a;
                    float t=saturate(dot(pixel-a,axis)/max(.0001,dot(axis,axis)));
                    if(t>.28)
                    {
                        float radius=_TorsoSleeveRadius*_ScreenParams.x*lerp(1.1,.80,t);
                        sleeveEdge=radius-distance(pixel,a+axis*t);clip(sleeveEdge);
                    }
                }
                if(_StockOcclusionEnabled>.5)
                {
                    float along=dot(i.uv-_StockUVOrigin,_StockUVAxis);
                    float2 pixel=(i.screen.xy/i.screen.w)*_ScreenParams.xy;
                    float2 a=_StockTorsoA.xy*_ScreenParams.xy,b=_StockTorsoB.xy*_ScreenParams.xy,axis=b-a;
                    float t=saturate(dot(pixel-a,axis)/max(.0001,dot(axis,axis)));
                    float radius=_StockTorsoRadius*_ScreenParams.x;
                    if(along<_StockFraction&&along>-.25&&distance(pixel,a+axis*t)<radius)clip(-1);
                }
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
                // Native ink is already shaded. Retain its interior pen strokes at gameplay scale.
                fixed4 source=tex2D(_MainTex,i.uv);fixed4 c=float4(source.rgb*i.color.rgb,source.a*i.color.a);
                if(sleeveEdge<1.2)c.rgb=float3(.012,.014,.022);
                float2 stroke=max(_MainTex_TexelSize.xy*2.8,fwidth(i.uv)*1.8);
                float nearAlpha=min(min(tex2D(_MainTex,i.uv+float2(stroke.x,0)).a,tex2D(_MainTex,i.uv-float2(stroke.x,0)).a),min(tex2D(_MainTex,i.uv+float2(0,stroke.y)).a,tex2D(_MainTex,i.uv-float2(0,stroke.y)).a));
                if(c.a>.05&&nearAlpha<.06)c.rgb=float3(.012,.014,.022);
                clip(c.a-.012);return c;
            }
            ENDCG
        }
    }
}

