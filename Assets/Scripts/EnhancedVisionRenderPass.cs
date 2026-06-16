using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class EnhancedVisionRenderPass : ScriptableRenderPass
{
    private Material _material;

    public EnhancedVisionRenderPass(Material material)
    {
        _material = material;
        renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    private class PassData
    {
        public TextureHandle source;
        public TextureHandle destination;
        public Material material;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();

        if (resourceData.isActiveTargetBackBuffer)
            return;

        TextureHandle source = resourceData.activeColorTexture;

        var descriptor = renderGraph.GetTextureDesc(source);
        descriptor.name = "_EnhancedVisionTemp";
        descriptor.clearBuffer = false;
        TextureHandle destination = renderGraph.CreateTexture(descriptor);

        using (var builder = renderGraph.AddUnsafePass<PassData>("EnhancedVision Blit", out var passData))
        {
            passData.source = source;
            passData.destination = destination;
            passData.material = _material;

            builder.UseTexture(passData.source, AccessFlags.Read);
            builder.UseTexture(passData.destination, AccessFlags.Write);

            builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                Blitter.BlitCameraTexture(cmd, data.source, data.destination, data.material, 0);
            });
        }
        using (var builder = renderGraph.AddUnsafePass<PassData>("EnhancedVision CopyBack", out var passData))
        {
            passData.source = destination;
            passData.destination = source;
            passData.material = null;

            builder.UseTexture(passData.source, AccessFlags.Read);
            builder.UseTexture(passData.destination, AccessFlags.Write);

            builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                Blitter.BlitCameraTexture(cmd, data.source, data.destination);
            });
        }
    }
}