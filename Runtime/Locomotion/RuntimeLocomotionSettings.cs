namespace MobX.Player.Locomotion
{
    internal class RuntimeLocomotionSettings : ILocomotionSettings
    {
        public MovementLocomotionSettings MovementSettings { get; set; }
        public GravityLocomotionSettings GravitySettings { get; set; }
        public ManeuverLocomotionSettings ManeuverSettings { get; set; }
        public CrouchLocomotionSettings CrouchSettings { get; set; }
        public StaminaLocomotionSettings StaminaSettings { get; set; }
        public BlinkLocomotionSettings BlinkSettings { get; set; }
        public SaveDataLocomotionSettings SaveDataSettings { get; set; }
    }
}