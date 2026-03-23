using KinematicCharacterController;
using UnityUtils;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace Player.Movement
{
    public class MoverStateMachine
    {
        private PlayerMovementSettings _settings;
        private PlayerCharacterMover _mover;
        public MoverStateMachine(PlayerMovementSettings settings, PlayerCharacterMover mover)
        {
            _settings = settings;
            _mover = mover;
            _statesList = new Dictionary<Type, PlayerState>();
        }
        private Dictionary<Type, PlayerState> _statesList;
        internal PlayerState currentState;
        public void AddState(params PlayerState[] states)
        {
            foreach (var state in states)
                if (!_statesList.ContainsKey(state.GetType()))
                    _statesList.Add(state.GetType(), state);
        }
        /// <summary>
        /// Old verison of this method
        /// </summary>
        /// <param name="type"></param>
        public void EnterState(Type type)
        {
            currentState?.Exit();
            currentState = _statesList[type];
            currentState?.Enter();
            Debug.Log($"Enter to {type}");
        }
        public void EnterState<T>() where T : PlayerState
        {
            currentState?.Exit();
            currentState = _statesList[typeof(T)];
            currentState?.Enter();
            Debug.Log($"Enter to {typeof(T)}");
        }
        internal T GetStateReference<T>() where T : PlayerState
        {
            return _statesList[typeof(T)] as T;
        }
    }
    public abstract class PlayerState
    {
        protected PlayerCharacterMover c;
        protected PlayerMovementSettings s;
        public float CameraHeight { get; protected set; }
        public PlayerState(PlayerCharacterMover character)
        {
            c = character;
            s = character.s;
        }
        public abstract void Enter();
        public abstract void Exit();
        public virtual void Update() { }
        public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            var forward = Vector3.ProjectOnPlane(c._requestRotation * Vector3.forward, c.defaultUp);

            if (forward != Vector3.zero)
                currentRotation = Quaternion.LookRotation(forward, c.defaultUp);
        }
        public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {

        }
        public virtual void BeforeCharacterUpdate(float deltaTime) { }
        public virtual void AfterCharacterUpdate(float deltaTime) { }
        public virtual void PostGroundingUpdate(float deltaTime) { }
        public virtual bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }
        public virtual void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }
        public virtual void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }
        public virtual void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }
        public virtual void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
    public class StandState : PlayerState
    {
        public StandState(PlayerCharacterMover character) : base(character)
        {
            CameraHeight = character.s.StandCameraHeight;
        }

        public override void Enter()
        {
            c.SetStandDemensions();
        }
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            base.UpdateRotation(ref currentRotation, deltaTime);
        }
        public override void BeforeCharacterUpdate(float deltaTime)
        {
            if (!c.IsGrounded || c._requestCrouch)
                c.EnterToAnotherState();
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            var groundedMovement = c.motor
               .GetDirectionTangentToSurface(c._requestMovement, c.motor.GroundingStatus.GroundNormal)
               * c._requestMovement.magnitude;

            var targetVelocity = groundedMovement * (c.IsSprinting ?
                Mathf.Lerp(c.s.WalkSpeed, c.s.RunSpeed, Mathf.Clamp01(c._inputReader.MoveInput.y)) : c.s.WalkSpeed);

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                targetVelocity,
                1f - Mathf.Exp(-c.s.SpeedChangeResponse * deltaTime)
                );
        }
        public override void Exit()
        {

        }
    }
    public class CrouchState : PlayerState
    {
        public CrouchState(PlayerCharacterMover character) : base(character)
        {
            CameraHeight = character.s.StandCameraHeight;
        }
        public override void Enter()
        {
            c.SetCrouchDemensions();
        }
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            base.UpdateRotation(ref currentRotation, deltaTime);
        }
        public override void BeforeCharacterUpdate(float deltaTime)
        {
            if (c._canUncrouch && (!c._IsCrouching || !c.IsGrounded))
                c.EnterToAnotherState();
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            var groundedMovement = c.motor
               .GetDirectionTangentToSurface(c._requestMovement, c.motor.GroundingStatus.GroundNormal)
               * c._requestMovement.magnitude;

            var targetVelocity = groundedMovement * c.s.Crouch_Speed;

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                targetVelocity,
                1f - Mathf.Exp(-c.s.SpeedChangeResponse * deltaTime)
                );
            if (!c.IsGrounded)
            {
                currentVelocity += c.motor.CharacterUp * c.s.Gravity * deltaTime;
            }
        }
        public override void Exit()
        {
        }
    }
    public class SlideState : PlayerState
    {
        private float _tempMaxStableSlopeAngle;
        private Vector3 _afterGroundHitRequestVelocity;
        private Vector3 _tempHorizontalForce;
        private Vector3 _groundNormal;
        private bool _wasGrounded;
        private bool IsGrounded => c.IsGrounded || _wasGrounded;
        public SlideState(PlayerCharacterMover character) : base(character)
        {
            CameraHeight = c.s.CrouchCameraHeight;
        }
        public override void Enter()
        {
            c.SetCrouchDemensions();
            c._slideTimer = c.s.Slide_MaxTime;
            c._canUncrouch = false;
            c.RefreshSlideCayoteTimer();
            _tempMaxStableSlopeAngle = c.motor.MaxStableSlopeAngle;
            c.motor.MaxStableSlopeAngle = 0;
        }
        public override void BeforeCharacterUpdate(float deltaTime)
        {
            if (!IsGrounded && c._slideCayoteTimer > 0f)
            {
                c._slideCayoteTimer -= deltaTime;
            }
            else
            {
                c._slideCayoteTimer = IsGrounded ? c.s.Slide_CayoteTime : 0f;
            }
            if (!(c._IsCrouching && c._slideTimer > 0) && !c._RequestSlide)
                c.EnterToAnotherState();
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            c._slideTimer -= deltaTime;
            c._canUncrouch = (c._slideTimer <= c.s.Slide_MaxTime - c.s.Slide_MinTime);
            if (currentVelocity.magnitude < s.Slide_MinSpeed)
                c.EnterToAnotherState();
            // Add as speed
            if (IsGrounded)
            {
                Debug.Log("IsGrounded");
                if (_afterGroundHitRequestVelocity != Vector3.zero)
                {
                    currentVelocity = _afterGroundHitRequestVelocity;
                }
                // Add friction
                currentVelocity -= currentVelocity * (c.s.Slide_Friction * deltaTime);
            }
            // Add slide gravity
            currentVelocity += c.motor.CharacterUp * (c.s.Slide_Gravity * deltaTime);

            // Controll slide direction
            var moveInput = c._inputReader.MoveInput;
            if (moveInput.sqrMagnitude > 0)
            {
                currentVelocity -= _tempHorizontalForce;
                var crossHorizontalDir = Vector3.Cross(c.motor.CharacterUp, currentVelocity).normalized;
                var crossVerticalDir = c.motor.GetDirectionTangentToSurface
                (
                    direction: currentVelocity,
                    surfaceNormal: c.motor.GroundingStatus.GroundNormal
                ).normalized;

                var verticalForce = (moveInput.y == -1 ? -crossVerticalDir * deltaTime : Vector3.zero)
                    * c.s.Slide_ControlForce;
                var horizontalForce = crossHorizontalDir * moveInput.x * c.s.Slide_ControlForce;
                currentVelocity += horizontalForce + verticalForce;
                _tempHorizontalForce = horizontalForce;
            }
            if (c._requestJump && IsGrounded)
            {
                // Unstick from ground
                c.motor.ForceUnground(time: 0.1f);
                // Set minimum jump speed to jump
                var currentVerticalSpeed = Vector3.Dot(currentVelocity, c.motor.CharacterUp);
                var targetVerticalSpeed = Mathf.Max(c.s.JumpSpeed, currentVerticalSpeed);
                // Add the difference between current and target vertical speeds to the current velocity
                currentVelocity += c.motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
                // Refresh sustain jump
                c.RefreshSustainJump();
                c._jumpCayoteTimer = 0f;
                c._requestJump = false;
            }
            _wasGrounded = false;
        }
        public override void AfterCharacterUpdate(float deltaTime)
        {
            _afterGroundHitRequestVelocity = Vector3.zero;
        }
        public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            base.OnMovementHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
            if (LayerMaskUtils.Contains(c.s.Slide_Mask, hitCollider.gameObject.layer) && Vector3.Angle(hitNormal, c.motor.CharacterUp) < c.s.Slide_StableSlopeAngle)
            {
                _wasGrounded = true;
            }
        }
        public override void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            c.RefreshSlideCayoteTimer();
            var speed = c.motor.Velocity.magnitude;
            _afterGroundHitRequestVelocity = c.motor.GetDirectionTangentToSurface
                (
                    direction: c.motor.Velocity,
                    surfaceNormal: hitNormal
                ) * speed;
        }
        public override void Exit()
        {
            c._slideTimer = 0f;
            c.motor.MaxStableSlopeAngle = _tempMaxStableSlopeAngle;
            _tempHorizontalForce = Vector3.zero;
        }
    }
    public class AirbornState : PlayerState
    {
        private bool _isSprinted;
        public AirbornState(PlayerCharacterMover character) : base(character)
        {
            CameraHeight = character.s.StandCameraHeight;
        }
        public override void Enter()
        {
            c.SetStandDemensions();
            _isSprinted = c.IsSprinting;
        }
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            base.UpdateRotation(ref currentRotation, deltaTime);
        }
        public override void BeforeCharacterUpdate(float deltaTime)
        {
            if (c.IsGrounded)
                c.EnterToAnotherState();
            c._jumpCayoteTimer -= deltaTime;
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (c._requestMovement.sqrMagnitude > 0f)
            {
                var maxPlanarSpeed = Mathf.Max(
                Vector3.ProjectOnPlane(c.motor.BaseVelocity, c.motor.CharacterUp).magnitude,
                _isSprinted ?
                Mathf.Lerp(c.s.WalkSpeed, c.s.AirSpeed, Mathf.Clamp01(c._inputReader.MoveInput.y)) :
                c.s.WalkSpeed
                );
                // Calculations
                var planarMovement = Vector3.ProjectOnPlane(c._requestMovement, c.motor.CharacterUp) * c._requestMovement.magnitude;
                var currentPlanarVelocity = Vector3.ProjectOnPlane(currentVelocity, c.motor.CharacterUp);

                var movementForce = planarMovement * c.s.AirAcceleration * deltaTime;
                var targetPlanarVelocity = currentPlanarVelocity + movementForce;
                // Clamp vector
                targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, maxPlanarSpeed);
                // Add delta
                currentVelocity += targetPlanarVelocity - currentPlanarVelocity;
            }

            if (c._requestSustainJump)
            {
                currentVelocity += c.motor.CharacterUp * (c.s.Gravity * c.s.SustainJumpGravity * deltaTime);
                c._sustainJumpTimer -= deltaTime;
            }
            else
            {
                currentVelocity += c.motor.CharacterUp * (c.s.Gravity * deltaTime);
            }

        }
        public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            if (c.TryLedgeGrab())
            {
                c.EnterState<LedgeGrabState>();
                return;
            }
            c.TryWall(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
        }
        public override void Exit()
        {

        }

    }
    public class WallRunState : PlayerState
    {
        private RaycastSensor _wallRunSensor;
        private float _wallRunTimer;
        private float _overMaxAngleCayoteTimer;
        private Vector3 _wallForward;
        public void UpdateWallForward()
        {
            _wallForward = Vector3.Cross(c._wallNormal, c.motor.CharacterUp);
            // Выбираем направление (вперед или назад) в зависимости от ввода
            if (Vector3.Dot(c.motor.CharacterForward, _wallForward) < 0)
                _wallForward = -_wallForward;
        }
        public bool IsValidWallRunVelocity(Vector3 currentVelocity)
        {
            return Vector3.ProjectOnPlane(currentVelocity, c.motor.CharacterUp).magnitude > c.s.WallRun_MinSpeed &&
                Vector3.Dot(currentVelocity, _wallForward) > 0;
        }
        public WallRunState(PlayerCharacterMover character) : base(character)
        {
            CameraHeight = c.s.StandCameraHeight;
            _wallRunSensor = new RaycastSensor(c.transform)
                        .SetCastDirection(-c._wallNormal)
                        .SetLayerMask(c.s.WallRun_Layers)
                        .SetCastLength(c.s.WallCheckDistance + c.motor.Capsule.radius)
                        .SetOrigin(Vector3.up * (c.motor.Capsule.height * 0.5f))
                        .SetIncludeRotation(false);
        }
        public override void Enter()
        {
            c.SetStandDemensions();
            _wallRunSensor.SetCastDirection(-c._wallNormal);
            _wallRunTimer = c.s.WallRun_MaxTime;
            UpdateWallForward();
        }
        public override void BeforeCharacterUpdate(float deltaTime)
        {
            _wallRunTimer -= deltaTime;
            _wallRunSensor.Cast();
            Debug.Log("WallRunSensor: " + _wallRunSensor.HasDetectedHit);
            if (!_wallRunSensor.HasDetectedHit)
                c.EnterToAnotherState();
        }
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = Quaternion.LookRotation(_wallForward, Vector3.ProjectOnPlane(c.motor.CharacterUp, c._wallNormal).normalized);
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // Убираем скорость в направлении стены для правильных расчетов
            currentVelocity = Vector3.ProjectOnPlane(currentVelocity, c._wallNormal);

            // Если скорость слишком маленькая, то отваливаемся от стены
            if (c.s.WallRun_UngrabIfLessThanMinSpeed && IsValidWallRunVelocity(currentVelocity))
            {
                c.EnterToAnotherState();
            }

            // Находим скорость по вертикали и горизонтали относительно стены
            float verticalVelocity = Vector3.Dot(currentVelocity, c.motor.CharacterUp);
            float horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, c.motor.CharacterUp).magnitude;

            // Кайотный таймер угла
            if (Vector3.Angle(_wallForward, c.motor.CharacterForward) > c.s.WallRun_MaxAngle)
            {
                _overMaxAngleCayoteTimer -= deltaTime;
                if (_overMaxAngleCayoteTimer <= 0f)
                    c.EnterToAnotherState();
            }
            else if (_overMaxAngleCayoteTimer < c.s.WallRun_OverMaxAngleCayoteTime)
            {
                _overMaxAngleCayoteTimer = c.s.WallRun_OverMaxAngleCayoteTime;
            }

            horizontalVelocity -= c.s.WallRun_HorizontalDeceleration * deltaTime;
            horizontalVelocity = Mathf.Clamp(horizontalVelocity, c.s.WallRun_MinSpeed, c.s.WallRun_MaxHorizontalSpeed);

            // Устанавливаем скорость бега
            currentVelocity = _wallForward * horizontalVelocity;

            // Липкая сила: слегка прижимаем к стене, чтобы не "отвалиться" на углах
            currentVelocity += c._wallNormal * c.s.WallRun_GravityToWall;

            // Ограничиваем вертикальную скорость, чтобы не улетать слишком высоко или быстро падать
            verticalVelocity = Mathf.Clamp(verticalVelocity, -c.s.WallRun_MaxAbsVerticalSpeed, c.s.WallRun_MaxAbsVerticalSpeed);

            // Гравитация если таймер бега закончился, чтобы игрок не мог бегать вечно
            if (_wallRunTimer <= 0f)
            {
                verticalVelocity = c.s.WallRun_Gravity * deltaTime + verticalVelocity;
            }
            else
            {
                // Вертикальная анимация бега по стене: плавно поднимаем игрока вверх, чтобы было ощущение "забирания" по стене
                if (verticalVelocity > c.s.WallRun_FinalVerticalSpeed)
                    verticalVelocity += c.s.WallRun_VerticalDeceleration * deltaTime;
                else
                    verticalVelocity = Mathf.Lerp(verticalVelocity, c.s.WallRun_FinalVerticalSpeed, deltaTime * c.s.SpeedChangeResponse);
            }
            // добовляем вертикальную скорость к общей
            currentVelocity += c.motor.CharacterUp * verticalVelocity;

            // Выход из бега: если прыгнули или коснулись настоящей земли
            if (c._requestJump || c.motor.GroundingStatus.IsStableOnGround)
            {
                if (c._requestJump) // Прыжок от стены
                {
                    c.motor.SetPosition(c.motor.TransientPosition + c._wallNormal * 0.1f);
                    currentVelocity += (c._wallNormal * c.s.Wall_JumpForcesRatio.x +
                     c.motor.CharacterUp * c.s.Wall_JumpForcesRatio.y +
                     c._requestMovement.normalized * c.s.Wall_JumpForcesRatio.z).normalized * c.s.Wall_JumpSpeed;
                    c.RefreshSustainJump();
                    c.EnterToAnotherState();
                    c._requestJump = false;
                }
            }
        }
        public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            if (Mathf.Abs(Vector3.Angle(hitNormal, c.motor.CharacterUp) - 90f) < c.s.MaxWallAngleMagnitude &&
                ((1 << hitCollider.gameObject.layer) & c.s.WallRun_Layers) != 0)
            {
                c._wallNormal = hitNormal;
                _wallRunSensor.SetCastDirection(-c._wallNormal);
                UpdateWallForward();
            }
            else
            {
                bool isWalled = c.TryWall(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
                if (!isWalled)
                {
                    c.EnterToAnotherState();
                }
            }
        }
        public override void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
            //hitStabilityReport.IsStable = true;
            //hitStabilityReport.LedgeGroundNormal = hitNormal;
        }
        public override void Exit()
        {
            c._wallNormal = Vector3.zero;
        }
    }
    public class WallGrabState : PlayerState
    {
        private RaycastSensor _wallGrabSensor;
        public WallGrabState(PlayerCharacterMover c) : base(c)
        {
            CameraHeight = c.s.StandCameraHeight;
            _wallGrabSensor = new RaycastSensor(c.transform)
                    .SetCastDirection(-c._wallNormal)
                    .SetLayerMask(c.s.WallGrab_Layers)
                    .SetCastLength(c.s.WallCheckDistance + c.motor.Capsule.radius)
                    .SetOrigin(Vector3.up * (c.motor.Capsule.height * 0.5f))
                    .SetIncludeRotation(false);
        }

        public override void Enter()
        {
            c.SetStandDemensions();
            _wallGrabSensor.SetCastDirection(-c._wallNormal);
        }
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = Quaternion.LookRotation(-c._wallNormal, c.motor.CharacterUp);
        }
        public override void BeforeCharacterUpdate(float deltaTime)
        {
            _wallGrabSensor.Cast();
            if (c._wallGrabTimer < 0 || !_wallGrabSensor.HasDetectedHit)
                c.EnterToAnotherState();

            if (c.TryLedgeGrab())
                c.EnterState<LedgeGrabState>();
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // To right and up relative to wall normal
            Vector3 wallHorizontal = Vector3.ProjectOnPlane(c.motor.CharacterRight, c._wallNormal).normalized;
            Vector3 wallVertical = Vector3.ProjectOnPlane(c.motor.CharacterUp, c._wallNormal).normalized;

            c._wallGrabTimer -= deltaTime;

            // Remove velocity towards wall
            float velocityVertical = Vector3.Dot(currentVelocity, wallVertical);
            currentVelocity -= velocityVertical * wallVertical;

            // Neutrilize velocity
            if (!c._inputReader.IsJumpHold)
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, deltaTime * c.s.SpeedChangeResponse);

            if (velocityVertical < 0)
                velocityVertical = Mathf.Lerp(velocityVertical, 0, deltaTime * c.s.SpeedChangeResponse);

            currentVelocity += -c._wallNormal;
            currentVelocity += velocityVertical * wallVertical;

            if (c._requestJump)
                WallGrabJump(ref currentVelocity, deltaTime);
        }
        protected void WallGrabJump(ref Vector3 currentVelocity, float deltaTime)
        {
            float angleToWall = Vector3.Angle(c._requestMovement, -Vector3.ProjectOnPlane(c._wallNormal, c.motor.CharacterUp));
            if (angleToWall > 90f)
            {
                currentVelocity = Vector3.zero;
                currentVelocity += (c._wallNormal * c.s.Wall_JumpForcesRatio.x +
                    c.motor.CharacterUp * c.s.Wall_JumpForcesRatio.y +
                    c._requestMovement.normalized * c.s.Wall_JumpForcesRatio.z).normalized * c.s.Wall_JumpSpeed;
                c.RefreshSustainJump();
                c.EnterToAnotherState();
            }
            else
            {
                //if angleToWall < 90f
                c.RefreshSustainJump();
                currentVelocity += Vector3.ProjectOnPlane(c.motor.CharacterUp, c._wallNormal).normalized * c.s.Wall_JumpSpeed;

            }
        }
        public override void Exit()
        {

        }
    }
    public class LedgeGrabState : PlayerState
    {
        private bool _mustLookAtWall;
        private Vector3 _finishPoint;
        private Vector3 _wallNormal;
        private MultiRS<LedgeGrabSensors> _sensors;
        private bool _climbUpRequested;
        private bool ClimbUpRequested => c._inputReader.MoveInput.y > 0.5f || c._inputReader.IsJumpHold || _climbUpRequested;
        private bool _inFinishPoint;
        public LedgeGrabState(PlayerCharacterMover c) : base(c)
        {
            CameraHeight = c.s.StandCameraHeight;
            // Register sensors
            _sensors = new MultiRS<LedgeGrabSensors>(c.transform);
            {
                _sensors
                    .AddSensor(LedgeGrabSensors.Forward,
                    new RaycastSensorSettings
                    {
                        Origin = (c.motor.Capsule.height - c.motor.Capsule.radius) * c.motor.CharacterUp,
                        CastLength = s.LedgeGrab_MaxWidth + c.motor.Capsule.radius,
                        LayerMask = s.LedgeGrab_Layers,
                        Direction = c.motor.CharacterForward
                    })
                .AddSensor(LedgeGrabSensors.Up,
                new RaycastSensorSettings
                {
                    Origin = c.motor.Capsule.height * c.motor.CharacterUp,
                    CastLength = s.LedgeGrab_MaxHeight + c.motor.Capsule.height,
                    LayerMask = s.LedgeGrab_Layers,
                    Direction = c.motor.CharacterUp
                })
                .AddSensor(LedgeGrabSensors.FromUpToforward,
                new RaycastSensorSettings
                {
                    Origin = (c.motor.Capsule.height + s.LedgeGrab_MaxHeight) * c.motor.CharacterUp,
                    CastLength = s.LedgeGrab_MaxWidth + c.motor.Capsule.radius,
                    LayerMask = s.LedgeGrab_Layers,
                    Direction = c.motor.CharacterForward
                })
                .AddSensor(LedgeGrabSensors.FromForwardUpToDown,
                new RaycastSensorSettings
                {
                    Origin = (c.motor.Capsule.height + s.LedgeGrab_MaxHeight) * c.motor.CharacterUp + (s.LedgeGrab_MaxWidth + c.motor.Capsule.radius) * c.motor.CharacterForward,
                    CastLength = c.motor.Capsule.height + s.LedgeGrab_MaxHeight,
                    LayerMask = s.LedgeGrab_Layers,
                    Direction = -c.motor.CharacterUp
                });
            }
        }
        public override void Enter()
        {
            c.SetStandDemensions();
            c._ledgeGrabTimer = c.s.LedgeGrab_MaxTime;
            _climbUpRequested = false;
            _inFinishPoint = false;
            _mustLookAtWall = false;
        }
        public bool CheckSensors()
        {
            Debug.Log("CheckSensors");
            _sensors.UpdateAllSensors();
            var forwardInfo = _sensors.GetSensorInfo(LedgeGrabSensors.Forward);
            var fromForwardUpToDownInfo = _sensors.GetSensorInfo(LedgeGrabSensors.FromForwardUpToDown);

            if (forwardInfo.Detected)
            {
                _wallNormal = forwardInfo.hitInfo.normal;
                _mustLookAtWall = Vector3.Angle(forwardInfo.hitInfo.normal, c.defaultUp) < 90;

                if (!_sensors.GetSensorInfo(LedgeGrabSensors.Up).Detected &&
                    !_sensors.GetSensorInfo(LedgeGrabSensors.FromUpToforward).Detected &&
                    fromForwardUpToDownInfo.Detected)
                {
                    _finishPoint = fromForwardUpToDownInfo.hitInfo.point;
                    return true;
                }
            }
            return false;
        }
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_mustLookAtWall)
            {
                currentRotation = Quaternion.LookRotation(-_wallNormal, Vector3.ProjectOnPlane(c.motor.CharacterUp, _wallNormal));
            }
            else
            {
                var forward = Vector3.ProjectOnPlane(-_wallNormal, c.defaultUp).normalized;
                currentRotation = Quaternion.LookRotation(forward, c.defaultUp);
            }
        }

        public override void BeforeCharacterUpdate(float deltaTime)
        {
            if (c._ledgeGrabTimer < 0 || c.IsGrounded)
                c.EnterToAnotherState();
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            c._ledgeGrabTimer -= deltaTime;
            _climbUpRequested = Vector3.Dot(currentVelocity, c.motor.CharacterUp) > s.LedgeGrab_ClimbUpSpeed ? true : _climbUpRequested;
            _climbUpRequested = ClimbUpRequested;
            // Neutrilize velocity
            currentVelocity = Vector3.zero;
            if (ClimbUpRequested && !_inFinishPoint)
            {
                currentVelocity += Vector3.ProjectOnPlane(c.motor.CharacterUp, _wallNormal).normalized * s.LedgeGrab_ClimbUpSpeed;
            }
            else if (_inFinishPoint)
            {
                currentVelocity += c.motor.CharacterForward * (c.IsSprinting ? s.RunSpeed : s.WalkSpeed);
                c.EnterState<StandState>();
            }

            if (Vector3.Dot(c.transform.position - _finishPoint, c.motor.CharacterUp) - 0.01f >= 0)
            {
                _inFinishPoint = true;
            }
        }
        public override void Exit()
        {
        }
        private enum LedgeGrabSensors : int
        {
            Forward,
            Up,
            FromUpToforward,
            FromForwardUpToDown,
        }
    }



    //public abstract class SubState 
    //{
    //    public bool IsActive { get; protected set; }
    //    protected PlayerCharacter c;
    //    public SubState(PlayerCharacter c)
    //    {
    //        this.c = c;
    //    }
    //    public abstract void Enter();
    //    public abstract void Exit();
    //    public virtual void Update() { }
    //    public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime) { }
    //    public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) { }
    //    public virtual void BeforeCharacterUpdate(float deltaTime) { }
    //    public virtual void AfterCharacterUpdate(float deltaTime) { }
    //    public virtual void PostGroundingUpdate(float deltaTime) { }
    //    public virtual bool IsColliderValidForCollisions(Collider coll)
    //    {
    //        return true;
    //    }
    //    public virtual void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    //    {
    //    }
    //    public virtual void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    //    {
    //    }
    //    public virtual void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    //    {
    //    }
    //    public virtual void OnDiscreteCollisionDetected(Collider hitCollider)
    //    {
    //    }
    //}

    //public class SustainJump : SubState
    //{
    //    public SustainJump(PlayerCharacter c) : base(c)
    //    {
    //    }
    //    public override void Enter() 
    //    {
    //        IsActive = true;
    //        c._sustainJumpTimer = c.sustainJumpDuration;
    //    }
    //    public override void BeforeCharacterUpdate(float deltaTime)
    //    {
    //        if(!IsActive) return;

    //        if  (c._isGrounded ||
    //            !(c.isSustainJumpEnabled &&
    //            !c._requestJump &&
    //            c._inputReader.IsJumpHold &&
    //            c._sustainJumpTimer > 0f)
    //            )
    //        {
    //            Exit();
    //        }
    //    }
    //    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    //    {
    //        if (!IsActive) return;
    //        // Remove delta of gravity for sustain
    //        currentVelocity -=  c.motor.CharacterUp * (c.gravity * deltaTime * (1f - c.sustainJumpGravity));
    //        c._sustainJumpTimer -= deltaTime;
    //    }
    //    public override void Exit()
    //    {
    //        if (!IsActive) return;
    //        IsActive = false;
    //    }
    //}

}