using System;
using UnityEngine;

namespace Player.Movement
{
    public class PlayerMovementStatus : MonoBehaviour
    {
        private PlayerCharacterMover _characterMover;
        private PlayerMovementSettings _movementSettings;
        public MovementState CurrentState { get; private set; }
        public bool IsRunning => (CurrentState == MovementState.Stand || CurrentState == MovementState.WallRun)
            && CurrentSpeed >= _movementSettings.WalkSpeed;
        public bool IsWalking => (CurrentState == MovementState.Stand || CurrentState == MovementState.Crouch) 
            && CurrentSpeed <= _movementSettings.WalkSpeed && CurrentSpeed > 0;
        public float CurrentSpeed => _characterMover.motor.BaseVelocity.magnitude;
        public Vector3 WallNormal => _characterMover._wallNormal;
        public event Action PlayerJumped;
        

        public void Initialize(PlayerCharacterMover mover)
        {
            _characterMover = mover;
            _movementSettings = mover.s;
            CurrentState = MovementState.Stand;
            mover.CharacterJumped += OnPlayerJump;
            mover.StateChanged += OnMovementStateChanged;
            Debug.Log("Movement Status initialized");
        }
        private void Update()
        {
        }
        private void OnPlayerJump() => PlayerJumped?.Invoke();
        private void OnMovementStateChanged(Type state)
        {
            CurrentState = state switch
            {
                _ when state == typeof(StandState) => MovementState.Stand,
                _ when state == typeof(CrouchState) => MovementState.Crouch,
                _ when state == typeof(AirbornState) => MovementState.Airborn,
                _ when state == typeof(SlideState) => MovementState.Slide,
                _ when state == typeof(WallRunState) => MovementState.WallRun,
                _ when state == typeof(WallGrabState) => MovementState.WallGrab,
                _ when state == typeof(LedgeGrabState) => MovementState.LedgeGrab,
                _ => CurrentState
            };
        }
        private void OnPlayerLanded() 
        { 

        }
    }
    public enum MovementState
    {
        Stand,
        Crouch,
        Airborn,
        Slide,
        WallRun,
        WallGrab,
        LedgeGrab
    }
}