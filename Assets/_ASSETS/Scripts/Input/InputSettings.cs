using UnityEngine;
[CreateAssetMenu(fileName = "InputSettings", menuName = "Player/InputSettings")]
public class InputSettings : ScriptableObject
{
    [field: SerializeField] public ButtonInputSwichType CrouchingType { get; private set; } = ButtonInputSwichType.Hold;
    [field: SerializeField] public ButtonInputSwichType SprintingInputType { get; private set; } = ButtonInputSwichType.Hold;
    [field: SerializeField] public float LookSensitivity { get; private set; } = 1f;
}