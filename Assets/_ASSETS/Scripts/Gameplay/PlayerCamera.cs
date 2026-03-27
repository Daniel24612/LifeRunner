using UnityEngine;
using VContainer;
using PrimeTween;
using Player.Movement;

public class PlayerCamera : MonoBehaviour
{
    [Inject] private InputSettings _inputSettings;
    [SerializeField] private Transform _pitchPivot;
    [SerializeField] private Transform _FX_Pivot;
    [field: SerializeField] public Camera _camera;
    [SerializeField] private PlayerMovementStatus _status;

    [Header("DynamicFX Settings")]
    [Header("ScreenShake")]
    [SerializeField] private ShakeSettings _landShake; // Для приземления
    [SerializeField] private float _bobFrequency = 10f;
    [SerializeField] private float _bobAmplitude = 0.05f;
    [Header("Tilt Settings")]
    [SerializeField] private float _wallRunTiltAngle = 15f;
    [SerializeField] private float _tiltSpeed = 10f;
    [Header("FOV Settings")]
    [SerializeField] private float _minFOV = 80f;
    [SerializeField] private float _maxFOV = 100f;
    [SerializeField] private float _minSpeed = 5f;
    [SerializeField] private float _maxSpeed = 15f;
    [SerializeField] private float _fovLerpSpeed = 5f;

    private float _currentTiltZ;
    private float sensitivity => _inputSettings.LookSensitivity;

    private Sequence _shakeSequence;
    private float _tempVerticalRot;
    private float _bobTimer;

    public void Initialize(Transform target)
    {
        transform.position = target.position;
        transform.eulerAngles = target.eulerAngles;
    }
    public void UpdateRotation(CameraInput input)
    {
        transform.Rotate(transform.up, input.LookInput.x * sensitivity);
        _tempVerticalRot -= input.LookInput.y * sensitivity;
        _tempVerticalRot = Mathf.Clamp(_tempVerticalRot, -89.9f, 89.9f);
        _pitchPivot.localRotation = Quaternion.Euler(_tempVerticalRot, 0f, 0f);
    }
    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;
    }
    private void Update()
    {
        HandleHeadBob();
        HandleTilt();
        HandleFOV();
    }
    private void HandleHeadBob()
    {
        if (_status.CurrentState == MovementState.Stand && _status.CurrentSpeed > 0.1f)
        {
            // Рассчитываем множитель скорости (бег = сильнее качка)
            float speedModifier = _status.IsRunning ? 1.5f : 1f;
            _bobTimer += Time.deltaTime * _bobFrequency * speedModifier;

            // Математический боббинг (плавная восьмерка)
            //float xOffset = Mathf.Cos(_bobTimer / 2) * _bobAmplitude * speedModifier;
            float yOffset = Mathf.Sin(_bobTimer) * _bobAmplitude * speedModifier;

            _FX_Pivot.localPosition = new Vector3(/*xOffset*/ 0, yOffset, 0);
        }
        else
        {
            // Плавно возвращаем камеру в центр, когда стоим
            _bobTimer = 0;
            _FX_Pivot.localPosition = Vector3.Lerp(_FX_Pivot.localPosition, Vector3.zero, Time.deltaTime * 5f);
        }
    }
    public void PlayLandShake()
    {
        Tween.ShakeLocalPosition(_FX_Pivot, _landShake);
    }
    private void HandleTilt()
    {
        float targetTilt = 0f;

        if (_status.CurrentState == MovementState.WallRun)
        {
            // Определяем сторону стены
            float dot = Vector3.Dot(transform.right, _status.WallNormal);
            targetTilt = dot > 0 ? -_wallRunTiltAngle : _wallRunTiltAngle;
        }
        else if (_status.CurrentState == MovementState.Slide)
        {
            // Можно добавить легкий случайный наклон при слайде для динамики
            targetTilt = 2f;
        }
        // Плавный переход к целевому наклону
        _currentTiltZ = Mathf.Lerp(_currentTiltZ, targetTilt, Time.deltaTime * _tiltSpeed);
        _FX_Pivot.localRotation = Quaternion.Euler(0, 0, _currentTiltZ);
    }
    private void HandleFOV()
    {
        // 1. Получаем текущую скорость
        float currentSpeed = _status.CurrentSpeed;

        // 2. Превращаем скорость в значение от 0 до 1
        // Если скорость < _minSpeed, вернет 0. Если > _maxSpeed, вернет 1.
        float speedPercent = Mathf.InverseLerp(_minSpeed, _maxSpeed, currentSpeed);

        // 3. Вычисляем целевой FOV
        float targetFOV = Mathf.Lerp(_minFOV, _maxFOV, speedPercent);

        // 4. Плавно применяем (чтобы не было резких скачков на кочках)
        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFOV, Time.deltaTime * _fovLerpSpeed);
    }
}
public struct CameraInput
{
    public Vector2 LookInput;
}