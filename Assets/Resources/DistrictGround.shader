Shader "Ashfall/DistrictGround" {
 Properties { _Generated("Generated ground",2D)="white"{} _Road("Road",Float)=0 _MainTex("Broken pavement",2D)="white"{} _BumpMap("Paving normal",2D)="bump"{} _GrassTex("Grass",2D)="white"{} _DustTex("Dust",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Park("Park",Float)=0 }
 SubShader { Tags {"RenderType"="Opaque"} LOD 200
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 sampler2D _Generated;float _Road;
 sampler2D _MainTex,_GrassTex,_DustTex,_BumpMap;fixed4 _Color;float _Park;
 struct Input {float3 worldPos;};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 void surf(Input IN,inout SurfaceOutputStandard o){
 float2 p=IN.worldPos.xz;float macro=noise(p*.045)+noise(p*.13)*.25;
 float2 uv=frac(p/7)*.486+.007;
 fixed3 paving=tex2D(_Generated,uv+float2(.5,.5)).rgb;
 fixed3 grass=tex2D(_Generated,uv+float2(.5,0)).rgb;
 fixed3 dust=tex2D(_Generated,uv).rgb;
 fixed3 asphalt=tex2D(_Generated,uv+float2(0,.5)).rgb;
 float growth=smoothstep(.38,.78,macro)*lerp(.7,.95,_Park);growth=lerp(growth,.7+.23*noise(p*.04),_Park);
 float erosion=smoothstep(.40,.7,noise(p*.075+13))*.85;
 fixed3 ground=lerp(paving,dust,erosion);ground=lerp(ground,grass,growth);
 o.Normal=normalize(lerp(float3(0,0,1),UnpackNormal(tex2D(_BumpMap,p/5)),.55*(1-growth)*(1-erosion)));
 ground=lerp(ground,asphalt,_Road);
 float h=dot(ground,float3(.3,.59,.11));float2 slope=float2(ddx(h),ddy(h));o.Normal=normalize(float3(-slope.x*2,-slope.y*2,1));
 o.Albedo=ground*(.86+.2*noise(p*.018))*_Color.rgb;o.Metallic=0;o.Smoothness=.06;o.Occlusion=.9;
 }
 ENDCG
 } Fallback "Diffuse"
}
