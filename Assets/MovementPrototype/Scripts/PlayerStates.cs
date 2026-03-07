using KinematicCharacterController;
using UnityEngine;

public partial class PlayerCharacter
{
    public abstract class PlayerState
    {
        protected PlayerCharacter c;
        public float CameraHeight { get; protected set; }
        public PlayerState(PlayerCharacter character)
        {
            c = character;
        }
        public abstract void Enter();
        public abstract void Exit();
        public virtual void Update() { }
        public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            var forward = Vector3.ProjectOnPlane(c._requestRotation * Vector3.forward, c.motor.CharacterUp);

            if (forward != Vector3.zero)
                currentRotation = Quaternion.LookRotation(forward, c.motor.CharacterUp);
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
        public StandState(PlayerCharacter character) : base(character)
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
            if(!c._isGrounded || c._requestCrouch)
            c.EnterToAnotherState();
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            var groundedMovement = c.motor
               .GetDirectionTangentToSurface(c._requestMovement, c.motor.GroundingStatus.GroundNormal)
               * c._requestMovement.magnitude;

            var targetVelocity = groundedMovement * (c._isSprinting ? 
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
        public CrouchState(PlayerCharacter character) : base(character)
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
            if(c._canUncrouch && (!c._isCrouching || !c._isGrounded))
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
            if(!c._isGrounded)
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
        private float _enterSpeed;
        public SlideState(PlayerCharacter character) : base(character)
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
            c.motor.MaxStableSlopeAngle = 70f;
        }
        public override void BeforeCharacterUpdate(float deltaTime)
        {
            if (!c._isGrounded && c._slideCayoteTimer > 0f)
            {
                c._slideCayoteTimer -= deltaTime;
            }
            else
            {
                c._slideCayoteTimer = c.motor.GroundingStatus.IsStableOnGround ? c.s.Slide_CayoteTime : 0f;
            }
            if(!c._canSlide)
                c.EnterToAnotherState();
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            c._slideTimer -= deltaTime;
            c._canUncrouch = (c._slideTimer <= c.s.Slide_MaxTime - c.s.Slide_MinTime);

            // Add as speed
            if (c._isGrounded)
            {
            var slideSpeed = Mathf.Max(((c._slideTimer + deltaTime == c.s.Slide_MaxTime) ? c.s.Slide_StartSpeed : 0), currentVelocity.magnitude);
            currentVelocity = c.motor.GetDirectionTangentToSurface
                (
                    direction: currentVelocity,
                    surfaceNormal: c.motor.GroundingStatus.GroundNormal
                ) * slideSpeed;

            if(_afterGroundHitRequestVelocity != Vector3.zero)
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

                var verticalForce = (moveInput.y == -1 ? -crossVerticalDir : Vector3.zero)
                    * c.s.Slide_ControlForce;
                var horizontalForce = crossHorizontalDir * moveInput.x * c.s.Slide_ControlForce;
                currentVelocity += horizontalForce + verticalForce;
                _tempHorizontalForce = horizontalForce;
            }

        }
        public override void AfterCharacterUpdate(float deltaTime)
        {
            _afterGroundHitRequestVelocity = Vector3.zero;
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
        private float _maxPlanarSpeed = 0f;
        public AirbornState(PlayerCharacter character) : base(character)
        {
            CameraHeight = character.s.StandCameraHeight;
        }
        public override void Enter()
        {
            c.SetStandDemensions();
            _maxPlanarSpeed = Mathf.Max(
                Vector3.ProjectOnPlane(c.motor.BaseVelocity, c.motor.CharacterUp).magnitude,
                c._isSprinting? 
                Mathf.Lerp(c.s.WalkSpeed, c.s.AirSpeed, Mathf.Clamp01(c._inputReader.MoveInput.y)) : 
                c.s.WalkSpeed
                );
        }
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            base.UpdateRotation(ref currentRotation, deltaTime);
        }
        public override void BeforeCharacterUpdate(float deltaTime)
        {
            if (c._isGrounded)
                c.EnterToAnotherState();
            c._jumpCayoteTimer -= deltaTime;
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (c._requestMovement.sqrMagnitude > 0f)
            {
                // Calculations
                var planarMovement = Vector3.ProjectOnPlane(c._requestMovement, c.motor.CharacterUp) * c._requestMovement.magnitude;
                var currentPlanarVelocity = Vector3.ProjectOnPlane(currentVelocity, c.motor.CharacterUp);

                var movementForce = planarMovement * c.s.AirAcceleration * deltaTime;
                var targetPlanarVelocity = currentPlanarVelocity + movementForce;
                // Clamp vector
                targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, _maxPlanarSpeed);
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
        public WallRunState(PlayerCharacter character) : base(character)
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
        }
        public override void BeforeCharacterUpdate(float deltaTime)
        {
            _wallRunTimer -= deltaTime;
            _wallRunSensor.Cast();
            Debug.Log("WallRunSensor: " + _wallRunSensor.HasDetectedHit);
            if (!_wallRunSensor.HasDetectedHit)
                c.EnterToAnotherState();
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            float verticalVelocity = Vector3.Dot(currentVelocity, c.motor.CharacterUp);


            // 1. Находим направление вдоль стены (перпендикуляр к нормали и "верху")
            Vector3 wallForward = Vector3.Cross(c._wallNormal, c.motor.CharacterUp);

            // Выбираем направление (вперед или назад) в зависимости от ввода
            if (Vector3.Dot(c.motor.CharacterForward, wallForward) < 0)
                wallForward = -wallForward;

            if (Vector3.Angle(wallForward, c.motor.CharacterForward) > c.s.WallRun_MaxAngle)
            {
                c.EnterToAnotherState();
            }

            // 2. Устанавливаем скорость бега
            currentVelocity = wallForward * c.s.WallRun_Speed;

            // 3. Липкая сила: слегка прижимаем к стене, чтобы не "отвалиться" на углах
            currentVelocity += -c._wallNormal * 2f;

            // 4. 
            verticalVelocity = Mathf.Clamp(verticalVelocity, -c.s.WallRun_MaxAbsVerticalSpeed, c.s.WallRun_MaxAbsVerticalSpeed);

            // 5. Гравитация если таймер бега закончился, чтобы игрок не мог бегать вечно
            if (_wallRunTimer <= 0f)
            {
                verticalVelocity = c.s.WallRun_Gravity * deltaTime + verticalVelocity;
            }
            else
            {
                // Вертикальная анимация бега по стене: плавно поднимаем игрока вверх, чтобы было ощущение "забирания" по стене
                if(verticalVelocity > c.s.WallRun_FinalVerticalSpeed)
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
                     c.motor.CharacterForward * c.s.Wall_JumpForcesRatio.z).normalized * c.s.Wall_JumpSpeed;
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
        public WallGrabState(PlayerCharacter c) : base(c)
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
            base.UpdateRotation(ref currentRotation, deltaTime);
        }
        public override void BeforeCharacterUpdate(float deltaTime)
        {
            _wallGrabSensor.Cast();
            if (c._wallGrabTimer < 0 || !_wallGrabSensor.HasDetectedHit)
                c.EnterToAnotherState();
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            c._wallGrabTimer -= deltaTime;
            // Neutrilize velocity
            if(!c._inputReader.IsJumpHold)
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, deltaTime * c.s.SpeedChangeResponse);
            currentVelocity += -c._wallNormal;
            if (c._requestJump)
            {
                if (Vector3.Angle(c.motor.CharacterForward, Vector3.ProjectOnPlane(c._wallNormal, c.motor.CharacterUp)) < 90f)
                {
                currentVelocity = Vector3.zero;
                currentVelocity += (c._wallNormal * c.s.Wall_JumpForcesRatio.x +
                    c.motor.CharacterUp * c.s.Wall_JumpForcesRatio.y +
                    c.motor.CharacterForward * c.s.Wall_JumpForcesRatio.z).normalized * c.s.Wall_JumpSpeed;
                c.RefreshSustainJump();
                c.EnterToAnotherState();
                }
                else
                {
                    //if angle > 90f
                    c.RefreshSustainJump();
                    currentVelocity += Vector3.ProjectOnPlane(c.motor.CharacterUp, c._wallNormal).normalized * c.s.Wall_JumpSpeed;

                }
            }
        }
        public override void Exit()
        {
            
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