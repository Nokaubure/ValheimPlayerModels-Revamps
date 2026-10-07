Shader "Valheim/Standard Transparent"
{
    Properties
    {
        _MainTex ("Skin (RGBA)", 2D) = "white" {}
        _SkinBumpMap ("Skin bump map", 2D) = "bump" {}
        _SkinColor ("Skin color", Color) = (1,1,1,1)
        _BumpScale ("Normal scale", Float) = 1
        _MetallicGlossMap ("Metallic", 2D) = "grey" {}
        _Glossiness ("Smoothness", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _SkinBumpMap;
        sampler2D _MetallicGlossMap;
        fixed4 _SkinColor;
        half _BumpScale;
        half _Glossiness;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_SkinBumpMap;
            float2 uv_MetallicGlossMap;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 color = tex2D(_MainTex, IN.uv_MainTex) * _SkinColor;
            fixed4 metallic = tex2D(_MetallicGlossMap, IN.uv_MetallicGlossMap);

            o.Albedo = color.rgb;
            o.Alpha = color.a;
            o.Normal = UnpackScaleNormal(tex2D(_SkinBumpMap, IN.uv_SkinBumpMap), _BumpScale);
            o.Metallic = metallic.r;
            o.Smoothness = _Glossiness * metallic.a;
        }
        ENDCG
    }

    FallBack "Transparent/VertexLit"
}
