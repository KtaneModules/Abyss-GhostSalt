Shader "PostProcess/Vignette"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Vignette("Vignette", Float) = 0.0
		_Grayscale("Grayscale", Float) = 0.0
	}

		SubShader
		{
			Pass
			{
				CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag

				#include "UnityCG.cginc"

				uniform sampler2D _MainTex;
				uniform float _Vignette;
				uniform float _Grayscale;

				float4 frag(v2f_img i) : COLOR
				{
					float2 uvOffset = float2(i.uv.x - 0.5, i.uv.y - 0.5);

					float4 color = lerp(tex2D(_MainTex, i.uv), float4(0, 0, 0, 1), _Vignette * length(uvOffset));
					float gray = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));

					return lerp(color, float4(gray, gray, gray, gray), _Grayscale);
				}
				ENDCG
			}
		}
}