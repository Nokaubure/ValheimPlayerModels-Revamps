Shader "Valheim/Standard" {
	Properties {
		_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
		_MainTex ("Skin (RGB)", 2D) = "white" {}
		_SkinBumpMap ("Skin bumpMap", 2D) = "bump" {}
		_SkinColor ("Skin color", Color) = (1,1,1,1)
		_BumpScale ("Normal scale", Float) = 1
		_MetallicGlossMap ("Metallic", 2D) = "grey" {}
		_Glossiness ("Smoothness", Range(0, 1)) = 0.6
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		float _Cutoff;
		float4 _SkinColor;
		sampler2D _MainTex;
		sampler2D _Glossiness;
		sampler2D _MetallicGlossMap;
		struct Input
		{
			float2 uv_MainTex;
			float2 uv_Glossiness;
			float2 uv_MetalGlossMap;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex)*_SkinColor;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			clip(c.a-_Cutoff);
			fixed4 g = tex2D(_Glossiness, IN.uv_MainTex);
			fixed4 m = tex2D(_MetallicGlossMap, IN.uv_MainTex);
			o.Smoothness = g.rgb;
			o.Metallic = m.rgb;
		}
		ENDCG
	}
	Fallback "Diffuse"
}