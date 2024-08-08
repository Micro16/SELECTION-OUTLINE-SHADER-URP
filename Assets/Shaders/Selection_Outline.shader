Shader "Unlit/Selection_Outline"
{
	Properties
    {
        _OutlineThickness ("Outline Thickness", float) = 10  
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _ScaleWithDepth("Scale With Depth", float) = 1
		_MaxDepth("Maximum Depth", float) = 0.065
		_MinThickness("Minimum Thickness", float) = 1
    }
	
	
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
			float _OutlineThickness;
			float4 _OutlineColor;
			float _ScaleWithDepth;
			float _MaxDepth;
			float _MinThickness;

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


			float MinimumDepthAround(float2 _uv, float _distance)
			{
				float2 directions[8];
				UVDirections(_uv, _distance, directions);

				float minDepth = 1;
				for (int i = 0; i < 8; i++)
				{
					float rd = SAMPLE_DEPTH_TEXTURE(_SelectionDepth, point_clamp_sampler, directions[i]);
					float ld = Linear01Depth(rd, _ZBufferParams);
					minDepth = min(minDepth, ld);
				}

				return minDepth;
			}


			float ScaleOutlineThickness(float2 _uv, float _thickness, float _maxDepth, float _scale)
			{
				float minDepth = MinimumDepthAround(_uv, _thickness);

				float scaledDepth = 1 - clamp(minDepth * (1 / _maxDepth), 0, 1);

				float scaledThickness = max(_MinThickness, _thickness * scaledDepth);

				return lerp(_thickness, scaledThickness, _scale);
			}

			
			float4 Frag(Varyings input) : SV_Target
			{
				float2 uv = input.texcoord.xy;

				float scaledThickness = ScaleOutlineThickness(uv, _OutlineThickness, _MaxDepth, _ScaleWithDepth);
				
				float maxAlphaAround = MaximumAlphaAround(uv, scaledThickness);

				float alpha = max(0, maxAlphaAround - _SelectionColor.Sample(point_clamp_sampler, uv).a);

				float4 color = _BlitTexture.Sample(point_clamp_sampler, uv);
				color = lerp(color, _OutlineColor, alpha);

				return color;
			}
			ENDHLSL
		}
	}
	CustomEditor "OutlineShaderCustomEditor"
}