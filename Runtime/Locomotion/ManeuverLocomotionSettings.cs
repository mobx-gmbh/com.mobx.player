using MobX.Mediator.Settings;
using UnityEngine;
using UnityEngine.Serialization;

namespace MobX.Player.Locomotion
{
    public class ManeuverLocomotionSettings : SettingsAsset
    {
        [SerializeField] private int maneuverCount = 2;
        [SerializeField] private float jumpPostGroundingGraceTime = .1f;
        [SerializeField] private Slow postWeakJumpSlow;
        [SerializeField] private Slow postWeakDashSlow;

        [Space]
        [Header("Standstill")]
        [FormerlySerializedAs("standstillManeuverOld")]
        [SerializeField] private ManeuverSettings[] standstillManeuverGrounded;
        [SerializeField] private ManeuverSettings[] standstillManeuverAirborne;

        [Header("Forwards")]
        [FormerlySerializedAs("forwardManeuverOld")]
        [SerializeField] private ManeuverSettings[] forwardManeuverGrounded;
        [SerializeField] private ManeuverSettings[] forwardManeuverAirborne;

        [Header("Side")]
        [FormerlySerializedAs("sideManeuverOld")]
        [SerializeField] private ManeuverSettings[] sideManeuverGrounded;
        [SerializeField] private ManeuverSettings[] sideManeuverAirborne;

        [Header("Backwards")]
        [FormerlySerializedAs("backwardsManeuverOld")]
        [SerializeField] private ManeuverSettings[] backwardsManeuverGrounded;
        [SerializeField] private ManeuverSettings[] backwardsManeuverAirborne;

        public int ManeuverCount => maneuverCount;
        public float JumpPostGroundingGraceTime => jumpPostGroundingGraceTime;
        public Slow PostWeakJumpSlow => postWeakJumpSlow;
        public Slow PostWeakDashSlow => postWeakDashSlow;

        public ManeuverSettings[] StandstillManeuverGrounded => standstillManeuverGrounded;
        public ManeuverSettings[] StandstillManeuverAirborne => standstillManeuverAirborne;
        public ManeuverSettings[] ForwardManeuverGrounded => forwardManeuverGrounded;
        public ManeuverSettings[] ForwardManeuverAirborne => forwardManeuverAirborne;
        public ManeuverSettings[] SideManeuverGrounded => sideManeuverGrounded;
        public ManeuverSettings[] SideManeuverAirborne => sideManeuverAirborne;
        public ManeuverSettings[] BackwardsManeuverGrounded => backwardsManeuverGrounded;
        public ManeuverSettings[] BackwardsManeuverAirborne => backwardsManeuverAirborne;
    }
}