using UnityEngine;
using UnityEngine.InputSystem;
using System;
using VContainer;

// Реализуем интерфейс, который сгенерировала Unity (I + имя Map + Actions)
public class GameplayInputReader : PlayerControls.IGameplayActions
{
    private InputSettings inputSettings;
    public Vector2 MoveInput => _moveInput;
    private Vector2 _moveInput;
    public Vector2 LookInput => _lookInput;
    private Vector2 _lookInput;
    public bool IsJumpHold { get; private set; }
    public bool IsCrouching { get; private set; }
    private ButtonInputSwichType crouchingType => inputSettings.CrouchingType;
    public bool IsSprinting { get; private set; }
    private ButtonInputSwichType sprintingInputType => inputSettings.SprintingInputType;
    public event Action OnJumpPerformed;
    public event Action OnVaultingPerformed;
    public event Action OnTeleportCalled;
    private PlayerControls _controls;

    public GameplayInputReader(InputSettings settings)
    {
        _controls = new PlayerControls();
        _controls.Gameplay.SetCallbacks(this);
        inputSettings = settings;
    }

    public void Enable() => _controls.Enable();
    public void Disable() => _controls.Disable();
    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        _lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started) OnJumpPerformed?.Invoke();
        IsJumpHold = SwichButtonInput(IsJumpHold, in context, ButtonInputSwichType.Hold);
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        IsSprinting = SwichButtonInput(IsSprinting, in context, sprintingInputType);
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        IsCrouching = SwichButtonInput(IsCrouching, in context, crouchingType);
    }

    public void OnTeleport(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnTeleportCalled?.Invoke();
    }
    private bool SwichButtonInput(bool isActive, in InputAction.CallbackContext context, ButtonInputSwichType swichType )
    {
        if (context.started)
        {
            isActive = swichType switch
            {
                ButtonInputSwichType.Toggle => !isActive,
                ButtonInputSwichType.Hold => true,
                _ => isActive
            };
        }
        else if (context.canceled)
        {
            isActive = swichType switch
            {
                ButtonInputSwichType.Hold => false,
                _ => isActive,
            };
        }
        return isActive;
    }
}
public enum ButtonInputSwichType
{
    Toggle,
    Hold
}