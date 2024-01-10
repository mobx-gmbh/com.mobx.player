using UnityEngine;
using UnityEngine.InputSystem;

namespace MobX.Player.Locomotion
{
    public readonly struct LocomotionInputs
    {
        public readonly Quaternion ForwardRotation;
        public readonly CameraMode CameraMode;
        public readonly Vector3 MovementInput;
        public readonly InputAction AimInput;
        public readonly InputAction JumpInput;
        public readonly InputAction SprintInput;
        public readonly InputAction CrouchInput;

        public LocomotionInputs(
            Vector3 movementInput,
            Quaternion forwardRotation,
            CameraMode cameraMode,
            InputAction aimInput,
            InputAction jumpInput,
            InputAction sprintInput,
            InputAction crouchInput)
        {
            MovementInput = movementInput;
            ForwardRotation = forwardRotation;
            CameraMode = cameraMode;
            AimInput = aimInput;
            JumpInput = jumpInput;
            SprintInput = sprintInput;
            CrouchInput = crouchInput;
        }
    }
}