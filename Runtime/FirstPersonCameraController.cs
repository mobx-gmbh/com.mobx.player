using MobX.Player.Locomotion;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MobX.Player
{
    public class FirstPersonCameraController : CameraController
    {
        [Header("Reference")]
        [SerializeField] [Required] private FirstPersonSettings settings;
        [SerializeField] [Required] private LocomotionController locomotionController;

        [Header("Transforms")]
        [SerializeField] [Required] private Transform cameraTransform;
        [SerializeField] [Required] private Transform bodyTransform;

        private float _rotationX;


        #region Setup & Shutdown

        private void Start()
        {
            transform.SetParent(Character.ExternalCameraFolder);
        }

        #endregion


        #region Camera Callbacks

        protected override void OnCameraUpdate()
        {
        }

        protected override void OnCameraLateUpdate()
        {
            bodyTransform.position = Character.transform.position;

            var rotation = settings.LookInput.action.ReadValue<Vector2>();

            var rotationHorizontal = settings.MouseSensitivity * rotation.x;
            var rotationVertical = settings.MouseSensitivity * rotation.y;

            bodyTransform.Rotate(Vector3.up * rotationHorizontal, Space.Self);

            var rotationY = cameraTransform.localEulerAngles.y;

            _rotationX += rotationVertical;
            _rotationX = Mathf.Clamp(_rotationX, settings.MinVerticalAngle, settings.MaxVerticalAngle);

            cameraTransform.localEulerAngles = new Vector3(_rotationX, rotationY, 0);

            var direction = settings.MovementInput.action.ReadValue<Vector2>();
            var movementVector = new Vector3(direction.x, 0, direction.y);

            var inputs = new LocomotionInputs(
                movementVector,
                bodyTransform.rotation,
                CameraMode.FirstPerson,
                settings.AimInput.action,
                settings.JumpInput.action,
                settings.SprintInput.action,
                settings.CrouchInput.action
            );

            locomotionController.SetInputs(inputs);
        }

        protected override void OnCameraDisabled()
        {
        }

        protected override void OnCameraEnabled()
        {
        }

        #endregion


        public void ResetCameraAngles()
        {
            var characterTransform = Character.transform;
            bodyTransform.position = characterTransform.position;
            var characterRotation = characterTransform.rotation;
            _rotationX = 0;
            cameraTransform.localEulerAngles = new Vector3(0, 0, 0);
            bodyTransform.rotation = characterRotation;
        }
    }
}