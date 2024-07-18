Shader "Unlit/DepthDraw"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
		ZWrite Off Cull Off
		
		Pass
		{
			Name "DepthPass"
			
			HLSLPROGRAM
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
			
			
			#pragma vertex Vert
			#pragma fragment Frag

			Texture2D _SelectionDepth;
			Texture2D _SelectionColor;
			SamplerState point_clamp_sampler;
			float4 _SelectionColor_TexelSize; 

			#define HALF_SQRT_2 0.70710678118

			
			void UVDirections(float2 _uv, float _distance, out float2 _directions[8])
			{
				float2 l = float2(_SelectionColor_TexelSize.x * _distance, _SelectionColor_TexelSize.y * _distance);
				_directions[0] = _uv + l * float2(0, 1);
				_directions[1] = _uv + l * float2(HALF_SQRT_2, HALF_SQRT_2);
				_directions[2] = _uv + l * float2(1, 0);
				_directions[3] = _uv + l * float2(HALF_SQRT_2, -HALF_SQRT_2);
				_directions[4] = _uv + l * float2(0, -1);
				_directions[5] = _uv + l * float2(-HALF_SQRT_2, -HALF_SQRT_2);
				_directions[6] = _uv + l * float2(-1, 0);
				_directions[7] = _uv + l * float2(-HALF_SQRT_2, HALF_SQRT_2);
			}


			float MaximumAlphaAround(float2 _uv, float _distance)
			{
				float2 directions[8];
				UVDirections(_uv, _distance, directions);

				float maxAlpha = 0;
				for (int i = 0; i < 8; i++)
				{
					maxAlpha = max(maxAlpha, _SelectionColor.Sample(point_clamp_sampler, directions[i]).a);
				}

				return maxAlpha;
			}

			
			float4 Frag(Varyings input) : SV_Target
			{
				// this is needed so we account XR platform differences in how they handle texture arrays
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float2 uv = input.texcoord.xy;

				// Sample depth texture 
				// float4 color = _SelectionColor.Sample(point_clamp_sampler, uv);
				// float4 depth = _SelectionDepth.Sample(point_clamp_sampler, uv);

				//float test = _SelectionColor_TexelSize.x;



				float distance = 4;
				
				float maxAlphaAround = MaximumAlphaAround(uv, distance);

				float alpha = max(0, maxAlphaAround - _SelectionColor.Sample(point_clamp_sampler, uv).a);

				float4 color = _BlitTexture.Sample(point_clamp_sampler, uv);
				color = lerp(color, float4(1, 1, 1, 1), alpha);

				return color;



				

				// float d = SAMPLE_DEPTH_TEXTURE(_SelectionDepth, point_clamp_sampler, uv);
				// float l = Linear01Depth(d, _ZBufferParams);

				// if (l < 1.0)
				// {
				// 	float4 color = float4(1,1,1,1);

				// 	return color * (1 - l);
				// }
				// else
				// {
				// 	return _BlitTexture.Sample(point_clamp_sampler, uv);
				// }

				

				// return _BlitTexture.Sample(point_clamp_sampler, uv);


				// sample the texture using the SAMPLE_TEXTURE2D_X_LOD
				
				//half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, _BlitMipLevel);
				// Inverts the sampled color
				// return depth;
			}
			ENDHLSL
		}
	}
}
