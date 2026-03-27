using KinematicCharacterController;
using UnityEngine;

[RequireComponent(typeof(PhysicsMover), typeof(Rigidbody))]
public class PhysicsFollower : MonoBehaviour, IMoverController
{
    [SerializeField] private Transform followObj;
    [SerializeField] private bool followRotation = true;
    private void Awake()
    {
        var mover = GetComponent<PhysicsMover>();
        mover.MoverController = this;
        mover.Rigidbody = GetComponent<Rigidbody>();
        mover.MoveWithPhysics = false;
    }
    public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
    {
        goalPosition = followObj.position;
        goalRotation = followRotation ? followObj.rotation : Quaternion.identity;
    }
}