using MobX.CursorManagement;
using MobX.Inspector;
using MobX.Mediator.Settings;
using MobX.Mediator.Values;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace MobX.Player
{
    public class TopdownSettings : SettingsAsset
    {
        #region Fields

        [Foldout("Settings")]
        [Header("Speed")]
        [SerializeField] private float movementSpeed = 10f;
        [SerializeField] private float movementSpeedEdgeScrolling = .5f;
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float rotationSpeedMouse = 4f;
        [SerializeField] private float scrollSpeed = 5f;

        [Header("Sharpness")]
        [SerializeField] private float movementSharpness = 10f;
        [SerializeField] private float rotationSharpness = 10f;
        [SerializeField] private float rotationSharpnessMouse = 10f;
        [SerializeField] private float scrollSharpness = 10f;

        [Header("Edge Scrolling")]
        [SerializeField] private int edgeScrollingPixelTolerance = 25;

        [Header("Scrolling")]
        [SerializeField] private AnimationCurve scrollDistanceMovementSpeedFactor;
        [SerializeField] private Quaternion topRotation;
        [SerializeField] private Quaternion bottomRotation;
        [SerializeField] [Range(0, 1)] private float startScrollDelta = .7f;

        [Header("Constraints")]
        [SerializeField] private float maxDistanceFromCharacter = 100f;
        [SerializeField] private LayerMask environmentLayer;

        [Header("Cursor")]
        [SerializeField] private CursorType dragCursor;
        [SerializeField] private CursorType rotateCursor;
        [SerializeField] private CursorType clickCursor;

        [Foldout("Input")]
        [SerializeField] private InputActionReference clickInput;
        [SerializeField] private InputActionReference movementInput;
        [SerializeField] private InputActionReference rotationInput;
        [SerializeField] private InputActionReference mouseDelta;
        [SerializeField] private InputActionReference mousePosition;
        [SerializeField] private InputActionReference useMouseRotation;
        [SerializeField] private InputActionReference lockToCharacter;
        [SerializeField] private InputActionReference scrollInput;
        [SerializeField] private InputActionReference dragMovement;

        [Foldout("Persistent Settings")]
        [FormerlySerializedAs("isEdgeScrollingEnabled")]
        [SerializeField] private ValueAssetRO<bool> enableEdgePanning;
        [SerializeField] private ValueAssetRO<bool> confineCursorOnMouseRotation;
        [FormerlySerializedAs("cameraInputSensitivity")]
        [Header("Input Sensitivity")]
        [SerializeField] private ValueAssetRO<int> topdownCameraMovementSpeed;
        [FormerlySerializedAs("movementInputSensitivityEdge")]
        [SerializeField] private ValueAssetRO<int> topdownCameraEdgePanningSpeed;
        [FormerlySerializedAs("mouseRotationInputSensitivity")]
        [SerializeField] private ValueAssetRO<int> topdownCameraRotationSpeedMouse;
        [FormerlySerializedAs("rotationInputSensitivity")]
        [SerializeField] private ValueAssetRO<int> topdownCameraRotationSpeedButtons;

        #endregion


        #region Properites

        public InputActionReference ClickInput => clickInput;
        public InputActionReference MovementInput => movementInput;
        public InputActionReference RotationInput => rotationInput;
        public InputActionReference MouseDelta => mouseDelta;
        public InputActionReference UseMouseRotation => useMouseRotation;
        public InputActionReference LockToCharacter => lockToCharacter;
        public InputActionReference MousePosition => mousePosition;
        public InputActionReference ScrollInput => scrollInput;
        public InputActionReference DragMovement => dragMovement;

        public CursorType DragCursor => dragCursor;
        public CursorType RotateCursor => rotateCursor;
        public CursorType ClickCursor => clickCursor;

        public int CameraMovementSpeed => topdownCameraMovementSpeed.Value;
        public int CameraEdgePanningSpeed => topdownCameraEdgePanningSpeed.Value;
        public int CameraRotationSpeedMouse => topdownCameraRotationSpeedMouse.Value;
        public int CameraRotationSpeedButtons => topdownCameraRotationSpeedButtons.Value;
        public float MaxDistanceFromCharacter => maxDistanceFromCharacter;
        public float MovementSpeed => movementSpeed;
        public float MovementSpeedEdgeScrolling => movementSpeedEdgeScrolling;
        public float RotationSpeed => rotationSpeed;
        public float RotationSpeedMouse => rotationSpeedMouse;
        public bool ConfineCursorOnMouseRotation => confineCursorOnMouseRotation.Value;
        public bool EnableEdgePanning => enableEdgePanning.Value;
        public AnimationCurve ScrollDistanceMovementSpeedFactor => scrollDistanceMovementSpeedFactor;
        public float MovementSharpness => movementSharpness;
        public float RotationSharpness => rotationSharpness;
        public float RotationSharpnessMouse => rotationSharpnessMouse;
        public int EdgeScrollingPixelTolerance => edgeScrollingPixelTolerance;
        public float ScrollSpeed => scrollSpeed;
        public float ScrollSharpness => scrollSharpness;
        public Quaternion TopRotation => topRotation;
        public Quaternion BottomRotation => bottomRotation;
        public float StartScrollDelta => startScrollDelta;
        public LayerMask EnvironmentLayer => environmentLayer;

        #endregion
    }
}