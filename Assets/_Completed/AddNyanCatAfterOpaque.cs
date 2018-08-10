using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [ExecuteInEditMode]
	public class AddNyanCatAfterOpaque : MonoBehaviour, IAfterOpaquePass
	{
		public Texture2D m_OverlayTexture;
	    public float m_Alpha = 0.5f;
	    private NyanCatPass m_Pass;

	    public void OnEnable()
	    {
	        Material material = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/SIGGRAPH Studio/CustomBlit"));
	        m_Pass = new NyanCatPass(material);
	    }

		public ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor,
			RenderTargetHandle colorAttachmentHandle,
			RenderTargetHandle depthAttachmentHandle)
		{
		    m_Pass.Setup(colorAttachmentHandle, m_OverlayTexture, m_Alpha);
			return m_Pass;
		}
	}


	public class NyanCatPass : ScriptableRenderPass
	{
	    public NyanCatPass(Material material)
	    {
	        m_Material = material;
	    }

		public override void Execute(ScriptableRenderer renderer, ref ScriptableRenderContext context,
			ref CullResults cullResults,
			ref RenderingData renderingData)
		{
			m_Material.SetTexture("_BlitTex", m_OverlayTexture);
		    m_Material.SetFloat("_Alpha", m_Alpha);

			CommandBuffer cmd = CommandBufferPool.Get("Render Nyan Cat");
			cmd.Blit(m_OverlayTexture, colorAttachment.Identifier(), m_Material);
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		public void Setup(RenderTargetHandle colorAttachmentHandle, Texture2D overlayTexture, float alpha)
		{
			colorAttachment = colorAttachmentHandle;
			m_OverlayTexture = overlayTexture;
		    m_Alpha = alpha;
		}

        private Material m_Material = null;
		private RenderTargetHandle colorAttachment;
		private Texture2D m_OverlayTexture;
	    private float m_Alpha;
	}
}
