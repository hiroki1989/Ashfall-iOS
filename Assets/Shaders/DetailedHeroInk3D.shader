Shader "NeonVault/DetailedHeroInk3D"
{
    Properties
    {
        _Color ("Pigment", Color) = (.35,.3,.2,1)
        _SurfaceKind ("Surface", Float) = 1
        _Glossiness ("Polish", Range(0,1)) = .2
        _Metallic ("Metal", Range(0,1)) = 0
        _InkColor ("Ink", Color) = (.035,.028,.022,1)
        _Outline ("Contour width", Range(0,.015)) = .00085
        _RustAmount ("Exposed rust", Range(0,1)) = .72
        _Grain ("Surface grain", Range(0,1)) = .55
        _CreaseInk ("Crease ink", Range(0,1)) = .18
        _MaterialAtlas ("Painted canvas / leather / steel / walnut", 2D) = "white" {}
        _AtlasStrength ("Authored surface detail", Range(0,1)) = 0
        _CreatureAtlas ("Creature skin / hide / bone / membrane", 2D) = "white" {}
        _CreatureSurface ("Creature material quadrant", Float) = -1
        _MicroRelief ("Material surface relief", Range(0,2)) = .7
        [HideInInspector] _ProjectionSheet ("Equipment projection", 2D) = "white" {}
        [HideInInspector] _ProjectionUv ("Equipment projection crop", Vector) = (0,0,1,1)
        [HideInInspector] _ProjectionSideUv ("Weapon side illustration bounds", Vector) = (.02,.35,.96,.3)
        [HideInInspector] _AccessoryProjectionSheet ("Accessory projection", 2D) = "white" {}
        [HideInInspector] _AccessoryProjectionUv ("Accessory projection crop", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 250
        Pass
        {
            Name "CONTOUR"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Outline; float4 _InkColor;
            struct v2f { float4 pos:SV_POSITION; };
            v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex + float4(v.normal*_Outline,0)); return o; }
            float4 frag(v2f i):SV_Target { return _InkColor; }
            ENDCG
        }
        CGPROGRAM
        #pragma surface surf Survivor fullforwardshadows addshadow vertex:SurfaceVertex
        #pragma target 3.0
        #include "UnityCG.cginc"
        #include "UnityGlobalIllumination.cginc"
        float4 _Color, _InkColor;
        float _SurfaceKind,_Glossiness,_Metallic,_Grain,_CreaseInk,_RustAmount;
        sampler2D _MaterialAtlas;
        float _AtlasStrength;
        sampler2D _CreatureAtlas;
        float _CreatureSurface,_MicroRelief;
        sampler2D _ProjectionSheet,_AccessoryProjectionSheet;
        float4 _ProjectionUv,_ProjectionSideUv,_AccessoryProjectionUv,_ProjectionMin,_ProjectionSize;
        float4x4 _ProjectionWorldToRoot,_ProjectionWorldNormalToRoot;
        float _ProjectionMode,_ProjectionEnabled,_AccessoryProjectionEnabled;
        // Full precision avoids Metal cross-compiler min-precision bytecode failures.
        struct SurfaceOutputSurvivor { float3 Albedo; float3 Normal; float3 Emission; float Specular; float Gloss; float Alpha; };
        struct Input { float3 worldPos; float3 geometricNormal; float4 detailTangent; float2 accessoryUV; float accessoryMask; };
        void SurfaceVertex(inout appdata_full v,out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.geometricNormal=UnityObjectToWorldNormal(v.normal);
            o.detailTangent=float4(UnityObjectToWorldDir(v.tangent.xyz),v.tangent.w*unity_WorldTransformParams.w);
            o.accessoryUV=v.texcoord2.xy;o.accessoryMask=v.texcoord3.x;
        }
        float hash3(float3 p) { p=frac(p*.1031); p+=dot(p,p.yzx+33.33); return frac((p.x+p.y)*p.z); }
        float noise3(float3 p)
        {
            float3 i=floor(p),f=frac(p); f=f*f*(3-2*f);
            return lerp(lerp(lerp(hash3(i),hash3(i+float3(1,0,0)),f.x),lerp(hash3(i+float3(0,1,0)),hash3(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash3(i+float3(0,0,1)),hash3(i+float3(1,0,1)),f.x),lerp(hash3(i+float3(0,1,1)),hash3(i+1),f.x),f.y),f.z);
        }
        float3 AtlasPlane(float2 uv,float2 cell)
        {
            // Keep derivatives continuous over repeat boundaries and samples inside one quadrant.
            float2 coord=cell+(.006+frac(uv)*.988)*.5;
            return tex2Dgrad(_MaterialAtlas,coord,ddx(uv)*.494,ddy(uv)*.494).rgb;
        }
        float3 PaintedMaterial(float3 p,float3 normal)
        {
            float2 cell=_SurfaceKind<1.5?float2(0,.5):_SurfaceKind<2.5?float2(.5,.5):_SurfaceKind<3.5?float2(0,0):float2(.5,0);
            float3 weights=pow(abs(normal),6);weights/=max(.001,weights.x+weights.y+weights.z);
            float scale=_SurfaceKind<1.5?3.2:_SurfaceKind<2.5?5:3;
            return AtlasPlane(p.zy*scale,cell)*weights.x+AtlasPlane(p.xz*scale,cell)*weights.y+AtlasPlane(p.xy*scale,cell)*weights.z;
        }
        float3 CreaturePlane(float2 uv,float2 cell)
        {
            return tex2Dgrad(_CreatureAtlas,cell+(.006+frac(uv)*.988)*.5,ddx(uv)*.494,ddy(uv)*.494).rgb;
        }
        float3 CreatureMaterial(float3 p,float3 n)
        {
            float2 cell=_CreatureSurface<.5?float2(0,.5):_CreatureSurface<1.5?float2(.5,.5):_CreatureSurface<2.5?float2(0,0):float2(.5,0);
            float3 w=pow(abs(n),6);w/=max(.001,w.x+w.y+w.z);
            float scale=_CreatureSurface<1.5?4.5:3.5;
            return CreaturePlane(p.zy*scale,cell)*w.x+CreaturePlane(p.xz*scale,cell)*w.y+CreaturePlane(p.xy*scale,cell)*w.z;
        }
        float4 LightingSurvivor(SurfaceOutputSurvivor s, float3 viewDir, UnityGI gi)
        {
            float3 lightDir=gi.light.dir;
            float nl=dot(s.Normal,lightDir);
            // Soft boundaries avoid temporal sparkle while keeping five drawn value groups.
            float value=.15+.17*smoothstep(-.28,-.12,nl)+.24*smoothstep(.02,.20,nl)+.28*smoothstep(.32,.52,nl)+.28*smoothstep(.64,.84,nl);
            float spec=pow(saturate(dot(s.Normal,normalize(lightDir+viewDir))),lerp(18,95,s.Specular))*s.Gloss;
            float3 c=s.Albedo*(gi.light.color*value+gi.indirect.diffuse*.85);
            float3 specTint=lerp(s.Albedo,float3(.65,.65,.60),step(2.5,_SurfaceKind)*step(_SurfaceKind,3.5));
            if(_SurfaceKind>5.5 && _SurfaceKind<6.5)specTint=float3(.38,.28,.20);
            if(_SurfaceKind>6.5) specTint=float3(.7,.8,.8);
            // Painted firearms carry their own metal glints in the illustration.
            // A second procedural white highlight washes out their authored colors.
            if(!(_ProjectionEnabled>.5 && (_ProjectionMode<.5 || _ProjectionMode>2.5)))
                c+=gi.light.color*specTint*spec*.65;
            if(_SurfaceKind>6.5)
            {
                float fresnel=pow(1-saturate(dot(s.Normal,viewDir)),3);
                c+=s.Albedo*(.22+fresnel*.75);
            }
            return float4(c,s.Alpha);
        }
        void LightingSurvivor_GI(SurfaceOutputSurvivor s, UnityGIInput data, inout UnityGI gi)
        {
            gi=UnityGlobalIllumination(data,1,s.Normal);
        }
        void surf(Input IN, inout SurfaceOutputSurvivor o)
        {
            // Object coordinates follow moving limbs and prevent swimming world-space texture.
            float3 p=mul(unity_WorldToObject,float4(IN.worldPos,1)).xyz;
            float broad=noise3(p*7), fine=noise3(p*140);
            float pigment=.91+.16*broad, mark=0;
            float relief=0,polish=1;
            float3 color=_Color.rgb;
            if(_SurfaceKind>.5 && _SurfaceKind<1.5)
            {
                // Suppress weave above the pixel footprint to avoid shimmering at gameplay distance.
                float aa=1-smoothstep(.001,.008,length(fwidth(p)));
                float3 wave=sin(p*570)*aa;
                float weave=(wave.x*wave.y+wave.y*wave.z+wave.z*wave.x)*.012;
                pigment+=_Grain*(weave+(fine-.5)*.13);
                float worn=noise3(p*float3(34,55,31));
                pigment+=(worn-.5)*.17;
                mark=smoothstep(.83,.94,noise3(p*float3(25,120,24)))*.035;
                // Screen-space normal bend adds restrained ink inside authored fold valleys.
                float3 n=normalize(IN.geometricNormal);
                float bend=length(ddx(n))+length(ddy(n));
                mark+=saturate((bend-.12)*1.3)*_CreaseInk;
            }
            else if(_SurfaceKind<2.5 && _SurfaceKind>1.5)
            { pigment+=(fine-.5)*.16+(noise3(p*28)-.5)*.22; mark=smoothstep(.64,.82,noise3(p*float3(70,12,80)))*.25; color=lerp(color,color*1.35,smoothstep(.76,.88,noise3(p*float3(135,18,110)))*.5); }
            else if(_SurfaceKind<3.5 && _SurfaceKind>2.5)
            {
                float rust=smoothstep(.61,.79,noise3(p*31)+.08*noise3(p*95));
                color=lerp(color,float3(.19,.075,.028),rust*_RustAmount);
                pigment+=(fine-.5)*.10;
                mark=smoothstep(.84,.94,noise3(p*float3(170,9,35)))*.22;
            }
            else if(_SurfaceKind<4.5 && _SurfaceKind>3.5)
            { float grain=sin(p.y*180+noise3(p*float3(9,2,9))*12); pigment+=grain*.065; mark=smoothstep(.8,.96,grain)*.09; }
            else if(_SurfaceKind<5.5 && _SurfaceKind>4.5) { pigment+=(fine-.5)*.075+(broad-.5)*.12; float pore=smoothstep(.78,.92,noise3(p*390)); pigment-=pore*.065; float crease=length(ddx(normalize(IN.geometricNormal)))+length(ddy(normalize(IN.geometricNormal))); mark+=saturate((crease-.06)*1.8)*.12; }
            else if(_SurfaceKind<6.5 && _SurfaceKind>5.5) { pigment+=sin(p.y*120+noise3(p*14)*9)*.055; }
            else if(_SurfaceKind>6.5) { pigment=1; o.Emission=color*.075; }
            if(_ProjectionEnabled>.5)
            {
                // Reconstruct projection coordinates from the existing interpolated world
                // position and geometric normal; this avoids extra ForwardBase varyings.
                float3 projectionPos=mul(_ProjectionWorldToRoot,float4(IN.worldPos,1)).xyz;
                float3 projectionNormal=normalize(mul((float3x3)_ProjectionWorldNormalToRoot,IN.geometricNormal));
                float3 q=(projectionPos-_ProjectionMin)/_ProjectionSize;
                float facing=(_ProjectionMode<.5||_ProjectionMode>1.5)?projectionNormal.x:projectionNormal.z;
                float2 uv=_ProjectionMode<.5?float2(q.z,q.y):_ProjectionMode<1.5?float2(q.x,q.y):float2(q.y,q.z);
                if(facing<0)uv.x=1-uv.x;
                if(_ProjectionMode>2.5)
                {
                    // Four-view atlas: upper row has independent left/right side
                    // paintings; lower row carries muzzle/stock end materials.
                    // Side art uses its measured occupied bounds in the source tile.
                    bool endFace=abs(projectionNormal.z)>abs(projectionNormal.x)*1.35;
                    float tileX=endFace?(projectionNormal.z>0?0:.5):(projectionNormal.x>0?0:.5);
                    float tileY=endFace?0:.5;
                    uv=endFace?float2(q.x,q.y):float2(q.z,q.y);
                    if((endFace&&projectionNormal.z<0)||(!endFace&&projectionNormal.x<0))uv.x=1-uv.x;
                    uv=endFace?float2(tileX+.01+uv.x*.48,tileY+.01+uv.y*.48)
                        :float2(tileX+.5*(_ProjectionSideUv.x+uv.x*_ProjectionSideUv.z),
                                tileY+.5*(_ProjectionSideUv.y+uv.y*_ProjectionSideUv.w));
                }
                else uv=_ProjectionUv.xy+uv*_ProjectionUv.zw;
                float4 illustration=tex2D(_ProjectionSheet,uv);
                color=lerp(color,illustration.rgb,illustration.a);
            }
            if(_AccessoryProjectionEnabled>.5 && IN.accessoryMask>.5)
            {
                float4 detail=tex2D(_AccessoryProjectionSheet,_AccessoryProjectionUv.xy+IN.accessoryUV*_AccessoryProjectionUv.zw);
                color=lerp(color,detail.rgb,detail.a);
            }
            // Gun illustrations already contain their own wood, metal and wear colors.
            // The generic surface atlas is a color multiplier, so applying it after
            // projection shifts the authored palette (especially on painted rifles).
            if(_AtlasStrength>0 && !(_ProjectionEnabled>.5 && (_ProjectionMode<.5 || _ProjectionMode>2.5)) && _SurfaceKind>.5 && _SurfaceKind<4.5)
            {
                float3 objectNormal=normalize(mul((float3x3)unity_WorldToObject,IN.geometricNormal));
                float3 paint=PaintedMaterial(p,objectNormal);
                float3 average=_SurfaceKind<1.5?float3(.32,.30,.21):_SurfaceKind<2.5?float3(.35,.19,.10):_SurfaceKind<3.5?float3(.36,.33,.30):float3(.39,.18,.075);
                float3 variation=clamp(paint/average,.28,2.25);
                color*=lerp(float3(1,1,1),variation,_AtlasStrength);
                float grainHeight=dot(paint,float3(.2126,.7152,.0722));
                relief=grainHeight*(_SurfaceKind<1.5?.00035:_SurfaceKind<2.5?.00065:.0004)*_AtlasStrength;
                polish=_SurfaceKind<1.5?.55:_SurfaceKind<2.5?lerp(.55,1.1,grainHeight):lerp(.40,1.20,grainHeight);
            }
            if(_ProjectionEnabled>.5 && (_ProjectionMode<.5 || _ProjectionMode>2.5))
            {
                // Do not stamp procedural grain or wear over an authored gun image.
                pigment=1;
                mark=0;
            }
            if(_CreatureSurface>=0)
            {
                float3 n=normalize(mul((float3x3)unity_WorldToObject,IN.geometricNormal));
                float3 paint=CreatureMaterial(p,n);
                float3 average=_CreatureSurface<.5?float3(.59,.49,.43):_CreatureSurface<1.5?float3(.18,.17,.16):_CreatureSurface<2.5?float3(.66,.55,.36):float3(.39,.24,.25);
                color*=lerp(float3(1,1,1),clamp(paint/average,.3,2.0),.78);
                float h=dot(paint,float3(.2126,.7152,.0722));
                relief=h*(_CreatureSurface<1.5?.00065:.0009);
                polish=_CreatureSurface>2.5?lerp(.7,1.2,h):lerp(.35,.85,h);
            }
            // A derivative-based surface gradient gives the painted pores, weave and pits
            // real light response, using the mesh's authored tangent frame.
            float3 normal=normalize(IN.geometricNormal);
            float3 dpdx=ddx(IN.worldPos),dpdy=ddy(IN.worldPos);
            float3 r1=cross(dpdy,normal),r2=cross(normal,dpdx);
            float determinant=dot(dpdx,r1);
            o.Normal=float3(0,0,1);
            if(abs(determinant)>1e-10 && dot(IN.detailTangent.xyz,IN.detailTangent.xyz)>.1)
            {
                float3 gradient=(ddx(relief)*r1+ddy(relief)*r2)/determinant;
                float3 bumped=normalize(normal-gradient*_MicroRelief);
                float3 tangent=normalize(IN.detailTangent.xyz-normal*dot(normal,IN.detailTangent.xyz));
                float3 bitangent=cross(normal,tangent)*IN.detailTangent.w;
                o.Normal=normalize(float3(dot(bumped,tangent),dot(bumped,bitangent),dot(bumped,normal)));
            }
            o.Albedo=max(.005,color*pigment*(1-mark));
            o.Specular=_Glossiness;
            o.Gloss=_Glossiness*_Glossiness*lerp(.35,.75,_Metallic)*polish;
            o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}

