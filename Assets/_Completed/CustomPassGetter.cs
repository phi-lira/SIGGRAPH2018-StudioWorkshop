using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;

public class CustomPassGetter : MonoBehaviour, IAfterOpaquePass
{
	private CustomPass pass = new CustomPass();
	
	public ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorAttachmentHandle,
		RenderTargetHandle depthAttachmentHandle)
	{
		pass.Setup(colorAttachmentHandle, depthAttachmentHandle);
		return pass;
	}
}


public class CustomPass : ScriptableRenderPass
{
	public override void Execute(ScriptableRenderer renderer, ref ScriptableRenderContext context, ref CullResults cullResults,
		ref RenderingData renderingData)
	{
		CommandBuffer cmd = CommandBufferPool.Get("Render");
		cmd.SetRenderTarget(colorhandle.Identifier(), depthHandle.Identifier());
		cmd.ClearRenderTarget(true, true, Color.blue);
		context.ExecuteCommandBuffer(cmd);
		CommandBufferPool.Release(cmd);
	}

	public void Setup(RenderTargetHandle colorAttachmentHandle, RenderTargetHandle depthAttachmentHandle)
	{
		this.colorhandle = colorAttachmentHandle;
		this.depthHandle = depthAttachmentHandle;
	}

	RenderTargetHandle colorhandle { get; set; }

	RenderTargetHandle depthHandle { get; set; }
}
