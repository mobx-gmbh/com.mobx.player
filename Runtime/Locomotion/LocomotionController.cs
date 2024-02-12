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
    [SuppressMessage("ReSharper", "ConvertToAutoPropertyWithPrivateSetter")]
    public class LocomotionController : MonoBehaviour, ICharacterController, IDrawGizmos, IForceReceiver,
        IFieldOfViewModifier
    {
        #region Fields

        [SerializeField] [Required] private LocomotionSettings settings;

        [Header("Mediator")]
        [SerializeField] [Required] private FieldOfViewModifierList fieldOfViewModifier;
        [SerializeField] [Required] private CameraShakeEvent cameraShakeEvent;

        [Header("Transforms")]
        [SerializeField] [Required] private Transform centerOfGravity;
        [SerializeField] [Required] private Transform headTransform;
        [SerializeField] [Required] private Transform standStillJumpDirection;
        [SerializeField] [Required] private Transform postSlideJumpDirection;
        [SerializeField] [Required] private Transform sprintJumpDirection;
        [SerializeField] [Required] private Transform forwardJumpDirection;

        private readonly List<Collider> _ignoredCollider = new();
        private KinematicCharacterMotor _motor;
        private SlowController SlowController { get; set; }

        // Basic Input Fields
        private bool _hasInputs;
        private MovementDirection _inputDirection;
        private CameraInputs _inputs;

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
        private float _movementMagnitude;
        private float _targetMovementMagnitude;
        private float _inputMovementSpeed;

        // Sliding Mechanics
        private float _slideTime;
        private Vector3 _slideVelocity;
        private float _slideMagnitude;
        private bool _isWeakSlide;

        // Jump Mechanics
        private float _jumpElapsedTime;
        private Vector3 _jumpDirection;

        // Dash Mechanics
        private Vector3 _dashDirection;
        private float _dashForce;
        private float _dashElapsedTime;
        private float _dashDuration;

        // Maneuver Mechanics
        private bool _requireHardInputForNextManeuver;
        private bool _maneuverPressedThisFrame;
        private bool _wasManeuverExecutedThisFrame;
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
        [Monitor]
        private BlinkState _blinkState;
        [Monitor]
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
        [Monitor] private bool _unbiasedGrounded;

        // Thrustdown
        private bool _isThrustDown;
        private Vector3 _accumulatedThrustDownForce;
        private float _thrustDownForceTimer;

        #endregion


        #region Properties

        private MovementLocomotionSettings MovementSettings => settings.MovementSettings;
        private GravityLocomotionSettings GravitySettings => settings.GravitySettings;
        private ManeuverLocomotionSettings ManeuverSettings => settings.ManeuverSettings;
        private CrouchLocomotionSettings CrouchSettings => settings.CrouchSettings;
        private BlinkLocomotionSettings BlinkSettings => settings.BlinkSettings;
        private ThrustDownLocomotionSettings ThrustDownSettings => settings.ThrustDownSettings;
        private InputLocomotionSettings InputSettings => settings.InputSettings;
        private SaveDataLocomotionSettings SaveDataSettings => settings.SaveDataSettings;

        public Quaternion Rotation => transform.rotation;
        public Vector3 TransientPosition => _motor.TransientPosition;
        public Vector3 Forward => _motor.CharacterForward;
        public Vector3 Velocity => transform.position - _lastPosition;
        public Vector3 CenterOfGravity => centerOfGravity.position;

        public KinematicCharacterMotor Motor => _motor;

        #endregion


        #region Setup & Shutdown

        private void Awake()
        {
            Assert.IsNotNull(settings);

            SlowController = GetComponent<SlowController>();
            _motor = GetComponent<KinematicCharacterMotor>();
            _motor.CharacterController = this;
            var childCollider = GetComponentsInChildren<Collider>(true);
            _ignoredCollider.AddRange(childCollider);
            _defaultHeight = _motor.Capsule.height;
            _height = _defaultHeight;
            this.StartMonitoring();

            fieldOfViewModifier.Add(this);
            Gameloop.AddTimeScaleModifier(ModifyTimeScale);
        }

        private void OnDestroy()
        {
            this.StopMonitoring();
            _ignoredCollider.Clear();
            fieldOfViewModifier.Remove(this);

            Gameloop.RemoveTimeScaleModifier(ModifyTimeScale);
        }

        #endregion


        #region Camera Inputs

        public void SetCameraInputs(in CameraInputs inputs)
        {
            _inputs = inputs;
            _hasInputs = true;
            if (InputSettings.ManeuverInput.action.WasPressedThisFrame())
            {
                _maneuverPressedThisFrame = true;
            }
            if (InputSettings.ManeuverInput.action.IsPressed())
            {
                _maneuverPressed = true;
            }

            if (InputSettings.BlinkInput.action.WasPressedThisFrame())
            {
                _blinkPressedThisFrame = true;
            }

            ProcessCrouchInput();
        }

        #endregion


        #region Input Direction & Speed Setup

        private void UpdateInputMovementSpeed()
        {
            var inputVector = _inputs.MovementInput.normalized;
            if (inputVector.magnitude <= 0.1f)
            {
                _inputMovementSpeed = 0;
                return;
            }

            var forwardsMovementSpeed = MovementSettings.MovementSpeedForward;
            var backwardsMovementSpeed = MovementSettings.MovementSpeedBackward;
            var sideMovementSpeed = MovementSettings.MovementSpeedSide;

            var angle = Mathf.Atan2(inputVector.x, inputVector.z) * Mathf.Rad2Deg;
            angle = (angle + 360) % 360;

            var offset = MovementSettings.MovementSpeedTransitionAngle * .5f;

            // Forwards
            if (angle > 315 + offset || angle < 45 - offset)
            {
                _inputMovementSpeed = forwardsMovementSpeed;
                return;
            }

            // Forwards to Right
            if (angle >= 45 - offset && angle <= 45 + offset)
            {
                var min = 45 - offset;
                var max = 45 + offset;
                var delta = Mathf.InverseLerp(min, max, angle);
                _inputMovementSpeed = Mathf.Lerp(forwardsMovementSpeed, sideMovementSpeed, delta);
                return;
            }

            // Right
            if (angle > 45 + offset && angle < 135 - offset)
            {
                _inputMovementSpeed = sideMovementSpeed;
                return;
            }

            // Right to Backward
            if (angle >= 135 - offset && angle <= 135 + offset)
            {
                var min = 135 - offset;
                var max = 135 + offset;
                var delta = Mathf.InverseLerp(min, max, angle);
                _inputMovementSpeed = Mathf.Lerp(sideMovementSpeed, backwardsMovementSpeed, delta);
                return;
            }

            // Backward
            if (angle > 135 + offset && angle < 225 - offset)
            {
                _inputMovementSpeed = backwardsMovementSpeed;
                return;
            }

            // Backward to Left
            if (angle >= 225 - offset && angle <= 225 + offset)
            {
                var min = 225 - offset;
                var max = 225 + offset;
                var delta = Mathf.InverseLerp(min, max, angle);
                _inputMovementSpeed = Mathf.Lerp(backwardsMovementSpeed, sideMovementSpeed, delta);
                return;
            }

            // Left
            if (angle > 225 + offset && angle < 315 - offset)
            {
                _inputMovementSpeed = sideMovementSpeed;
                return;
            }

            // Left to Forward
            if (angle >= 315 - offset && angle <= 315 + offset)
            {
                var min = 315 - offset;
                var max = 315 + offset;
                var delta = Mathf.InverseLerp(min, max, angle);
                _inputMovementSpeed = Mathf.Lerp(sideMovementSpeed, forwardsMovementSpeed, delta);
            }
        }

        private void SetupInputDirection()
        {
            var inputVector = _inputs.MovementInput;
            if (inputVector.magnitude <= 0.1f)
            {
                _inputDirection = MovementDirection.None;
                return;
            }

            // Calculate the angle in degrees
            var angle = Mathf.Atan2(inputVector.x, inputVector.z) * Mathf.Rad2Deg;

            // Normalize the angle to be between 0 and 360
            angle = (angle + 360) % 360;

            switch (angle)
            {
                // Determine the direction based on the angle
                case <= 45:
                case >= 315:
                    _inputDirection = MovementDirection.Forward;
                    break;
                case > 45 and < 135:
                    _inputDirection = MovementDirection.Right;
                    break;
                case >= 135 and <= 225:
                    _inputDirection = MovementDirection.Backward;
                    break;
                case > 225 and < 315:
                    _inputDirection = MovementDirection.Left;
                    break;
                default:
                    _inputDirection = MovementDirection.None;
                    break;
            }
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

            var ray = new Ray(_motor.Transform.position, -_motor.Transform.up);
            _unbiasedGrounded = Physics.Raycast(ray, MovementSettings.GroundCheckDistance,
                MovementSettings.UnbiasedGroundLayer);

            SetupInputDirection();
            UpdateInputMovementSpeed();
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            _lastPosition = transform.position;

            _maneuverPressed = false;
            _maneuverPressedThisFrame = false;
            _blinkPressedThisFrame = false;
            _wasManeuverExecutedThisFrame = false;
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
                ProcessManeuver(ref currentVelocity, deltaTime);
                ProcessExternalForce(ref currentVelocity, deltaTime);
                return;
            }

            ProcessMovementInput(ref currentVelocity, deltaTime);
            ProcessGravity(ref currentVelocity, deltaTime);
            ProcessManeuver(ref currentVelocity, deltaTime);
            ProcessThrustDown(ref currentVelocity, deltaTime);
            ProcessExternalForce(ref currentVelocity, deltaTime);
        }

        private void Update()
        {
            ProcessBlinkUpdate();
            ProcessMovementUpdate();
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

        private float _blinkFieldOfViewOffset;
        private float _blinkChargeFieldOfView;

        public void ModifyFieldOfView(ref float fieldOfView, float unmodifiedFieldOfView)
        {
            _blinkChargeFieldOfView = Mathf.Lerp(_blinkChargeFieldOfView, 0, Time.deltaTime * 10);
            _blinkFieldOfViewOffset = Mathf.Lerp(_blinkFieldOfViewOffset, 0, Time.deltaTime * 10);

            if (_blinkState == BlinkState.Blinking)
            {
                _blinkFieldOfViewOffset =
                    settings.BlinkSettings.FieldOfViewOffsetOverTime.Evaluate(_blinkExecutionTimer.Delta());
            }
            if (_blinkState == BlinkState.Charging)
            {
                var max = settings.BlinkSettings.MaxCharges;
                var delta = (_currentBlinkCharge + _blinkCharges) / max;
                _blinkChargeFieldOfView = settings.BlinkSettings.ChargeFieldOfViewOffsetOverTime.Evaluate(delta);
            }

            fieldOfView += _blinkChargeFieldOfView;
            fieldOfView += _blinkFieldOfViewOffset;
        }

        private void ProcessBlinkUpdate()
        {
            if (_blinkState is not BlinkState.Charging)
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

            if (InputSettings.BlinkInput.action.IsPressed() is false)
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


        #region KKC: Thrustdown

        private void ProcessThrustDown(ref Vector3 currentVelocity, float deltaTime)
        {
            if (ShouldStartThrustDown())
            {
                StartThrustDown();
            }

            if (_isThrustDown)
            {
                var forceDirection = -_motor.CharacterUp;
                var timerFactor = ThrustDownSettings.DownwardForceCurve.Evaluate(_thrustDownForceTimer);
                var force = ThrustDownSettings.DownwardForce * deltaTime * timerFactor;
                var forceVector = forceDirection * force;
                _accumulatedThrustDownForce += forceVector;

                var maxMagnitude = ThrustDownSettings.MaxDownwardForceMagnitude;
                currentVelocity += _accumulatedThrustDownForce;
                currentVelocity = Vector3.ClampMagnitude(currentVelocity, maxMagnitude);
                _thrustDownForceTimer += deltaTime;

                _accumulatedGravity = Vector3.zero;
            }
        }

        private void StartThrustDown()
        {
            _isThrustDown = true;
            _requireHardInputForNextManeuver = true;
            _thrustDownForceTimer = 0;
            _accumulatedThrustDownForce = Vector3.zero;
        }

        private void StopThrustDown()
        {
            if (_isThrustDown is false)
            {
                return;
            }

            _isThrustDown = false;
            cameraShakeEvent.Raise(ThrustDownSettings.CameraShake);
            ForceSystem.Singleton.AddForceAtPosition(_motor.Transform.position, ThrustDownSettings.ForceSettings);
        }

        private bool ShouldStartThrustDown()
        {
            if (_isThrustDown)
            {
                return false;
            }
            if (_unbiasedGrounded)
            {
                return false;
            }
            if (_motor.GroundingStatus.FoundAnyGround)
            {
                return false;
            }

            if (ThrustDownSettings.ActivationMode.HasFlagUnsafe(ThrustDownActivation.LastManeuver))
            {
                if (CheckManeuver())
                {
                    return true;
                }
            }

            if (ThrustDownSettings.ActivationMode.HasFlagUnsafe(ThrustDownActivation.Input))
            {
                if (InputSettings.ThrustDown.action.IsPressed() is false)
                {
                    return false;
                }

                var ray = new Ray(_motor.Transform.position, -_motor.Transform.up);
                if (Physics.Raycast(ray, ThrustDownSettings.MinHeight, ThrustDownSettings.GroundCheckLayer))
                {
                    return false;
                }

                return true;
            }

            return false;

            bool CheckManeuver()
            {
                if (_maneuverPressedThisFrame is false)
                {
                    return false;
                }
                if (_wasManeuverExecutedThisFrame)
                {
                    return false;
                }
                if (_performedManeuverCount < ManeuverSettings.ManeuverCount)
                {
                    return false;
                }

                var ray = new Ray(_motor.Transform.position, -_motor.Transform.up);
                if (Physics.Raycast(ray, ThrustDownSettings.MinHeight, ThrustDownSettings.GroundCheckLayer))
                {
                    return false;
                }

                return true;
            }
        }

        #endregion


        #region KKC: Maneuver

        private void ProcessManeuver(ref Vector3 currentVelocity, float deltaTime)
        {
            var requestManeuver = _isManeuver || _requireHardInputForNextManeuver
                ? _maneuverPressedThisFrame
                : _maneuverPressed;
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
            switch (_inputDirection)
            {
                case MovementDirection.None when _unbiasedGrounded:
                    _maneuver = ManeuverSettings.StandstillManeuverGrounded[_performedManeuverCount];
                    break;
                case MovementDirection.None:
                    _maneuver = ManeuverSettings.StandstillManeuverAirborne[_performedManeuverCount];
                    break;

                case MovementDirection.Forward when _unbiasedGrounded:
                    _maneuver = ManeuverSettings.ForwardManeuverGrounded[_performedManeuverCount];
                    break;
                case MovementDirection.Forward:
                    _maneuver = ManeuverSettings.ForwardManeuverAirborne[_performedManeuverCount];
                    break;

                case MovementDirection.Left when _unbiasedGrounded:
                case MovementDirection.Right when _unbiasedGrounded:
                    _maneuver = ManeuverSettings.SideManeuverGrounded[_performedManeuverCount];
                    break;
                case MovementDirection.Left:
                case MovementDirection.Right:
                    _maneuver = ManeuverSettings.SideManeuverAirborne[_performedManeuverCount];
                    break;

                case MovementDirection.Backward when _unbiasedGrounded:
                    _maneuver = ManeuverSettings.BackwardsManeuverGrounded[_performedManeuverCount];
                    break;
                case MovementDirection.Backward:
                    _maneuver = ManeuverSettings.BackwardsManeuverAirborne[_performedManeuverCount];
                    break;
                case var _:
                    throw new ArgumentOutOfRangeException();
            }

            _currentManeuverType = _maneuver.type;

            currentVelocity.y = 0;

            _normalizedManeuverDirection = GetManeuverDirection(_currentManeuverType);
            _calculatedManeuverForce = _isWeakManeuver ? _maneuver.weakForce : _maneuver.force;

            if (_postSlideTimer.IsRunning)
            {
                var bonusForce = _maneuver.postSlideBonusForce;
                var min = CrouchSettings.MinSlideMagnitude;
                var max = CrouchSettings.MaxSlideMagnitude;
                var slideMagnitudeDelta = Mathf.InverseLerp(min, max, _slideMagnitude);
                bonusForce *= _maneuver.postSlideMagnitudeFactor.Evaluate(slideMagnitudeDelta);
                _calculatedManeuverForce += bonusForce;
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

            _wasManeuverExecutedThisFrame = true;
            _requireHardInputForNextManeuver = false;
        }

        private void StopManeuver()
        {
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

        private void ProcessMovementInput(ref Vector3 currentVelocity, float deltaTime)
        {
            var wasSprinting = _isSprinting;
            _isSprinting = (MovementSettings.EnableSprint
                            && InputSettings.SprintInput.action.IsPressed()
                            && !_isCrouching
                            && (!wasSprinting
                                || _isSprinting)
                            && _inputDirection is MovementDirection.Forward
                            && _motor.GroundingStatus.IsStableOnGround)
                           || (wasSprinting && _isManeuver);

            var targetMovementSpeed =
                _isSprinting ? settings.MovementSettings.MovementSpeedSprint : _inputMovementSpeed;

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

            _targetMovementMagnitude = _motor.GroundingStatus.IsStableOnGround
                ? orientedInput.magnitude
                : horizontalVelocity.magnitude;

            if (_movementMagnitude < orientedInput.magnitude)
            {
                _movementMagnitude = orientedInput.magnitude;
            }
            if (_slideMagnitude < CrouchSettings.MinSlideMagnitudeToOvercomeMovementLimit)
            {
                _movementMagnitude = orientedInput.magnitude;
            }

            //var magnitude = orientedInput.magnitude;
            var surfaceTangent = _motor.GetDirectionTangentToSurface(orientedInput, effectiveGroundNormal);
            var targetVelocity = surfaceTangent * _movementMagnitude;
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity,
                deltaTime * sharpness);

            // Reassemble the current velocity with the adjusted horizontal component.
            currentVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);
        }

        private void ProcessMovementUpdate()
        {
            _movementMagnitude = Mathf.Lerp(_movementMagnitude, _targetMovementMagnitude, Time.deltaTime * 5);
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
            _externalForce = Vector3.ClampMagnitude(_externalForce, 15);
        }

        private void ProcessExternalForce(ref Vector3 currentVelocity, float deltaTime)
        {
            _externalForce =
                Vector3.ClampMagnitude(_externalForce, settings.MovementSettings.MaxExternalForceMagnitude);
            currentVelocity += _externalForce;
            _externalForce = Vector3.zero;
            currentVelocity = Vector3.ClampMagnitude(currentVelocity, settings.MovementSettings.MaxMagnitude);
        }

        #endregion


        #region KKC: Crouch & Slide

        private void ProcessCrouchInput()
        {
            var isCrouchToggle = Controls.IsGamepadScheme
                ? SaveDataSettings.ToggleCrouchGamepadSetting.Value
                : SaveDataSettings.ToggleCrouchDesktopSetting.Value;

            var isCrouchReleasedThisFrame = InputSettings.CrouchInput.action.WasReleasedThisFrame();
            var isCrouchPressedThisFrame = InputSettings.CrouchInput.action.WasPressedThisFrame();

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

            StopManeuver();
            _requireHardInputForNextManeuver = true;
        }

        private void StopSliding()
        {
            _isSliding = false;
            _slideTime = 0;
            _slideVelocity = default(Vector3);
            _postSlideTimer = Timer.FromSeconds(CrouchSettings.PostSlideGraceTime);
            if (_isWeakSlide)
            {
                SlowController.AddSlow(CrouchSettings.PostWeakSlideSlow);
            }
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

            _slideMagnitude = _slideVelocity.magnitude;

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
            var isAiming = InputSettings.AimInput.action.IsPressed();

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


        #region KKC: Ground Hit

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
            if (_maneuverTimer.IsRunning is false)
            {
                StopManeuver();
            }
            StopThrustDown();
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


        public void KillGravity()
        {
            _accumulatedGravity = default(Vector3);
        }
    }
}