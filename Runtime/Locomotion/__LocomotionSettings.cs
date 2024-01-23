using MobX.Inspector;
using MobX.Mediator;
using MobX.Mediator.Settings;
using Sirenix.OdinInspector;
using UnityEngine;

//#pragma warning disable CS0414 // Field is assigned but its value is never used

namespace MobX.Player.Locomotion
{
    public class __LocomotionSettings : SettingsAsset
    {
        [Foldout("Movement")]
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

        [Foldout("Gravity")]
        [SerializeField] private float gravityForce = 9.81f;
        [SerializeField] private float maxGravityVelocity = 1;
        [SerializeField] private float airResistance = 1;

        [Foldout("Maneuver")]
        [SerializeField] private int maneuverCount = 2;
        [SerializeField] private float jumpPostGroundingGraceTime = .1f;
        [Tooltip("Factor is calculated between post slide bonus jump force min magnitude and max slide magnitude")]
        [SerializeField] private AnimationCurve postSlideMagnitudeJumpFactor;
        [SerializeField] private Slow postWeakJumpSlow;
        [SerializeField] private Slow postWeakDashSlow;
        [Space]
        [SerializeField] private ManeuverSettings[] forwardManeuver;
        [SerializeField] private ManeuverSettings[] sideManeuver;
        [SerializeField] private ManeuverSettings[] backwardsManeuver;
        [SerializeField] private ManeuverSettings[] standstillManeuver;

        [Foldout("Crouch")]
        [SerializeField] private float crouchMovementSpeedFactor = .4f;
        [SerializeField] private float crouchHeight = 1.4f;
        [Header("Slide")]
        [Range(-1, 0)]
        [SerializeField] private float slideDownDotThreshold = -.2f;
        [SerializeField] private float slideHeight = 1f;
        [SerializeField] private float slideFriction = 1f;
        [SerializeField] private float slideDuration = 1f;
        [ShowIf(nameof(enableSprint))]
        [SerializeField] private float slideDurationSprint = 1f;
        [SerializeField] private float slideAdjustmentStrength = 10;
        [SerializeField] private float maxSlideMagnitude = 17;
        [SerializeField] private float slideDownVelocityIncrease = .1f;
        [SerializeField] private float postSlideGraceTime = .7f;
        [SerializeField] private AnimationCurve slideFrictionFactor;
        [ShowIf(nameof(enableSprint))]
        [SerializeField] private bool requireSprintForForwardSlide;

        [Foldout("Stamina")]
        [ShowIf(nameof(enableSprint))]
        [SerializeField] private StaminaCost staminaCostSprint = StaminaCost.PerSeconds(10);
        [SerializeField] private StaminaCost staminaCostSlide = StaminaCost.Flat(10);
        [SerializeField] private int staminaPerBar = 25;
        [SerializeField] private int staminaBars = 6;
        [Tooltip("Duration in seconds until stamina regeneration starts after stamina was consumed")]
        [SerializeField] private float staminaRegenerationCooldown = 3f;
        [Tooltip("How much stamina is regenerated per second")]
        [SerializeField] private float staminaRegenerationSpeed = 50f;

        [Foldout("Persistent Settings")]
        [SerializeField] [Required] private BoolSaveAsset toggleCrouchDesktopSetting;
        [SerializeField] [Required] private BoolSaveAsset toggleCrouchGamepadSetting;

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

        public float GravityForce => gravityForce;
        public float MaxGravityVelocity => maxGravityVelocity;
        public float AirResistance => airResistance;
        public float JumpPostGroundingGraceTime => jumpPostGroundingGraceTime;

        public int ManeuverCount => maneuverCount;
        public Slow PostWeakJumpSlow => postWeakJumpSlow;
        public Slow PostWeakDashSlow => postWeakDashSlow;
        public ManeuverSettings[] ForwardManeuver => forwardManeuver;
        public ManeuverSettings[] SideManeuver => sideManeuver;
        public ManeuverSettings[] BackwardsManeuver => backwardsManeuver;
        public ManeuverSettings[] StandstillManeuver => standstillManeuver;

        public StaminaCost StaminaCostSprint => staminaCostSprint;
        public StaminaCost StaminaCostSlide => staminaCostSlide;
        public float CrouchMovementSpeedFactor => crouchMovementSpeedFactor;
        public float SlideDownDotThreshold => slideDownDotThreshold;
        public float SlideHeight => slideHeight;
        public float CrouchHeight => crouchHeight;
        public float SlideFriction => slideFriction;
        public float SlideDuration => slideDuration;
        public float SlideDurationSprint => slideDurationSprint;
        public float SlideAdjustmentStrength => slideAdjustmentStrength;
        public float MaxSlideMagnitude => maxSlideMagnitude;
        public float SlideDownVelocityIncrease => slideDownVelocityIncrease;
        public float PostSlideGraceTime => postSlideGraceTime;
        public AnimationCurve SlideFrictionFactor => slideFrictionFactor;
        public bool RequireSprintForForwardSlide => requireSprintForForwardSlide;

        public int StaminaPerBar => staminaPerBar;
        public int StaminaBars => staminaBars;
        public float StaminaRegenerationCooldown => staminaRegenerationCooldown;
        public float StaminaRegenerationSpeed => staminaRegenerationSpeed;

        public bool ToggleCrouchDesktopSetting => toggleCrouchDesktopSetting.Value;
        public bool ToggleCrouchGamepadSetting => toggleCrouchGamepadSetting.Value;
    }
}