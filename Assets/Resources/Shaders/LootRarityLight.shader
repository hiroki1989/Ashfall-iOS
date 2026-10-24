Shader "NeonVault/LootRarityLight"
{
    Properties
    {
        _Color ("Rarity color", Color) = (1,1,1,1)
        _Ground ("Ground glow", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        ZTest LEqual
        Cull Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _Ground;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                float halo = pow(saturate(1.0 - length(input.uv - .5) * 2.0), 2.3);
                float centre = pow(saturate(1.0 - abs(input.uv.x * 2.0 - 1.0)), 2.5);
                float rise = smoothstep(0.0, .12, input.uv.y);
                float fade = 1.0 - smoothstep(.38, 1.0, input.uv.y);
                float pulse = .88 + .12 * sin(_Time.y * 2.8 + input.uv.y * 6.0);
                float alpha = _Ground > .5 ? halo * .34 : centre * rise * fade * .43 * pulse;
                return fixed4(_Color.rgb * 1.25, alpha * _Color.a);
            }
            ENDCG
        }
    }
}
