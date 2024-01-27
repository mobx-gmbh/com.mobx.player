using MobX.Mediator.Settings;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MobX.Player.Locomotion
{
    public class InputLocomotionSettings : SettingsAsset
    {
        [SerializeField] [Required] private InputActionReference maneuverInput;
        [SerializeField] [Required] private InputActionReference sprintInput;
        [SerializeField] [Required] private InputActionReference blinkInput;
        [SerializeField] [Required] private InputActionReference crouchInput;
        [SerializeField] [Required] private InputActionReference aimInput;
        [SerializeField] [Required] private InputActionReference thrustDown;

        public InputActionReference ManeuverInput => maneuverInput;
        public InputActionReference SprintInput => sprintInput;
        public InputActionReference BlinkInput => blinkInput;
        public InputActionReference CrouchInput => crouchInput;
        public InputActionReference AimInput => aimInput;
        public InputActionReference ThrustDown => thrustDown;
    }
}