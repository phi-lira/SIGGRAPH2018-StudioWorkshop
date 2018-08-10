Shader "Hidden/SIGGRAPH Studio/CustomBlit"
{
	Properties
	{
		_BlitTex("BlitTexture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderPipeline" = "LightweightPipeline" }
		
		Pass
	    {
	        Blend SrcAlpha OneMinusSrcAlpha
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "LWRP/ShaderLibrary/Core.hlsl"
			
			TEXTURE2D(_BlitTex);
			SAMPLER(sampler_BlitTex);
			
			struct VertexOutput
			{
			    float4 positionCS : SV_POSITION;
			    float3 uv0 : TEXCOORD0;
			};
			
			VertexOutput vert (float4 positionOS : POSITION, float uv0 : TEXCOORD0)
			{
			    VertexOutput OUT;
				OUT.positionCS = TransformObjectToHClip(positionOS.xyz);
				OUT.uv0 = uv0;
				return OUT;
			}
			
			half4 frag (VertexOutput IN) : SV_Target
			{
			    half3 color = SAMPLE_TEXTURE2D(_BlitTex, sampler_BlitTex, IN.uv0).rgb;
			    return half4(color, 0.5);
			}
			ENDHLSL
		}
	}
}
