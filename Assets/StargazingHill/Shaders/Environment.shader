Shader "StargazingHill/Environment"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _Ambient ("Ambient", Range(0, 1)) = 0.35
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
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half3 normal : TEXCOORD0;
                fixed4 color : COLOR;
            };

            fixed4 _Color;
            half _Ambient;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half ndotl = saturate(dot(normalize(i.normal), normalize(_WorldSpaceLightPos0.xyz)));
                half lighting = _Ambient + ndotl * (1.0 - _Ambient) * _LightColor0.a;
                return fixed4(_Color.rgb * i.color.rgb * lighting, 1.0);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
