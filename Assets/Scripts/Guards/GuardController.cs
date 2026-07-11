using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GuardController : MonoBehaviour
{
    [Header("Waypoints")]
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _waypointStopDistance = 0.5f;
    [SerializeField] private bool _loopWaypoints = false;

    [Header("Movement")]
    [SerializeField] private float _patrolSpeed = 2f;
    [SerializeField] private float _suspiciousSpeed = 4f;

    [Header("Detection")]
    [SerializeField] private float _visionRange = 10f;
    [SerializeField] private float _coneAngle = 90f;
    [SerializeField] private float _alertSpeed = 10f;
    [SerializeField] private float _alertDecaySpeed = 5f;
    [SerializeField] private float _suspiciousThreshold = 50f;
    [SerializeField] private float _suspiciousWeightMultiplier = 1.5f;
    [SerializeField] private LayerMask _obstacleMask;

    [Header("Movement Weights")]
    [SerializeField] private float _weightIdle = 0.1f;
    [SerializeField] private float _weightCrouching = 0.2f;
    [SerializeField] private float _weightWalking = 0.5f;
    [SerializeField] private float _weightRunning = 1f;

    [Header("Suspicious")]
    [SerializeField] private float _positionUpdateInterval = 0.5f;
    [SerializeField] private float _positionUpdateThreshold = 30f;
    [SerializeField] private float _lookAroundAngle = 60f;
    [SerializeField] private float _lookAroundSpeed = 2f;

    [Header("Debug")]
    [SerializeField] private bool _showDebugInfo = true;

    private NavMeshAgent _agent;
    private Transform _player;
    private PlayerController _playerController;
    private VisionCone _visionCone;

    private float _alertLevel = 0f;
    private int _currentWaypointIndex = 0;
    private bool _isMovingForward = true;
    private Vector3 _lastKnownPlayerPosition;
    private bool _hasLastKnownPosition = false;
    private float _positionUpdateTimer = 0f;

 
    public enum GuardState { Patrol, Suspicious, Detected }
    private GuardState _currentState = GuardState.Patrol;
    public float AlertLevel => _alertLevel;
    public GuardState CurrentState => _currentState;
    
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _player = GameObject.FindWithTag("Player").transform;
        _playerController = _player.GetComponent<PlayerController>();
        _visionCone = GetComponentInChildren<VisionCone>();
    }

    private void Start()
    {
        _agent.speed = _patrolSpeed;
        _currentWaypointIndex = 0;
        _agent.SetDestination(_waypoints[_currentWaypointIndex].position);

        if (_visionCone != null)
            _visionCone.SetConeParameters(_visionRange, _coneAngle);
    }

    private void Update()
    {
        UpdateDetection();
        UpdateState();
        UpdateMovement();
    }

    private void UpdateDetection()
    {
        if (!CanSeePlayer())
        {
            bool isInvestigatingTravel = _currentState == GuardState.Suspicious && !_isLookingAround;
            if (!isInvestigatingTravel)
                _alertLevel = Mathf.Max(0, _alertLevel - _alertDecaySpeed * Time.deltaTime);
            return;
        }

        float distanceFactor = 1f - Mathf.InverseLerp(0, _visionRange,
            Vector3.Distance(transform.position, _player.position));

        Vector3 directionToPlayer = (_player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        float angleFactor = 1f - Mathf.InverseLerp(0, _coneAngle / 2, angleToPlayer);

        float movementFactor = GetMovementFactor();

        float stateMultiplier = _currentState == GuardState.Suspicious ? _suspiciousWeightMultiplier : 1f;

        _alertLevel += (distanceFactor + angleFactor + movementFactor) * stateMultiplier * _alertSpeed * Time.deltaTime;
        _alertLevel = Mathf.Clamp(_alertLevel, 0, 100);

        UpdateLastKnownPosition();
    }

    private bool _isLookingAround = false;

    private bool CanSeePlayer()
    {
        Vector3 directionToPlayer = _player.position - transform.position;
        float distance = directionToPlayer.magnitude;

        if (distance > _visionRange)
            return false;

        float angle = Vector3.Angle(transform.forward, directionToPlayer.normalized);
        if (angle > _coneAngle / 2)
            return false;

        if (Physics.Raycast(transform.position, directionToPlayer.normalized, distance, _obstacleMask))
            return false;

        return true;
    }

    private float GetMovementFactor()
    {
        switch (_playerController.MovementState)
        {
            case PlayerController.PlayerMovementState.Idle: return _weightIdle;
            case PlayerController.PlayerMovementState.Crouching: return _weightCrouching;
            case PlayerController.PlayerMovementState.Walking: return _weightWalking;
            case PlayerController.PlayerMovementState.Running: return _weightRunning;
            default: return _weightWalking;
        }
    }

    private void UpdateLastKnownPosition()
    {
        _positionUpdateTimer += Time.deltaTime;

        if (_positionUpdateTimer >= _positionUpdateInterval)
        {
            _positionUpdateTimer = 0f;

            if (_alertLevel >= _positionUpdateThreshold)
            {
                _lastKnownPlayerPosition = _player.position;
                _hasLastKnownPosition = true;
            }
        }

        if (_alertLevel >= _suspiciousThreshold && !_hasLastKnownPosition)
        {
            _lastKnownPlayerPosition = _player.position;
            _hasLastKnownPosition = true;
        }
    }

    private void UpdateState()
    {
        if (_alertLevel >= 100f && _currentState != GuardState.Detected)
        {
            _currentState = GuardState.Detected;
            OnDetected();
            return;
        }

        if (_alertLevel >= _suspiciousThreshold && _currentState == GuardState.Patrol)
        {
            _currentState = GuardState.Suspicious;
            _agent.speed = _suspiciousSpeed;
            StartCoroutine(SuspiciousRoutine());
        }
    }

    private void UpdateMovement()
    {
        if (_currentState != GuardState.Patrol) return;
        if (_agent.pathPending) return;
        if (_agent.remainingDistance > _waypointStopDistance) return;

        GoToNextWaypoint();
    }

    private void GoToNextWaypoint()
    {
        if (_waypoints.Length == 0) return;
        AdvanceWaypointIndex();
        _agent.SetDestination(_waypoints[_currentWaypointIndex].position);
    }

    private void AdvanceWaypointIndex()
    {
        if (_waypoints.Length == 1) return;

        if (_loopWaypoints)
        {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
        }
        else
        {
            if (_isMovingForward)
            {
                if (_currentWaypointIndex < _waypoints.Length - 1)
                    _currentWaypointIndex++;
                else
                {
                    _isMovingForward = false;
                    _currentWaypointIndex--;
                }
            }
            else
            {
                if (_currentWaypointIndex > 0)
                    _currentWaypointIndex--;
                else
                {
                    _isMovingForward = true;
                    _currentWaypointIndex++;
                }
            }
        }
    }

    private IEnumerator SuspiciousRoutine()
    {
        while (_alertLevel >= _suspiciousThreshold)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_lastKnownPlayerPosition);
            Vector3 investigatedPosition = _lastKnownPlayerPosition;

            while (_agent.remainingDistance > _waypointStopDistance || _agent.pathPending)
            {
                if (_alertLevel >= 100f) yield break;
                yield return null;
            }

            _isLookingAround = true;
            _agent.isStopped = true;
            Quaternion baseRotation = transform.rotation;
            float lookTimer = 0f;

            while (_alertLevel >= _suspiciousThreshold)
            {
                if (_alertLevel >= 100f) yield break;

                if (CanSeePlayer())
                {
                    Vector3 lookDirection = _player.position - transform.position;
                    lookDirection.y = 0;
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(lookDirection), 5f * Time.deltaTime);
                    baseRotation = transform.rotation;
                    lookTimer = 0f;
                }
                else
                {
                    if (_lastKnownPlayerPosition != investigatedPosition)
                        break;

                    lookTimer += Time.deltaTime * _lookAroundSpeed;
                    float angle = Mathf.Sin(lookTimer) * _lookAroundAngle;
                    transform.rotation = baseRotation * Quaternion.Euler(0, angle, 0);
                }
                yield return null;
            }

            _isLookingAround = false;
        }

        _isLookingAround = false;
        _hasLastKnownPosition = false;
        _currentState = GuardState.Patrol;
        _agent.speed = _patrolSpeed;
        GoToClosestWaypoint();
    }

    private void GoToClosestWaypoint()
    {
        float closestDistance = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < _waypoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, _waypoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        _currentWaypointIndex = closestIndex;
        _agent.SetDestination(_waypoints[_currentWaypointIndex].position);
    }

    private void OnDetected()
    {
        _agent.isStopped = true;
        LevelManager.Instance.PlayerDetected();
    }


    private void OnGUI()
    {
        if (!_showDebugInfo) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);
        if (screenPos.z < 0) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = _currentState == GuardState.Patrol ? Color.green :
                                 _currentState == GuardState.Suspicious ? Color.yellow : Color.red;

        string info = $"{_currentState} | Alert: {_alertLevel:F1}% | Sees: {CanSeePlayer()}";
        GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y, 250, 30), info, style);
    }

    private void OnDrawGizmos()
    {
        if (!_showDebugInfo) return;

        Gizmos.color = Color.yellow;
        Vector3 leftBoundary = Quaternion.Euler(0, -_coneAngle / 2, 0) * transform.forward * _visionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, _coneAngle / 2, 0) * transform.forward * _visionRange;
        Gizmos.DrawRay(transform.position, leftBoundary);
        Gizmos.DrawRay(transform.position, rightBoundary);

        if (_player != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.red : Color.gray;
            Gizmos.DrawLine(transform.position, _player.position);
        }
    }
}