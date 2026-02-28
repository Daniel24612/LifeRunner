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
    [field: SerializeField] public float CrouchHeight { get; private set; } = 1f;
    [field: SerializeField] public float CrouchSpeed { get; private set; } = 5f;

    [field: Header("Slide")]
    [field: SerializeField] public float SlideGravity { get; private set; } = -30f;
    [field: SerializeField] public float StartSlideSpeed { get; private set; } = 20f;
    [field: SerializeField] public float MinSlideSpeed { get; private set; } = 10f;
    [field: SerializeField] public float MinSlideTime { get; private set; } = 1f;
    [field: SerializeField] public float MaxSlideTime { get; private set; } = 3f;
    [field: SerializeField] public float SlideCayoteTime { get; private set; } = 0.3f;
    [field: SerializeField] public float SlideFriction { get; private set; } = 0.8f;
    [field: SerializeField] public float SlideControlForce { get; private set; } = 5f;

    [field: Header("Airborn")]
    [field: SerializeField] public float AirSpeed { get; private set; } = 12f;
    [field: SerializeField] public float AirAcceleration { get; private set; } = 30f;

    [field: Header("General Wall Settings")]
    [field: SerializeField] public float WallCheckDistance { get; private set; } = 1f;
    [field: SerializeField] public LayerMask WallLayers { get; private set; }
    [field: SerializeField] public LayerMask GrabWallLayers { get; private set; }
    [field: SerializeField, Range(0, 89)] public float MaxWallRunAngle { get; private set; } = 45f;
    [field: SerializeField, Range(0, 20)] public float MaxWallAngleMagnitude { get; private set; } = 15f;
    [field: SerializeField] public float GravityToWall { get; private set; } = 10f;

    [field: Header("Wall Run")]
    [field: SerializeField]
    public RaycastSensorSettings WallRunSensorSettings { get; private set; } = new RaycastSensorSettings()
    {
        Origin = new Vector3(0f, 1f, 0f),
        Direction = Vector3.right,
        CastLength = 1f,
    };
    [field: SerializeField] public float MaxWallRunTime { get; private set; } = 4f;
    [field: SerializeField] public float WallRunGravity { get; private set; } = -10f;
    [field: SerializeField] public float WallRunSpeed { get; private set; } = 12f;

    [field: Header("Wall Grab")]
    [field: SerializeField] public float MaxWallGrabTime { get; private set; } = 1f;
    [field: SerializeField] public float MinSpeedToRefreshGrabTimer { get; private set; } = 6f;

    [field: Header("Wall Jump")]
    [field: SerializeField] public float WallJumpSpeed { get; private set; } = 10f;
    [field: SerializeField, Tooltip("x = Wall normal ratio; y = CharUp ratio; z = CharForward ratio")]
    public Vector3 WallJumpForcesRatio { get; private set; } = new(1, 1, 1);
}