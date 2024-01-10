using MobX.Mediator.Settings;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MobX.Player
{
    public class FreeCameraSettings : SettingsAsset
    {
        [SerializeField] [Range(0f, 2f)] private float mouseSensitivity = 1f;
        [SerializeField] private float accelerationSharpness = 5f;
        [SerializeField] [Range(0f, 90f)] private float maxXAngle = 90f;

        [Header("Speed")]
        [SerializeField] private float startSpeed = 10f;
        [SerializeField] private float minSpeed = .1f;
        [SerializeField] private float maxSpeed = 100f;
        [SerializeField] [Range(0f, 1f)] private float breakSpeedMultiplier = .35f;
        [SerializeField] [Range(1f, 10f)] private float sprintSpeedMultiplier = 2f;
        [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 1, 1, 2);

        [Header("Input")]
        [SerializeField] [Required] private InputActionReference movementInput;
        [SerializeField] [Required] private InputActionReference lookInput;
        [SerializeField] [Required] private InputActionReference speedInput;
        [SerializeField] [Required] private InputActionReference breakInput;
        [SerializeField] [Required] private InputActionReference sprintInput;

        public float MouseSensitivity => mouseSensitivity;
        public float AccelerationSharpness => accelerationSharpness;
        public float MaxXAngle => maxXAngle;
        public float StartSpeed => startSpeed;
        public float MinSpeed => minSpeed;
        public float MaxSpeed => maxSpeed;
        public float BreakSpeedMultiplier => breakSpeedMultiplier;
        public float SprintSpeedMultiplier => sprintSpeedMultiplier;
        public AnimationCurve SpeedCurve => speedCurve;
        public InputActionReference MovementInput => movementInput;
        public InputActionReference LookInput => lookInput;
        public InputActionReference SpeedInput => speedInput;
        public InputActionReference BreakInput => breakInput;
        public InputActionReference SprintInput => sprintInput;
    }
}