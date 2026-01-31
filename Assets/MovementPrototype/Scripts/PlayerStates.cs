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
        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Update() { }
        public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime) { }
        public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) { }
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
            CameraHeight = character.standCameraHeight;
        }

        public override void Enter()
        {
            c.SetStandDemensions();
        }
        public override void BeforeCharacterUpdate(float deltaTime)
        {
            c.EnterToAnotherStateIf(!c._isGrounded || c._requestCrouch);
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            var groundedMovement = c.motor
               .GetDirectionTangentToSurface(c._requestMovement, c.motor.GroundingStatus.GroundNormal)
               * c._requestMovement.magnitude;

            var targetVelocity = groundedMovement * (c._isSprinting ? c.runSpeed : c.walkSpeed);

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                targetVelocity,
                1f - Mathf.Exp(-c.speedChangeResponse * deltaTime)
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
            CameraHeight = character.standCameraHeight;
        }
        public override void Enter()
        {
            c.SetCrouchDemensions();
        }
        public override void BeforeCharacterUpdate(float deltaTime)
        {
            c.EnterToAnotherStateIf(!c._requestCrouch || !c._isGrounded);
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            var groundedMovement = c.motor
               .GetDirectionTangentToSurface(c._requestMovement, c.motor.GroundingStatus.GroundNormal)
               * c._requestMovement.magnitude;

            var targetVelocity = groundedMovement * c.crouchSpeed;

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                targetVelocity,
                1f - Mathf.Exp(-c.speedChangeResponse * deltaTime)
                );
        }
        public override void Exit()
        {
        }
    }
    public class SlideState : PlayerState
    {
        private float _tempMaxStableSlopeAngle;
        private Vector3 _afterGroundHitRequestVelocity;
        public SlideState(PlayerCharacter character) : base(character)
        {
            CameraHeight = c.crouchCameraHeight;
        }
        public override void Enter()
        {
            c.SetCrouchDemensions();
            c._slideTimer = c.maxSlideTime;
            c._canUncrouch = false;
            c._slideCayoteTimer = c.slideCayoteTime;
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
                c._slideCayoteTimer = c.motor.GroundingStatus.IsStableOnGround ? c.slideCayoteTime : 0f;
            }
            c.EnterToAnotherStateIf(!c._canSlide);
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // Add as speed
            if (c.motor.GroundingStatus.IsStableOnGround)
            {
            var slideSpeed = Mathf.Max(((c._slideTimer == c.maxSlideTime) ? c.startSlideSpeed : 0), currentVelocity.magnitude);
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
            currentVelocity -= currentVelocity * (c.slideFriction * deltaTime); 
            }
            // Add slide gravity
            currentVelocity += c.motor.CharacterUp * (c.slideGravity * deltaTime);
            c._slideTimer -= deltaTime;
            c._canUncrouch = (c._slideTimer <= c.maxSlideTime - c.minSlideTime);
        }
        public override void AfterCharacterUpdate(float deltaTime)
        {
            _afterGroundHitRequestVelocity = Vector3.zero;
        }
        public override void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            c._slideCayoteTimer = c.slideCayoteTime;
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
        }
    }
    public class AirbornState : PlayerState
    {
        public AirbornState(PlayerCharacter character) : base(character)
        {
            CameraHeight = character.standCameraHeight;
        }
        public override void Enter()
        {
            c.SetStandDemensions();
        }
        public override void BeforeCharacterUpdate(float deltaTime)
        {
            c.EnterToAnotherStateIf(c._isGrounded);
        }
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (c._requestMovement.sqrMagnitude > 0f)
            {
                // Calculations
                var planarMovement = Vector3.ProjectOnPlane(c._requestMovement, c.motor.CharacterUp) * c._requestMovement.magnitude;
                var currentPlanarVelocity = Vector3.ProjectOnPlane(currentVelocity, c.motor.CharacterUp);

                var movementForce = planarMovement * c.airAcceleration * deltaTime;
                var targetPlanarVelocity = currentPlanarVelocity + movementForce;
                // Clamp vector
                targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, (c._inputReader.IsSprinting ? c.airSpeed : c.walkSpeed));
                // Add delta
                currentVelocity += targetPlanarVelocity - currentPlanarVelocity;
            }

            if (c._requestSustainJump)
            {
                currentVelocity += c.motor.CharacterUp * (c.gravity * deltaTime * c.sustainJumpGravity);
                c._sustainJumpTimer -= deltaTime;
            }
            else
            {
                currentVelocity += c.motor.CharacterUp * c.gravity * deltaTime;
            }
        }
        public override void Exit()
        {
            
        }

    }
}