Shader "StargazingHill/Moon"
{
    Properties
    {
        _Color ("Color", Color) = (0.84,0.89,1,1)
        _Intensity ("Intensity", Range(0, 4)) = 1.4
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            fixed4 _Color;
            half _Intensity;
            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                float radius = length(centered);
                half disk = 1.0 - smoothstep(0.88, 1.0, radius);
                half limb = saturate(1.0 - radius * radius * 0.42);
                return fixed4(_Color.rgb * _Intensity * limb, disk * _Color.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
