using UnityEngine;
using Player.Movement;
using VContainer;

namespace Player
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private PlayerCharacterMover _playerCharacter;
        [SerializeField] private PlayerMovementStatus _playerMovementStatus;
        [SerializeField] private PlayerCamera _playerCamera;
        [SerializeField] private Camera _camera;
        [Inject] private InputSettings _inputSettings;
        private GameplayInputReader _inputReader;
        void Start()
        {
            _inputReader = new GameplayInputReader(_inputSettings);
            _inputReader.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _playerCharacter?.Initialize(_inputReader);
            _playerCamera?.Initialize(_playerCharacter.GetCameraTarget());
            _playerMovementStatus?.Initialize(_playerCharacter);

#if UNITY_EDITOR
            _inputReader.OnTeleportCalled += () =>
            {
                Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
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
                Rotation = _playerCamera.transform.rotation,
            };
            _playerCharacter?.UpdateInput(characterInput);
            _playerCharacter?.UpdateBody();
        }
        private void LateUpdate()
        {
            CameraInput cameraInput = new CameraInput { LookInput = _inputReader.LookInput };
            _playerCamera?.UpdateRotation(cameraInput);
            _playerCamera?.UpdatePosition(_playerCharacter.GetCameraTarget());
        }
        private void Teleport(Vector3 position)
        {
            _playerCharacter.SetPosition(position);
        }
    }
}
