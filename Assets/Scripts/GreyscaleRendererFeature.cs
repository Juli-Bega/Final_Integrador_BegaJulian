using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GreyscaleRendererFeature : ScriptableRendererFeature
{
    public Shader shader;

    private Material _material;
    private GreyscaleRenderPass _pass;

    private static readonly int CircleActiveId = Shader.PropertyToID("_CircleActive");

    public override void Create()
    {
        if (shader == null)
        {
            Debug.LogError("GreyscaleRendererFeature: shader es null.");
            return;
        }

        if (_material == null)
            _material = new Material(shader);

        _material.SetFloat(CircleActiveId, 1f);
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
        if (_material == null)
            Create();

        if (_material == null)
        {
            Debug.LogError("GreyscaleRendererFeature: no se pudo recrear el material.");
            return;
        }

        Debug.Log("SetCircleActive: " + active + " | material: " + _material.GetFloat(CircleActiveId));
        _material.SetFloat(CircleActiveId, active ? 1f : 0f);
        Debug.Log("Despues de set: " + _material.GetFloat(CircleActiveId));
    }
}