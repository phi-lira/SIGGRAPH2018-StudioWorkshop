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

        public GBufferAndLightingPass()
        {
            m_GBufferAlbedo = new RenderPassAttachment(RenderTextureFormat.ARGB32);
            m_GBufferSpecRough = new RenderPassAttachment(RenderTextureFormat.ARGB32);
            m_GBufferNormal = new RenderPassAttachment(RenderTextureFormat.ARGB2101010);
            m_CameraTarget = new RenderPassAttachment(RenderTextureFormat.ARGB32);
            m_DepthAttachment = new RenderPassAttachment(RenderTextureFormat.Depth);

            m_DeferredShadingMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("SIGGRAPH Studio/DeferredLighting"));

            m_CameraTarget.Clear(Color.black);
            m_DepthAttachment.Clear(Color.black);
        }

        public override void Execute(ScriptableRenderer renderer, ref ScriptableRenderContext context,
            ref CullResults cullResults, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            m_CameraTarget.BindSurface(BuiltinRenderTextureType.CameraTarget, false, true);

            context.SetupCameraProperties(renderingData.cameraData.camera, false);

            using (RenderPass rp = new RenderPass(context, camera.pixelWidth, camera.pixelHeight, 1,
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
            CommandBuffer cmd = CommandBufferPool.Get("Render GBuffer");

            var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("GBuffer Pass"))
            {
                sorting = {flags = SortFlags.CommonOpaque},
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
                cmd.DrawMesh(LightweightPipeline.fullscreenMesh, Matrix4x4.identity, m_DeferredShadingMaterial, 0, 0, m_LightPropertiesBlock);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}


