using MobX.CursorManagement;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MobX.Player
{
    public class FreeCameraController : CameraController, IHideCursor, ILockCursor
    {
        #region Fields

        [SerializeField] [Required] private FreeCameraSettings settings;

        private float _speed;
        private float _rotationX;
        private int _frameCount;
        private Vector3 _targetDirection;

        #endregion


        #region Public

        public float Speed { get; private set; }

        #endregion


        #region Logic

        protected void Awake()
        {
            _speed = settings.StartSpeed;
        }

        private void OnSpeedInputAxis(InputAction.CallbackContext context)
        {
            var normalizedSpeed = Mathf.InverseLerp(settings.MinSpeed, settings.MaxSpeed, _speed);
            var speedMultiplier = settings.SpeedCurve.Evaluate(normalizedSpeed);
            var value = context.action.ReadValue<float>() * .01f;
            _speed += value * speedMultiplier;
            _speed = Mathf.Clamp(_speed, settings.MinSpeed, settings.MaxSpeed);
        }

        protected override void OnCameraLateUpdate()
        {
            HandleMouseRotation();

            var direction = CalculateDirectionVector();
            _targetDirection =
                Vector3.Lerp(_targetDirection, direction, settings.AccelerationSharpness * Time.deltaTime);
            transform.Translate(_targetDirection * Time.deltaTime);
            _frameCount++;
        }

        protected override void OnCameraUpdate()
        {
        }

        private Vector3 CalculateDirectionVector()
        {
            var acceleration = settings.MovementInput.action.ReadValue<Vector3>();
            var breakValue = settings.BreakInput.action.IsPressed() ? settings.BreakSpeedMultiplier : 1f;
            var sprintValue = settings.SprintInput.action.IsPressed() ? settings.SprintSpeedMultiplier : 1f;

            Speed = _speed * breakValue * sprintValue;

            return acceleration.normalized * Speed;
        }

        private void HandleMouseRotation()
        {
            var rotationInput = _frameCount < 2 ? Vector2.zero : settings.LookInput.action.ReadValue<Vector2>();
            var self = transform;

            var rotationHorizontal = settings.MouseSensitivity * rotationInput.x;
            var rotationVertical = settings.MouseSensitivity * rotationInput.y;

            self.Rotate(Vector3.up * rotationHorizontal, Space.World);

            var rotationY = self.localEulerAngles.y;

            _rotationX += rotationVertical;
            _rotationX = Mathf.Clamp(_rotationX, -settings.MaxXAngle, settings.MaxXAngle);

            self.localEulerAngles = new Vector3(-_rotationX, rotationY, 0);
        }

        #endregion


        #region Camera Callbacks

        protected override void OnCameraDisabled()
        {
            settings.SpeedInput.action.performed -= OnSpeedInputAxis;
        }

        protected override void OnCameraEnabled()
        {
            settings.SpeedInput.action.performed += OnSpeedInputAxis;
        }

        #endregion


        private void OnDestroy()
        {
            settings.SpeedInput.action.performed -= OnSpeedInputAxis;
        }
    }
}