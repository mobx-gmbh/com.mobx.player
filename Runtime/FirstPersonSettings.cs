using MobX.Mediator;
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

        [Header("Persistent")]
        [SerializeField] [Required] private FloatSaveAsset lookSensitivityDesktop;
        [SerializeField] [Required] private FloatSaveAsset lookSensitivityGamepad;

        public float MouseSensitivity => mouseSensitivity;
        public float MinVerticalAngle => minVerticalAngle;
        public float MaxVerticalAngle => maxVerticalAngle;

        public InputActionReference MovementInput => movementInput;
        public InputActionReference LookInput => lookInput;

        public FloatSaveAsset LookSensitivityDesktop => lookSensitivityDesktop;
        public FloatSaveAsset LookSensitivityGamepad => lookSensitivityGamepad;
    }
}