using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private ShaderService _shaderService;
    [SerializeField] private float _circleRadius = 0.25f;

    private bool _isCircleActive = true;

    private IEnumerator Start()
    {
        yield return null;
        _shaderService.SetCircleRadius(_circleRadius);
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