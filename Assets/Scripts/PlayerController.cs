using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 15f;

    [Header("Camara")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _mouseSensitivity = 0.1f;
    [SerializeField] private float _verticalClamp = 80f;

    [Header("Circulo")]
    [SerializeField] private ShaderService _shaderService;
    [SerializeField] private float _circleRadius = 0.25f;

    [Header("Gravedad")]
    [SerializeField] private float _gravity = -9.8f;

    private float _verticalVelocity;

    private CharacterController _characterController;
    private Vector3 _currentVelocity;
    private float _verticalRotation;
    private bool _isCircleActive = true;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private System.Collections.IEnumerator Start()
    {
        yield return null;
        _shaderService.SetCircleRadius(_circleRadius);
        _shaderService.EnableCircle();
    }
    private void Update()
    {
        PlayerMovement();
        CameraMovment();
        PlayerInput();
    }
    private void PlayerMovement()
    {
        Vector2 input = new Vector2(
            Keyboard.current.dKey.isPressed ? 1 : Keyboard.current.aKey.isPressed ? -1 : 0,
            Keyboard.current.wKey.isPressed ? 1 : Keyboard.current.sKey.isPressed ? -1 : 0
        );

        Vector3 targetVelocity = (transform.right * input.x + transform.forward * input.y) * _moveSpeed;

        float smoothing = input.magnitude > 0 ? _acceleration : _deceleration;
        _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, smoothing * Time.deltaTime);

        if (_characterController.isGrounded)
            _verticalVelocity = -1f;
        else
            _verticalVelocity += _gravity * Time.deltaTime;

        Vector3 finalMovement = _currentVelocity + Vector3.up * _verticalVelocity;
        _characterController.Move(finalMovement * Time.deltaTime);
    }

    private void CameraMovment()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        transform.Rotate(Vector3.up, mouseDelta.x * _mouseSensitivity);

        _verticalRotation -= mouseDelta.y * _mouseSensitivity;
        _verticalRotation = Mathf.Clamp(_verticalRotation, -_verticalClamp, _verticalClamp);
        _cameraTransform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
    }
    private void PlayerInput()
    {
        //Dejo esto solo para poder hacer mas facil el probar cosas en editor, borrar despues
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }
        //////////////////////////////////////////////

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            _isCircleActive = !_isCircleActive;

            if (_isCircleActive)
                _shaderService.EnableCircle();
            else
                _shaderService.DisableCircle();
        }
    }
}
