using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EnhancedVisionRendererFeature : ScriptableRendererFeature
{
    public Shader shader;

    private Material _material;
    private EnhancedVisionRenderPass _pass;


    public override void Create()
    {
        if (shader == null) return;
        

        if (_material == null)
            _material = new Material(shader);

        _pass = new EnhancedVisionRenderPass(_material);
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null || _pass == null) return;
        if (renderingData.cameraData.cameraType != CameraType.Game) return;
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        _pass = null;
        if (Application.isPlaying)
            Destroy(_material);
        else
            DestroyImmediate(_material);
    }

    public void EnableVision(bool active)
    {
        if (_material == null) Create();
        if (_material == null) return;
        SetActive(active);
    }

}