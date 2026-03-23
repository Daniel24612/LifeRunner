using UnityEngine;
using KinematicCharacterController;
using System;
using TMPro;

namespace Player.Movement
{
    public partial class PlayerCharacterMover : MonoBehaviour, ICharacterController
    {
        #region Fields
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] internal KinematicCharacterMotor motor;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] internal Transform root;
        [SerializeField] internal InputReader _inputReader;
        [Header("Settings")]
        [SerializeField] private PlayerMovementSettings movementSettings;
        [SerializeField] internal Vector3 defaultUp = Vector3.up;
        private MoverStateMachine _stateMachine;
        public PlayerMovementSettings s => movementSettings;
        private PlayerState _currentState => _stateMachine.currentState;
        internal Quaternion _requestRotation;
        internal Vector3 _requestMovement;
        private bool _isSubscribedToInput;
        //
        public bool IsGrounded => motor.GroundingStatus.IsStableOnGround;
        // Sprint
        internal float PreparedRunSpeed => Mathf.Lerp(s.WalkSpeed, s.RunSpeed, Mathf.Clamp01(_inputReader.MoveInput.y));
        internal bool IsSprinting => _inputReader != null && _inputReader.IsSprinting;
        // Jump
        internal bool _requestJump;
        internal float _jumpCayoteTimer;
        internal float _sustainJumpTimer;
        internal bool _requestSustainJump => s.IsSustainJumpEnabled &&
                    !_requestJump &&
                    _inputReader.IsJumpHold &&
                    _sustainJumpTimer > 0f;
        // Crouch
        internal bool _requestCrouch => _inputReader.IsCrouching;
        internal bool _IsCrouching => _requestCrouch || !_canUncrouch;
        internal bool _canUncrouch = true;
        internal Collider[] _uncrouchOverlapResults = new Collider[8];
        // Slide
        internal bool _RequestSlide => (_IsCrouching || _slideTimer > s.Slide_MaxTime - s.Slide_MinTime) &&
            motor.Velocity.magnitude > s.Slide_MinSpeed &&
            (IsGrounded || _slideCayoteTimer > 0f) &&
            _slideTimer >= 0f;
        internal float _slideCayoteTimer;
        internal float _slideTimer;
        // Walls
        internal Vector3 _wallNormal;
        internal float _wallGrabTimer;
        // Ledge grab
        internal float _ledgeGrabTimer;

        // Other
        private Vector3 _requestAddVelocity;
        #endregion
        public void Initialize(InputReader inputReader)
        {
            _inputReader = inputReader;
            _stateMachine = new MoverStateMachine(movementSettings, this);
            _stateMachine.AddState
                (
                new StandState(this),
                new CrouchState(this),
                new SlideState(this),
                new AirbornState(this),
                new WallRunState(this),
                new WallGrabState(this),
                new LedgeGrabState(this)
                );
            EnterState<StandState>();

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
        internal void RefreshJumpCayoteTimer()
        {
            _jumpCayoteTimer = s.JumpCayoteTime;
        }
        internal void RefreshSustainJump()
        {
            _sustainJumpTimer = s.SustainJumpDuration;
        }
        internal void RefreshSlideCayoteTimer()
        {
            _slideCayoteTimer = s.Slide_CayoteTime;
        }
        internal void RefreshWallGrab()
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
                        var wallState = _stateMachine.GetStateReference<WallRunState>();
                        wallState.UpdateWallForward();
                        if (wallState.IsValidWallRunVelocity(motor.Velocity))
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
        public bool TryLedgeGrab()
        {
            return _stateMachine.GetStateReference<LedgeGrabState>().CheckSensors();
        }
        internal void SetCrouchDemensions()
        {
            motor.SetCapsuleDimensions
                    (
                    radius: motor.Capsule.radius,
                    height: s.Crouch_Height,
                    yOffset: s.Crouch_Height * 0.5f
                    );
        }
        internal void SetStandDemensions()
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
            _stateMachine.EnterState(type);
            Debug.Log($"Enter to {type}");
        }
        public void EnterState<T>() where T : PlayerState
        {
            _stateMachine.EnterState<T>();
            Debug.Log($"Enter to {typeof(T)}");
        }
        /// <summary>
        /// Enter to BASIC states 
        /// !This method may not work correctly!
        /// </summary>
        /// <param name="condition"> </param>
        public void EnterToAnotherState()
        {
            if (IsGrounded)
            {
                if (_requestCrouch || !_canUncrouch)
                {
                    if (_RequestSlide)
                    {
                        EnterState<SlideState>();
                        return;
                    }
                    else if (_IsCrouching)
                    {
                        EnterState<CrouchState>();
                        return;
                    }
                }
                else
                {
                    EnterState<StandState>();
                    return;
                }
            }
            else if (!IsGrounded)
            {
                EnterState<AirbornState>();
                return;
            }
        }
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            _currentState.UpdateRotation(ref currentRotation, deltaTime);
        }
        public void BeforeCharacterUpdate(float deltaTime)
        {
            if ((_currentState is SlideState || _currentState is CrouchState) && (!_requestCrouch || !IsGrounded))
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
            if (_requestAddVelocity != Vector3.zero)
            {
                if (Vector3.Dot(_requestAddVelocity, motor.CharacterUp) > 0)
                    motor.ForceUnground(0f);
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
}
