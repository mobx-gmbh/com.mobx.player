using MobX.Mediator.Settings;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MobX.Player
{
    public class FirstPersonSettings : SettingsAsset
    {
        [Header("Settings")]
        [SerializeField] private float mouseSensitivity = 1;
        [SerializeField] private float maxVerticalAngle = 90f;
        [SerializeField] private float minVerticalAngle = -90f;

        [Header("Input")]
        [SerializeField] [Required] private InputActionReference movementInput;
        [SerializeField] [Required] private InputActionReference lookInput;
        [SerializeField] [Required] private InputActionReference jumpInput;
        [SerializeField] [Required] private InputActionReference sprintInput;
        [SerializeField] [Required] private InputActionReference crouchInput;
        [SerializeField] [Required] private InputActionReference aimInput;

        public float MouseSensitivity => mouseSensitivity;
        public float MinVerticalAngle => minVerticalAngle;
        public float MaxVerticalAngle => maxVerticalAngle;

        public InputActionReference MovementInput => movementInput;
        public InputActionReference LookInput => lookInput;
        public InputActionReference JumpInput => jumpInput;
        public InputActionReference SprintInput => sprintInput;
        public InputActionReference CrouchInput => crouchInput;
        public InputActionReference AimInput => aimInput;
    }
}