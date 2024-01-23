using UnityEngine;
using UnityEngine.InputSystem;

namespace MobX.Player.Locomotion
{
    public readonly struct LocomotionInputs
    {
        public readonly Transform CameraTransform;
        public readonly Quaternion ForwardRotation;
        public readonly CameraMode CameraMode;
        public readonly Vector3 MovementInput;
        public readonly InputAction AimInput;
        public readonly InputAction ManeuverInput;
        public readonly InputAction SprintInput;
        public readonly InputAction CrouchInput;
        public readonly InputAction BlinkInput;

        public LocomotionInputs(
            Transform cameraTransform,
            Vector3 movementInput,
            Quaternion forwardRotation,
            CameraMode cameraMode,
            InputAction aimInput,
            InputAction maneuverInput,
            InputAction sprintInput,
            InputAction crouchInput,
            InputAction blinkInput)
        {
            CameraTransform = cameraTransform;
            MovementInput = movementInput;
            ForwardRotation = forwardRotation;
            CameraMode = cameraMode;
            AimInput = aimInput;
            ManeuverInput = maneuverInput;
            SprintInput = sprintInput;
            CrouchInput = crouchInput;
            BlinkInput = blinkInput;
        }
    }
}