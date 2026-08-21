Shader "StargazingHill/NightModeOverlay"
{
    Properties
    {
        _Darkness ("Darkness", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Overlay-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Front
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            float _Darkness;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            // Pure black. A tinted overlay lifted the floor of an already very dark night scene
            // instead of darkening it, which read as a pale haze rather than nightfall. With black,
            // SrcAlpha/OneMinusSrcAlpha is a straight multiply by (1 - _Darkness), so full darkness
            // is genuinely black.
            fixed4 frag(v2f i) : SV_Target { return fixed4(0, 0, 0, _Darkness); }
            ENDCG
        }
    }
}
