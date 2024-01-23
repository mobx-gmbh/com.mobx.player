using MobX.Mediator.Settings;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    public class MovementLocomotionSettings : SettingsAsset
    {
        [SerializeField] private bool enableSprint = true;
        [SerializeField] private float movementSpeedForward = 4.6f;
        [SerializeField] private float movementSpeedSide = 4.3f;
        [SerializeField] private float movementSpeedBackward = 3.2f;
        [ShowIf(nameof(enableSprint))]
        [SerializeField] private float movementSpeedSprint = 9f;
        [SerializeField] private float movementSpeedIncreaseSharpness = 7f;
        [SerializeField] private float movementSpeedDecaySharpness = 25f;
        [SerializeField] private float movementDirectionSharpness = 25f;
        [SerializeField] private float airborneDirectionSharpness = 5f;
        [SerializeField] private float minimumMovementSpeed = .5f;

        [Header("Rotation")]
        [SerializeField] private float rotationSharpness = 25;

        public bool EnableSprint => enableSprint;
        public float MovementSpeedForward => movementSpeedForward;
        public float MovementSpeedSide => movementSpeedSide;
        public float MovementSpeedBackward => movementSpeedBackward;
        public float MovementSpeedSprint => movementSpeedSprint;
        public float MovementSpeedIncreaseSharpness => movementSpeedIncreaseSharpness;
        public float MovementSpeedDecaySharpness => movementSpeedDecaySharpness;
        public float MovementDirectionSharpness => movementDirectionSharpness;
        public float AirborneDirectionSharpness => airborneDirectionSharpness;
        public float MinimumMovementSpeed => minimumMovementSpeed;
        public float RotationSharpness => rotationSharpness;
    }
}