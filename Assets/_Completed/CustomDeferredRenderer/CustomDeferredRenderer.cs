namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [ExecuteInEditMode]
    public class CustomDeferredRenderer : MonoBehaviour, IRendererSetup
    {
        GBufferAndLightingPass m_GBufferAndLightingPass;

        public void OnEnable()
        {
            m_GBufferAndLightingPass = new GBufferAndLightingPass();
        }

        public void Setup(ScriptableRenderer renderer, ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            renderer.Clear();
            renderer.EnqueuePass(m_GBufferAndLightingPass);
        }
    }
}

