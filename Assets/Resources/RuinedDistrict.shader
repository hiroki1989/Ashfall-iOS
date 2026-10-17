Shader "NeonVault/RuinedDistrict"
{
    Properties
    {
        _MainTex("Ruin surface atlas",2D)="white"{}
        _Color("Weathered tint",Color)=(1,1,1,1)
        _Tile("Atlas quadrant",Vector)=(0,0.5,0,0)
        _Scale("World texture scale",Float)=0.8
    }
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        float4 _Tile;
        float _Scale;
        struct Input {float3 worldPos;float3 worldNormal;};
        float3 sampleRuin(float2 uv)
        {
            // Half-texel margins prevent neighboring atlas quadrants bleeding in.
            return tex2D(_MainTex,_Tile.xy+0.003+(1-abs(frac(uv*.5)*2-1))*0.494).rgb;
        }
        void surf(Input i,inout SurfaceOutputStandard o)
        {
            float3 w=pow(abs(i.worldNormal),4);w/=max(dot(w,float3(1,1,1)),0.0001);
            float3 p=i.worldPos*_Scale;
            float3 tex=sampleRuin(p.zy)*w.x+sampleRuin(p.xz)*w.y+sampleRuin(p.xy)*w.z;
            if(_Tile.x>.25 && _Tile.y<.25) { float paint=saturate((tex.r-tex.b-.035)*15); tex=lerp(tex,dot(tex,float3(.299,.587,.114))*.72,paint); float soot=.78+.18*sin(i.worldPos.x*.17+sin(i.worldPos.z*.23))*sin(i.worldPos.z*.19); tex*=soot; }
            o.Albedo=tex*_Color.rgb;
            o.Metallic=_Tile.z;
            o.Smoothness=0.13;
            o.Occlusion=0.88;
            o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
