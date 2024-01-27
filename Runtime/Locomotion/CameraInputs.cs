using UnityEngine;

namespace MobX.Player.Locomotion
{
    public readonly struct CameraInputs
    {
        public readonly Transform CameraTransform;
        public readonly Quaternion ForwardRotation;
        public readonly CameraMode CameraMode;
        public readonly Vector3 MovementInput;

        public CameraInputs(
            Transform cameraTransform,
            Vector3 movementInput,
            Quaternion forwardRotation,
            CameraMode cameraMode)
        {
            CameraTransform = cameraTransform;
            MovementInput = movementInput;
            ForwardRotation = forwardRotation;
            CameraMode = cameraMode;
        }
    }
}