using MobX.Mediator.Settings;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    public class CrouchLocomotionSettings : SettingsAsset
    {
        [SerializeField] private float crouchMovementSpeedFactor = .4f;
        [SerializeField] private float crouchHeight = 1.4f;
        [Header("Slide")]
        [SerializeField] private StaminaCost staminaCostSlide = StaminaCost.Flat(10);
        [Range(-1, 0)]
        [SerializeField] private float slideDownDotThreshold = -.2f;
        [SerializeField] private float slideHeight = 1f;
        [SerializeField] private float slideFriction = 1f;
        [SerializeField] private float slideDuration = 1f;
        [SerializeField] private float slideDurationSprint = 1f;
        [SerializeField] private float slideAdjustmentStrength = 10;
        [SerializeField] private float minSlideMagnitude = 7;
        [SerializeField] private float maxSlideMagnitude = 15;
        [SerializeField] private float minSlideMagnitudeToOvercomeMovementLimit = 19;
        [SerializeField] private float slideDownVelocityIncrease = .1f;
        [SerializeField] private float postSlideGraceTime = .7f;
        [SerializeField] private AnimationCurve slideFrictionFactor;
        [SerializeField] private bool requireSprintForForwardSlide;
        [SerializeField] private Slow postWeakSlideSlow;
        [SerializeField] private bool canPerformWeakSlides = true;

        public float CrouchMovementSpeedFactor => crouchMovementSpeedFactor;
        public float CrouchHeight => crouchHeight;

        public StaminaCost StaminaCostSlide => staminaCostSlide;
        public float SlideDownDotThreshold => slideDownDotThreshold;
        public float SlideHeight => slideHeight;
        public float SlideFriction => slideFriction;
        public float SlideDuration => slideDuration;
        public float SlideDurationSprint => slideDurationSprint;
        public float SlideAdjustmentStrength => slideAdjustmentStrength;
        public float MinSlideMagnitude => minSlideMagnitude;
        public float MaxSlideMagnitude => maxSlideMagnitude;
        public float MinSlideMagnitudeToOvercomeMovementLimit => minSlideMagnitudeToOvercomeMovementLimit;
        public float SlideDownVelocityIncrease => slideDownVelocityIncrease;
        public float PostSlideGraceTime => postSlideGraceTime;
        public AnimationCurve SlideFrictionFactor => slideFrictionFactor;
        public bool RequireSprintForForwardSlide => requireSprintForForwardSlide;
        public Slow PostWeakSlideSlow => postWeakSlideSlow;
        public bool CanPerformWeakSlides => canPerformWeakSlides;
    }
}