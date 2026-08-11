Shader "StargazingHill/Meteor"
{
    Properties
    {
        _Color ("Color", Color) = (0.65,0.82,1,1)
        _Intensity ("Intensity", Range(0, 8)) = 6.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

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
                half edge = saturate(1.0 - abs(input.uv.x * 2.0 - 1.0));
                half tail = smoothstep(0.0, 0.18, input.uv.y) * pow(input.uv.y, 1.8);
                half head = 1.0 - smoothstep(0.96, 1.0, input.uv.y);
                half brightness = edge * tail * head * _Intensity;
                return fixed4(_Color.rgb * brightness, brightness);
            }
            ENDCG
        }
    }
}
