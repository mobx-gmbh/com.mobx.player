namespace MobX.Player
{
    public class TopdownCameraState : CameraState
    {
        protected override void OnCameraStateEnter(CameraState previousState)
        {
            PlayerCharacter.TopdownCameraController.Activate();
        }

        protected override void OnCameraStateExit(CameraState nextState)
        {
            PlayerCharacter.TopdownCameraController.Deactivate();
        }

        protected override void OnCameraStateEnabled()
        {
            PlayerCharacter.TopdownCameraController.Activate();
        }

        protected override void OnCameraStateDisabled()
        {
            PlayerCharacter.TopdownCameraController.Deactivate();
        }
    }
}