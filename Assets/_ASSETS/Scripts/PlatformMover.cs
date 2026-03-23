using UnityEngine;
using KinematicCharacterController;
using Cysharp.Threading.Tasks;
using System.Threading;

[RequireComponent(typeof(Rigidbody), typeof(PhysicsMover))]
public class PlatformMover : MonoBehaviour, IMoverController
{
    [SerializeField] private PhysicsMover mover;
    [SerializeField] private bool moveOnStart = true;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 2f;

    private bool canMove;
    private Vector3 _currentRequestPosition;
    private Vector3 _currentTargetPosition;
    private int _currentWaypointIndex = 0;
    private bool _isMoving = false;
    private void Start()
    {
        mover.MoverController = this;
        mover.MoveWithPhysics = true;
        if (waypoints.Length < 2)
        {
            Debug.LogError("PlatformMover requires at least 2 waypoints to function properly.");
            canMove = false;
            return;
        }
        else         
            canMove = true;

        _currentRequestPosition = waypoints[0].position;
        _currentTargetPosition = waypoints[1].position;
        _currentWaypointIndex = 1;
        if (moveOnStart && canMove)
            StartMoving();
    }
    public void StartMoving()
    {
        ImplementMovement(this.GetCancellationTokenOnDestroy()).Forget();
        _isMoving = true;
    }
    public void StopMoving()
    {
        _isMoving = false;
    }
    public void ResetPosition()
    {
        _currentTargetPosition = waypoints[0].position;
    }
    private Vector3 GetNextWaypoint()
    {
        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
        return waypoints[_currentWaypointIndex].position;
    }
    private async UniTaskVoid ImplementMovement(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_isMoving)
                MoveIteration();
            await UniTask.WaitForFixedUpdate(token);
        }
    }
    private void MoveIteration()
    {
        var requetLeap = moveSpeed * Time.fixedDeltaTime;
        while (Vector3.Distance(_currentRequestPosition, _currentTargetPosition) <= requetLeap)
            {
                requetLeap -= Vector3.Distance(_currentRequestPosition, _currentTargetPosition);
                _currentRequestPosition = _currentTargetPosition;
                _currentTargetPosition = GetNextWaypoint();
                if (requetLeap <= 0f)
                    break;
        }

        var direction = (_currentTargetPosition - _currentRequestPosition).normalized;
        _currentRequestPosition += direction * requetLeap;
    }
    public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
    {
        goalPosition = _currentRequestPosition;
        goalRotation = Quaternion.identity;
    }
}