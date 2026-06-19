using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EnhancedVisionRendererFeature : ScriptableRendererFeature
{
    public Shader shader;

    [Range(0f, 1f)]
    public float noiseIntensity = 0.1f;

    [Range(1f, 10f)]
    public float pixelSize = 1f;

    private Material _material;
    private EnhancedVisionRenderPass _pass;

    private static readonly int NoiseIntensityId = Shader.PropertyToID("_NoiseIntensity");
    private static readonly int PixelSizeId = Shader.PropertyToID("_PixelSize");

    public override void Create()
    {
        if (shader == null) return;
        

        if (_material == null)
            _material = new Material(shader);

        _material.SetFloat(NoiseIntensityId, noiseIntensity);
        _material.SetFloat(PixelSizeId, pixelSize);
        Debug.Log("PixelSize seteado: " + _material.GetFloat(PixelSizeId));
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