using MobX.Inspector;
using MobX.Player.Locomotion;
using MobX.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MobX.Player
{
    public class ThirdPersonCameraController : CameraController
    {
        [FormerlySerializedAs("locomotionSystem")]
        [Foldout("References")]
        [SerializeField] [Required] private LocomotionController locomotionController;
        [SerializeField] [Required] private Transform shoulderTransform;
        [Foldout("Settings")]
        [SerializeField] [Required] private ThirdPersonSettings settings;

        private Quaternion _targetVerticalRotation;
        private Quaternion _targetHorizontalRotation;
        private Quaternion _currentVerticalRotation;
        private Quaternion _currentHorizontalRotation;
        private float _currentVerticalAngle;
        private float _currentHorizontalAngle;

        private void Awake()
        {
            _targetVerticalRotation = shoulderTransform.rotation;
            _targetHorizontalRotation = transform.rotation;
            _currentVerticalRotation = _targetVerticalRotation;
        }

        private void Start()
        {
            transform.SetParent(Character.ExternalCameraFolder);
        }

        protected override void OnCameraDisabled()
        {
        }

        protected override void OnCameraEnabled()
        {
        }

        protected override void OnCameraUpdate()
        {
        }

        protected override void OnCameraLateUpdate()
        {
            var direction = settings.MovementInput.action.ReadValue<Vector2>();
            var movementVector = new Vector3(direction.x, 0, direction.y);
            var rotation = settings.LookInput.action.ReadValue<Vector2>();

            rotation *= Controls.IsGamepadScheme
                ? settings.LookSensitivityGamepad.Value
                : settings.LookSensitivityDesktop.Value;

            var rotationAxisHorizontal = rotation.x;
            var rotationAxisVertical = rotation.y;
            var axisHorizontal = rotationAxisHorizontal * settings.LookSensitivity;
            var axisVertical = rotationAxisVertical * settings.LookSensitivity;

            _currentHorizontalAngle += axisHorizontal;
            _targetHorizontalRotation = Quaternion.Euler(0, _currentHorizontalAngle, 0);

            _currentVerticalAngle += axisVertical;
            _currentVerticalAngle = Mathf.Clamp(
                _currentVerticalAngle,
                settings.MinVerticalAngle,
                settings.MaxVerticalAngle);
            _targetVerticalRotation = Quaternion.Euler(_currentVerticalAngle, 0, 0);

            var sharpness = Time.unscaledDeltaTime * settings.LookSharpness;

            shoulderTransform.localRotation = Quaternion.Lerp(
                _currentVerticalRotation,
                _targetVerticalRotation,
                sharpness);

            var self = transform;
            self.localRotation = Quaternion.Lerp(
                _currentHorizontalRotation,
                _targetHorizontalRotation,
                sharpness);

            _currentVerticalRotation = shoulderTransform.localRotation;
            _currentHorizontalRotation = self.localRotation;

            self.position = Character.transform.position;

            var inputs = new CameraInputs(
                VirtualCamera.transform,
                movementVector,
                self.rotation,
                CameraMode.ThirdPerson);

            locomotionController.SetCameraInputs(inputs);
        }

        public void ResetCameraAngles()
        {
            _currentVerticalAngle = 20;
            _currentHorizontalAngle = Character.transform.rotation.eulerAngles.y;
        }
    }
}