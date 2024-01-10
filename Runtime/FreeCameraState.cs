using MobX.Inspector;
using MobX.Mediator;
using MobX.Utilities;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace MobX.Player
{
    public class FreeCameraState : CameraState
    {
        [Foldout("Mediator")]
        [SerializeField] [Required] private FreeCameraController cameraControllerPrefab;
        [SerializeField] [Required] private QuaternionValueAsset inheritedRotation;
        [SerializeField] [Required] private PlayerCharacterValueAsset selectedCharacter;

        [NonSerialized] private FreeCameraController _cameraController;

        protected override void OnCameraStateEnter(CameraState previousState)
        {
            if (_cameraController == null)
            {
                _cameraController = Instantiate(cameraControllerPrefab);
                _cameraController.DontDestroyOnLoad();
            }

            _cameraController.Activate();

            var isCharacterNull = selectedCharacter.Value.IsNull();
            if (isCharacterNull)
            {
                return;
            }

            if (previousState is FirstPersonCameraState)
            {
                var position = selectedCharacter
                    .Value
                    .FreeCameraDefaultPosition;
                _cameraController.SetPositionAndRotation(position);

                return;
            }

            if (previousState is ThirdPersonCameraState)
            {
                var position = selectedCharacter
                    .Value
                    .ThirdPersonCameraController
                    .VirtualCamera
                    .transform;
                _cameraController.SetPositionAndRotation(position);
                return;
            }

            if (previousState is TopdownCameraState)
            {
                var position = selectedCharacter
                    .Value
                    .TopdownCameraController
                    .VirtualCamera
                    .transform;
                _cameraController.SetPositionAndRotation(position);
                _cameraController.transform.rotation = inheritedRotation.Value;
            }
        }

        protected override void OnCameraStateExit(CameraState nextState)
        {
            _cameraController.Deactivate();
        }
    }
}