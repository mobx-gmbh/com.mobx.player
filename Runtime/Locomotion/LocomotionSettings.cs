using MobX.Inspector;
using MobX.Mediator.Callbacks;
using MobX.Mediator.Settings;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    public class LocomotionSettings : SettingsAsset
    {
        [Foldout("Movement")]
        [Line(DrawTiming = DrawTiming.After)]
        [SerializeField] private MovementLocomotionSettings movementSettings;
        [Foldout("Movement")]
        [InlineFoldout]
        public MovementLocomotionSettings MovementSettings { get; private set; }

        [Foldout("Gravity")]
        [Line(DrawTiming = DrawTiming.After)]
        [SerializeField] private GravityLocomotionSettings gravitySettings;
        [Foldout("Gravity")]
        [InlineFoldout]
        public GravityLocomotionSettings GravitySettings { get; private set; }

        [Foldout("Maneuver")]
        [Line(DrawTiming = DrawTiming.After)]
        [SerializeField] private ManeuverLocomotionSettings maneuverSettings;
        [Foldout("Maneuver")]
        [InlineFoldout]
        public ManeuverLocomotionSettings ManeuverSettings { get; private set; }

        [Foldout("Crouch")]
        [Line(DrawTiming = DrawTiming.After)]
        [SerializeField] private CrouchLocomotionSettings crouchSettings;
        [Foldout("Crouch")]
        [InlineFoldout]
        public CrouchLocomotionSettings CrouchSettings { get; private set; }

        [Foldout("Stamina")]
        [Line(DrawTiming = DrawTiming.After)]
        [SerializeField] private StaminaLocomotionSettings staminaSettings;
        [Foldout("Stamina")]
        [InlineFoldout]
        public StaminaLocomotionSettings StaminaSettings { get; private set; }

        [Foldout("Blink")]
        [Line(DrawTiming = DrawTiming.After)]
        [SerializeField] private BlinkLocomotionSettings blinkSettings;
        [Foldout("Blink")]
        [InlineFoldout]
        public BlinkLocomotionSettings BlinkSettings { get; private set; }

        [Foldout("ThrustDown")]
        [Line(DrawTiming = DrawTiming.After)]
        [SerializeField] private ThrustDownLocomotionSettings thrustDownSettings;
        [Foldout("ThrustDown")]
        [InlineFoldout]
        public ThrustDownLocomotionSettings ThrustDownSettings { get; private set; }

        [Foldout("Input")]
        [Line(DrawTiming = DrawTiming.After)]
        [SerializeField] private InputLocomotionSettings inputSettings;
        [Foldout("Input")]
        [InlineFoldout]
        public InputLocomotionSettings InputSettings { get; private set; }

        [Foldout("SaveGame")]
        [Line(DrawTiming = DrawTiming.After)]
        [SerializeField] private SaveDataLocomotionSettings saveDataSettings;
        [Foldout("SaveGame")]
        [InlineFoldout]
        public SaveDataLocomotionSettings SaveDataSettings { get; private set; }


        #region Initialization & Shutdown

        [CallbackOnInitialization]
        private void Initialize()
        {
            SetDefaultSettings();
        }

        [CallbackOnApplicationQuit]
        private void Shutdown()
        {
            SetDefaultSettings();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            SetDefaultSettings();
        }

        private void SetDefaultSettings()
        {
            MovementSettings = movementSettings;
            GravitySettings = gravitySettings;
            CrouchSettings = crouchSettings;
            ManeuverSettings = maneuverSettings;
            StaminaSettings = staminaSettings;
            BlinkSettings = blinkSettings;
            ThrustDownSettings = thrustDownSettings;
            InputSettings = inputSettings;
            SaveDataSettings = saveDataSettings;
        }

        #endregion


        #region Overrides

        public void OverrideMovementSettings(MovementLocomotionSettings settings)
        {
            MovementSettings = settings;
        }

        public void OverrideGravitySettings(GravityLocomotionSettings settings)
        {
            GravitySettings = settings;
        }

        public void OverrideManeuverSettings(ManeuverLocomotionSettings settings)
        {
            ManeuverSettings = settings;
        }

        public void OverrideCrouchSettings(CrouchLocomotionSettings settings)
        {
            CrouchSettings = settings;
        }

        public void OverrideBlinkSettings(BlinkLocomotionSettings settings)
        {
            BlinkSettings = settings;
        }

        public void OverrideStaminaSettings(StaminaLocomotionSettings settings)
        {
            StaminaSettings = settings;
        }

        public void OverrideThrustDownSettings(ThrustDownLocomotionSettings settings)
        {
            ThrustDownSettings = settings;
        }

        public void OverrideInputSettings(InputLocomotionSettings settings)
        {
            InputSettings = settings;
        }

        public void OverrideSaveGameSettings(SaveDataLocomotionSettings settings)
        {
            SaveDataSettings = settings;
        }

        #endregion
    }
}