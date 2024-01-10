using Baracuda.Monitoring;
using Drawing;
using KinematicCharacterController;
using MobX.DevelopmentTools.Visualization;
using MobX.Mediator.Cooldown;
using MobX.Utilities;
using MobX.Utilities.Reflection;
using MobX.Utilities.Types;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Assertions;

namespace MobX.Player.Locomotion
{
    [MGroupName("Locomotion")]
    [ExecuteAfter(typeof(KinematicCharacterMotor))]
    [RequireComponent(typeof(KinematicCharacterMotor))]
    [RequireComponent(typeof(StaminaController))]
    [RequireComponent(typeof(SlowController))]
    [SuppressMessage("ReSharper", "ConvertToAutoPropertyWithPrivateSetter")]
    public class LocomotionController : MonoBehaviour, ICharacterController, IDrawGizmos
    {
        #region Fields

        [SerializeField] [Required] private LocomotionSettings settings;

        [Header("Transforms")]
        [SerializeField] private Transform headTransform;
        [SerializeField] private Transform standStillJumpDirection;
        [SerializeField] private Transform postSlideJumpDirection;
        [SerializeField] private Transform sprintJumpDirection;
        [SerializeField] private Transform forwardJumpDirection;

        private readonly List<Collider> _ignoredCollider = new();
        private KinematicCharacterMotor _motor;

        private StaminaController StaminaController { get; set; }
        private SlowController SlowController { get; set; }

        private bool _hasInputs;
        private LocomotionInputs _inputs;
        private MovementDirection _inputDirection;
        private Vector3 _accumulatedGravity;
        private Vector3 _accumulatedJumpForce;
        private float _movementSpeed;
        private float _defaultHeight;
        private float _height;
        private bool _isSliding;
        private bool _isSlidingDown;
        private bool _isCrouching;
        private float _slideMagnitude;
        private float _slideTime;
        private Vector3 _slideVelocity;
        private Vector3 _lastPosition;
        private Timer _postSlideTimer;
        private float _jumpElapsedTime;
        private Vector3 _jumpDirection;
        private Vector3 _dashDirection;
        private float _dashForce;
        private float _dashElapsedTime;
        private float _dashDuration;
        private Timer _postGroundJumpGraceTimer;
        private Cooldown _dashCooldown;
        private float _jumpForce;
        private bool _isJumping;
        private bool _isDashing;
        private bool _isFalling;
        private bool _isSprinting;
        private bool _isWeakDash;
        private bool _isWeakJump;

        #endregion


        #region Properties

        [Monitor]
        private Vector3 Velocity => transform.position - _lastPosition;

        [Monitor]
        public float Speed => _motor.Velocity.magnitude;

        [Monitor]
        public bool IsSliding => _isSliding;

        [Monitor]
        public bool IsCrouching => _isCrouching;

        [Monitor]
        public float SlideTime => _slideTime;

        [Monitor]
        public Vector3 SlideVelocity => _slideVelocity;

        [Monitor]
        public float SlideMagnitude => _slideMagnitude;

        [Monitor]
        public float JumpForce => _jumpForce;

        [Monitor]
        public bool IsJumping => _isJumping;

        [Monitor]
        public bool IsDashing => _isDashing;

        [Monitor]
        public bool IsSprinting => _isSprinting;

        [Monitor]
        public bool IsWeakDash => _isWeakDash;

        [Monitor]
        public bool IsWeakJump => _isWeakJump;

        #endregion


        #region Setup & Shutdown

        private void Awake()
        {
            Assert.IsNotNull(settings);
            StaminaController = GetComponent<StaminaController>();
            SlowController = GetComponent<SlowController>();
            _motor = GetComponent<KinematicCharacterMotor>();
            _motor.CharacterController = this;
            var childCollider = GetComponentsInChildren<Collider>(true);
            _ignoredCollider.AddRange(childCollider);
            _defaultHeight = _motor.Capsule.height;
            _height = _defaultHeight;
            _dashCooldown = new Cooldown(settings.DashCooldown);
            this.StartMonitoring();
        }

        private void OnDestroy()
        {
            this.StopMonitoring();
            _ignoredCollider.Clear();
        }

        #endregion


        #region KKC: Inputs

        public void SetInputs(in LocomotionInputs inputs)
        {
            _inputs = inputs;
            _hasInputs = true;
            ProcessCrouchInput();
        }

        #endregion


        #region KKC: Pre Update

        public void BeforeCharacterUpdate(float deltaTime)
        {
            if (_hasInputs is false)
            {
                return;
            }

            if (_motor.GroundingStatus.FoundAnyGround)
            {
                _postGroundJumpGraceTimer = new Timer(settings.JumpPostGroundingGraceTime);
            }

            SetupInputDirection();
        }

        private void SetupInputDirection()
        {
            var isStandingStill = _inputs.MovementInput.magnitude <= 0;
            if (isStandingStill)
            {
                _inputDirection = MovementDirection.None;
                return;
            }

            var hasForwardInput = _inputs.MovementInput.z > 0;
            if (hasForwardInput)
            {
                _inputDirection = MovementDirection.Forward;
                return;
            }

            var hasBackwardInput = _inputs.MovementInput.z < 0;
            if (hasBackwardInput)
            {
                _inputDirection = MovementDirection.Backward;
                return;
            }

            var hasLeftInput = _inputs.MovementInput.x > 0;
            if (hasLeftInput)
            {
                _inputDirection = MovementDirection.Right;
                return;
            }

            var hasRightInput = _inputs.MovementInput.x < 0;
            if (hasRightInput)
            {
                _inputDirection = MovementDirection.Left;
            }
        }

        #endregion


        #region KKC: Velocity

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_hasInputs is false)
            {
                return;
            }
            if (_isSliding)
            {
                ProcessSlide(ref currentVelocity, deltaTime);
                ProcessGravity(ref currentVelocity, deltaTime);
            }
            else
            {
                ProcessMovementInput(ref currentVelocity, deltaTime);
                ProcessGravity(ref currentVelocity, deltaTime);
                ProcessJumpOrDash(ref currentVelocity, deltaTime);
            }
        }

        #endregion


        #region KKC: Jump & Dash

        private void ProcessJumpOrDash(ref Vector3 currentVelocity, float deltaTime)
        {
            var jumpRequested = _inputs.JumpInput.IsPressed() && CanDashOrJump();

            if (jumpRequested)
            {
                var result = CalculateJumpDashValues();
                switch (result.mode)
                {
                    case SpaceMovementMode.Jump:
                        StartJump(ref currentVelocity, result.direction);
                        break;
                    case SpaceMovementMode.Dash:
                        StartDash(result.direction);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (_isJumping)
            {
                ProcessJump(ref currentVelocity, deltaTime);
            }
            if (_isDashing)
            {
                ProcessDash(ref currentVelocity, deltaTime);
            }
        }

        private (Vector3 direction, SpaceMovementMode mode) CalculateJumpDashValues()
        {
            switch (_inputDirection)
            {
                case MovementDirection.None when _postSlideTimer.IsRunning:
                case MovementDirection.Forward when _postSlideTimer.IsRunning:
                    return (postSlideJumpDirection.forward, SpaceMovementMode.Jump);

                case MovementDirection.None:
                    return (standStillJumpDirection.forward, SpaceMovementMode.Jump);

                case MovementDirection.Forward when _inputs.SprintInput.IsPressed():
                    return (sprintJumpDirection.forward, SpaceMovementMode.Jump);

                case MovementDirection.Forward:
                    return (forwardJumpDirection.forward, SpaceMovementMode.Jump);

                case MovementDirection.Left:
                    return (-_motor.CharacterRight, SpaceMovementMode.Dash);

                case MovementDirection.Right:
                    return (_motor.CharacterRight, SpaceMovementMode.Dash);

                case MovementDirection.Backward:
                    return ((_inputs.ForwardRotation * _inputs.MovementInput).normalized, SpaceMovementMode.Dash);

                default:
                    return (standStillJumpDirection.forward, SpaceMovementMode.Dash);
            }
        }

        private bool CanDashOrJump()
        {
            if (_isJumping)
            {
                return false;
            }

            if (_isDashing)
            {
                return false;
            }

            if (_dashCooldown.IsRunning)
            {
                return false;
            }

            if (_motor.GroundingStatus.FoundAnyGround)
            {
                return true;
            }

            if (_postGroundJumpGraceTimer.IsRunning)
            {
                return true;
            }

            return false;
        }

        #endregion


        #region KKC: Jump

        private void ProcessJump(ref Vector3 currentVelocity, float deltaTime)
        {
            var jumpFactor = settings.JumpForceFactor.Evaluate(_jumpElapsedTime);
            var jumpForce = jumpFactor * _jumpForce;
            var jumpVector = _jumpDirection * jumpForce;
            var jumpVectorDelta = jumpVector * deltaTime;
            currentVelocity += jumpVectorDelta;
            _jumpElapsedTime += deltaTime;
        }

        private void StartJump(ref Vector3 currentVelocity, Vector3 direction)
        {
            _isWeakJump = !StaminaController.HasEnoughStamina(settings.StaminaCostJump);
            _isJumping = true;
            _jumpElapsedTime = 0;
            _jumpDirection = direction;
            _jumpForce = _isWeakJump ? settings.WeakJumpForce : settings.JumpForce;

            var minSlideMagnitude = settings.PostSlideBonusJumpForceMinMagnitude;
            if (_postSlideTimer.IsRunning && _slideMagnitude >= minSlideMagnitude)
            {
                var normalizedSlideMagnitude = Mathf.InverseLerp(
                    minSlideMagnitude,
                    settings.MaxSlideMagnitude,
                    _slideMagnitude);

                var magnitudeFactor = settings.PostSlideMagnitudeJumpFactor.Evaluate(normalizedSlideMagnitude);
                var postSlideBonusJumpForce = settings.PostSlideBonusJumpForce * magnitudeFactor;
                _jumpForce += postSlideBonusJumpForce;
            }

            SlowController.SlowSpeedValue(ref _jumpForce);
            _jumpForce = _jumpForce.WithMinLimit(settings.MinJumpForce);

            StaminaController.ConsumeStamina(settings.StaminaCostJump);
            _motor.ForceUnground();
            _accumulatedGravity = Vector3.zero;
            currentVelocity.y = 0;
        }

        private void StopJump()
        {
            if (_isJumping is false)
            {
                return;
            }
            if (_isWeakJump)
            {
                SlowController.AddSlow(settings.PostWeakJumpSlow);
            }
            _isJumping = false;
            _jumpDirection = Vector3.zero;
            _jumpElapsedTime = 0;
            _isWeakJump = false;
        }

        #endregion


        #region KKC: Dash

        private void ProcessDash(ref Vector3 currentVelocity, float deltaTime)
        {
            var dashVector = _dashForce * _dashDirection;
            var dashVectorDelta = dashVector * deltaTime;
            currentVelocity += dashVectorDelta;
            _dashElapsedTime += deltaTime;
            if (_dashElapsedTime > _dashDuration)
            {
                StopDash();
            }
        }

        private void StartDash(Vector3 direction)
        {
            _isDashing = true;
            _isWeakDash = !StaminaController.HasEnoughStamina(settings.StaminaCostDash);
            _dashCooldown.Value = _isWeakDash ? settings.WeakDashCooldown : settings.DashCooldown;
            _dashCooldown.Start();
            _dashForce = _isWeakDash ? settings.WeakDashForce : settings.DashForce;
            _dashDuration = _isWeakDash ? settings.WeakDashDuration : settings.DashDuration;
            StaminaController.ConsumeStamina(settings.StaminaCostDash);
            _dashDirection = direction;
            _dashElapsedTime = 0;
        }

        private void StopDash()
        {
            if (_isWeakDash)
            {
                SlowController.AddSlow(settings.PostWeakDashSlow);
            }
            _isDashing = false;
            _dashDirection = Vector3.zero;
            _dashElapsedTime = 0;
            _isWeakDash = false;
        }

        #endregion


        #region KKC: Movement Input

        private float CalculateTargetMovementSpeed()
        {
            switch (_inputDirection)
            {
                case MovementDirection.Forward:
                    return _isSprinting
                        ? settings.MovementSpeedSprint
                        : settings.MovementSpeedForward;

                case MovementDirection.Left:
                case MovementDirection.Right:
                    return settings.MovementSpeedSide;

                case MovementDirection.Backward:
                    return settings.MovementSpeedBackward;
                default:
                    return 0;
            }
        }

        private void ProcessMovementInput(ref Vector3 currentVelocity, float deltaTime)
        {
            var wasSprinting = _isSprinting;
            _isSprinting = _inputs.SprintInput.IsPressed()
                           && !_isCrouching
                           && ((StaminaController.Stamina >= StaminaController.StaminaPerBar && !wasSprinting) ||
                               (_isSprinting && StaminaController.Stamina > 0))
                           && _inputDirection is MovementDirection.Forward
                           && _motor.GroundingStatus.IsStableOnGround;

            if (wasSprinting && _isJumping)
            {
                _isSprinting = true;
            }

            if (_isSprinting)
            {
                StaminaController.ConsumeStamina(settings.StaminaCostSprint);
            }

            var targetMovementSpeed = CalculateTargetMovementSpeed();

            if (_isCrouching)
            {
                targetMovementSpeed *= settings.CrouchMovementSpeedFactor;
            }

            var speedSharpness = _isSprinting
                ? settings.MovementSpeedIncreaseSharpness
                : settings.MovementSpeedDecaySharpness;

            SlowController.SlowSpeedValue(ref targetMovementSpeed);
            targetMovementSpeed = targetMovementSpeed.WithMinLimit(settings.MinimumMovementSpeed);

            _movementSpeed = Mathf.Lerp(_movementSpeed, targetMovementSpeed, deltaTime * speedSharpness);

            // Calculate oriented input based on movement input and forward rotation.
            var orientedInput = (_inputs.ForwardRotation * _inputs.MovementInput).normalized * _movementSpeed;

            // Separate the horizontal and vertical components of the current velocity.
            var horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);

            // Limit the horizontal velocity magnitude to prevent excessive speed.
            horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, 20);

            // Calculate effective ground normal.
            var effectiveGroundNormal = Vector3.up;
            if (_motor.GroundingStatus.IsStableOnGround)
            {
                effectiveGroundNormal = CalculateEffectiveGroundNormal(currentVelocity);
                // Set vertical velocity to 0 if the character is stable on the ground.
                currentVelocity.y = 0;
            }

            // Apply sharpness based on whether the character is grounded or airborne.
            var sharpness = _motor.GroundingStatus.IsStableOnGround
                ? settings.MovementDirectionSharpness
                : settings.AirborneDirectionSharpness;

            // Adjust horizontal velocity towards the oriented input, tangent to the effective ground normal.
            var targetVelocity = _motor.GetDirectionTangentToSurface(orientedInput, effectiveGroundNormal) *
                                 orientedInput.magnitude;
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, deltaTime * sharpness);

            // Reassemble the current velocity with the adjusted horizontal component.
            currentVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);
        }

        private Vector3 CalculateEffectiveGroundNormal(Vector3 currentVelocity)
        {
            var effectiveGroundNormal = _motor.GroundingStatus.GroundNormal;
            if (currentVelocity.magnitude > 0f && _motor.GroundingStatus.SnappingPrevented)
            {
                var groundPointToCharacter = _motor.TransientPosition - _motor.GroundingStatus.GroundPoint;
                effectiveGroundNormal = Vector3.Dot(currentVelocity, groundPointToCharacter) >= 0f
                    ? _motor.GroundingStatus.OuterGroundNormal
                    : _motor.GroundingStatus.InnerGroundNormal;
            }
            return effectiveGroundNormal;
        }

        #endregion


        #region KCC: Gravity

        private void ProcessGravity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!_motor.GroundingStatus.FoundAnyGround)
            {
                var jumpFactor = _isJumping ? settings.JumpGravityFactor.Evaluate(_jumpElapsedTime) : 1f;
                var gravity = _motor.CharacterUp * (-settings.GravityForce * jumpFactor);

                _accumulatedGravity += gravity * deltaTime;

                // Apply a drag-like effect to simulate air resistance.
                var dragFactor = settings.AirResistance;
                _accumulatedGravity *= 1 - dragFactor * deltaTime;

                // Clamp the accumulated gravity to a maximum value.
                var maxGravityVelocity = settings.MaxGravityVelocity;
                if (_accumulatedGravity.magnitude > maxGravityVelocity)
                {
                    _accumulatedGravity = Vector3.ClampMagnitude(_accumulatedGravity, maxGravityVelocity);
                }

                currentVelocity += _accumulatedGravity;
            }
            else
            {
                _accumulatedGravity = Vector3.zero;
            }
        }

        #endregion


        #region KKC: Crouch & Slide

        private void ProcessCrouchInput()
        {
            var isCrouchReleasedThisFrame = _inputs.CrouchInput.WasReleasedThisFrame();
            var isCrouchPressedThisFrame = _inputs.CrouchInput.WasPressedThisFrame();
            var isCrouchPressed = _inputs.CrouchInput.IsPressed();

            if (isCrouchPressedThisFrame)
            {
                if (_isSliding is false)
                {
                    if (_isSprinting)
                    {
                        StartSlide();
                    }
                    else if (_inputDirection is MovementDirection.Right or MovementDirection.Left)
                    {
                        StartSlide();
                    }
                    else if (settings.RequireSprintForForwardSlide is false &&
                             _inputDirection is MovementDirection.Forward)
                    {
                        StartSlide();
                    }
                }
            }
            if (isCrouchReleasedThisFrame)
            {
                if (_isSliding)
                {
                    StopSliding();
                }
                else if (_isCrouching)
                {
                    StopCrouch();
                }
            }

            if (isCrouchPressed && !_isSliding && !_isCrouching)
            {
                StartCrouch();
            }

            var targetHeight = isCrouchPressed ? settings.SlideHeight : _defaultHeight;
            _height = Mathf.Lerp(_height, targetHeight, Time.deltaTime * (isCrouchPressed ? 15f : 4f));
            headTransform.localPosition = headTransform.localPosition.With(y: _height - .25f);
        }

        #endregion


        #region KKC: Crouch

        private void StartCrouch()
        {
            _isCrouching = true;
        }

        private void StopCrouch()
        {
            _isCrouching = false;
        }

        #endregion


        #region KKC: Slide

        private void StartSlide()
        {
            _isSliding = true;
            _slideTime = _isSprinting ? settings.SlideDurationSprint : settings.SlideDuration;
            _slideVelocity = _motor.Velocity;
            StaminaController.ConsumeStamina(settings.StaminaCostSlide);
        }

        private void StopSliding()
        {
            _isSliding = false;
            _slideTime = 0;
            _slideVelocity = default(Vector3);
            _postSlideTimer = new Timer(.3f);
        }

        private void ProcessSlide(ref Vector3 currentVelocity, float deltaTime)
        {
            var downDotVelocity = Vector3.Dot(_motor.CharacterUp, Velocity.normalized);
            var slidingThreshold = settings.SlideDownDotThreshold;
            _isSlidingDown = downDotVelocity < slidingThreshold;

            if (_isSlidingDown)
            {
                _slideTime += deltaTime;
                _slideTime =
                    _slideTime.WithMaxLimit(_isSprinting ? settings.SlideDurationSprint : settings.SlideDuration);
            }
            else
            {
                _slideTime -= deltaTime;
            }
            if (_slideTime <= 0)
            {
                StopSliding();
                return;
            }

            var magnitude = _slideVelocity.magnitude;
            var movementInput = _inputs.MovementInput;
            var forwardInput = _inputs.ForwardRotation;
            var orientedInput = (forwardInput * movementInput).normalized;
            var slideAdjustment = orientedInput * (deltaTime * settings.SlideAdjustmentStrength);
            _slideVelocity += slideAdjustment;
            _slideVelocity = Vector3.ClampMagnitude(_slideVelocity, magnitude);

            if (_isSlidingDown)
            {
                _slideVelocity *= 1 + deltaTime * settings.SlideDownVelocityIncrease;
                _slideVelocity = Vector3.ClampMagnitude(_slideVelocity, settings.MaxSlideMagnitude);
            }
            else
            {
                var slideDuration = _isSprinting ? settings.SlideDurationSprint : settings.SlideDuration;
                var normalizedTime = 1 - _slideTime / slideDuration;
                var friction = settings.SlideFriction * settings.SlideFrictionFactor.Evaluate(normalizedTime);
                var frictionFactor = 1 - friction * deltaTime;
                _slideVelocity *= frictionFactor;
            }

            _slideMagnitude = _slideVelocity.magnitude;

            currentVelocity = new Vector3(_slideVelocity.x, currentVelocity.y, _slideVelocity.z);
        }

        #endregion


        #region KKC: Rotation

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_hasInputs is false)
            {
                return;
            }

            var self = transform;
            var selfPosition = self.position;
            Visualize.BlueLine(selfPosition, selfPosition + self.forward);

            var movementInput = _inputs.MovementInput;
            var movementMagnitude = movementInput.magnitude;
            var isAiming = _inputs.AimInput.IsPressed();

            switch (_inputs.CameraMode)
            {
                case CameraMode.FirstPerson:
                    currentRotation = _inputs.ForwardRotation;
                    break;

                case CameraMode.ThirdPerson when isAiming:
                    currentRotation = Quaternion.Slerp(currentRotation, _inputs.ForwardRotation,
                        deltaTime * settings.RotationSharpness);
                    break;

                case CameraMode.ThirdPerson when movementMagnitude > 0:
                {
                    var movementRotation = Quaternion.LookRotation(_inputs.MovementInput.normalized, Vector3.up);
                    var targetRotation = _inputs.ForwardRotation * movementRotation;
                    currentRotation = Quaternion.Slerp(currentRotation, targetRotation,
                        deltaTime * settings.RotationSharpness);

                    break;
                }
            }
        }

        #endregion


        #region KKC: Unused Callbacks

        public void PostGroundingUpdate(float deltaTime)
        {
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            _lastPosition = transform.position;
        }

        public bool IsColliderValidForCollisions(Collider other)
        {
            return _ignoredCollider.Contains(other) is false;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
            StopJump();
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            Vector3 atCharacterPosition,
            Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }

        #endregion


        #region Gizmos

        protected LocomotionController()
        {
            DrawingManager.Register(this);
        }

        public void DrawGizmos()
        {
            _motor ??= GetComponent<KinematicCharacterMotor>();
            var position = transform.position;
            var up = _motor.CharacterUp;
            var forward = _motor.CharacterForward;
            var height = _motor.Capsule.height;

            Draw.WireCapsule(
                position,
                up,
                height,
                _motor.Capsule.radius,
                Color.cyan);

            Draw.ArrowheadArc(
                position,
                forward,
                .5f,
                Color.cyan);
        }

        #endregion
    }
}