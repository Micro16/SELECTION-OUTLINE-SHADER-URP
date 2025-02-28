Shader "Unlit/Outline_Transfer"
{
    Properties
    {
        _Outlines ("Outlines datas", 2D) = "" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
		ZWrite Off Cull Off

        Pass
        {
            Name "BlitOutlines"

            HLSLPROGRAM

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
			
			
			#pragma vertex Vert
			#pragma fragment Frag

			
			Texture2D _Outlines;
			SamplerState point_clamp_sampler;

			
			float4 Frag(Varyings input) : SV_Target
			{
				float2 uv = input.texcoord.xy;

				float4 blitColor = _BlitTexture.Sample(point_clamp_sampler, uv);

				float4 outlineColor = _Outlines.Sample(point_clamp_sampler, uv);
				
				float alpha = outlineColor.a;
				
				float4 color = lerp(blitColor, outlineColor, alpha);
				
				return color;
			}
			ENDHLSL
        }
    }
}
