Shader "StargazingHill/Environment"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _Ambient ("Ambient", Range(0, 1)) = 0.35
        _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        [Toggle] _UseTexture ("Use Texture", Float) = 0
        [Toggle] _UseNormal ("Use Normal", Float) = 0
        [Toggle] _UseVertexColor ("Use Vertex Color", Float) = 1
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.36
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Off
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half3 normal : TEXCOORD0;
                half3 tangent : TEXCOORD1;
                half3 binormal : TEXCOORD2;
                float2 uv : TEXCOORD3;
                fixed4 color : COLOR;
                UNITY_FOG_COORDS(4)
            };

            fixed4 _Color;
            half _Ambient;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _BumpMap;
            half _UseTexture;
            half _UseNormal;
            half _UseVertexColor;
            half _Cutoff;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
                o.binormal = cross(o.normal, o.tangent) * (v.tangent.w * unity_WorldTransformParams.w);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 sampled = tex2D(_MainTex, i.uv);
                fixed4 albedo = lerp(fixed4(1, 1, 1, 1), sampled, _UseTexture);
                #ifdef _ALPHATEST_ON
                    clip(albedo.a - _Cutoff);
                #endif

                half3 worldNormal = normalize(i.normal);
                if (_UseNormal > 0.5)
                {
                    half3 tangentNormal = UnpackNormal(tex2D(_BumpMap, i.uv));
                    worldNormal = normalize(
                        normalize(i.tangent) * tangentNormal.x +
                        normalize(i.binormal) * tangentNormal.y +
                        worldNormal * tangentNormal.z);
                }
                half ndotl = saturate(dot(worldNormal, normalize(_WorldSpaceLightPos0.xyz)));
                half3 lighting = _Ambient + ndotl * (1.0 - _Ambient) * _LightColor0.rgb;
                fixed3 vertexColor = lerp(fixed3(1, 1, 1), i.color.rgb, _UseVertexColor);
                fixed4 result = fixed4(_Color.rgb * vertexColor * albedo.rgb * lighting, 1.0);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
