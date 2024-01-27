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
        public AnimationCurve forceFactorOverTime;
        public AnimationCurve gravityFactorOverTime;
        public StaminaCost staminaCost;
        public float maneuverCooldownInSeconds;
        public float minDurationInSeconds;
        public float postSlideBonusForce;
        [Tooltip("Factor is calculated between post slide bonus jump force min magnitude and max slide magnitude")]
        public AnimationCurve postSlideMagnitudeFactor;
    }
}