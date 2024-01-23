using MobX.Mediator;
using MobX.Mediator.Settings;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    public class SaveDataLocomotionSettings : SettingsAsset
    {
        [SerializeField] [Required] private BoolSaveAsset toggleCrouchDesktopSetting;
        [SerializeField] [Required] private BoolSaveAsset toggleCrouchGamepadSetting;

        public BoolSaveAsset ToggleCrouchDesktopSetting => toggleCrouchDesktopSetting;
        public BoolSaveAsset ToggleCrouchGamepadSetting => toggleCrouchGamepadSetting;
    }
}