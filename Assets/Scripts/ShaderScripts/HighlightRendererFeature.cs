using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class HighlightRendererFeature : ScriptableRendererFeature
{
    public List<HighlightType> highlightTypes = new List<HighlightType>();
    public RenderTexture highlightRT;

    private HighlightRenderPass _pass;

    public override void Create()
    {
        if (highlightRT == null || highlightTypes.Count == 0) return;
        _pass = new HighlightRenderPass(highlightTypes, highlightRT);
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