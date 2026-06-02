using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShaderService : MonoBehaviour
{
    [SerializeField] private UniversalRendererData _rendererData;

    private GreyscaleRendererFeature _greyscaleFeature;
    private float _currentCircleRadius;

    private void Awake()
    {
        foreach (var feature in _rendererData.rendererFeatures)
        {
            if (feature is GreyscaleRendererFeature greyscale)
            {
                _greyscaleFeature = greyscale;
                break;
            }
        }

        if (_greyscaleFeature == null)
            Debug.LogError("ShaderService: No se encontró el GreyscaleRendererFeature.");
    }

    public void EnableCircle()
    {
        _greyscaleFeature.SetCircleActive(true);
    }

    public void DisableCircle()
    {
        _greyscaleFeature.SetCircleActive(false);
    }

    public void SetCircleRadius(float radius)
    {
        _currentCircleRadius = radius;
        _greyscaleFeature.SetCircleRadius(radius);
    }

    public float GetCircleRadius()
    {
        return _currentCircleRadius;
    }
}