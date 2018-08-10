using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [ExecuteInEditMode]
    public class OnTileDeferredRenderer : MonoBehaviour, IRendererSetup
    {
        GBufferAndLightingPass m_GBufferAndLightingPass;

        public void OnEnable()
        {
            SceneViewOverrider.AddRendererSetup(this);
            m_GBufferAndLightingPass = new GBufferAndLightingPass();
        }

        public void OnDisable()
        {
            SceneViewOverrider.RemoveRendererSetup(this);
        }

        public void Setup(ScriptableRenderer renderer, ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            renderer.Clear();
            renderer.EnqueuePass(m_GBufferAndLightingPass);
        }
    }

    public class GBufferAndLightingPass : ScriptableRenderPass
    {
        RenderPassAttachment m_GBufferAlbedo;
        RenderPassAttachment m_GBufferSpecRough;
        RenderPassAttachment m_GBufferNormal;
        RenderPassAttachment m_CameraTarget;
        RenderPassAttachment m_DepthAttachment;

        Material m_DeferredShadingMaterial;
        MaterialPropertyBlock m_LightPropertiesBlock = new MaterialPropertyBlock();

        int m_CameraColorTexture;
        RenderTargetIdentifier m_CameraRT;

        public GBufferAndLightingPass()
        {
            m_GBufferAlbedo = new RenderPassAttachment(RenderTextureFormat.ARGB32);
            m_GBufferSpecRough = new RenderPassAttachment(RenderTextureFormat.ARGB32);
            m_GBufferNormal = new RenderPassAttachment(RenderTextureFormat.ARGB2101010);
            m_CameraTarget = new RenderPassAttachment(RenderTextureFormat.ARGBHalf);
            m_DepthAttachment = new RenderPassAttachment(RenderTextureFormat.Depth);

            m_DeferredShadingMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/SIGGRAPH Studio/DeferredLighting"));

            m_CameraTarget.Clear(Color.black);
            m_DepthAttachment.Clear(Color.black);

            m_CameraColorTexture = Shader.PropertyToID("_CameraColorTexture");
            m_CameraRT = new RenderTargetIdentifier(m_CameraColorTexture);
        }

        public override void Execute(ScriptableRenderer renderer, ref ScriptableRenderContext context,
            ref CullResults cullResults, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;

            RenderTargetIdentifier cameraRT = BuiltinRenderTextureType.CameraTarget;
            if (camera.cameraType == CameraType.SceneView)
            {
                CommandBuffer cmd = CommandBufferPool.Get("Create Textures");
                cmd.GetTemporaryRT(m_CameraColorTexture, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Point);
                context.ExecuteCommandBuffer(cmd);
                context.Submit();
                CommandBufferPool.Release(cmd);
                cameraRT = m_CameraRT;
            }

            m_CameraTarget.BindSurface(cameraRT, false, true);

            context.SetupCameraProperties(renderingData.cameraData.camera, false);

            using (RenderPass rp = new RenderPass(context, camera.pixelWidth, camera.pixelHeight, 1,
                new[] { m_GBufferAlbedo, m_GBufferSpecRough, m_GBufferNormal, m_CameraTarget }, m_DepthAttachment))
            {
                using (new RenderPass.SubPass(rp,
                    new[] { m_GBufferAlbedo, m_GBufferSpecRough, m_GBufferNormal, m_CameraTarget }, null))
                {
                    RenderGBuffer(ref context, ref cullResults, camera);
                }

                using (new RenderPass.SubPass(rp, new[] { m_CameraTarget },
                    new[] { m_GBufferAlbedo, m_GBufferSpecRough, m_GBufferNormal, m_DepthAttachment }, true))
                {
                    RenderDeferredLights(ref context, ref cullResults, ref renderingData.lightData);
                }

                using (new RenderPass.SubPass(rp, new[] { m_CameraTarget }, null))
                {
                    context.DrawSkybox(camera);
                }
            }

            if (cameraRT != BuiltinRenderTextureType.CameraTarget)
            {
                CommandBuffer blitCmd = CommandBufferPool.Get("Final Blit");
                blitCmd.Blit(m_CameraRT, BuiltinRenderTextureType.CameraTarget);
                blitCmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                context.ExecuteCommandBuffer(blitCmd);
                CommandBufferPool.Release(blitCmd);
            }
        }

        void RenderGBuffer(ref ScriptableRenderContext context, ref CullResults cullResults, Camera camera)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Render GBuffer");

            var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("GBuffer Pass"))
            {
                sorting = { flags = SortFlags.CommonOpaque },
                rendererConfiguration = RendererConfiguration.PerObjectReflectionProbes | RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe,
            };

            var filterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque,
            };

            context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, filterSettings);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void RenderDeferredLights(ref ScriptableRenderContext context, ref CullResults cullResults,
            ref LightData lightData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Render Deferred Lights");
            List<VisibleLight> visibleLights = lightData.visibleLights;

            m_LightPropertiesBlock.Clear();

            for (int i = 0; i < visibleLights.Count; ++i)
            {
                VisibleLight currLight = visibleLights[i];

                if (currLight.lightType != LightType.Directional)
                    continue;
                Vector4 lightDirection = -currLight.localToWorld.GetColumn(2);
                m_LightPropertiesBlock.SetVector("_MainLightPosition", new Vector4(lightDirection.x, lightDirection.y, lightDirection.z, 0.0f));
                m_LightPropertiesBlock.SetVector("_MainLightColor", currLight.finalColor);
                cmd.DrawMesh(LightweightPipeline.fullscreenMesh, Matrix4x4.identity, m_DeferredShadingMaterial, 0, 0, m_LightPropertiesBlock);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}

