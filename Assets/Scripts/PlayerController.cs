using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _sprintSpeed = 6f;
    [SerializeField] private float _crouchSpeed = 1.5f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 15f;

    [Header("Camera")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _mouseSensitivity = 0.1f;
    [SerializeField] private float _verticalClamp = 80f;

    [Header("Crouch")]
    [SerializeField] private float _crouchCameraOffset = -0.5f;
    [SerializeField] private float _crouchCameraSpeed = 5f;

    [Header("Camera Bobbing - Walking")]
    [SerializeField] private float _walkBobFrequency = 2f;
    [SerializeField] private float _walkBobAmplitudeY = 0.05f;
    [SerializeField] private float _walkBobAmplitudeX = 0.025f;

    [Header("Camera Bobbing - Running")]
    [SerializeField] private float _runBobFrequency = 4f;
    [SerializeField] private float _runBobAmplitudeY = 0.1f;
    [SerializeField] private float _runBobAmplitudeX = 0.05f;

    [Header("Shader")]
    [SerializeField] private ShaderService _shaderService;

    [Header("Gravity")]
    [SerializeField] private float _gravity = -9.8f;

    private float _verticalVelocity;
    private CharacterController _characterController;
    private Vector3 _currentVelocity;
    private float _verticalRotation;
    private bool _isEnhancedVisionActive = false;

    private float _cameraBaseY;
    private float _bobTimer = 0f;

    public enum PlayerMovementState { Idle, Crouching, Walking, Running }
    public PlayerMovementState MovementState { get; private set; }

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _cameraBaseY = _cameraTransform.localPosition.y;
    }

    private System.Collections.IEnumerator Start()
    {
        yield return null;
        _shaderService.VisionState(false);
    }

    private void Update()
    {
        HandleMovement();
        HandleCamera();
        HandleInput();
    }

    private void HandleMovement()
    {
        if (_isEnhancedVisionActive)
        {
            _currentVelocity = Vector3.zero;
            MovementState = PlayerMovementState.Idle;

            if (_characterController.isGrounded)
                _verticalVelocity = -1f;
            else
                _verticalVelocity += _gravity * Time.deltaTime;

            _characterController.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
            return;
        }

        bool isCrouching = Keyboard.current.leftCtrlKey.isPressed;
        bool isSprinting = Keyboard.current.leftShiftKey.isPressed && !isCrouching;

        Vector2 input = new Vector2(
            Keyboard.current.dKey.isPressed ? 1 : Keyboard.current.aKey.isPressed ? -1 : 0,
            Keyboard.current.wKey.isPressed ? 1 : Keyboard.current.sKey.isPressed ? -1 : 0
        );

        float currentSpeed = isSprinting ? _sprintSpeed : isCrouching ? _crouchSpeed : _moveSpeed;

        if (input.magnitude == 0)
            MovementState = isCrouching ? PlayerMovementState.Crouching : PlayerMovementState.Idle;
        else
            MovementState = isSprinting ? PlayerMovementState.Running : isCrouching ? PlayerMovementState.Crouching : PlayerMovementState.Walking;

        Vector3 targetVelocity = (transform.right * input.x + transform.forward * input.y) * currentSpeed;
        float smoothing = input.magnitude > 0 ? _acceleration : _deceleration;
        _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, smoothing * Time.deltaTime);

        if (_characterController.isGrounded)
            _verticalVelocity = -1f;
        else
            _verticalVelocity += _gravity * Time.deltaTime;

        _characterController.Move((_currentVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime);
    }

    private void HandleCamera()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        transform.Rotate(Vector3.up, mouseDelta.x * _mouseSensitivity);
        _verticalRotation -= mouseDelta.y * _mouseSensitivity;
        _verticalRotation = Mathf.Clamp(_verticalRotation, -_verticalClamp, _verticalClamp);

        bool isCrouching = Keyboard.current.leftCtrlKey.isPressed;
        float targetCameraY = _cameraBaseY + (isCrouching ? _crouchCameraOffset : 0f);

        bool isMoving = _currentVelocity.magnitude > 0.1f;
        float bobY = 0f;
        float bobX = 0f;

        if (isMoving && !_isEnhancedVisionActive)
        {
            float frequency = MovementState == PlayerMovementState.Running ? _runBobFrequency : _walkBobFrequency;
            float amplitudeY = MovementState == PlayerMovementState.Running ? _runBobAmplitudeY : _walkBobAmplitudeY;
            float amplitudeX = MovementState == PlayerMovementState.Running ? _runBobAmplitudeX : _walkBobAmplitudeX;

            _bobTimer += Time.deltaTime * frequency;
            bobY = Mathf.Sin(_bobTimer) * amplitudeY;
            bobX = Mathf.Sin(_bobTimer * 0.5f) * amplitudeX;
        }
        else
        {
            _bobTimer = 0f;
        }

        float smoothCameraY = Mathf.Lerp(_cameraTransform.localPosition.y, targetCameraY + bobY, _crouchCameraSpeed * Time.deltaTime);
        _cameraTransform.localPosition = new Vector3(bobX, smoothCameraY, _cameraTransform.localPosition.z);
        _cameraTransform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
    }

    private void HandleInput()
    {
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

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            _isEnhancedVisionActive = !_isEnhancedVisionActive;
            _shaderService.VisionState(_isEnhancedVisionActive);
        }
    }
}