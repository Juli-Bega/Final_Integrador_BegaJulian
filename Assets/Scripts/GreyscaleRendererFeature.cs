using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GreyscaleRendererFeature : ScriptableRendererFeature
{
    public Shader shader;

    private Material _material;
    private GreyscaleRenderPass _pass;

    public override void Create()
    {
        if (shader == null)
        {
            Debug.LogError("Shader es null!");
            return;
        }

        _material = new Material(shader);
        Debug.Log("Material creado: " + _material);
        _pass = new GreyscaleRenderPass(_material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null) return;
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
}