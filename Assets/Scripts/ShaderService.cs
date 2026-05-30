using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShaderService : MonoBehaviour
{
    [SerializeField] private UniversalRendererData _rendererData;

    private GreyscaleRendererFeature _greyscaleFeature;

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
        Debug.Log("ShaderService: EnableCircle");
        _greyscaleFeature.SetCircleActive(true);
    }

    public void DisableCircle()
    {
        Debug.Log("ShaderService: DisableCircle");
        _greyscaleFeature.SetCircleActive(false);
    }
}