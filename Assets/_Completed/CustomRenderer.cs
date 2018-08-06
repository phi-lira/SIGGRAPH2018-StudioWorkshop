using System;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class CustomRenderer : MonoBehaviour, IRendererSetup
    {
        private GBufferAndLightingPass m_GBufferAndLightingPass;

        [NonSerialized]
        private bool m_Initialized = false;

        private void Init()
        {
            if (m_Initialized)
                return;

            m_GBufferAndLightingPass = new GBufferAndLightingPass();
            m_Initialized = true;
        }

        public void Setup(ScriptableRenderer renderer, ref ScriptableRenderContext context,
            ref CullResults cullResults, ref RenderingData renderingData)
        {
            Init();

            renderer.Clear();
            renderer.EnqueuePass(m_GBufferAndLightingPass);
        }
    }
}

