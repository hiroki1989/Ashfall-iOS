Shader "NeonVault/PaintedActorCutout"
{
 Properties {
 _MainTex("Painting",2D)="white"{} _Color("Color",Color)=(1,1,1,1)
 _NextTex("Adjacent direction",2D)="white"{} _NextCrop("Adjacent crop",Vector)=(0,0,1,1) _DirectionBlend("Direction interpolation",Range(0,1))=0
 _ArmorTex("Alternate armor painting",2D)="white"{} _ArmorCrop("Armor crop",Vector)=(0,0,1,1) _ArmorParts("Head chest arms legs variants",Vector)=(0,0,0,0) _ArmorFeet("Feet variant",Float)=0
 _HeadTint("Head",Color)=(1,1,1,1) _ChestTint("Chest",Color)=(1,1,1,1) _ArmsTint("Arms",Color)=(1,1,1,1)
 _LegsTint("Legs",Color)=(1,1,1,1) _FeetTint("Feet",Color)=(1,1,1,1) _Crop("Crop",Vector)=(0,0,1,1)
 }
 SubShader {
 Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout" "CanUseSpriteAtlas"="True"}
 Cull Off ZWrite On Blend SrcAlpha OneMinusSrcAlpha
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex,_NextTex,_ArmorTex;fixed4 _Color,_HeadTint,_ChestTint,_ArmsTint,_LegsTint,_FeetTint;float4 _Crop,_NextCrop,_ArmorCrop,_ArmorParts;float _DirectionBlend,_ArmorFeet;
 struct Input{float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct Output{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 Output vert(Input v){Output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
 fixed4 frag(Output i):SV_Target {
 float2 p=(i.uv-_Crop.xy)/max(_Crop.zw,float2(.0001,.0001));
 fixed4 a=tex2D(_MainTex,i.uv),b=tex2D(_NextTex,_NextCrop.xy+p*_NextCrop.zw);
 // Premultiplied interpolation prevents dark fringes at changing silhouettes.
 float alpha=lerp(a.a,b.a,_DirectionBlend);fixed4 c=fixed4(lerp(a.rgb*a.a,b.rgb*b.a,_DirectionBlend)/max(alpha,.001),alpha)*i.color;clip(c.a-.12);
 float head=smoothstep(.78,.84,p.y),feet=1-smoothstep(.12,.20,p.y),legs=(1-smoothstep(.37,.46,p.y))*(1-feet);
 float arms=smoothstep(.21,.32,abs(p.x-.5))*smoothstep(.34,.45,p.y)*(1-smoothstep(.73,.81,p.y));
 float armorMix=head*_ArmorParts.x+feet*_ArmorFeet+legs*_ArmorParts.w+arms*_ArmorParts.z+(1-head)*(1-feet)*(1-legs)*(1-arms)*_ArmorParts.y;
 fixed4 armor=tex2D(_ArmorTex,_ArmorCrop.xy+p*_ArmorCrop.zw);c.rgb=lerp(c.rgb,armor.rgb*i.color.rgb,saturate(armorMix)*smoothstep(.3,.7,armor.a));
 fixed3 tint=_ChestTint.rgb;
 tint=lerp(tint,_ArmsTint.rgb,smoothstep(.21,.32,abs(p.x-.5))*smoothstep(.34,.45,p.y)*(1-smoothstep(.73,.81,p.y)));
 tint=lerp(tint,_HeadTint.rgb,smoothstep(.77,.84,p.y));
 tint=lerp(tint,_LegsTint.rgb,1-smoothstep(.37,.46,p.y));
 tint=lerp(tint,_FeetTint.rgb,1-smoothstep(.12,.20,p.y));
 c.rgb*=tint;return c;
 }
 ENDCG
 }
 }
}
