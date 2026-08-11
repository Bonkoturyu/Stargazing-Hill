Shader "StargazingHill/Starfield"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 4)) = 1.35
        _HorizonStart ("Horizon Start", Range(-0.2, 0.3)) = 0.0
        _HorizonFull ("Horizon Full", Range(0.01, 0.5)) = 0.259
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float horizon : TEXCOORD1;
            };

            float _Intensity;
            float _HorizonStart;
            float _HorizonFull;

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 viewDirection = normalize(worldPosition - _WorldSpaceCameraPos.xyz);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.horizon = smoothstep(_HorizonStart, _HorizonFull, viewDirection.y);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centered = i.uv * 2.0 - 1.0;
                float radiusSquared = dot(centered, centered);
                float core = saturate(1.0 - radiusSquared);
                core *= core;
                float sparkle = saturate(1.0 - radiusSquared * 3.0);
                float alpha = (core + sparkle * 0.45) * i.horizon * _Intensity;
                return fixed4(i.color.rgb * i.color.a * alpha, alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
