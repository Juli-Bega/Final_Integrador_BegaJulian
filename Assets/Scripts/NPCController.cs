using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NPCController : MonoBehaviour
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

    [Header("Movement Weights")]
    [SerializeField] private float _weightIdle = 0.1f;
    [SerializeField] private float _weightCrouching = 0.2f;
    [SerializeField] private float _weightWalking = 0.5f;
    [SerializeField] private float _weightRunning = 1f;

    [Header("Suspicious")]
    [SerializeField] private float _positionUpdateInterval = 0.5f;
    [SerializeField] private float _positionUpdateThreshold = 30f;
    [SerializeField] private float _lookAroundDuration = 3f;

    private NavMeshAgent _agent;
    private Transform _player;
    private PlayerController _playerController;
    private VisionCone _visionCone;

    private float _alertLevel = 0f;
    private int _currentWaypointIndex = 0;
    private bool _isMovingForward = true;
    private Vector3 _lastKnownPlayerPosition;
    private float _positionUpdateTimer = 0f;

    private enum NPCState { Patrol, Suspicious, Detected }
    private NPCState _currentState = NPCState.Patrol;

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
        if (!IsPlayerInCone())
        {
            if (_currentState != NPCState.Suspicious)
                _alertLevel = Mathf.Max(0, _alertLevel - _alertDecaySpeed * Time.deltaTime);
            return;
        }

        float distanceFactor = 1f - Mathf.InverseLerp(0, _visionRange,
            Vector3.Distance(transform.position, _player.position));

        Vector3 directionToPlayer = (_player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        float angleFactor = 1f - Mathf.InverseLerp(0, _coneAngle / 2, angleToPlayer);

        float movementFactor = GetMovementFactor();

        _alertLevel += (distanceFactor + angleFactor + movementFactor) * _alertSpeed * Time.deltaTime;
        _alertLevel = Mathf.Clamp(_alertLevel, 0, 100);

        UpdateLastKnownPosition();
    }

    private bool IsPlayerInCone()
    {
        Vector3 directionToPlayer = _player.position - transform.position;
        float distance = directionToPlayer.magnitude;

        if (distance > _visionRange)
            return false;

        float angle = Vector3.Angle(transform.forward, directionToPlayer.normalized);
        return angle <= _coneAngle / 2;
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
                _lastKnownPlayerPosition = _player.position;
        }

        if (_alertLevel >= _suspiciousThreshold && _lastKnownPlayerPosition == Vector3.zero)
            _lastKnownPlayerPosition = _player.position;
    }

    private void UpdateState()
    {
        if (_alertLevel >= 100f && _currentState != NPCState.Detected)
        {
            _currentState = NPCState.Detected;
            OnDetected();
            return;
        }

        if (_alertLevel >= _suspiciousThreshold && _currentState == NPCState.Patrol)
        {
            _currentState = NPCState.Suspicious;
            _agent.speed = _suspiciousSpeed;
            StartCoroutine(SuspiciousRoutine());
        }

        if (_alertLevel < _suspiciousThreshold && _currentState == NPCState.Suspicious)
        {
            _currentState = NPCState.Patrol;
            _agent.speed = _patrolSpeed;
        }
    }

    private void UpdateMovement()
    {
        if (_currentState != NPCState.Patrol) return;
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
        _agent.SetDestination(_lastKnownPlayerPosition);

        while (_agent.remainingDistance > _waypointStopDistance || _agent.pathPending)
            yield return null;

        float timer = 0f;
        while (timer < _lookAroundDuration)
        {
            timer += Time.deltaTime;
            transform.Rotate(Vector3.up, 60f * Time.deltaTime);
            yield return null;
        }

        GoToClosestWaypoint();
        _currentState = NPCState.Patrol;
        _agent.speed = _patrolSpeed;
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
        Debug.Log("DETECTED - Game Over");
    }
}