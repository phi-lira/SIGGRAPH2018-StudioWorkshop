using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class GBufferAndLightingPass : ScriptableRenderPass
    {
        RenderPassAttachment m_GBufferAlbedo;
        RenderPassAttachment m_GBufferSpecRough;
        RenderPassAttachment m_GBufferNormal;
        RenderPassAttachment m_CameraTarget;
        RenderPassAttachment m_DepthAttachment;

        Material m_DeferredShadingMaterial;
        MaterialPropertyBlock m_LightPropertiesBlock = new MaterialPropertyBlock();

        const string k_DeferredPassName = "GBuffer Pass";
        const string k_GBufferCB = "Render GBuffer";
        const string k_LightingCB = "Render Deferred Lights";

        public GBufferAndLightingPass()
        {
            m_GBufferAlbedo = new RenderPassAttachment(RenderTextureFormat.ARGB32);
            m_GBufferSpecRough = new RenderPassAttachment(RenderTextureFormat.ARGB32);
            m_GBufferNormal = new RenderPassAttachment(RenderTextureFormat.ARGB2101010);
            m_CameraTarget = new RenderPassAttachment(RenderTextureFormat.DefaultHDR);
            m_DepthAttachment = new RenderPassAttachment(RenderTextureFormat.Depth);

            m_DeferredShadingMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("SIGGRAPH Studio/DeferredLighting"));

            m_CameraTarget.Clear(Color.black, 1.0f, 0);
            m_DepthAttachment.Clear(Color.black, 1.0f, 0);
        }

        public override void Execute(ScriptableRenderer renderer, ref ScriptableRenderContext context,
            ref CullResults cullResults, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            float renderScale = renderingData.cameraData.renderScale;
            int cameraPixelWidth = (int) (camera.pixelWidth * renderScale);
            int cameraPixelHeight = (int) (camera.pixelHeight * renderScale);

            m_CameraTarget.BindSurface(BuiltinRenderTextureType.CameraTarget, false, true);

            context.SetupCameraProperties(renderingData.cameraData.camera, renderingData.cameraData.isStereoEnabled);

            using (RenderPass rp = new RenderPass(context, cameraPixelWidth, cameraPixelHeight, 1,
                new[] {m_GBufferAlbedo, m_GBufferSpecRough, m_GBufferNormal, m_CameraTarget}, m_DepthAttachment))
            {
                using (new RenderPass.SubPass(rp,
                    new[] {m_GBufferAlbedo, m_GBufferSpecRough, m_GBufferNormal, m_CameraTarget}, null))
                {
                    RenderGBuffer(ref context, ref cullResults, camera);
                }

                using (new RenderPass.SubPass(rp, new[] {m_CameraTarget},
                    new[] {m_GBufferAlbedo, m_GBufferSpecRough, m_GBufferNormal, m_DepthAttachment}, true))
                {
                    RenderDeferredLights(ref context, ref cullResults, ref renderingData.lightData);
                }
            }
        }

        void RenderGBuffer(ref ScriptableRenderContext context, ref CullResults cullResults, Camera camera)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_GBufferCB);

            var drawSettings = new DrawRendererSettings(camera, new ShaderPassName(k_DeferredPassName))
            {
                sorting = {flags = SortFlags.CommonOpaque},
                rendererConfiguration = RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe,
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
            CommandBuffer cmd = CommandBufferPool.Get(k_LightingCB);
            List<VisibleLight> visibleLights = lightData.visibleLights;

            m_LightPropertiesBlock.Clear();

            for (int i = 0; i < visibleLights.Count; ++i)
            {
                VisibleLight currLight = visibleLights[i];

                if (currLight.lightType == LightType.Directional)
                {
                    Vector4 lightDirection = -currLight.localToWorld.GetColumn(2);
                    m_LightPropertiesBlock.SetVector("_MainLightPosition", new Vector4(lightDirection.x, lightDirection.y, lightDirection.z, 0.0f));
                }
                else
                {
                    Vector4 lightPosition = currLight.localToWorld.GetColumn(3);
                    m_LightPropertiesBlock.SetVector("_MainLightPosition", new Vector4(lightPosition.x, lightPosition.y, lightPosition.z, 1.0f));
                }

                m_LightPropertiesBlock.SetVector("_MainLightColor", currLight.finalColor);
                LightweightPipeline.DrawFullScreen(cmd, m_DeferredShadingMaterial, m_LightPropertiesBlock);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}


