using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

public class HighlightRenderPass : ScriptableRenderPass
{
    private Material _enemyMaterial;
    private Material _objectiveMaterial;
    private RTHandle _highlightRT;

    private int _enemyLayer;
    private int _objectiveLayer;

    public HighlightRenderPass(Material enemyMaterial, Material objectiveMaterial, RenderTexture highlightRT)
    {
        _enemyMaterial = enemyMaterial;
        _objectiveMaterial = objectiveMaterial;
        _highlightRT = RTHandles.Alloc(highlightRT);
        _enemyLayer = LayerMask.NameToLayer("Enemies");
        _objectiveLayer = LayerMask.NameToLayer("Objectives");
        renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    private class PassData
    {
        public Material enemyMaterial;
        public Material objectiveMaterial;
        public TextureHandle highlightRT;
        public RendererListHandle enemyRendererList;
        public RendererListHandle objectiveRendererList;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var renderingData = frameData.Get<UniversalRenderingData>();
        var cameraData = frameData.Get<UniversalCameraData>();
        var lightData = frameData.Get<UniversalLightData>();

        var enemyRendererListDesc = CreateRendererListDesc(
            cameraData, renderingData, lightData, _enemyLayer, _enemyMaterial);
        var objectiveRendererListDesc = CreateRendererListDesc(
            cameraData, renderingData, lightData, _objectiveLayer, _objectiveMaterial);

        using (var builder = renderGraph.AddUnsafePass<PassData>("Highlight Pass", out var passData))
        {
            passData.enemyMaterial = _enemyMaterial;
            passData.objectiveMaterial = _objectiveMaterial;
            passData.highlightRT = renderGraph.ImportTexture(_highlightRT);
            passData.enemyRendererList = renderGraph.CreateRendererList(enemyRendererListDesc);
            passData.objectiveRendererList = renderGraph.CreateRendererList(objectiveRendererListDesc);

            builder.UseTexture(passData.highlightRT, AccessFlags.Write);
            builder.UseRendererList(passData.enemyRendererList);
            builder.UseRendererList(passData.objectiveRendererList);

            builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                cmd.SetRenderTarget(data.highlightRT);
                cmd.ClearRenderTarget(false, true, Color.clear);

                ctx.cmd.DrawRendererList(data.enemyRendererList);
                ctx.cmd.DrawRendererList(data.objectiveRendererList);
            });
        }
    }

    private RendererListDesc CreateRendererListDesc(
     UniversalCameraData cameraData,
     UniversalRenderingData renderingData,
     UniversalLightData lightData,
     int layer,
     Material overrideMaterial)
    {
        return new RendererListDesc(
            new ShaderTagId("UniversalForward"),
            renderingData.cullResults,
            cameraData.camera)
        {
            rendererConfiguration = PerObjectData.None,
            renderQueueRange = RenderQueueRange.opaque,
            layerMask = 1 << layer,
            overrideMaterial = overrideMaterial,
            overrideMaterialPassIndex = 0
        };
    }

    public void Dispose()
    {
        _highlightRT?.Release();
    }
}