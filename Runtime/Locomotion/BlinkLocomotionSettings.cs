using MobX.Mediator.Settings;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    public class BlinkLocomotionSettings : SettingsAsset
    {
        [SerializeField] private float force = 100;
        [SerializeField] private float durationPerCharge = .05f;
        [SerializeField] private float cooldownInSecondsPerCharge = .3f;
        [Space]
        [SerializeField] private int maxCharges = 3;
        [SerializeField] private float chargePerSeconds = 1;
        [Space]
        [SerializeField] private float chargeTimeScale = .1f;
        [SerializeField] private float timeScaleFadeInSharpness = 25f;
        [SerializeField] private float postBlinkMagnitudeLimitation = 20;

        public float Force => force;
        public float DurationPerCharge => durationPerCharge;
        public float CooldownInSecondsPerCharge => cooldownInSecondsPerCharge;
        public int MaxCharges => maxCharges;
        public float ChargePerSeconds => chargePerSeconds;
        public float ChargeTimeScale => chargeTimeScale;
        public float TimeScaleFadeInSharpness => timeScaleFadeInSharpness;
        public float PostBlinkMagnitudeLimitation => postBlinkMagnitudeLimitation;
    }
}