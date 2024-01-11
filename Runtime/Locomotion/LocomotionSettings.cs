using MobX.Mediator;
using MobX.Mediator.Settings;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    public class LocomotionSettings : SettingsAsset
    {
        [Header("Movement")]
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

        [Header("Gravity")]
        [SerializeField] private float gravityForce = 9.81f;
        [SerializeField] private float maxGravityVelocity = 1;
        [SerializeField] private float airResistance = 1;

        [Header("Jump & Dash")]
        [SerializeField] private ManeuverType forwardJumpBehaviour = ManeuverType.Jump;
        [SerializeField] private ManeuverType sideJumpBehaviour = ManeuverType.Dash;
        [SerializeField] private ManeuverType backwardsJumpBehaviour = ManeuverType.Dash;

        [Header("Jump")]
        [SerializeField] private float jumpPostGroundingGraceTime = .1f;
        [SerializeField] private float jumpForce = 50;
        [SerializeField] private float weakJumpForce = 35;
        [SerializeField] private float minJumpForce = 15f;
        [SerializeField] private float postSlideBonusJumpForce = 25;
        [SerializeField] private float postSlideBonusJumpForceMinMagnitude = 7;
        [Tooltip("Factor is calculated between post slide bonus jump force min magnitude and max slide magnitude")]
        [SerializeField] private AnimationCurve postSlideMagnitudeJumpFactor;
        [SerializeField] private AnimationCurve jumpForceFactor;
        [SerializeField] private AnimationCurve jumpGravityFactor;
        [SerializeField] private Slow postWeakJumpSlow;
        [SerializeField] private bool enableDoubleJumps;
        [ShowIf(nameof(enableDoubleJumps))]
        [SerializeField] private int jumpCount = 2;

        [Header("Dash")]
        [SerializeField] private float dashForce = 150;
        [SerializeField] private float weakDashForce = 75;
        [SerializeField] private float dashDuration = .4f;
        [SerializeField] private float weakDashDuration = .2f;
        [SerializeField] private float dashCooldown = .7f;
        [SerializeField] private float weakDashCooldown = 1.1f;
        [SerializeField] private Slow postWeakDashSlow;

        [Header("Crouch")]
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

        [Header("Stamina")]
        [ShowIf(nameof(enableSprint))]
        [SerializeField] private StaminaCost staminaCostSprint = StaminaCost.PerSeconds(10);
        [SerializeField] private StaminaCost staminaCostSlide = StaminaCost.Flat(10);
        [SerializeField] private StaminaCost staminaCostJump = StaminaCost.Bar();
        [SerializeField] private StaminaCost staminaCostDash = StaminaCost.Bar();
        [SerializeField] private int staminaPerBar = 25;
        [SerializeField] private int staminaBars = 6;
        [Tooltip("Duration in seconds until stamina regeneration starts after stamina was consumed")]
        [SerializeField] private float staminaRegenerationCooldown = 3f;
        [Tooltip("How much stamina is regenerated per second")]
        [SerializeField] private float staminaRegenerationSpeed = 50f;

        [Header("Persistent Settings")]
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
        public float JumpForce => jumpForce;
        public float WeakJumpForce => weakJumpForce;
        public Slow PostWeakJumpSlow => postWeakJumpSlow;
        public float MinJumpForce => minJumpForce;

        public ManeuverType ForwardJumpBehaviour => forwardJumpBehaviour;
        public ManeuverType SideJumpBehaviour => sideJumpBehaviour;
        public ManeuverType BackwardsJumpBehaviour => backwardsJumpBehaviour;

        public float DashForce => dashForce;
        public float WeakDashForce => weakDashForce;
        public float DashDuration => dashDuration;
        public float WeakDashDuration => weakDashDuration;
        public float DashCooldown => dashCooldown;
        public float WeakDashCooldown => weakDashCooldown;
        public Slow PostWeakDashSlow => postWeakDashSlow;
        public float PostSlideBonusJumpForceMinMagnitude => postSlideBonusJumpForceMinMagnitude;
        public float PostSlideBonusJumpForce => postSlideBonusJumpForce;
        public AnimationCurve PostSlideMagnitudeJumpFactor => postSlideMagnitudeJumpFactor;
        public AnimationCurve JumpForceFactor => jumpForceFactor;
        public AnimationCurve JumpGravityFactor => jumpGravityFactor;
        public bool EnableDoubleJumps => enableDoubleJumps;
        public int JumpCount => jumpCount;

        public StaminaCost StaminaCostSprint => staminaCostSprint;
        public StaminaCost StaminaCostSlide => staminaCostSlide;
        public StaminaCost StaminaCostJump => staminaCostJump;
        public StaminaCost StaminaCostDash => staminaCostDash;
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