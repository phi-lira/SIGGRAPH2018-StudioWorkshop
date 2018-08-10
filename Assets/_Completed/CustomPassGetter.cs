using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
	public class CustomPassGetter : MonoBehaviour, IAfterOpaquePass
	{
		public Texture2D m_OverlayTexture;
		private CustomPass pass = new CustomPass();

		public ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor,
			RenderTargetHandle colorAttachmentHandle,
			RenderTargetHandle depthAttachmentHandle)
		{
			pass.Setup(colorAttachmentHandle, m_OverlayTexture);
			return pass;
		}
	}


	public class CustomPass : ScriptableRenderPass
	{
		public override void Execute(ScriptableRenderer renderer, ref ScriptableRenderContext context,
			ref CullResults cullResults,
			ref RenderingData renderingData)
		{
			if (m_Material == null)
				m_Material = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/SIGGRAPH Studio/CustomBlit"));
			m_Material.SetTexture("_BlitTex", m_OverlayTexture);

			if (m_OverlayTexture != null)
			{
				MaterialPropertyBlock props = new MaterialPropertyBlock();
				props.SetTexture("_BlitTex", m_OverlayTexture);
			}
			
			CommandBuffer cmd = CommandBufferPool.Get("Render Overlay");
			cmd.Blit(m_OverlayTexture, colorAttachment.Identifier(), m_Material);
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		public void Setup(RenderTargetHandle colorAttachmentHandle, Texture2D overlayTexture)
		{
			colorAttachment = colorAttachmentHandle;
			m_OverlayTexture = overlayTexture;
		}

		private Material m_Material = null;
		private RenderTargetHandle colorAttachment;
		private Texture2D m_OverlayTexture;

	}
}
