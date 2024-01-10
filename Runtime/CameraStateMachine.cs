using MobX.Inspector;
using MobX.Mediator.Callbacks;
using MobX.Mediator.States;
using MobX.Utilities;
using MobX.Utilities.Types;
using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MobX.Player
{
    public class CameraStateMachine : StateMachine<CameraState>
    {
        [Foldout("Input")]
        [SerializeField] private InputActionReference nextCameraInput;
        [SerializeField] private InputActionReference previousCameraInput;
        [SerializeField] [Required] private InputActionReference enableFirstPersonCamera;
        [SerializeField] [Required] private InputActionReference enableThirdPersonCamera;
        [SerializeField] [Required] private InputActionReference enableTopdownCamera;
        [SerializeField] [Required] private InputActionReference enableFreeCamera;
        [Foldout("Camera States")]
        [SerializeField] [Required] private CameraState firstPersonCameraState;
        [SerializeField] [Required] private CameraState thirdPersonCameraState;
        [SerializeField] [Required] private CameraState topdownCameraState;
        [SerializeField] [Required] private CameraState freeCameraState;

        private DynamicLoop _stateIndex;
        private CameraState[] _selectableCameraStates;

        [CallbackOnInitialization]
        private void Initialize()
        {
            nextCameraInput.action.performed += OnNextCameraInput;
            previousCameraInput.action.performed += OnPreviousCameraInput;

            enableFirstPersonCamera.action.performed += OnSelectFirstPersonPressed;
            enableThirdPersonCamera.action.performed += OnSelectThirdPersonPressed;
            enableTopdownCamera.action.performed += OnSelectTopdownPressed;
            enableFreeCamera.action.performed += OnSelectFreeCameraPressed;

            StateChanged += OnStateChanged;

            _selectableCameraStates = new[]
            {
                firstPersonCameraState,
                thirdPersonCameraState,
                topdownCameraState,
                freeCameraState
            };

            _stateIndex = _selectableCameraStates.Contains(State)
                ? DynamicLoop.Create(_selectableCameraStates.IndexOf(State), _selectableCameraStates)
                : DynamicLoop.Create(_selectableCameraStates);
        }

        private void OnStateChanged(CameraState previousState, CameraState nextState)
        {
            if (_selectableCameraStates.Contains(nextState))
            {
                var index = _selectableCameraStates.IndexOf(nextState);
                _stateIndex = DynamicLoop.Create(index, _selectableCameraStates);
            }
        }

        [CallbackOnApplicationQuit]
        private void Shutdown()
        {
            nextCameraInput.action.performed -= OnNextCameraInput;
            previousCameraInput.action.performed -= OnPreviousCameraInput;

            enableFirstPersonCamera.action.performed -= OnSelectFirstPersonPressed;
            enableThirdPersonCamera.action.performed -= OnSelectThirdPersonPressed;
            enableTopdownCamera.action.performed -= OnSelectTopdownPressed;
            enableFreeCamera.action.performed -= OnSelectFreeCameraPressed;
        }

        private void OnNextCameraInput(InputAction.CallbackContext context)
        {
            _stateIndex++;
            var next = _selectableCameraStates[_stateIndex];
            next.Activate();
        }

        private void OnPreviousCameraInput(InputAction.CallbackContext context)
        {
            _stateIndex--;
            var previous = _selectableCameraStates[_stateIndex];
            previous.Activate();
        }

        private void OnSelectFirstPersonPressed(InputAction.CallbackContext context)
        {
            firstPersonCameraState.Activate();
        }

        private void OnSelectThirdPersonPressed(InputAction.CallbackContext context)
        {
            thirdPersonCameraState.Activate();
        }

        private void OnSelectTopdownPressed(InputAction.CallbackContext context)
        {
            topdownCameraState.Activate();
        }

        private void OnSelectFreeCameraPressed(InputAction.CallbackContext context)
        {
            freeCameraState.Activate();
        }
    }
}