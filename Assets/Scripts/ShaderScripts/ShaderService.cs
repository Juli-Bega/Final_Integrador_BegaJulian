using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShaderService : MonoBehaviour
{
    [SerializeField] private UniversalRendererData _rendererData;

    private EnhancedVisionRendererFeature _enhancedVisionFeature;
    private VisionCone[] _visionCones;

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
            Debug.LogError("ShaderService: EnhancedVisionRendererFeature not found.");

        _visionCones = FindObjectsByType<VisionCone>(FindObjectsSortMode.None);
    }

    public void VisionState(bool state)
    {
        _enhancedVisionFeature.EnableVision(state);

        foreach (var cone in _visionCones)
            cone.SetVisible(state);
    }
}