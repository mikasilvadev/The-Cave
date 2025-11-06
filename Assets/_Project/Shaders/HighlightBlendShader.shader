Shader "Custom/HighlightBlendShader"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB)", 2D) = "white" {}

        _HighlightColor ("Highlight Color", Color) = (1,1,1,1)
        _HighlightIntensity ("Highlight Intensity", Range(0,1)) = 0.5
        _HighlightTex ("Highlight Texture", 2D) = "white" {}

        _BlendMode ("Blend Mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _Color;

            sampler2D _HighlightTex;
            float4 _HighlightColor;
            float _HighlightIntensity;

            float _BlendMode;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                fixed4 highlight = tex2D(_HighlightTex, i.uv) * _HighlightColor;

                fixed4 finalColor = lerp(col, highlight, _HighlightIntensity);

                finalColor.a = 1.0;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                return finalColor;
            }
            ENDCG
        }
    }
}