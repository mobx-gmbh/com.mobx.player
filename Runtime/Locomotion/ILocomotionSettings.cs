namespace MobX.Player.Locomotion
{
    public interface ILocomotionSettings
    {
        MovementLocomotionSettings MovementSettings { get; }
        GravityLocomotionSettings GravitySettings { get; }
        ManeuverLocomotionSettings ManeuverSettings { get; }
        CrouchLocomotionSettings CrouchSettings { get; }
        StaminaLocomotionSettings StaminaSettings { get; }
        BlinkLocomotionSettings BlinkSettings { get; }
        SaveDataLocomotionSettings SaveDataSettings { get; }
    }
}