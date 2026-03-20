using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField] private float jumpForce = 10f;
    private void OnTriggerEnter(Collider other)
    {
       if(other.TryGetComponent<PlayerCharacterMover>(out PlayerCharacterMover player))
       {
            Debug.Log("Player hit the jump pad!");
            player.AddVelocity(transform.up * jumpForce);
       }
       else if(other.TryGetComponent<Rigidbody>(out Rigidbody rb))
       {
            Debug.Log("Rigidbody hit the jump pad!");
            rb.AddForce(transform.up * jumpForce, ForceMode.VelocityChange);
       }
    }
}