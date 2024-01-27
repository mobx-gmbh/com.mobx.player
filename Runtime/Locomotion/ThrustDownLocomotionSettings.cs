using MobX.Mediator.Settings;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    public class ThrustDownLocomotionSettings : SettingsAsset
    {
        [SerializeField] private ThrustDownActivation activationMode;
        [Space]
        [SerializeField] private float downwardForce;
        [SerializeField] private AnimationCurve downwardForceCurve;
        [Space]
        [SerializeField] private float maxDownwardForceMagnitude = 32;
        [SerializeField] private float minHeight;
        [SerializeField] private LayerMask groundCheckLayer;
        [Space]
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] private ForceSettings forceSettings;

        public ThrustDownActivation ActivationMode => activationMode;
        public float DownwardForce => downwardForce;
        public AnimationCurve DownwardForceCurve => downwardForceCurve;
        public float MaxDownwardForceMagnitude => maxDownwardForceMagnitude;
        public float MinHeight => minHeight;
        public LayerMask GroundCheckLayer => groundCheckLayer;
        public CameraShake CameraShake => cameraShake;
        public ForceSettings ForceSettings => forceSettings;
    }
}