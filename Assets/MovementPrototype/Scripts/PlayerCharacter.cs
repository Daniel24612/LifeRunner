using UnityEngine;
using KinematicCharacterController;
using System.Collections.Generic;
using System;
public partial class PlayerCharacter : MonoBehaviour, ICharacterController
{
    #region Fields
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform root;
    [SerializeField] private InputReader _inputReader;
    [Header("Base settings")]
    [SerializeField] private float gravity = -30f;
    [SerializeField] private float heightChangeResponse = 15f;
    [SerializeField] private float speedChangeResponse = 20f;
    [SerializeField, Range(0f, 1f)] private float standCameraHeight = 0.9f;
    [SerializeField, Range(0f, 1f)] private float crouchCameraHeight = 0.6f;
    [Header("Stand")]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float walkSpeed = 7f;
    [SerializeField] private float runSpeed = 15f;
    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 10f;
    [SerializeField] private bool isSustainJumpEnabled = true;
    [SerializeField, Range(0f, 1f)] private float sustainJumpGravity = 0.3f;
    [SerializeField] private float sustainJumpDuration = 0.5f;
    [Header("Crouch")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchSpeed = 5f;
    [Header("Slide")]
    [SerializeField] private float slideGravity = -30f;
    [SerializeField] private float startSlideSpeed = 20f;
    [SerializeField] private float minSlideSpeed = 10f;
    [SerializeField] private float minSlideTime = 1f;
    [SerializeField] private float maxSlideTime = 3f;
    [SerializeField] private float slideCayoteTime = 0.3f;
    [SerializeField] private float slideFriction = 0.8f;
    [Header("Airborn")]
    [SerializeField] private float airSpeed = 12f;
    [SerializeField] private float airAcceleration = 70f;

    private Dictionary<Type, PlayerState> _statesList;
    private PlayerState _currentState;
    private Stance _stance;
    private Quaternion _requestRotation;
    private Vector3 _requestMovement;
    private bool _isSubscribedToInput;
    //
    private bool _isGrounded => motor.GroundingStatus.IsStableOnGround;
    // Sprint
    private float PreparedRunSpeed => Mathf.Lerp(walkSpeed, runSpeed, Mathf.Clamp01(_inputReader.MoveInput.y));
    private bool _isSprinting => _inputReader != null && _inputReader.IsSprinting;
    // Jump
    private bool _requestJump;
    private float _sustainJumpTimer;
    private bool _requestSustainJump => isSustainJumpEnabled &&
                !_requestJump &&
                _inputReader.IsJumpHold &&
                _sustainJumpTimer > 0f;
    // Crouch
    private bool _requestCrouch => _inputReader != null && _inputReader.IsCrouching;
    private bool _isCrouching => _requestCrouch || !_canUncrouch;
    private bool _canUncrouch = true;
    private Collider[] _uncrouchOverlapResults = new Collider[8];
    // Slide
    private bool _canSlide => (_isCrouching || _slideTimer > maxSlideTime - minSlideTime) && 
        motor.Velocity.magnitude > minSlideSpeed && 
        (_isGrounded || _slideCayoteTimer > 0f) && 
        _slideTimer >= 0f;
    private float _slideCayoteTimer;
    private float _slideTimer;

    #endregion
    public void Initialize(InputReader inputReader)
    {
        _statesList = new Dictionary<Type, PlayerState>()
        {
            {typeof(StandState), new StandState(this) },
            {typeof(CrouchState), new CrouchState(this)},
            {typeof(AirbornState), new AirbornState(this)},
            {typeof(SlideState), new SlideState(this) }
        };
        EnterState<StandState>();
       
        _inputReader = inputReader;
        motor.CharacterController = this;
        SubscribeToInput(true);
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
        var normalizedRootHeight = currentHeight / standHeight;

        var cameraTargetHeight = currentHeight * _currentState.CameraHeight;
        var rootTargetScale = new Vector3(1f, normalizedRootHeight, 1f);

        cameraTarget.localPosition = Vector3.Lerp
            (
            cameraTarget.localPosition,
            new Vector3(0f, cameraTargetHeight, 0f),
            1f - Mathf.Exp(-heightChangeResponse * Time.deltaTime)
            );

        root.localScale = Vector3.Lerp
            (
            root.localScale,
            rootTargetScale,
            1f - Mathf.Exp(-heightChangeResponse * Time.deltaTime)
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
        _requestJump = motor.GroundingStatus.IsStableOnGround;
    }
    private void SetCrouchDemensions()
    {
        motor.SetCapsuleDimensions
                (
                radius: motor.Capsule.radius,
                height: crouchHeight,
                yOffset: crouchHeight * 0.5f
                );
    }
    private void SetStandDemensions()
    {
        motor.SetCapsuleDimensions
                (
                radius: motor.Capsule.radius,
                height: standHeight,
                yOffset: standHeight * 0.5f
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
    public void EnterToAnotherStateIf(bool condition)
    {
        if (!condition) return;

        if (_isGrounded)
        {
            if (_requestCrouch)
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




    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        var forward = Vector3.ProjectOnPlane(_requestRotation * Vector3.forward, motor.CharacterUp);

        if(forward != Vector3.zero)
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
    }
    public void BeforeCharacterUpdate(float deltaTime)
    {
        if ( (_currentState is CrouchState || _currentState is SlideState) && (!_requestCrouch || !_isGrounded))
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

        if (_requestJump)
        {
            if (_isGrounded)
            {
                // Unstick from ground
                motor.ForceUnground(time: 0f);
                _slideCayoteTimer = 0f;
                // Set minimum jump speed to jump
                var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                var targetVerticalSpeed = Mathf.Max(jumpSpeed, currentVerticalSpeed);
                // Add the difference between current and target vertical speeds to the current velocity
                currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
                // Refresh sustain jump
                _sustainJumpTimer = sustainJumpDuration;
            }
            _requestJump = false;
        }
    }
    public void AfterCharacterUpdate(float deltaTime)
    {
    }
    public void PostGroundingUpdate(float deltaTime)
    {
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
    }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
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
public enum Stance
{
    Stand,
    Crouch
}
public enum CrouchInput
{
    None,
    Toggle,
}
