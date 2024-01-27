using MobX.Mediator;
using MobX.Mediator.Settings;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MobX.Player
{
    public class ThirdPersonSettings : SettingsAsset
    {
        [Header("Settings")]
        [SerializeField] private float lookSharpness = 15f;
        [SerializeField] private float lookSensitivity = 1f;
        [SerializeField] private float minVerticalAngle = -60;
        [SerializeField] private float maxVerticalAngle = 60;

        [Header("Input")]
        [SerializeField] [Required] private InputActionReference movementInput;
        [SerializeField] [Required] private InputActionReference lookInput;

        [Header("Persistent")]
        [SerializeField] [Required] private FloatSaveAsset lookSensitivityDesktop;
        [SerializeField] [Required] private FloatSaveAsset lookSensitivityGamepad;

        public float LookSharpness => lookSharpness;
        public float LookSensitivity => lookSensitivity;
        public float MinVerticalAngle => minVerticalAngle;
        public float MaxVerticalAngle => maxVerticalAngle;

        public InputActionReference MovementInput => movementInput;
        public InputActionReference LookInput => lookInput;

        public FloatSaveAsset LookSensitivityDesktop => lookSensitivityDesktop;
        public FloatSaveAsset LookSensitivityGamepad => lookSensitivityGamepad;
    }
}