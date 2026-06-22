using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class HighlightRenderPass : ScriptableRenderPass
{
    private Material _enemyMaterial;
    private Material _objectiveMaterial;
    private Material _coneMaterial;
    private RTHandle _highlightRT;

    private int _enemyLayer;
    private int _objectiveLayer;
    private int _coneLayer;

    public HighlightRenderPass(Material enemyMaterial, Material objectiveMaterial, Material coneMaterial, RenderTexture highlightRT)
    {
        _enemyMaterial = enemyMaterial;
        _objectiveMaterial = objectiveMaterial;
        _coneMaterial = coneMaterial;
        _highlightRT = RTHandles.Alloc(highlightRT);
        _enemyLayer = LayerMask.NameToLayer("Enemies");
        _objectiveLayer = LayerMask.NameToLayer("Objectives");
        _coneLayer = LayerMask.NameToLayer("ConeHighlight");
        renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    private class PassData
    {
        public TextureHandle highlightRT;
        public RendererListHandle enemyRendererList;
        public RendererListHandle objectiveRendererList;
        public RendererListHandle coneRendererList;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var renderingData = frameData.Get<UniversalRenderingData>();
        var cameraData = frameData.Get<UniversalCameraData>();
        var lightData = frameData.Get<UniversalLightData>();

        var enemyDesc = CreateRendererListDesc(cameraData, renderingData, lightData, _enemyLayer, _enemyMaterial);
        var objectiveDesc = CreateRendererListDesc(cameraData, renderingData, lightData, _objectiveLayer, _objectiveMaterial);
        var coneDesc = CreateRendererListDesc(cameraData, renderingData, lightData, _coneLayer, _coneMaterial);

        using (var builder = renderGraph.AddUnsafePass<PassData>("Highlight Pass", out var passData))
        {
            passData.highlightRT = renderGraph.ImportTexture(_highlightRT);
            passData.enemyRendererList = renderGraph.CreateRendererList(enemyDesc);
            passData.objectiveRendererList = renderGraph.CreateRendererList(objectiveDesc);
            passData.coneRendererList = renderGraph.CreateRendererList(coneDesc);

            builder.UseTexture(passData.highlightRT, AccessFlags.Write);
            builder.UseRendererList(passData.enemyRendererList);
            builder.UseRendererList(passData.objectiveRendererList);
            builder.UseRendererList(passData.coneRendererList);

            builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                cmd.SetRenderTarget(data.highlightRT);
                cmd.ClearRenderTarget(false, true, Color.clear);

                ctx.cmd.DrawRendererList(data.enemyRendererList);
                ctx.cmd.DrawRendererList(data.objectiveRendererList);
                ctx.cmd.DrawRendererList(data.coneRendererList);
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
            renderQueueRange = RenderQueueRange.all,
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