using UnityEngine;
using UnityEngine.UI;

public class GuardAlertUI : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private GameObject _container;

    [Header("Colors")]
    [SerializeField] private Color _patrolColor = Color.white;
    [SerializeField] private Color _suspiciousColor = Color.yellow;
    [SerializeField] private Color _detectedColor = Color.red;

    private GuardController _guard;
    private Camera _camera;

    private void Awake()
    {
        _guard = GetComponentInParent<GuardController>();
        _camera = Camera.main;
    }

    private void LateUpdate()
    {
        bool visible = _guard.AlertLevel > 0f;
        _container.SetActive(visible);

        if (!visible) return;

        _fillImage.fillAmount = _guard.AlertLevel / 100f;
        _fillImage.color = GetStateColor();

        transform.rotation = _camera.transform.rotation;
    }

    private Color GetStateColor()
    {
        switch (_guard.CurrentState)
        {
            case GuardController.GuardState.Suspicious: return _suspiciousColor;
            case GuardController.GuardState.Detected: return _detectedColor;
            default: return _patrolColor;
        }
    }
}