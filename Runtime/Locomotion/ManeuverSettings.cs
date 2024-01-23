using MobX.Utilities.Types;
using System;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    [Serializable]
    public struct ManeuverSettings
    {
        public ManeuverType type;
        public float force;
        public float weakForce;
        public float postSlideForce;
        public AnimationCurve forceFactorOverTime;
        public AnimationCurve gravityFactorOverTime;
        public float maneuverCooldownInSeconds;
        public float minDurationInSeconds;
        public StaminaCost staminaCost;
    }

    [Serializable]
    public struct ManeuverSettingsOverride
    {
        public float graceTime;
        public Optional<ManeuverType> type;
        public Optional<float> force;
        public Optional<float> weakForce;
        public Optional<float> postSlideForce;
        public Optional<AnimationCurve> forceFactorOverTime;
        public Optional<AnimationCurve> gravityFactorOverTime;
        public Optional<float> maneuverCooldownInSeconds;
        public Optional<float> minDurationInSeconds;
        public Optional<StaminaCost> staminaCost;
    }
}