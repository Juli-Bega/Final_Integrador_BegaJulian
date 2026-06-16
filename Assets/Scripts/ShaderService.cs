using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShaderService : MonoBehaviour
{
    [SerializeField] private UniversalRendererData _rendererData;

    private EnhancedVisionRendererFeature _enhancedVisionFeature;

    private void Awake()
    {
        foreach (var feature in _rendererData.rendererFeatures)
        {
            if (feature is EnhancedVisionRendererFeature enhancedVision)
            {
                _enhancedVisionFeature = enhancedVision;
                break;
            }
        }

        if (_enhancedVisionFeature == null)
            Debug.LogError("ShaderService: No se encontró el enhancedVisionRendererFeature.");
    }

    public void VisionState(bool state)
    {
        _enhancedVisionFeature.EnableVision(state);
    }
   
}