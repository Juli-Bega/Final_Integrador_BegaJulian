using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private ShaderService _shaderService;

    private bool _isCircleActive = true;

    private void Start()
    {
        _isCircleActive = true;
        _shaderService.EnableCircle();
    }
    private void Update()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            _isCircleActive = !_isCircleActive;
            Debug.Log("_isCircleActive ahora es: " + _isCircleActive);

            if (_isCircleActive)
                _shaderService.EnableCircle();
            else
                _shaderService.DisableCircle();
        }
    }
}