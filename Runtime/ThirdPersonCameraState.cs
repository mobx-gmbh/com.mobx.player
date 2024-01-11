namespace MobX.Player
{
    public class ThirdPersonCameraState : CameraState
    {
        protected override void OnCameraStateEnter(CameraState previousState)
        {
            PlayerCharacter.ThirdPersonCameraController.Activate();

            if (previousState is FirstPersonCameraState)
            {
                PlayerCharacter.ThirdPersonCameraController.ResetCameraAngles();
            }
        }

        protected override void OnCameraStateExit(CameraState nextState)
        {
            PlayerCharacter.ThirdPersonCameraController.Deactivate();
        }

        protected override void OnCameraStateEnabled()
        {
            PlayerCharacter.ThirdPersonCameraController.Activate();
        }

        protected override void OnCameraStateDisabled()
        {
            PlayerCharacter.ThirdPersonCameraController.Deactivate();
        }
    }
}