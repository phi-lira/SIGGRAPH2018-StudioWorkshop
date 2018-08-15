using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
	[ExecuteInEditMode]
	public class AddPassAfterOpaque : MonoBehaviour, IAfterOpaquePass
	{
		const string k_CustomBlitShader = "Hidden/SIGGRAPH Studio/CustomBlit";
		public void OnEnable()
		{
		}

		public ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor desc, RenderTargetHandle colorHandle, RenderTargetHandle depthHandle)
		{
			return null;
		}
	}
}
