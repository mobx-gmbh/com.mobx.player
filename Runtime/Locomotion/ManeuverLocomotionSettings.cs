using MobX.Mediator.Settings;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    public class ManeuverLocomotionSettings : SettingsAsset
    {
        [SerializeField] private int maneuverCount = 2;
        [SerializeField] private float jumpPostGroundingGraceTime = .1f;
        [Tooltip("Factor is calculated between post slide bonus jump force min magnitude and max slide magnitude")]
        [SerializeField] private AnimationCurve postSlideMagnitudeJumpFactor;
        [SerializeField] private Slow postWeakJumpSlow;
        [SerializeField] private Slow postWeakDashSlow;
        [Space]
        [SerializeField] private ManeuverSettings[] standstillManeuver;
        [SerializeField] private ManeuverSettings[] forwardManeuver;
        [SerializeField] private ManeuverSettings[] sideManeuver;
        [SerializeField] private ManeuverSettings[] backwardsManeuver;

        [Space]
#pragma warning disable CS0414 // Field is assigned but its value is never used
        [SerializeField] private ManeuverSettingsOverride postBlinkManeuverOverride;
#pragma warning restore CS0414 // Field is assigned but its value is never used

        public int ManeuverCount => maneuverCount;
        public float JumpPostGroundingGraceTime => jumpPostGroundingGraceTime;
        public AnimationCurve PostSlideMagnitudeJumpFactor => postSlideMagnitudeJumpFactor;
        public Slow PostWeakJumpSlow => postWeakJumpSlow;
        public Slow PostWeakDashSlow => postWeakDashSlow;
        public ManeuverSettings[] StandstillManeuver => standstillManeuver;
        public ManeuverSettings[] ForwardManeuver => forwardManeuver;
        public ManeuverSettings[] SideManeuver => sideManeuver;
        public ManeuverSettings[] BackwardsManeuver => backwardsManeuver;
    }
}