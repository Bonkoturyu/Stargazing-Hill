Shader "StargazingHill/NightSkyGradient"
{
    Properties
    {
        _ZenithColor ("Zenith Color", Color) = (0.006,0.009,0.020,1)
        _HorizonColor ("Horizon Color", Color) = (0.020,0.050,0.095,1)
        _GroundColor ("Below Horizon Color", Color) = (0.004,0.006,0.012,1)
        _HorizonStrength ("Horizon Strength", Range(0,1)) = 1
        _HorizonFalloff ("Horizon Falloff", Range(1,8)) = 3
        _GroundFade ("Ground Fade", Range(0.02,0.5)) = 0.12
        _DitherStrength ("Dither", Range(0,0.002)) = 0.00035
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            fixed4 _ZenithColor, _HorizonColor, _GroundColor;
            float _HorizonStrength, _HorizonFalloff, _GroundFade, _DitherStrength;
            struct appdata { float4 vertex : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            float Hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 74.7);
                return frac(p.x * p.y);
            }
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float h = normalize(i.dir).y;
                float band = pow(1.0 - saturate(h), _HorizonFalloff) * _HorizonStrength;
                float3 sky = lerp(_ZenithColor.rgb, _HorizonColor.rgb, band);
                sky = lerp(sky, _GroundColor.rgb, saturate(-h / _GroundFade));
                float dither = (Hash21(i.pos.xy) + Hash21(i.pos.xy + 17.13) - 1.0) * _DitherStrength;
                return fixed4(max(sky + dither, 0.0), 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
