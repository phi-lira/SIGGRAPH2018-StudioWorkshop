using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
	[ExecuteInEditMode]
	public class AddPassAfterOpaqueCompleted : MonoBehaviour, IAfterOpaquePass
	{
		const string k_CustomBlitShader = "Hidden/SIGGRAPH Studio/CustomBlit";
		
		public Texture2D m_NyanCatTexture;
		MyNyanCatPassCompleted m_NyanCat;
		
		Material m_Material;

		public void OnEnable()
		{
			m_NyanCat = new MyNyanCatPassCompleted();
			m_Material = CoreUtils.CreateEngineMaterial(Shader.Find(k_CustomBlitShader));
		}

		public ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor desc, RenderTargetHandle colorHandle, RenderTargetHandle depthHandle)
		{
			m_NyanCat.Setup(m_NyanCatTexture, colorHandle.Identifier(), m_Material);
			return m_NyanCat;
		}
	}

	public class MyNyanCatPassCompleted : ScriptableRenderPass
	{
		Texture2D m_NyanCatTexture;
		RenderTargetIdentifier m_DestinationTarget;
		Material m_Material;

		public void Setup(Texture2D texture, RenderTargetIdentifier destination, Material material)
		{
			m_NyanCatTexture = texture;
			m_DestinationTarget = destination;
			m_Material = material;
		}

		public override void Execute(ScriptableRenderer renderer, ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
		{
			m_Material.SetTexture("_BlitTex", m_NyanCatTexture);

			CommandBuffer cmd = CommandBufferPool.Get("Render Nyan Cat");
			cmd.Blit(m_NyanCatTexture, m_DestinationTarget, m_Material);
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}
	}
}
