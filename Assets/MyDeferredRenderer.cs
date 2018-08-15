using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
	[ExecuteInEditMode]
	public class MyDeferredRenderer : MonoBehaviour, IRendererSetup 
	{
		public void Setup(ScriptableRenderer renderer, ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
		{
		}
	}

	public class MyGBufferAndLightingPass : ScriptableRenderPass
	{
		RenderPassAttachment m_GBufferDiffuse;
		RenderPassAttachment m_GBufferSpecularAndRoughness;
		RenderPassAttachment m_GBufferNormal;
		RenderPassAttachment m_CameraTarget;
		RenderPassAttachment m_Depth;

		Material m_DeferredShadingMaterial;
	    MaterialPropertyBlock m_LightPropertiesBlock = new MaterialPropertyBlock();

		public MyGBufferAndLightingPass()
		{
			m_GBufferDiffuse = new RenderPassAttachment(RenderTextureFormat.ARGB32);
			m_GBufferSpecularAndRoughness = new RenderPassAttachment(RenderTextureFormat.ARGB32);
			m_GBufferNormal = new RenderPassAttachment(RenderTextureFormat.ARGB2101010);
			m_CameraTarget = new RenderPassAttachment(RenderTextureFormat.ARGBHalf);
			m_Depth = new RenderPassAttachment(RenderTextureFormat.Depth);

			m_DeferredShadingMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/SIGGRAPH Studio/DeferredLighting"));

			m_CameraTarget.Clear(Color.black);
			m_Depth.Clear(Color.black);
		}

		public override void Execute(ScriptableRenderer renderer, ref ScriptableRenderContext context,
			 ref CullResults cullResults, ref RenderingData renderingData)
		{
		}
	}
}
