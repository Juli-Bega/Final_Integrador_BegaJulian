using UnityEngine;

public class InteractableButton : MonoBehaviour, IInteractable
{
    [SerializeField] private Material _activeMaterial;
    [SerializeField] private Material _inactiveMaterial;
    [SerializeField] private Renderer _lightRenderer;
    [SerializeField] private bool _isActivated;

    private Door _door;
    public bool IsActivated => _isActivated;

    public void SetDoor(Door door)
    {
        _door = door;
    }

    private void Start()
    {
        UpdateLight();
    }

    public void Interact()
    {
        _isActivated = !_isActivated;
        UpdateLight();
        _door?.OnButtonChanged();
    }

    private void UpdateLight()
    {
        _lightRenderer.material = _isActivated ? _activeMaterial : _inactiveMaterial;
    }
}