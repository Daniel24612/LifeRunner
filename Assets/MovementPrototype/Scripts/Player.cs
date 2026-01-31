using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private InputReader inputReader;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerCharacter?.Initialize(inputReader);
        playerCamera?.Initialize(playerCharacter.GetCameraTarget());
    }

    void Update()
    {
        CameraInput cameraInput = new CameraInput { LookInput = inputReader.LookInput };
        playerCamera?.UpdateRotation(cameraInput);
        CharacterInput characterInput = new CharacterInput
        {
            Rotation = playerCamera.transform.rotation,
        };
        playerCharacter?.UpdateInput(characterInput);
        playerCharacter?.UpdateBody();
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.T))
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if(Physics.Raycast(ray, out var hit))
            {
                Teleport(hit.point);
            }
        } 
#endif
    }
    private void LateUpdate()
    {
        playerCamera?.UpdatePosition(playerCharacter.GetCameraTarget());
    }
    private void Teleport(Vector3 position)
    {
        playerCharacter.SetPosition(position);
    }
}
