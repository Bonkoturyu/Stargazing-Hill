Shader "StargazingHill/Meteor"
{
    Properties
    {
        _TailColor ("Tail Color", Color) = (0.78,0.76,0.72,1)
        _CoreColor ("Core Color", Color) = (1.0,0.97,0.88,1)
        _HeadColor ("Head Color", Color) = (1.0,0.91,0.72,1)
        _Intensity ("Intensity", Range(0, 8)) = 3.4
        _CoreWidth ("Core Width", Range(0.02, 0.35)) = 0.14
        _HeadSize ("Head Size", Range(0.02, 0.30)) = 0.09
        _FlareStrength ("Flare Strength", Range(0, 2)) = 0.12
        _Afterglow ("Afterglow", Range(0, 1)) = 0.20
        _HorizonStart ("Horizon Start", Range(-0.2, 0.3)) = 0.0
        _HorizonFull ("Horizon Full", Range(0.01, 0.5)) = 0.208
        _ExtinctionCoefficient ("Atmospheric Extinction (mag/airmass)", Range(0, 1)) = 0.23
        _MinimumSinAltitude ("Minimum Sin Altitude", Range(0.01, 0.25)) = 0.05
    }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend One One
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "StargazingAtmosphere.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                half atmosphere : TEXCOORD1;
            };

            fixed4 _TailColor;
            fixed4 _CoreColor;
            fixed4 _HeadColor;
            half _Intensity;
            half _CoreWidth;
            half _HeadSize;
            half _FlareStrength;
            half _Afterglow;
            half _HorizonStart;
            half _HorizonFull;
            half _ExtinctionCoefficient;
            half _MinimumSinAltitude;

            v2f vert(appdata input)
            {
                v2f output;
                float3 worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                float3 viewDirection = normalize(worldPosition - _WorldSpaceCameraPos.xyz);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                half horizon = smoothstep(_HorizonStart, _HorizonFull, viewDirection.y);
                half transmission = StargazingAtmosphericTransmission(
                    viewDirection.y, _ExtinctionCoefficient, _MinimumSinAltitude);
                output.atmosphere = horizon * transmission;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                half x = abs(input.uv.x * 2.0 - 1.0);
                half y = saturate(input.uv.y);
                half longitudinal = smoothstep(0.0, 0.08, y) *
                                    (1.0 - smoothstep(0.93, 1.0, y));
                half tailEnvelope = pow(y, 0.72) * longitudinal;
                half tailWidth = lerp(0.10, 0.62, pow(y, 0.65));
                half softTail = pow(saturate(1.0 - x / max(0.02, tailWidth)), 2.2) * tailEnvelope;

                half coreWidth = max(0.015, _CoreWidth * lerp(0.38, 1.0, y));
                half core = pow(saturate(1.0 - x / coreWidth), 4.0) *
                            pow(y, 1.15) * longitudinal;

                half headAlong = pow(saturate(1.0 - abs(y - 0.88) / max(0.02, _HeadSize)), 2.0);
                half headWidth = lerp(0.30, 0.92, saturate(_FlareStrength));
                half head = pow(saturate(1.0 - x / headWidth), 2.4) * headAlong;
                half afterglow = pow(saturate(1.0 - x / max(0.05, tailWidth * 1.25)), 2.0) *
                                 pow(y, 0.42) * longitudinal * _Afterglow;

                fixed3 color = _TailColor.rgb * (softTail + afterglow) +
                               _CoreColor.rgb * core * 1.35 +
                               _HeadColor.rgb * head * (0.55 + _FlareStrength);
                half brightness = (softTail + afterglow + core * 1.35 +
                                   head * (0.55 + _FlareStrength)) * _Intensity;
                return fixed4(color * _Intensity * input.atmosphere,
                              brightness * input.atmosphere);
            }
            ENDCG
        }
    }
}
