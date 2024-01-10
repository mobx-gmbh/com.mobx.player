using MobX.Inspector;
using MobX.UI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MobX.Player
{
    public class FirstPersonCameraState : CameraState
    {
        [Foldout("Mediator")]
        [SerializeField] [Required] private UIAsset firstPersonHUD;
        [SerializeField] [Required] private PlayerCharacterValueAsset character;

        protected override void OnCameraStateEnter(CameraState previousState)
        {
            character.Value.FirstPersonCameraController.Activate();
            if (previousState is ThirdPersonCameraState or TopdownCameraState)
            {
                character.Value.FirstPersonCameraController.ResetCameraAngles();
            }

            firstPersonHUD.Open();
        }

        protected override void OnCameraStateExit(CameraState nextState)
        {
            character.Value.FirstPersonCameraController.Deactivate();
            firstPersonHUD.Close();
        }
    }
}