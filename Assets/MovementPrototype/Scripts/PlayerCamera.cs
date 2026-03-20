using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private InputSettings inputSettings;
    private float sensitivity => inputSettings.LookSensitivity;
    private Vector3 _eulerAngles;

    public void Initialize(Transform target)
    {
        transform.position = target.position;
        transform.eulerAngles = _eulerAngles = target.eulerAngles;
    }
    public void UpdateRotation(CameraInput input)
    {
        _eulerAngles += new Vector3(-input.LookInput.y, input.LookInput.x, 0f) * sensitivity;
        _eulerAngles.x = Mathf.Clamp(_eulerAngles.x, -89.99f, 89.99f);
        transform.eulerAngles = _eulerAngles;
    }

    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;
    }
}
public struct CameraInput
{
    public Vector2 LookInput;
}