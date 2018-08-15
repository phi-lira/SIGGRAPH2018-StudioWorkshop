using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [ExecuteInEditMode]
    public class MyDeferredRendererCompleted : MonoBehaviour, IRendererSetup 
    {
        MyGBufferAndLightingPassCompleted m_RenderPass;

        public void OnEnable()
        {
            m_RenderPass = new MyGBufferAndLightingPassCompleted();
        }

        public void Setup(ScriptableRenderer renderer, ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            renderer.Clear();
            renderer.EnqueuePass(m_RenderPass);
        }
    }

    public class MyGBufferAndLightingPassCompleted : ScriptableRenderPass
    {
        RenderPassAttachment m_GBufferDiffuse;
        RenderPassAttachment m_GBufferSpecularAndRoughness;
        RenderPassAttachment m_GBufferNormal;
        RenderPassAttachment m_CameraTarget;
        RenderPassAttachment m_Depth;

        Material m_DeferredShadingMaterial;
        MaterialPropertyBlock m_LightPropertiesBlock = new MaterialPropertyBlock();

        public MyGBufferAndLightingPassCompleted()
        {
            m_GBufferDiffuse = new RenderPassAttachment(RenderTextureFormat.ARGB32);
            m_GBufferSpecularAndRoughness = new RenderPassAttachment(RenderTextureFormat.ARGB32);
            m_GBufferNormal = new RenderPassAttachment(RenderTextureFormat.ARGB2101010);
            m_CameraTarget = new RenderPassAttachment(RenderTextureFormat.ARGBHalf);
            m_Depth = new RenderPassAttachment(RenderTextureFormat.Depth);

            m_DeferredShadingMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/SIGGRAPH Studio/DeferredLighting"));

            m_CameraTarget.Clear(Color.black);
            m_Depth.Clear(Color.black);
        }

        public override void Execute(ScriptableRenderer renderer, ref ScriptableRenderContext context,
             ref CullResults cullResults, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            
            m_CameraTarget.BindSurface(BuiltinRenderTextureType.CameraTarget, false, true);
            context.SetupCameraProperties(camera, false);

            using (RenderPass rp = new RenderPass(context, camera.pixelWidth, camera.pixelHeight, 1, new[] {m_GBufferDiffuse, m_GBufferSpecularAndRoughness, m_GBufferNormal, m_CameraTarget}, m_Depth))
            {
                using (new RenderPass.SubPass(rp, new[] {m_GBufferDiffuse, m_GBufferSpecularAndRoughness, m_GBufferNormal, m_CameraTarget}, null))
                {
                    RenderGBuffer(context, cullResults, camera);
                }

                using (new RenderPass.SubPass(rp, new[] {m_CameraTarget}, new[] {m_GBufferDiffuse, m_GBufferSpecularAndRoughness, m_GBufferNormal, m_Depth}, true))
                {
                    RenderLights(context, cullResults, renderingData.lightData);    
                }
            }
        }

        public void RenderGBuffer(ScriptableRenderContext context, CullResults cullResults, Camera camera)
        {
            DrawRendererSettings drawSettings = new DrawRendererSettings(camera, new ShaderPassName("GBuffer Pass"))
            {
                sorting = { flags = SortFlags.CommonOpaque},
                rendererConfiguration = RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe | RendererConfiguration.PerObjectReflectionProbes,
            };

            FilterRenderersSettings filterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque,
            };

            context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, filterSettings);
        }

        public void RenderLights(ScriptableRenderContext context, CullResults cullResults, LightData lightData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Render Deferred Lights");
            List<VisibleLight> visibleLights = lightData.visibleLights;

            for (int i = 0 ; i < visibleLights.Count; ++i)
            {
                VisibleLight currLight = visibleLights[i];
                if (currLight.lightType != LightType.Directional)
                    continue;

                Vector4 lightDirection = -currLight.localToWorld.GetRow(2);
                Vector4 lightColor = currLight.finalColor;
                m_LightPropertiesBlock.Clear();
                m_LightPropertiesBlock.SetVector("_MainLightPosition", lightDirection);
                m_LightPropertiesBlock.SetVector("_MainLightColor", lightColor);
                cmd.DrawMesh(LightweightPipeline.fullscreenMesh, Matrix4x4.identity, m_DeferredShadingMaterial, 0, 0, m_LightPropertiesBlock);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
