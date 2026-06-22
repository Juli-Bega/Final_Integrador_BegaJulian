using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class HighlightRendererFeature : ScriptableRendererFeature
{
    public Material enemyMaterial;
    public Material objectiveMaterial;
    public Material coneMaterial;
    public RenderTexture highlightRT;

    private HighlightRenderPass _pass;

    public override void Create()
    {
        if (enemyMaterial == null || objectiveMaterial == null || coneMaterial == null || highlightRT == null) return;
        _pass = new HighlightRenderPass(enemyMaterial, objectiveMaterial, coneMaterial, highlightRT);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_pass == null) return;
        if (renderingData.cameraData.cameraType != CameraType.Game) return;
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
    }
}