using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovementSettings", menuName = "Player/Movement Settings")]
public class PlayerMovementSettings : ScriptableObject
{
    [field: Header("Base Settings")]
    [field: SerializeField] public float Gravity { get; private set; } = -30f;
    [field: SerializeField] public float HeightChangeResponse { get; private set; } = 15f;
    [field: SerializeField] public float SpeedChangeResponse { get; private set; } = 20f;
    [field: SerializeField, Range(0f, 1f)] public float StandCameraHeight { get; private set; } = 0.9f;
    [field: SerializeField, Range(0f, 1f)] public float CrouchCameraHeight { get; private set; } = 0.6f;

    [field: Header("Stand")]
    [field: SerializeField] public float StandHeight { get; private set; } = 2f;
    [field: SerializeField] public float WalkSpeed { get; private set; } = 7f;
    [field: SerializeField] public float RunSpeed { get; private set; } = 15f;

    [field: Header("Jump")]
    [field: SerializeField] public float JumpSpeed { get; private set; } = 10f;
    [field: SerializeField] public float JumpCayoteTime { get; private set; } = 0.2f;
    [field: SerializeField] public bool IsSustainJumpEnabled { get; private set; } = true;
    [field: SerializeField, Range(0f, 1f)] public float SustainJumpGravity { get; private set; } = 0.3f;
    [field: SerializeField] public float SustainJumpDuration { get; private set; } = 0.5f;

    [field: Header("Crouch")]
    [field: SerializeField] public float Crouch_Height { get; private set; } = 1f;
    [field: SerializeField] public float Crouch_Speed { get; private set; } = 5f;

    [field: Header("Slide")]
    [field: SerializeField] public float Slide_Gravity { get; private set; } = -30f;
    [field: SerializeField] public float Slide_StartSpeed { get; private set; } = 20f;
    [field: SerializeField] public float Slide_MinSpeed { get; private set; } = 10f;
    [field: SerializeField] public float Slide_MinTime { get; private set; } = 1f;
    [field: SerializeField] public float Slide_MaxTime { get; private set; } = 3f;
    [field: SerializeField, Range(0, 89)] public float Slide_StableSlopeAngle { get; private set; } = 60f;
    [field: SerializeField] public float Slide_CayoteTime { get; private set; } = 0.3f;
    [field: SerializeField] public float Slide_Friction { get; private set; } = 0.8f;
    [field: SerializeField] public float Slide_ControlForce { get; private set; } = 5f;
    [field: SerializeField] public LayerMask Slide_Mask { get; private set; }

    [field: Header("Airborn")]
    [field: SerializeField] public float AirSpeed { get; private set; } = 12f;
    [field: SerializeField] public float AirAcceleration { get; private set; } = 30f;

    [field: Header("General Wall Settings")]
    [field: SerializeField] public float WallCheckDistance { get; private set; } = 1f;
    [field: SerializeField] public LayerMask WallRun_Layers { get; private set; }
    [field: SerializeField] public LayerMask WallGrab_Layers { get; private set; }
    [field: SerializeField, Range(0, 89)] public float WallRun_MaxAngle { get; private set; } = 45f;
    [field: SerializeField, Range(0, 20)] public float MaxWallAngleMagnitude { get; private set; } = 15f;
    [field: SerializeField] public float GravityToWall { get; private set; } = 10f;

    [field: Header("Wall Run")]
    [field: SerializeField]
    public RaycastSensorSettings WallRun_SensorSettings { get; private set; } = new RaycastSensorSettings()
    {
        Origin = new Vector3(0f, 1f, 0f),
        Direction = Vector3.right,
        CastLength = 1f,
    };
    [field: SerializeField, Min(0)] public float WallRun_MaxTime { get; private set; } = 4f;
    [field: SerializeField] public float WallRun_Gravity { get; private set; } = -10f;
    [field: SerializeField] public float WallRun_VerticalDeceleration { get; private set; } = -2f;
    [field: SerializeField, Min(0)] public float WallRun_HorizontalDeceleration { get; private set; } = 1f;
    [field: SerializeField, Min(0)] public float WallRun_MinSpeed { get; private set; } = 10f;
    [field: SerializeField, Min(0)] public float WallRun_MaxHorizontalSpeed { get; private set; } = 16f;
    [field: SerializeField, Min(0)] public float WallRun_MaxAbsVerticalSpeed { get; private set; } = 10f;
    [field: SerializeField] public float WallRun_FinalVerticalSpeed { get; private set; } = -1f;
    [field: SerializeField, Min(0)] public float WallRun_OverMaxAngleCayoteTime { get; private set; } = 1f;
    [field: SerializeField] public float WallRun_GravityToWall { get; private set; } = -2f;
    [field: SerializeField] public bool WallRun_UngrabIfLessThanMinSpeed { get; private set; }

    [field: Header("Wall Grab")]
    [field: SerializeField] public float WallGrab_MaxTime { get; private set; } = 1f;
    [field: SerializeField] public float WallGrab_MinSpeedToRefreshTimer { get; private set; } = 6f;

    [field: Header("Wall Jump")]
    [field: SerializeField] public float Wall_JumpSpeed { get; private set; } = 10f;
    [field: SerializeField, Tooltip("x = Wall normal ratio; y = CharUp ratio; z = CharForward ratio")]
    public Vector3 Wall_JumpForcesRatio { get; private set; } = new(1, 1, 1);

    [Header("Ledge Grab")]
    [field: SerializeField] public float LedgeGrab_MaxTime { get; private set; } = 3f;
    [field: SerializeField] public float LedgeGrab_ClimbUpSpeed { get; private set; } = 7f;
    [field: SerializeField] public float LedgeGrab_MaxHeight { get; private set; } = 1f;
    [field: SerializeField] public float LedgeGrab_MaxWidth { get; private set; } = 0.4f;
    [field: SerializeField] public float LedgeGrab_MaxAngle { get; private set; } = 45f;
    [field: SerializeField] public LayerMask LedgeGrab_Layers { get; private set; }
}