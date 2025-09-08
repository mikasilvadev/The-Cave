// Este código não é C#, é HLSL (linguagem de shader)
Shader "Custom/ColorAndOutlineShader"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.5, 0.5, 0.5, 1) // Cinza por padrão
        _OutlineColor("Outline Color", Color) = (1, 1, 0, 1)
        _OutlinePower("Outline Power", Range(1.0, 20.0)) = 5.0
        _OutlineWidth("Outline Width", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldNormal : NORMAL;
                float3 viewDir : TEXCOORD1;
            };

            fixed4 _BaseColor;
            fixed4 _OutlineColor;
            float _OutlinePower;
            float _OutlineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, v.vertex).xyz));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calcula o efeito Fresnel para o contorno
                float fresnel = 1.0 - saturate(dot(i.worldNormal, i.viewDir));
                float fresnelPowered = pow(fresnel, _OutlinePower);
                float outline = step(_OutlineWidth, fresnelPowered);
                
                // Mistura a cor base com a cor do contorno
                fixed4 finalColor = lerp(_BaseColor, _OutlineColor, outline);
                
                return finalColor;
            }
            ENDCG
        }
    }
}