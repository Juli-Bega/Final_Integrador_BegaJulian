using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GreyscaleRendererFeature : ScriptableRendererFeature
{
    public Shader shader;

    private Material _material;
    private GreyscaleRenderPass _pass;

    private static readonly int CircleActiveId = Shader.PropertyToID("_CircleActive");
    private static readonly int CircleRadiusId = Shader.PropertyToID("_CircleRadius");

    public override void Create()
    {
        if (shader == null) return;
        

        if (_material == null)
            _material = new Material(shader);

        _material.SetFloat(CircleActiveId, 1f);
        _material.SetFloat(CircleRadiusId, 0.25f);
        _pass = new GreyscaleRenderPass(_material);
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

    public void SetCircleActive(bool active)
    {
        if (_material == null) Create();
        if (_material == null) return;
        _material.SetFloat(CircleActiveId, active ? 1f : 0f);
    }
    public void SetCircleRadius(float radius)
    {
        if (_material == null) Create();
        if (_material == null) return;
        _material.SetFloat(CircleRadiusId, radius);
    }
}