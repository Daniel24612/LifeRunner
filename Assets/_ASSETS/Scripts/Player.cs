using UnityEngine;
using Player.Movement;

namespace Player
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private PlayerCharacterMover playerCharacter;
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private InputSettings inputSettings;
        private InputReader inputReader;
        void Start()
        {
            inputReader = new InputReader(inputSettings);
            inputReader.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            playerCharacter?.Initialize(inputReader);
            playerCamera?.Initialize(playerCharacter.GetCameraTarget());


#if UNITY_EDITOR
            inputReader.OnTeleportCalled += () =>
            {
                Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                if (Physics.Raycast(ray, out var hit))
                {
                    Teleport(hit.point);
                }
            };
#endif
        }

        void Update()
        {
            CharacterInput characterInput = new CharacterInput
            {
                Rotation = playerCamera.transform.rotation,
            };
            playerCharacter?.UpdateInput(characterInput);
            playerCharacter?.UpdateBody();
        }
        private void LateUpdate()
        {
            CameraInput cameraInput = new CameraInput { LookInput = inputReader.LookInput };
            playerCamera?.UpdateRotation(cameraInput);
            playerCamera?.UpdatePosition(playerCharacter.GetCameraTarget());
        }
        private void Teleport(Vector3 position)
        {
            playerCharacter.SetPosition(position);
        }
    }
}
