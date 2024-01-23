using Baracuda.Monitoring;
using Drawing;
using KinematicCharacterController;
using MobX.Mediator.Callbacks;
using MobX.UI;
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
    public class LocomotionController : MonoBehaviour, ICharacterController, IDrawGizmos, IForceReceiver
    {
        #region Fields

        [SerializeField] [Required] private LocomotionSettings settings;

        [Header("Transforms")]
        [SerializeField] private Transform centerOfGravity;
        [SerializeField] private Transform headTransform;
        [SerializeField] private Transform standStillJumpDirection;
        [SerializeField] private Transform postSlideJumpDirection;
        [SerializeField] private Transform sprintJumpDirection;
        [SerializeField] private Transform forwardJumpDirection;

        // Reference Fields
        private readonly List<Collider> _ignoredCollider = new();
        private KinematicCharacterMotor _motor;
        private StaminaController StaminaController { get; set; }
        private SlowController SlowController { get; set; }

        // Basic Input Fields
        private bool _hasInputs;
        private MovementDirection _inputDirection;
        private LocomotionInputs _inputs;

        // Player Movement and State Fields
        private Vector3 _accumulatedGravity;
        private float _movementSpeed;
        private float _defaultHeight;
        private float _height;
        private bool _isSliding;
        private bool _isSlidingDown;
        private bool _isCrouching;
        private bool _isFalling;
        private bool _isSprinting;
        private int _jumpCounter;

        // Sliding Mechanics
        private float _slideTime;
        private Vector3 _slideVelocity;

        // Jump Mechanics
        private float _jumpElapsedTime;
        private Vector3 _jumpDirection;

        // Dash Mechanics
        private Vector3 _dashDirection;
        private float _dashForce;
        private float _dashElapsedTime;
        private float _dashDuration;

        // Maneuver Mechanics
        private bool _maneuverPressedThisFrame;
        private bool _maneuverPressed;
        private int _performedManeuverCount;
        private bool _isManeuver;
        private bool _isWeakManeuver;
        private ManeuverType _currentManeuverType;
        private Vector3 _normalizedManeuverDirection;
        private ManeuverSettings _maneuver;
        private float _elapsedManeuverTime;
        private float _calculatedManeuverForce;
        private Timer _maneuverTimer;
        private Timer _maneuverCooldown;

        // Blink Mechanics
        private bool _blinkPressedThisFrame;
        private bool _blinkPressed;
        private Vector3 _blinkDirection;
        private float _blinkForce;
        private BlinkState _blinkState;
        private float _currentBlinkCharge;
        private int _blinkCharges;
        private Timer _blinkCooldown;
        private Timer _blinkExecutionTimer;
        private bool _requestBlinkVelocityReset;

        // Position Tracking and Timers
        private Vector3 _lastPosition;
        private Timer _postSlideTimer;
        private Timer _postGroundJumpGraceTimer;
        private float _timeScaleFactor = 1;

        #endregion


        #region Properties

        private MovementLocomotionSettings MovementSettings => settings.MovementSettings;
        private GravityLocomotionSettings GravitySettings => settings.GravitySettings;
        private ManeuverLocomotionSettings ManeuverSettings => settings.ManeuverSettings;
        private CrouchLocomotionSettings CrouchSettings => settings.CrouchSettings;
        private StaminaLocomotionSettings StaminaSettings => settings.StaminaSettings;
        private BlinkLocomotionSettings BlinkSettings => settings.BlinkSettings;
        private SaveDataLocomotionSettings SaveDataSettings => settings.SaveDataSettings;

        private Vector3 Velocity => transform.position - _lastPosition;
        public Vector3 CenterOfGravity => centerOfGravity.position;

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
            this.StartMonitoring();

            Gameloop.AddTimeScaleModifier(ModifyTimeScale);
        }

        private void OnDestroy()
        {
            this.StopMonitoring();
            _ignoredCollider.Clear();

            Gameloop.RemoveTimeScaleModifier(ModifyTimeScale);
        }

        #endregion


        #region KKC: Inputs

        public void SetInputs(in LocomotionInputs inputs)
        {
            _inputs = inputs;
            _hasInputs = true;
            if (_inputs.ManeuverInput.WasPressedThisFrame())
            {
                _maneuverPressedThisFrame = true;
            }
            if (_inputs.ManeuverInput.IsPressed())
            {
                _maneuverPressed = true;
            }

            if (_inputs.BlinkInput.WasPressedThisFrame())
            {
                _blinkPressedThisFrame = true;
            }

            ProcessCrouchInput();
        }

        #endregion


        #region KKC: Before & After Updates

        public void BeforeCharacterUpdate(float deltaTime)
        {
            if (_hasInputs is false)
            {
                return;
            }

            if (_motor.GroundingStatus.FoundAnyGround)
            {
                _postGroundJumpGraceTimer = Timer.FromSeconds(ManeuverSettings.JumpPostGroundingGraceTime);
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

        public void AfterCharacterUpdate(float deltaTime)
        {
            _lastPosition = transform.position;

            _maneuverPressed = false;
            _maneuverPressedThisFrame = false;
            _blinkPressedThisFrame = false;
        }

        #endregion


        #region KKC: Velocity Update

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_hasInputs is false)
            {
                return;
            }

            if (ProcessBlink(ref currentVelocity, deltaTime))
            {
                return;
            }

            if (ProcessSlide(ref currentVelocity, deltaTime))
            {
                ProcessGravity(ref currentVelocity, deltaTime);
                ProcessExternalForce(ref currentVelocity, deltaTime);
                return;
            }

            ProcessMovementInput(ref currentVelocity, deltaTime);
            ProcessGravity(ref currentVelocity, deltaTime);
            ProcessManeuver(ref currentVelocity, deltaTime);
            ProcessExternalForce(ref currentVelocity, deltaTime);
        }

        private void Update()
        {
            ProcessBlinkUpdate();
        }

        #endregion


        #region KKC: Blink

        private bool ProcessBlink(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_blinkCooldown.IsRunning)
            {
                return false;
            }

            switch (_blinkState)
            {
                case BlinkState.None:
                    if (_blinkPressedThisFrame)
                    {
                        StartBlinkCharge();
                    }
                    return false;

                case BlinkState.Charging:
                    ProcessGravity(ref currentVelocity, deltaTime);
                    return true;

                case BlinkState.Blinking:
                    if (_requestBlinkVelocityReset)
                    {
                        currentVelocity = Vector3.zero;
                        _requestBlinkVelocityReset = false;
                    }
                    currentVelocity = _blinkDirection * _blinkForce;
                    ProcessGravity(ref currentVelocity, deltaTime);
                    if (_blinkExecutionTimer.Expired)
                    {
                        StopBlink();
                        currentVelocity =
                            Vector3.ClampMagnitude(currentVelocity, BlinkSettings.PostBlinkMagnitudeLimitation);
                    }

                    return true;

                default:
                    return true;
            }
        }

        private void StartBlinkCharge()
        {
            if (_blinkState != BlinkState.None)
            {
                return;
            }
            var hasEnoughStamina = StaminaController.HasEnoughStaminaFor(StaminaCost.Bar());
            if (hasEnoughStamina is false)
            {
                return;
            }
            _blinkState = BlinkState.Charging;
            _blinkCharges = 0;
            _currentBlinkCharge = 0;
        }

        private void StartBlinkExecution()
        {
            if (_blinkState != BlinkState.Charging)
            {
                return;
            }
            if (_blinkCharges < BlinkSettings.MaxCharges)
            {
                StaminaController.ConsumeStamina(StaminaCost.RemainingBar());
                _blinkCharges++;
            }
            _requestBlinkVelocityReset = true;
            _accumulatedGravity = Vector3.zero;
            _blinkState = BlinkState.Blinking;

            var forward = _inputs.CameraTransform.forward;
            var right = _inputs.CameraTransform.right;
            _blinkDirection = _inputDirection switch
            {
                MovementDirection.None => forward,
                MovementDirection.Forward => forward,
                MovementDirection.Left => -right,
                MovementDirection.Right => right,
                MovementDirection.Backward => -forward,
                var _ => throw new ArgumentOutOfRangeException()
            };

            _blinkForce = BlinkSettings.Force;

            var duration = BlinkSettings.DurationPerCharge * _blinkCharges;
            _blinkExecutionTimer = Timer.FromSeconds(duration);
            _motor.ForceUnground(duration);
        }

        private void StopBlink()
        {
            var cooldown = BlinkSettings.CooldownInSecondsPerCharge * _blinkCharges;
            _blinkCharges = 0;
            _blinkState = BlinkState.None;
            _currentBlinkCharge = 0;
            _blinkExecutionTimer = Timer.None;
            _accumulatedGravity = Vector3.zero;
            _blinkCooldown = Timer.FromSeconds(cooldown);
        }

        private void ProcessBlinkUpdate()
        {
            if (_blinkState != BlinkState.Charging)
            {
                return;
            }

            var unscaledDeltaTime = Time.unscaledDeltaTime;
            var chargePerSecond = BlinkSettings.ChargePerSeconds;
            var charge = chargePerSecond * unscaledDeltaTime;
            _currentBlinkCharge += charge;

            if (_currentBlinkCharge >= 1)
            {
                _currentBlinkCharge = 0;
                _blinkCharges++;
            }

            var staminaCost = StaminaCost.BarsPerSecond(chargePerSecond);
            var hasEnoughStamina = StaminaController.HasEnoughStaminaFor(staminaCost);

            if (hasEnoughStamina is false)
            {
                StartBlinkExecution();
                return;
            }

            StaminaController.ConsumeStamina(staminaCost);

            if (_inputs.BlinkInput.IsPressed() is false)
            {
                StartBlinkExecution();
                return;
            }

            if (_blinkCharges >= BlinkSettings.MaxCharges)
            {
                StartBlinkExecution();
            }
        }

        #endregion


        #region KKC: Maneuver

        private void ProcessManeuver(ref Vector3 currentVelocity, float deltaTime)
        {
            var requestManeuver = _isManeuver ? _maneuverPressedThisFrame : _maneuverPressed;
            var performManeuver = requestManeuver && CanPerformManeuver();

            if (performManeuver)
            {
                StartManeuver(ref currentVelocity);
            }

            if (_isManeuver)
            {
                var maneuverFactor = _maneuver.forceFactorOverTime.Evaluate(_elapsedManeuverTime);
                var maneuverForce = maneuverFactor * _calculatedManeuverForce;
                var maneuverVector = maneuverForce * _normalizedManeuverDirection;
                var maneuverVectorDelta = maneuverVector * deltaTime;
                currentVelocity += maneuverVectorDelta;
                _elapsedManeuverTime += deltaTime;
            }
        }

        private bool CanPerformManeuver()
        {
            if (_performedManeuverCount >= ManeuverSettings.ManeuverCount)
            {
                return false;
            }

            if (_maneuverCooldown.IsRunning)
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

            if (_performedManeuverCount < ManeuverSettings.ManeuverCount)
            {
                return true;
            }

            return false;
        }

        #endregion


        #region KKC: Start & Stop Maneuver

        private void StartManeuver(ref Vector3 currentVelocity)
        {
            _maneuver = _inputDirection switch
            {
                MovementDirection.None => ManeuverSettings.StandstillManeuver[_performedManeuverCount],
                MovementDirection.Forward => ManeuverSettings.ForwardManeuver[_performedManeuverCount],
                MovementDirection.Left => ManeuverSettings.SideManeuver[_performedManeuverCount],
                MovementDirection.Right => ManeuverSettings.SideManeuver[_performedManeuverCount],
                MovementDirection.Backward => ManeuverSettings.BackwardsManeuver[_performedManeuverCount],
                var _ => throw new ArgumentOutOfRangeException()
            };

            _currentManeuverType = _maneuver.type;

            currentVelocity.y = 0;

            var staminaCosts = _maneuver.staminaCost;
            var hasStamina = StaminaController.HasEnoughStaminaFor(staminaCosts);
            StaminaController.ConsumeStamina(staminaCosts);

            _normalizedManeuverDirection = GetManeuverDirection(_currentManeuverType);

            _isWeakManeuver = hasStamina is false;
            if (_isWeakManeuver)
            {
                _calculatedManeuverForce = _maneuver.weakForce;
            }
            else
            {
                _calculatedManeuverForce = _postSlideTimer.IsRunning
                    ? _maneuver.postSlideForce
                    : _maneuver.force;
            }

            _motor.ForceUnground();
            _isManeuver = true;
            _elapsedManeuverTime = 0;
            _maneuverCooldown = Timer.FromSeconds(_maneuver.maneuverCooldownInSeconds);
            _performedManeuverCount++;
            _accumulatedGravity = Vector3.zero;
            if (_currentManeuverType is ManeuverType.Dash)
            {
                _maneuverTimer = Timer.FromSeconds(_maneuver.minDurationInSeconds);
            }
        }

        private void StopManeuver()
        {
            if (_maneuverTimer.IsRunning)
            {
                return;
            }

            if (_isManeuver is false)
            {
                return;
            }

            if (_isWeakManeuver)
            {
                switch (_maneuver.type)
                {
                    case ManeuverType.Jump:
                        SlowController.AddSlow(ManeuverSettings.PostWeakJumpSlow);
                        break;
                    case ManeuverType.Dash:
                        SlowController.AddSlow(ManeuverSettings.PostWeakDashSlow);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _performedManeuverCount = 0;
            _isManeuver = false;
            _isWeakManeuver = false;
            _currentManeuverType = default(ManeuverType);
            _maneuverCooldown = Timer.None;
            _normalizedManeuverDirection = Vector3.zero;
            _maneuver = default(ManeuverSettings);
            _elapsedManeuverTime = 0;
            _calculatedManeuverForce = 0;
        }

        private Vector3 GetManeuverDirection(ManeuverType maneuverType)
        {
            var isJump = maneuverType is ManeuverType.Jump;
            var isSprint = _isSprinting;
            var isPostSlide = _postSlideTimer.IsRunning;

            return _inputDirection switch
            {
                MovementDirection.None when isJump => standStillJumpDirection.forward,
                MovementDirection.None => _motor.CharacterUp,

                MovementDirection.Forward when isJump && isPostSlide => postSlideJumpDirection.forward,
                MovementDirection.Forward when isJump && isSprint => sprintJumpDirection.forward,
                MovementDirection.Forward when isJump => forwardJumpDirection.forward,
                MovementDirection.Forward => _motor.CharacterForward,

                MovementDirection.Left when isJump => _motor.CharacterUp,
                MovementDirection.Left => -_motor.CharacterRight,

                MovementDirection.Right when isJump => _motor.CharacterUp,
                MovementDirection.Right => _motor.CharacterRight,

                MovementDirection.Backward when isJump => _motor.CharacterUp,
                MovementDirection.Backward => -_motor.CharacterForward,

                var _ => throw new ArgumentOutOfRangeException()
            };
        }

        #endregion


        #region KKC: Movement Input

        private float CalculateTargetMovementSpeed()
        {
            switch (_inputDirection)
            {
                case MovementDirection.Forward:
                    return _isSprinting
                        ? MovementSettings.MovementSpeedSprint
                        : MovementSettings.MovementSpeedForward;

                case MovementDirection.Left:
                case MovementDirection.Right:
                    return MovementSettings.MovementSpeedSide;

                case MovementDirection.Backward:
                    return MovementSettings.MovementSpeedBackward;
                default:
                    return 0;
            }
        }

        private void ProcessMovementInput(ref Vector3 currentVelocity, float deltaTime)
        {
            var wasSprinting = _isSprinting;
            _isSprinting = (MovementSettings.EnableSprint
                            && _inputs.SprintInput.IsPressed()
                            && !_isCrouching
                            && ((StaminaController.Stamina >= StaminaController.StaminaPerBar && !wasSprinting)
                                || (_isSprinting && StaminaController.Stamina > 0))
                            && _inputDirection is MovementDirection.Forward
                            && _motor.GroundingStatus.IsStableOnGround)
                           || (wasSprinting && _isManeuver);

            if (_isSprinting)
            {
                StaminaController.ConsumeStamina(StaminaSettings.StaminaCostSprint);
            }

            var targetMovementSpeed = CalculateTargetMovementSpeed();

            if (_isCrouching)
            {
                targetMovementSpeed *= CrouchSettings.CrouchMovementSpeedFactor;
            }

            var speedSharpness = _isSprinting
                ? MovementSettings.MovementSpeedIncreaseSharpness
                : MovementSettings.MovementSpeedDecaySharpness;

            SlowController.SlowSpeedValue(ref targetMovementSpeed);
            targetMovementSpeed = targetMovementSpeed.WithMinLimit(MovementSettings.MinimumMovementSpeed);

            _movementSpeed = Mathf.Lerp(_movementSpeed, targetMovementSpeed, deltaTime * speedSharpness);

            // Calculate oriented input based on movement input and forward rotation.
            var orientedInput = (_inputs.ForwardRotation * _inputs.MovementInput).normalized * _movementSpeed;

            // Separate the horizontal and vertical components of the current velocity.
            var horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);

            // Limit the horizontal velocity magnitude to prevent excessive speed.
            horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, 30);

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
                ? MovementSettings.MovementDirectionSharpness
                : MovementSettings.AirborneDirectionSharpness;

            // Adjust horizontal velocity towards the oriented input, tangent to the effective ground normal.
            // TODO: check magnitude but dont make it overpowered
            // var magnitude = _motor.GroundingStatus.IsStableOnGround
            //     ? orientedInput.magnitude
            //     : horizontalVelocity.magnitude;
            var magnitude = orientedInput.magnitude;
            var targetVelocity = _motor.GetDirectionTangentToSurface(orientedInput, effectiveGroundNormal) * magnitude;
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity,
                deltaTime * sharpness);

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
                var jumpFactor = _isManeuver ? _maneuver.gravityFactorOverTime.Evaluate(_elapsedManeuverTime) : 1f;
                var gravity = _motor.CharacterUp * (-GravitySettings.GravityForce * jumpFactor);

                _accumulatedGravity += gravity * deltaTime;

                // Apply a drag-like effect to simulate air resistance.
                var dragFactor = GravitySettings.AirResistance;
                if (_postSlideTimer.IsRunning)
                {
                    dragFactor *= _postSlideTimer.Delta();
                }
                _accumulatedGravity *= 1 - dragFactor * deltaTime;

                // Clamp the accumulated gravity to a maximum value.
                var maxGravityVelocity = GravitySettings.MaxGravityVelocity;
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


        #region KKC: External Force

        private Vector3 _externalForce;

        public void AddForce(Vector3 force, ForceFlags forceFlags = ForceFlags.None)
        {
            if (forceFlags.HasFlagUnsafe(ForceFlags.IgnoreWhenGrounded) && _motor.GroundingStatus.IsStableOnGround)
            {
                return;
            }

            if (forceFlags.HasFlagUnsafe(ForceFlags.GroundSensitive))
            {
                if (_motor.GroundingStatus.IsStableOnGround)
                {
                    force *= GravitySettings.GroundedForceFactor;
                }
                else if (_motor.GroundingStatus.FoundAnyGround)
                {
                    force *= GravitySettings.UnsatableForceFactor;
                }
            }

            if (forceFlags.HasFlagUnsafe(ForceFlags.ForceUnground))
            {
                _motor.ForceUnground();
            }

            if (forceFlags.HasFlagUnsafe(ForceFlags.KillGravity))
            {
                _accumulatedGravity = Vector3.zero;
            }

            _externalForce += force;
        }

        private void ProcessExternalForce(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity += _externalForce;
            _externalForce = Vector3.zero;
        }

        #endregion


        #region KKC: Crouch & Slide

        private void ProcessCrouchInput()
        {
            var isCrouchToggle = Controls.IsGamepadScheme
                ? SaveDataSettings.ToggleCrouchGamepadSetting.Value
                : SaveDataSettings.ToggleCrouchDesktopSetting.Value;

            var isCrouchReleasedThisFrame = _inputs.CrouchInput.WasReleasedThisFrame();
            var isCrouchPressedThisFrame = _inputs.CrouchInput.WasPressedThisFrame();

            HandleCrouchToggle(isCrouchToggle, isCrouchPressedThisFrame, isCrouchReleasedThisFrame);

            UpdateCharacterHeight();
        }

        private void HandleCrouchToggle(bool isCrouchToggle, bool isCrouchPressedThisFrame,
            bool isCrouchReleasedThisFrame)
        {
            if (isCrouchToggle)
            {
                ToggleCrouchOrSlide(isCrouchPressedThisFrame);
            }
            else
            {
                ProcessCrouchHold(isCrouchPressedThisFrame, isCrouchReleasedThisFrame);
            }
        }

        private void ToggleCrouchOrSlide(bool isCrouchPressedThisFrame)
        {
            if (!isCrouchPressedThisFrame)
            {
                return;
            }

            if (_isCrouching || _isSliding)
            {
                StopCrouch();
                StopSliding();
            }
            else
            {
                DetermineCrouchOrSlide();
            }
        }

        private void ProcessCrouchHold(bool isCrouchPressedThisFrame, bool isCrouchReleasedThisFrame)
        {
            if (isCrouchPressedThisFrame)
            {
                DetermineCrouchOrSlide();
            }

            if (isCrouchReleasedThisFrame is false)
            {
                return;
            }

            StopCrouch();

            if (_isSliding)
            {
                StopSliding();
            }
        }

        private void DetermineCrouchOrSlide()
        {
            if (_isSliding)
            {
                return;
            }

            if (ShouldStartSlide())
            {
                StartCrouch();
                StartSlide();
            }
            else
            {
                StartCrouch();
            }
        }

        private bool ShouldStartSlide()
        {
            return _isSprinting ||
                   _inputDirection is MovementDirection.Right or MovementDirection.Left ||
                   (!_isSprinting && CrouchSettings.RequireSprintForForwardSlide is false &&
                    _inputDirection is MovementDirection.Forward);
        }

        private void UpdateCharacterHeight()
        {
            var targetHeight = CalculateTargetHeight();
            _height = Mathf.Lerp(_height, targetHeight, Time.deltaTime * GetHeightAdjustmentSpeed());
            headTransform.localPosition = headTransform.localPosition.With(y: _height - .25f);
        }

        private float CalculateTargetHeight()
        {
            if (_motor.GroundingStatus.FoundAnyGround is false)
            {
                return _defaultHeight;
            }
            if (_isSliding)
            {
                return CrouchSettings.SlideHeight;
            }
            if (_isCrouching)
            {
                return CrouchSettings.CrouchHeight;
            }
            return _defaultHeight;
        }

        private float GetHeightAdjustmentSpeed()
        {
            return (_isCrouching && !_postSlideTimer.IsRunning) || _isSliding ? 25f : 15f;
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
            _slideTime = _isSprinting ? CrouchSettings.SlideDurationSprint : CrouchSettings.SlideDuration;
            _slideVelocity = _motor.Velocity;
            StaminaController.ConsumeStamina(StaminaSettings.StaminaCostSlide);
        }

        private void StopSliding()
        {
            _isSliding = false;
            _slideTime = 0;
            _slideVelocity = default(Vector3);
            _postSlideTimer = Timer.FromSeconds(CrouchSettings.PostSlideGraceTime);
        }

        private bool ProcessSlide(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_isSliding is false)
            {
                return false;
            }

            var downDotVelocity = Vector3.Dot(_motor.CharacterUp, Velocity.normalized);
            var slidingThreshold = CrouchSettings.SlideDownDotThreshold;
            _isSlidingDown = downDotVelocity < slidingThreshold;

            if (_isSlidingDown)
            {
                _slideTime += deltaTime;
                _slideTime = _slideTime.WithMaxLimit(_isSprinting
                    ? CrouchSettings.SlideDurationSprint
                    : CrouchSettings.SlideDuration);
            }
            else
            {
                _slideTime -= deltaTime;
            }
            if (_slideTime <= 0)
            {
                StopSliding();
                return true;
            }

            var magnitude = _slideVelocity.magnitude;
            var movementInput = _inputs.MovementInput;
            var forwardInput = _inputs.ForwardRotation;
            var orientedInput = (forwardInput * movementInput).normalized;
            var slideAdjustment = orientedInput * (deltaTime * CrouchSettings.SlideAdjustmentStrength);

            _slideVelocity += slideAdjustment;
            _slideVelocity = Vector3.ClampMagnitude(_slideVelocity, magnitude);

            if (_isSlidingDown)
            {
                _slideVelocity *= 1 + deltaTime * CrouchSettings.SlideDownVelocityIncrease;
                _slideVelocity = Vector3.ClampMagnitude(_slideVelocity, CrouchSettings.MaxSlideMagnitude);
            }
            else
            {
                var slideDuration = _isSprinting ? CrouchSettings.SlideDurationSprint : CrouchSettings.SlideDuration;
                var normalizedTime = 1 - _slideTime / slideDuration;
                var friction = CrouchSettings.SlideFriction *
                               CrouchSettings.SlideFrictionFactor.Evaluate(normalizedTime);
                var frictionFactor = 1 - friction * deltaTime;
                _slideVelocity *= frictionFactor;
            }

            currentVelocity = new Vector3(_slideVelocity.x, currentVelocity.y, _slideVelocity.z);
            return true;
        }

        #endregion


        #region KKC: Rotation

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_hasInputs is false)
            {
                return;
            }

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
                        deltaTime * MovementSettings.RotationSharpness);
                    break;

                case CameraMode.ThirdPerson when movementMagnitude > 0:
                {
                    var movementRotation = Quaternion.LookRotation(_inputs.MovementInput.normalized, Vector3.up);
                    var targetRotation = _inputs.ForwardRotation * movementRotation;
                    currentRotation = Quaternion.Slerp(currentRotation, targetRotation,
                        deltaTime * MovementSettings.RotationSharpness);

                    break;
                }
            }
        }

        #endregion


        #region KKC: Unused Callbacks

        public void PostGroundingUpdate(float deltaTime)
        {
        }

        public bool IsColliderValidForCollisions(Collider other)
        {
            return _ignoredCollider.Contains(other) is false;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
            StopManeuver();
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


        #region KKC: Teleport

        public void Teleport(Vector3 position, Quaternion rotation, bool bypassInterpolation = true)
        {
            _motor.SetPositionAndRotation(position, rotation, bypassInterpolation);
        }

        #endregion


        #region Time Scale

        private void ModifyTimeScale(ref float timescale)
        {
            ModifyBlinkTimeScale(ref timescale);
        }

        private void ModifyBlinkTimeScale(ref float timescale)
        {
            if (_blinkState is not BlinkState.Charging)
            {
                _timeScaleFactor = 1;
                return;
            }

            var deltaTime = Time.unscaledDeltaTime;

            var sharpness = BlinkSettings.TimeScaleFadeInSharpness;
            var chargeTimeScale = BlinkSettings.ChargeTimeScale;

            var deltaValue = deltaTime * sharpness;
            _timeScaleFactor = Mathf.Lerp(_timeScaleFactor, chargeTimeScale, deltaValue);

            timescale *= _timeScaleFactor;
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