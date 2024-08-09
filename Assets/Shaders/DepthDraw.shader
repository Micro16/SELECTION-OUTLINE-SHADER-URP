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

			
			float4 Frag(Varyings input) : SV_Target
			{
				// this is needed so we account XR platform differences in how they handle texture arrays
				// UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float2 uv = input.texcoord.xy;


				float rd = SAMPLE_DEPTH_TEXTURE(_SelectionDepth, point_clamp_sampler, uv);
				float ld = Linear01Depth(rd, _ZBufferParams);

				float4 white = float4(1.0, 1.0, 1.0, 1.0);

				if (ld < 1.0f)
					return white;
				else
					return _BlitTexture.Sample(point_clamp_sampler, uv);

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