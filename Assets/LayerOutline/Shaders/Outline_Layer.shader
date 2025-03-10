Shader "Unlit/Outline_Layer"
{
    Properties
    {
        _LayerMask ("Layer Mask Texture", 2D) = ""{}
		_Thickness ("Outline Thickness", float) = 10  
        _Color ("Outline Color", Color) = (1,1,1,1)
    }
	
	SubShader
	{
		Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
		ZWrite Off Cull Off
		
		Pass
		{
			Name "DrawOutlines"
			
			HLSLPROGRAM
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
			
			#pragma vertex Vert
			#pragma fragment Frag


			// sampler2D _LayerMask;
			// tex2D(_LayerMask, directions[i].a);
			

			Texture2D _LayerMask;
			float4    _LayerMask_TexelSize;
			float4    _Color;
			float     _Thickness;
			

			SamplerState point_clamp_sampler;


			#define COS_01_16_SIN_15_16 0.9951847266721968
			#define COS_02_16_SIN_14_16 0.9807852804032304
			#define COS_03_16_SIN_13_16 0.9569403357322088
			#define COS_04_16_SIN_12_16 0.9238795325112867
			#define COS_05_16_SIN_11_16 0.8819212643483550
			#define COS_06_16_SIN_10_16 0.8314696123025452
			#define COS_07_16_SIN_09_16 0.7730104533627369
			#define COS_08_16_SIN_08_16 0.7071067811865475
			#define COS_09_16_SIN_07_16 0.6343932841636454
			#define COS_10_16_SIN_06_16 0.5555702330196022
			#define COS_11_16_SIN_05_16 0.4713967368259976
			#define COS_12_16_SIN_04_16 0.3826834323650897
			#define COS_13_16_SIN_03_16 0.2902846772544623
			#define COS_14_16_SIN_02_16 0.1950903220161282
			#define COS_15_16_SIN_01_16 0.0980171403295606


			void UVDirections(float2 _uv, float _distance, out float2 _directions[64])
			{
				float2 l = float2(_LayerMask_TexelSize.x * _distance, _LayerMask_TexelSize.y * _distance);

				_directions[0]  = _uv + l * float2(0, 1);
				_directions[1]  = _uv + l * float2(COS_15_16_SIN_01_16, COS_01_16_SIN_15_16);
				_directions[2]  = _uv + l * float2(COS_14_16_SIN_02_16, COS_02_16_SIN_14_16);
				_directions[3]  = _uv + l * float2(COS_13_16_SIN_03_16, COS_03_16_SIN_13_16);
				_directions[4]  = _uv + l * float2(COS_12_16_SIN_04_16, COS_04_16_SIN_12_16);
				_directions[5]  = _uv + l * float2(COS_11_16_SIN_05_16, COS_05_16_SIN_11_16);
				_directions[6]  = _uv + l * float2(COS_10_16_SIN_06_16, COS_06_16_SIN_10_16);
				_directions[7]  = _uv + l * float2(COS_09_16_SIN_07_16, COS_07_16_SIN_09_16);
				_directions[8]  = _uv + l * float2(COS_08_16_SIN_08_16, COS_08_16_SIN_08_16);
				_directions[9]  = _uv + l * float2(COS_07_16_SIN_09_16, COS_09_16_SIN_07_16);
				_directions[10] = _uv + l * float2(COS_06_16_SIN_10_16, COS_10_16_SIN_06_16);
				_directions[11] = _uv + l * float2(COS_05_16_SIN_11_16, COS_11_16_SIN_05_16);
				_directions[12] = _uv + l * float2(COS_04_16_SIN_12_16, COS_12_16_SIN_04_16);
				_directions[13] = _uv + l * float2(COS_03_16_SIN_13_16, COS_13_16_SIN_03_16);
				_directions[14] = _uv + l * float2(COS_02_16_SIN_14_16, COS_14_16_SIN_02_16);
				_directions[15] = _uv + l * float2(COS_01_16_SIN_15_16, COS_15_16_SIN_01_16);

				_directions[16] = _uv + l * float2(1, 0);
				_directions[17] = _uv + l * float2(COS_01_16_SIN_15_16, -COS_15_16_SIN_01_16);
				_directions[18] = _uv + l * float2(COS_02_16_SIN_14_16, -COS_14_16_SIN_02_16);
				_directions[19] = _uv + l * float2(COS_03_16_SIN_13_16, -COS_13_16_SIN_03_16);
				_directions[20] = _uv + l * float2(COS_04_16_SIN_12_16, -COS_12_16_SIN_04_16);
				_directions[21] = _uv + l * float2(COS_05_16_SIN_11_16, -COS_11_16_SIN_05_16);
				_directions[22] = _uv + l * float2(COS_06_16_SIN_10_16, -COS_10_16_SIN_06_16);
				_directions[23] = _uv + l * float2(COS_07_16_SIN_09_16, -COS_09_16_SIN_07_16);
				_directions[24] = _uv + l * float2(COS_08_16_SIN_08_16, -COS_08_16_SIN_08_16);
				_directions[25] = _uv + l * float2(COS_09_16_SIN_07_16, -COS_07_16_SIN_09_16);
				_directions[26] = _uv + l * float2(COS_10_16_SIN_06_16, -COS_06_16_SIN_10_16);
				_directions[27] = _uv + l * float2(COS_11_16_SIN_05_16, -COS_05_16_SIN_11_16);
				_directions[28] = _uv + l * float2(COS_12_16_SIN_04_16, -COS_04_16_SIN_12_16);
				_directions[29] = _uv + l * float2(COS_13_16_SIN_03_16, -COS_03_16_SIN_13_16);
				_directions[30] = _uv + l * float2(COS_14_16_SIN_02_16, -COS_02_16_SIN_14_16);
				_directions[31] = _uv + l * float2(COS_15_16_SIN_01_16, -COS_01_16_SIN_15_16);

				_directions[32] = _uv + l * float2(0, -1);
				_directions[33] = _uv + l * float2(-COS_15_16_SIN_01_16, -COS_01_16_SIN_15_16);
				_directions[34] = _uv + l * float2(-COS_14_16_SIN_02_16, -COS_02_16_SIN_14_16);
				_directions[35] = _uv + l * float2(-COS_13_16_SIN_03_16, -COS_03_16_SIN_13_16);
				_directions[36] = _uv + l * float2(-COS_12_16_SIN_04_16, -COS_04_16_SIN_12_16);
				_directions[37] = _uv + l * float2(-COS_11_16_SIN_05_16, -COS_05_16_SIN_11_16);
				_directions[38] = _uv + l * float2(-COS_10_16_SIN_06_16, -COS_06_16_SIN_10_16);
				_directions[39] = _uv + l * float2(-COS_09_16_SIN_07_16, -COS_07_16_SIN_09_16);
				_directions[40] = _uv + l * float2(-COS_08_16_SIN_08_16, -COS_08_16_SIN_08_16);
				_directions[41] = _uv + l * float2(-COS_07_16_SIN_09_16, -COS_09_16_SIN_07_16);
				_directions[42] = _uv + l * float2(-COS_06_16_SIN_10_16, -COS_10_16_SIN_06_16);
				_directions[43] = _uv + l * float2(-COS_05_16_SIN_11_16, -COS_11_16_SIN_05_16);
				_directions[44] = _uv + l * float2(-COS_04_16_SIN_12_16, -COS_12_16_SIN_04_16);
				_directions[45] = _uv + l * float2(-COS_03_16_SIN_13_16, -COS_13_16_SIN_03_16);
				_directions[46] = _uv + l * float2(-COS_02_16_SIN_14_16, -COS_14_16_SIN_02_16);
				_directions[47] = _uv + l * float2(-COS_01_16_SIN_15_16, -COS_15_16_SIN_01_16);

				_directions[48] = _uv + l * float2(-1, 0);
				_directions[49] = _uv + l * float2(-COS_01_16_SIN_15_16, COS_15_16_SIN_01_16);
				_directions[50] = _uv + l * float2(-COS_02_16_SIN_14_16, COS_14_16_SIN_02_16);
				_directions[51] = _uv + l * float2(-COS_03_16_SIN_13_16, COS_13_16_SIN_03_16);
				_directions[52] = _uv + l * float2(-COS_04_16_SIN_12_16, COS_12_16_SIN_04_16);
				_directions[53] = _uv + l * float2(-COS_05_16_SIN_11_16, COS_11_16_SIN_05_16);
				_directions[54] = _uv + l * float2(-COS_06_16_SIN_10_16, COS_10_16_SIN_06_16);
				_directions[55] = _uv + l * float2(-COS_07_16_SIN_09_16, COS_09_16_SIN_07_16);
				_directions[56] = _uv + l * float2(-COS_08_16_SIN_08_16, COS_08_16_SIN_08_16);
				_directions[57] = _uv + l * float2(-COS_09_16_SIN_07_16, COS_07_16_SIN_09_16);
				_directions[58] = _uv + l * float2(-COS_10_16_SIN_06_16, COS_06_16_SIN_10_16);
				_directions[59] = _uv + l * float2(-COS_11_16_SIN_05_16, COS_05_16_SIN_11_16);
				_directions[60] = _uv + l * float2(-COS_12_16_SIN_04_16, COS_04_16_SIN_12_16);
				_directions[61] = _uv + l * float2(-COS_13_16_SIN_03_16, COS_03_16_SIN_13_16);
				_directions[62] = _uv + l * float2(-COS_14_16_SIN_02_16, COS_02_16_SIN_14_16);
				_directions[63] = _uv + l * float2(-COS_15_16_SIN_01_16, COS_01_16_SIN_15_16);
				
				// 16 subdivisions de 5.625 degrés par quartier soit 15 directions intermédiaires par quartiers + 4 directions de base  (haut, droite, bas, gauche)
			}


			float MaximumAlphaAround(float2 _uv, float _distance)
			{
				float2 directions[64];
				UVDirections(_uv, _distance, directions);

				float maxAlpha = 0;
				
				for (int i = 0; i < 64; i++)
				{
					maxAlpha = max(maxAlpha, _LayerMask.SampleLevel(point_clamp_sampler, directions[i], 0).a);
					if (maxAlpha == 1)
						break;
				}

				return maxAlpha;
			}


			float4 Frag(Varyings input) : SV_Target
			{
				float2 uv = input.texcoord.xy;

				float max_alpha_around = MaximumAlphaAround(uv, _Thickness);

				float this_alpha = _LayerMask.Sample(point_clamp_sampler, uv).a;

				float4 color = lerp(_BlitTexture.Sample(point_clamp_sampler, uv), float4(0, 0, 0, 0), this_alpha);
				
				float alpha = max(0, max_alpha_around - this_alpha);

				color = lerp(color, _Color, alpha);

				return color;
			}
			ENDHLSL
		}
	}
	//CustomEditor "OutlineShaderCustomEditor"
}
