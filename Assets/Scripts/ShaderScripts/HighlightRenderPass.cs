using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

public class HighlightRenderPass : ScriptableRenderPass
{
    private List<HighlightType> _highlightTypes;
    private RTHandle _highlightRT;

    public HighlightRenderPass(List<HighlightType> highlightTypes, RenderTexture highlightRT)
    {
        _highlightTypes = highlightTypes;
        _highlightRT = RTHandles.Alloc(highlightRT);
        renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    private class PassData
    {
        public TextureHandle highlightRT;
        public List<RendererListHandle> rendererLists = new List<RendererListHandle>();
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var renderingData = frameData.Get<UniversalRenderingData>();
        var cameraData = frameData.Get<UniversalCameraData>();

        using (var builder = renderGraph.AddUnsafePass<PassData>("Highlight Pass", out var passData))
        {
            passData.highlightRT = renderGraph.ImportTexture(_highlightRT);
            passData.rendererLists.Clear();

            builder.UseTexture(passData.highlightRT, AccessFlags.Write);

            foreach (var type in _highlightTypes)
            {
                if (type.material == null) continue;

                var desc = CreateRendererListDesc(cameraData, renderingData, type);
                var handle = renderGraph.CreateRendererList(desc);

                passData.rendererLists.Add(handle);
                builder.UseRendererList(handle);
            }

            builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                cmd.SetRenderTarget(data.highlightRT);
                cmd.ClearRenderTarget(false, true, Color.clear);

                foreach (var list in data.rendererLists)
                    ctx.cmd.DrawRendererList(list);
            });
        }
    }

    private RendererListDesc CreateRendererListDesc(
        UniversalCameraData cameraData,
        UniversalRenderingData renderingData,
        HighlightType type)
    {
        return new RendererListDesc(
            new ShaderTagId("UniversalForward"),
            renderingData.cullResults,
            cameraData.camera)
        {
            rendererConfiguration = PerObjectData.None,
            renderQueueRange = RenderQueueRange.all,
            layerMask = type.layers,
            overrideMaterial = type.material,
            overrideMaterialPassIndex = 0
        };
    }

    public void Dispose()
    {
        _highlightRT?.Release();
    }
}