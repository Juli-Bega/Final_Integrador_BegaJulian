using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShaderService : MonoBehaviour
{
    [SerializeField] private UniversalRendererData _rendererData;

    private EnhancedVisionRendererFeature _enhancedVisionFeature;
    private HighlightRendererFeature _highlightFeature;
    private VisionCone[] _visionCones;

    private void Awake()
    {
        foreach (var feature in _rendererData.rendererFeatures)
        {
            if (feature is EnhancedVisionRendererFeature enhancedVision)
                _enhancedVisionFeature = enhancedVision;

            if (feature is HighlightRendererFeature highlight)
                _highlightFeature = highlight;
        }

        if (_enhancedVisionFeature == null)
            Debug.LogError("ShaderService: EnhancedVisionRendererFeature not found.");

        if (_highlightFeature == null)
            Debug.LogError("ShaderService: HighlightRendererFeature not found.");

        _visionCones = FindObjectsByType<VisionCone>(FindObjectsSortMode.None);
    }

    public void VisionState(bool state)
    {
        _enhancedVisionFeature.EnableVision(state);
        _highlightFeature.SetActive(state);

        foreach (var cone in _visionCones)
            cone.SetVisible(state);
    }
}