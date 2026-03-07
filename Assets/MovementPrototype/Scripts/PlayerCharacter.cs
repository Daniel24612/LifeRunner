using UnityEngine;
using KinematicCharacterController;
using System.Collections.Generic;
using System;
using TMPro;
public partial class PlayerCharacter : MonoBehaviour, ICharacterController
{
    #region Fields
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform root;
    [SerializeField] private InputReader _inputReader;
    [Header("Settings")]
    [SerializeField] private PlayerMovementSettings movementSettings;
    public PlayerMovementSettings s => movementSettings;
    private Dictionary<Type, PlayerState> _statesList;
    private PlayerState _currentState;
    private Quaternion _requestRotation;
    private Vector3 _requestMovement;
    private bool _isSubscribedToInput;
    //
    private bool _isGrounded => motor.GroundingStatus.IsStableOnGround;
    // Sprint
    private float PreparedRunSpeed => Mathf.Lerp(s.WalkSpeed, s.RunSpeed, Mathf.Clamp01(_inputReader.MoveInput.y));
    private bool _isSprinting => _inputReader != null && _inputReader.IsSprinting;
    // Jump
    private bool _requestJump;
    private float _jumpCayoteTimer;
    private float _sustainJumpTimer;
    private bool _requestSustainJump => s.IsSustainJumpEnabled &&
                !_requestJump &&
                _inputReader.IsJumpHold &&
                _sustainJumpTimer > 0f;
    // Crouch
    private bool _requestCrouch => _inputReader != null && _inputReader.IsCrouching;
    private bool _isCrouching => _requestCrouch || !_canUncrouch;
    private bool _canUncrouch = true;
    private Collider[] _uncrouchOverlapResults = new Collider[8];
    // Slide
    private bool _canSlide => (_isCrouching || _slideTimer > s.Slide_MaxTime - s.Slide_MinTime) && 
        motor.Velocity.magnitude > s.Slide_MinSpeed && 
        (_isGrounded || _slideCayoteTimer > 0f) && 
        _slideTimer >= 0f;
    private float _slideCayoteTimer;
    private float _slideTimer;
    // Walls
    private Vector3 _wallNormal;
    private float _wallGrabTimer;
    private List<RaycastSensor> _wallSensors;
    // Other
    private Vector3 _requestAddVelocity;
    #endregion
    public void Initialize(InputReader inputReader)
    {
        _statesList = new Dictionary<Type, PlayerState>()
        {
            {typeof(StandState), new StandState(this) },
            {typeof(CrouchState), new CrouchState(this)},
            {typeof(AirbornState), new AirbornState(this)},
            {typeof(SlideState), new SlideState(this) },
            {typeof(WallRunState), new WallRunState(this) },
            {typeof(WallGrabState), new WallGrabState(this) }
        };
        _wallSensors = new List<RaycastSensor>()
        {
            new RaycastSensor(transform).SetSettings(s.WallRun_SensorSettings).SetLayerMask(s.WallRun_Layers),
            new RaycastSensor(transform).SetSettings(s.WallRun_SensorSettings).SetLayerMask(s.WallRun_Layers).SetCastDirection(-s.WallRun_SensorSettings.Direction),
        };
        EnterState<StandState>();
       
        _inputReader = inputReader;
        motor.CharacterController = this;
        SubscribeToInput(true);
        RefreshWallGrab();
        RefreshJumpCayoteTimer();
    }
    public void AddVelocity(Vector3 velocity)
    {
        _requestAddVelocity += velocity;
    }
    public void UpdateInput(CharacterInput input)
    {
        _requestRotation = input.Rotation;

        _requestMovement = new Vector3(_inputReader.MoveInput.x, 0f, _inputReader.MoveInput.y);
        _requestMovement = Vector3.ClampMagnitude(_requestMovement, 1f);
        _requestMovement = input.Rotation * _requestMovement;
    }
    public void UpdateBody()
    {
        var currentHeight = motor.Capsule.height;
        var normalizedRootHeight = currentHeight / s.StandHeight;

        var cameraTargetHeight = currentHeight * _currentState.CameraHeight;
        var rootTargetScale = new Vector3(1f, normalizedRootHeight, 1f);

        cameraTarget.localPosition = Vector3.Lerp
            (
            cameraTarget.localPosition,
            new Vector3(0f, cameraTargetHeight, 0f),
            1f - Mathf.Exp(-s.HeightChangeResponse * Time.deltaTime)
            );

        root.localScale = Vector3.Lerp
            (
            root.localScale,
            rootTargetScale,
            1f - Mathf.Exp(-s.HeightChangeResponse * Time.deltaTime)
            );
    }
    private void SubscribeToInput(bool wantSub)
    {
        if (wantSub == _isSubscribedToInput) return;
        if (_inputReader == null) return;
        if (wantSub)
        {
            _inputReader.OnJumpPerformed += RequestJump;
            _isSubscribedToInput = true;
        }
        else
        {
            _inputReader.OnJumpPerformed -= RequestJump;
            _isSubscribedToInput = false;
        }
    }
    private void RequestJump()
    {
        _requestJump = true;
    }
    private void RefreshJumpCayoteTimer()
    {
        _jumpCayoteTimer = s.JumpCayoteTime;
    }
    private void RefreshSustainJump()
    {
        _sustainJumpTimer = s.SustainJumpDuration;
    }
    private void RefreshSlideCayoteTimer()
    {
        _slideCayoteTimer = s.Slide_CayoteTime;
    }
    private void RefreshWallGrab()
    {
        _wallGrabTimer = s.WallGrab_MaxTime;
    }
    public bool TryWall(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        if (Mathf.Abs(Vector3.Angle(hitNormal, motor.CharacterUp) - 90f) < s.MaxWallAngleMagnitude)
        {
            var angleToWall = Vector3.Angle(Vector3.ProjectOnPlane(-hitNormal, motor.CharacterUp), motor.CharacterForward);
            //var angleVelocityToWall = Vector3.Angle(Vector3.ProjectOnPlane(-hitNormal, c.motor.CharacterUp), c.motor.Velocity);
            if (angleToWall > s.WallRun_MaxAngle && angleToWall < s.WallRun_MaxAngle + 90 && ((1 << hitCollider.gameObject.layer) & s.WallRun_Layers) != 0)
            {
                //if (angleVelocityToWall > c.maxWallRunAngle && angleVelocityToWall < c.maxWallRunAngle + 90)
                {
                    _wallNormal = hitNormal;
                    EnterState<WallRunState>();
                }
            }
            else if (angleToWall < s.WallRun_MaxAngle && ((1 << hitCollider.gameObject.layer) & s.WallGrab_Layers) != 0)
            {
                _wallNormal = hitNormal;
                EnterState<WallGrabState>();
                if (Vector3.Dot(motor.BaseVelocity, -hitNormal) > s.WallGrab_MinSpeedToRefreshTimer)
                {
                    RefreshWallGrab();
                }
            }
            return true;
        }
        return false;
    }
    private void SetCrouchDemensions()
    {
        motor.SetCapsuleDimensions
                (
                radius: motor.Capsule.radius,
                height: s.Crouch_Height,
                yOffset: s.Crouch_Height * 0.5f
                );
    }
    private void SetStandDemensions()
    {
        motor.SetCapsuleDimensions
                (
                radius: motor.Capsule.radius,
                height: s.StandHeight,
                yOffset: s.StandHeight * 0.5f
                );
    }
    /// <summary>
    /// Old verison of this method
    /// </summary>
    /// <param name="type"></param>
    public void EnterState(Type type)
    {
        _currentState?.Exit();
        _currentState = _statesList[type];
        _currentState?.Enter();
        Debug.Log($"Enter to {type}");
    }
    public void EnterState<T>() where T : PlayerState
    {
        _currentState?.Exit();
        _currentState = _statesList[typeof(T)];
        _currentState?.Enter();
        Debug.Log($"Enter to {typeof(T)}");
    }
    /// <summary>
    /// Enter to BASIC states 
    /// !This method may not work correctly!
    /// </summary>
    /// <param name="condition"> </param>
    public void EnterToAnotherState()
    {
        if (_isGrounded)
        {
            if (_requestCrouch || !_canUncrouch)
            {
                if (_canSlide)
                {
                    EnterState<SlideState>();
                    return;
                }
                else if (_isCrouching)
                {
                    EnterState<CrouchState>();
                    return ;
                }
            }
            else
            {
                EnterState<StandState>();
                return;
            }
        }
        else if (!_isGrounded)
        {
            EnterState<AirbornState>();
            return;
        }
    }
    public void UpdateWallSensors()
    {
        foreach (var sensor in _wallSensors)
        {
            sensor.Cast();
        }
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        _currentState.UpdateRotation(ref currentRotation, deltaTime);
    }
    public void BeforeCharacterUpdate(float deltaTime)
    {
        if ((_currentState is SlideState || _currentState is CrouchState) && (!_requestCrouch || !_isGrounded))
        {
            // Stand up character capsule
            SetStandDemensions();
            // Check for colliders
            _canUncrouch = !(motor.CharacterOverlap(motor.TransientPosition, motor.TransientRotation, _uncrouchOverlapResults, motor.CollidableLayers, QueryTriggerInteraction.Ignore) > 0);
            SetCrouchDemensions();
        }
        _currentState.BeforeCharacterUpdate(deltaTime); 
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _currentState.UpdateVelocity(ref currentVelocity, deltaTime);
        if( _requestAddVelocity != Vector3.zero)
        {
            currentVelocity += _requestAddVelocity;
            _requestAddVelocity = Vector3.zero;
        }

        if (_requestJump)
        {
            if (_jumpCayoteTimer > 0)
            {
                // Unstick from ground
                motor.ForceUnground(time: 0f);
                _slideCayoteTimer = 0f;
                // Set minimum jump speed to jump
                var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                var targetVerticalSpeed = Mathf.Max(s.JumpSpeed, currentVerticalSpeed);
                // Add the difference between current and target vertical speeds to the current velocity
                currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
                // Refresh sustain jump
                RefreshSustainJump();
                _jumpCayoteTimer = 0f;
            }
            _requestJump = false;
        }
        speedText.text = $"Speed: {currentVelocity.magnitude:0.0}\n" +
                         $"Planar speed: {Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp).magnitude:0.0}\n" +
                         $"Vertical speed: {Vector3.Dot(currentVelocity, motor.CharacterUp):0.0}";
    }
    public void AfterCharacterUpdate(float deltaTime)
    {
        _currentState.AfterCharacterUpdate(deltaTime);
    }
    public void PostGroundingUpdate(float deltaTime)
    {
        _currentState.PostGroundingUpdate(deltaTime);
    }
    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }
    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        _currentState.OnGroundHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
        RefreshJumpCayoteTimer();
        RefreshWallGrab();
    }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        _currentState.OnMovementHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
    }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
        _currentState.ProcessHitStabilityReport(hitCollider, hitNormal, hitPoint, atCharacterPosition, atCharacterRotation, ref hitStabilityReport);
    }
    public void SetPosition(Vector3 position, bool killVelocity = true)
    {
        motor.SetPosition(position);
        if (killVelocity) motor.BaseVelocity = Vector3.zero;
    }
    public Transform GetCameraTarget()
    {
        return cameraTarget;
    }
}
public struct CharacterInput
{
    public Quaternion Rotation;
}
public enum CrouchInput
{
    None,
    Toggle,
}
