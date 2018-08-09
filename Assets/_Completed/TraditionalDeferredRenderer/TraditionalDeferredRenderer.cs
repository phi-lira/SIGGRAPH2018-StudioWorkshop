
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [ExecuteInEditMode]
    public class TraditionalDeferredRenderer : MonoBehaviour, IRendererSetup
    {
        private GBufferPass m_GBufferPass;

        public void OnEnable()
        {
            m_GBufferPass = new GBufferPass();
            SceneViewOverrider.AddRendererSetup(this);
        }

        public void OnDisable()
        {
            SceneViewOverrider.RemoveRendererSetup(this);
        }

        public void Setup(ScriptableRenderer renderer, ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            renderer.Clear();
            renderer.EnqueuePass(m_GBufferPass);
        }
    }

    public class GBufferPass : ScriptableRenderPass
    {
        private int gbuffer0; // diffuse
        private int gbuffer1; // specularRoughnees
        private int gbuffer2; // normals
        private int gbuffer3; // lightAccum
        private int depth;

        public override void Execute(ScriptableRenderer renderer, ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;

            gbuffer0 = Shader.PropertyToID("_GBuffer0");
            gbuffer1 = Shader.PropertyToID("_GBuffer1");
            gbuffer2 = Shader.PropertyToID("_GBuffer2");
            gbuffer3 = Shader.PropertyToID("_GBuffer3");
            depth = Shader.PropertyToID("_CameraDepth");

            RenderTargetIdentifier[] colors = { new RenderTargetIdentifier(gbuffer0), new RenderTargetIdentifier(gbuffer1), new RenderTargetIdentifier(gbuffer2), new RenderTargetIdentifier(gbuffer3) };
            RenderTargetIdentifier depthRT = new RenderTargetIdentifier(depth);

            CommandBuffer cmd = CommandBufferPool.Get("Allocate Textures");
            cmd.GetTemporaryRT(gbuffer0, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(gbuffer1, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(gbuffer2, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGB2101010);
            cmd.GetTemporaryRT(gbuffer3, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf);
            cmd.GetTemporaryRT(depth, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Point, RenderTextureFormat.Depth);
            cmd.SetRenderTarget(colors, depthRT);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            RenderOpaques(ref context, camera, ref cullResults);
        }

        void RenderOpaques(ref ScriptableRenderContext context, Camera camera, ref CullResults cullResults)
        {
            DrawRendererSettings drawSettings = new DrawRendererSettings(camera, new ShaderPassName("GBuffer Pass"))
            {
                sorting = { flags = SortFlags.CommonOpaque },
                rendererConfiguration = RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe | RendererConfiguration.PerObjectReflectionProbes,
            };

            FilterRenderersSettings filterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque,
            };

            context.SetupCameraProperties(camera, false);
            context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, filterSettings);
        }
    }
}

