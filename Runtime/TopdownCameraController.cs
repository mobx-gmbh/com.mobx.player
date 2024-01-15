using MobX.CursorManagement;
using MobX.DevelopmentTools.Visualization;
using MobX.Inspector;
using MobX.Mediator.Values;
using MobX.Utilities;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

namespace MobX.Player
{
    public class TopdownCameraController : CameraController, IHideCursor, ILockCursor
    {
        #region Fields

        [SerializeField] [InlineInspector] private TopdownSettings settings;
        [Header("Mediator")]
        [SerializeField] [Required] private ValueAsset<Quaternion> targetRotation;
        [SerializeField] [Required] private ValueAsset<Quaternion> rotationRotation;
        [SerializeField] [Required] private ValueAsset<float> targetScroll;
        [SerializeField] [Required] private HideCursorLocks cursorHide;
        [SerializeField] [Required] private LockCursorLocks cursorLock;
        [Header("References")]
        [SerializeField] [RequiredIn(PrefabKind.PrefabInstance)]
        private PlayerCharacter character;
        [SerializeField] [Required] private SplineContainer spline;

        private Transform _transform;
        private Vector3 _targetPosition;
        private float _scrollDelta;
        private bool _enableHeightPositionCalculation = true;
        private Vector3 _dragOrigin;
        private float _forceHeightCheckTimer;
        private bool _isEdgeScrolling;
        private bool _inputGainedThisFrame;
        private Transform _targetTransform;
        private const float ForcedHeightCheckDuration = .5f;

        #endregion


        #region Setup & Shutdown

        private void Awake()
        {
            _transform = transform;
            _targetTransform = character.transform;
            _transform.position = _targetTransform.position;
            _targetPosition = _transform.position;
            targetScroll.Value = settings.StartScrollDelta;
            _scrollDelta = targetScroll.Value;

            var bottomRotation = settings.BottomRotation;
            var topRotation = settings.TopRotation;
            VirtualCamera.transform.position = spline.EvaluatePosition(targetScroll.Value);
            VirtualCamera.transform.localRotation = Quaternion.Lerp(bottomRotation, topRotation, targetScroll.Value);
        }

        private void Start()
        {
            _transform.SetParent(character.ExternalCameraFolder);
        }

        private void OnDestroy()
        {
            settings.ScrollInput.action.performed -= UpdateScrollPosition;
        }

        #endregion


        #region Callbacks

        public readonly ref struct TopdownCameraInputData
        {
            public readonly bool IsClickPressed;
            public readonly bool IsClickStart;
            public readonly bool IsClickEnd;
            public readonly bool LockToPlayer;
            public readonly bool IsDragPressed;
            public readonly bool IsDragStart;
            public readonly bool IsDragEnd;
            public readonly bool IsMouseRotation;
            public readonly bool IsMouseRotationStart;
            public readonly bool IsMouseRotationEnd;
            public readonly Vector2 MouseDelta;
            public readonly Vector2 MousePosition;
            public readonly float RotationInput;
            public readonly Vector2 MovementInput;

            public TopdownCameraInputData(
                bool isClickPressed,
                bool isClickStart,
                bool isClickEnd,
                bool lockToPlayer,
                bool isDragPressed,
                bool isDragStart,
                bool isDragEnd,
                bool isMouseRotation,
                bool isMouseRotationStart,
                bool isMouseRotationEnd,
                Vector2 mouseDelta,
                Vector2 mousePosition,
                float rotationInput,
                Vector2 movementInput)
            {
                IsClickPressed = isClickPressed;
                IsClickStart = isClickStart;
                IsClickEnd = isClickEnd;
                LockToPlayer = lockToPlayer;
                IsDragPressed = isDragPressed;
                IsDragStart = isDragStart;
                IsDragEnd = isDragEnd;
                IsMouseRotation = isMouseRotation;
                IsMouseRotationStart = isMouseRotationStart;
                IsMouseRotationEnd = isMouseRotationEnd;
                MouseDelta = mouseDelta;
                MousePosition = mousePosition;
                RotationInput = rotationInput;
                MovementInput = movementInput;
            }
        }

        protected override void OnCameraLateUpdate()
        {
            var inputData = new TopdownCameraInputData(
                settings.ClickInput.action.IsPressed(),
                settings.ClickInput.action.WasPerformedThisFrame(),
                settings.ClickInput.action.WasReleasedThisFrame(),
                settings.LockToCharacter.action.IsPressed(),
                settings.DragMovement.action.IsPressed(),
                settings.DragMovement.action.WasPerformedThisFrame(),
                settings.DragMovement.action.WasReleasedThisFrame(),
                settings.UseMouseRotation.action.IsPressed(),
                settings.UseMouseRotation.action.WasPressedThisFrame(),
                settings.UseMouseRotation.action.WasReleasedThisFrame(),
                settings.MouseDelta.action.ReadValue<Vector2>(),
                settings.MousePosition.action.ReadValue<Vector2>(),
                settings.RotationInput.action.ReadValue<float>(),
                settings.MovementInput.action.ReadValue<Vector2>()
            );

            UpdateCursor(in inputData);

            if (inputData.IsDragPressed || inputData.IsDragEnd)
            {
                ExecuteDragMovement(in inputData);
                return;
            }

            UpdateTargetPosition(in inputData);
            UpdateTargetRotation(in inputData);
            UpdateTargetScrollPosition(in inputData);

            UpdatePositionAndRotation(in inputData);
            UpdateScrollPosition();

            _inputGainedThisFrame = false;
            rotationRotation.Value = _transform.rotation;
        }

        protected override void OnCameraUpdate()
        {
        }

        protected override void OnCameraDisabled()
        {
            _targetPosition = _transform.position;
            targetRotation.Value = _transform.rotation;
            targetScroll.Value = _scrollDelta;

            cursorLock.Remove(this);
            cursorHide.Remove(this);
            CursorManager.Singleton.RemoveCursorOverride(settings.DragCursor);
            CursorManager.Singleton.RemoveCursorOverride(settings.RotateCursor);
            CursorManager.Singleton.RemoveCursorOverride(settings.ClickCursor);

            settings.ScrollInput.action.performed -= UpdateScrollPosition;
            _enableHeightPositionCalculation = true;
        }

        protected override void OnCameraEnabled()
        {
            _inputGainedThisFrame = true;
            settings.ScrollInput.action.performed += UpdateScrollPosition;
            _transform.rotation = targetRotation.Value;
            _scrollDelta = targetScroll.Value;
            _enableHeightPositionCalculation = true;
            UpdateScrollPosition();
        }

        #endregion


        #region Drag Movement

        private void ExecuteDragMovement(in TopdownCameraInputData input)
        {
            var ray = Camera.ScreenPointToRay(input.MousePosition);

            var groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));

            if (!groundPlane.Raycast(ray, out var enter))
            {
                return;
            }

            var hitPoint = ray.GetPoint(enter);

            if (Vector3.Distance(hitPoint, Camera.transform.position) > 50)
            {
                _dragOrigin = hitPoint;
                return;
            }

            if (Vector3.Distance(hitPoint, _targetTransform.position) > settings.MaxDistanceFromCharacter + 20)
            {
                _dragOrigin = hitPoint;
                return;
            }

            if (input.IsDragStart)
            {
                _dragOrigin = hitPoint;
            }

            var screenPoint = Camera.WorldToScreenPoint(_dragOrigin);
            var newRay = Camera.ScreenPointToRay(screenPoint);
            if (groundPlane.Raycast(newRay, out enter))
            {
                _dragOrigin = newRay.GetPoint(enter);
            }

            var dragDelta = hitPoint - _dragOrigin;
            _targetPosition -= dragDelta;
            _transform.position = _targetPosition;

            if (input.IsDragEnd)
            {
                _targetPosition -= dragDelta;
            }
        }

        #endregion


        #region Target Rotation

        private void UpdateTargetRotation(in TopdownCameraInputData input)
        {
            var useMouseRotation = input.IsMouseRotation;
            var rotationAxis = useMouseRotation
                ? input.MouseDelta.x * .001f
                : input.RotationInput * Time.deltaTime;

            var rotationSpeed = useMouseRotation
                ? settings.RotationSpeedMouse * settings.CameraRotationSpeedMouse
                : settings.RotationSpeed * settings.CameraRotationSpeedButtons;

            var rotationValue = rotationAxis * rotationSpeed;
            var rotationVector = targetRotation.Value.eulerAngles + new Vector3(0, rotationValue, 0);
            targetRotation.Value = Quaternion.Euler(rotationVector);
        }

        private float CalculateTargetY()
        {
            if (_enableHeightPositionCalculation is false)
            {
                return _targetTransform.position.y;
            }

            const QueryTriggerInteraction Interaction = QueryTriggerInteraction.Ignore;
            var layer = settings.EnvironmentLayer;
            var targetPosition = _targetPosition;

            var sphereRadius = .25f;
            var sphereColor = new Color(0f, 1f, 0f, 0.5f);
            var spherePosition = _targetPosition + Vector3.up;
            var deltaTime = Time.deltaTime;

            _forceHeightCheckTimer -= deltaTime;
            if (Physics.CheckSphere(spherePosition, sphereRadius, layer))
            {
                _forceHeightCheckTimer = ForcedHeightCheckDuration;
            }

            if (_forceHeightCheckTimer > 0)
            {
                var height = VirtualCamera.transform.localPosition.y + 1;
                var downRayCastOrigin = targetPosition + Vector3.up * height;
                sphereColor = new Color(1f, 0f, 0f, 0.5f);

                if (Physics.Raycast(downRayCastOrigin, Vector3.down, out var downHit, height, layer, Interaction))
                {
                    Visualize.RedLine(downRayCastOrigin, downHit.point);
                    return downHit.point.y;
                }
            }

            Visualize.Sphere(spherePosition, sphereRadius, sphereColor);

            var upDistance = Vector3.up * 2;
            var upRayCastOrigin = targetPosition + upDistance;

            if (Physics.Raycast(upRayCastOrigin, Vector3.down, out var upHit, 50f, layer, Interaction))
            {
                Visualize.GreenLine(upRayCastOrigin, upHit.point);
                return upHit.point.y;
            }

            return _targetTransform.position.y;
        }

        #endregion


        #region Target Position

        private bool IsPositionInputFaulted(in TopdownCameraInputData input)
        {
            return _inputGainedThisFrame && input.MousePosition == Vector2.zero;
        }

        private void UpdateTargetPosition(in TopdownCameraInputData input)
        {
            if (input.IsDragEnd)
            {
                _forceHeightCheckTimer = ForcedHeightCheckDuration;
            }

            if (input.LockToPlayer)
            {
                _targetPosition = _targetTransform.position;
                return;
            }

            var inputValue = CalculateMovementVector(input);

            var deltaTime = Time.deltaTime;
            var inputVector = new Vector3(inputValue.x, 0, inputValue.y);

            var rotation = _transform.rotation;
            var scrollFactor = settings.ScrollDistanceMovementSpeedFactor.Evaluate(_scrollDelta);
            var movementSpeed = _isEdgeScrolling
                ? settings.MovementSpeedEdgeScrolling * settings.CameraEdgePanningSpeed
                : settings.MovementSpeed * settings.CameraMovementSpeed;
            var movementVector = rotation * inputVector * (movementSpeed * scrollFactor * deltaTime);

            var targetPosition = _targetPosition + movementVector;
            _targetPosition = targetPosition;
        }

        private Vector2 CalculateMovementVector(in TopdownCameraInputData input)
        {
            var inputValue = input.MovementInput;

            if (!Application.isFocused
                || input.IsMouseRotation
                || settings.EnableEdgePanning is false)
            {
                _isEdgeScrolling = false;
                return inputValue;
            }

            if (IsPositionInputFaulted(in input))
            {
                return inputValue;
            }

            var mousePosition = input.MousePosition;
            var isMouseRight = Math.Abs(mousePosition.x - Screen.width) < settings.EdgeScrollingPixelTolerance;
            if (isMouseRight)
            {
                inputValue.x += 1;
                inputValue.y += -1 + mousePosition.y / Screen.height * 2;
                _isEdgeScrolling = true;
                return inputValue;
            }

            var isMouseLeft = mousePosition.x <= settings.EdgeScrollingPixelTolerance;
            if (isMouseLeft)
            {
                inputValue.x += -1;
                inputValue.y += -1 + mousePosition.y / Screen.height * 2;
                _isEdgeScrolling = true;
                return inputValue;
            }

            var isMouseTop = Math.Abs(mousePosition.y - Screen.height) < settings.EdgeScrollingPixelTolerance;
            if (isMouseTop)
            {
                inputValue.x += -1 + mousePosition.x / Screen.width * 2;
                inputValue.y += 1;
                _isEdgeScrolling = true;
                return inputValue;
            }

            var isMouseBottom = mousePosition.y <= settings.EdgeScrollingPixelTolerance;
            if (isMouseBottom)
            {
                inputValue.x += -1 + mousePosition.x / Screen.width * 2;
                inputValue.y += -1;
                _isEdgeScrolling = true;
                return inputValue;
            }

            _isEdgeScrolling = false;
            return inputValue;
        }

        #endregion


        #region Apply Target Position & Rotation

        private void UpdatePositionAndRotation(in TopdownCameraInputData input)
        {
            var characterPosition = _targetTransform.position;
            var cameraPosition = _targetPosition;
            var deltaTime = Time.deltaTime;

            var distance = Vector3.Distance(cameraPosition, characterPosition);
            var isOvershoot = distance > settings.MaxDistanceFromCharacter;

            if (isOvershoot)
            {
                var difference = cameraPosition - characterPosition;
                var maxDistance = settings.MaxDistanceFromCharacter;
                var desiredPosition = characterPosition + difference.normalized * maxDistance;
                _targetPosition = desiredPosition;
            }

            var position = _transform.position;
            var positionSharpness = settings.MovementSharpness * deltaTime;

            var targetPositionY = input.LockToPlayer ? _targetPosition.y : CalculateTargetY();
            _targetPosition.y = Mathf.Lerp(_targetPosition.y, targetPositionY, deltaTime * 5f);

            _transform.position = Vector3.Lerp(position, _targetPosition, positionSharpness);

            var useMouseRotation = input.IsMouseRotation;
            var rotationSharpness = useMouseRotation
                ? settings.RotationSharpnessMouse * deltaTime
                : settings.RotationSharpness * deltaTime;

            _transform.rotation = Quaternion.Lerp(_transform.rotation, targetRotation.Value, rotationSharpness);
        }

        #endregion


        #region Scroll Position

        private void UpdateScrollPosition(InputAction.CallbackContext context)
        {
            const float ScrollBreak = .0001f;
            var scrollInput = context.action.ReadValue<Vector2>();
            targetScroll.Value -= scrollInput.y * ScrollBreak * settings.ScrollSpeed;
            targetScroll.Value = targetScroll.Value.Clamp(0, 1);
        }

        private void UpdateTargetScrollPosition(in TopdownCameraInputData input)
        {
            var delta = settings.ScrollSharpness * Time.deltaTime;
            _scrollDelta = Mathf.Lerp(_scrollDelta, targetScroll.Value, delta);
        }

        private void UpdateScrollPosition()
        {
            var topRotation = settings.TopRotation;
            var bottomRotation = settings.BottomRotation;
            VirtualCamera.transform.position = spline.EvaluatePosition(_scrollDelta);
            VirtualCamera.transform.localRotation = Quaternion.Lerp(bottomRotation, topRotation, _scrollDelta);
        }

        #endregion


        #region Cursor

        private void UpdateCursor(in TopdownCameraInputData input)
        {
            if (input.IsDragEnd)
            {
                CursorManager.Singleton.RemoveCursorOverride(settings.DragCursor);
            }
            if (input.IsMouseRotationEnd)
            {
                CursorManager.Singleton.RemoveCursorOverride(settings.RotateCursor);
                cursorLock.Remove(this);
                cursorHide.Remove(this);
            }
            if (input.IsClickEnd)
            {
                CursorManager.Singleton.RemoveCursorOverride(settings.ClickCursor);
            }

            if (input.IsDragPressed)
            {
                CursorManager.Singleton.AddCursorOverride(settings.DragCursor);
                return;
            }

            if (input.IsMouseRotation)
            {
                CursorManager.Singleton.AddCursorOverride(settings.RotateCursor);
                if (settings.ConfineCursorOnMouseRotation)
                {
                    cursorLock.Add(this);
                    cursorHide.Add(this);
                }
                return;
            }

            if (input.IsClickPressed)
            {
                CursorManager.Singleton.AddCursorOverride(settings.ClickCursor);
            }
        }

        #endregion
    }
}