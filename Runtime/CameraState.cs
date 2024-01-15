using MobX.Inspector;
using MobX.Mediator.Provider;
using MobX.Mediator.States;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MobX.Player
{
    public abstract class CameraState : State<CameraState>
    {
        [Foldout("State")]
        [Header("Mediator")]
        [SerializeField] [Required] private PlayerCharacterValueAsset playerCharacter;
        [SerializeField] [Required] private LockAsset playerRepresentationBlocker;
        [SerializeField] [Required] private CameraValueAsset mainCamera;

        [Header("Input")]
        [SerializeField] [Required] private InputState inputState;

        [Header("Settings")]
        [SerializeField] private LayerMask cullingMask = -1;
        [SerializeField] private bool showLocalPlayerRepresentation;

        protected PlayerCharacter PlayerCharacter => playerCharacter.Value;

        protected sealed override void OnStateEnter(CameraState previousState)
        {
            inputState.Activate();
            if (mainCamera.Value != null)
            {
                mainCamera.Value.cullingMask = cullingMask;
            }
            if (showLocalPlayerRepresentation is false)
            {
                playerRepresentationBlocker.Add(this);
            }
            OnCameraStateEnter(previousState);
        }

        protected sealed override void OnStateExit(CameraState nextState)
        {
            inputState.Deactivate();
            if (showLocalPlayerRepresentation is false)
            {
                playerRepresentationBlocker.Remove(this);
            }
            OnCameraStateExit(nextState);
        }

        protected sealed override void OnStateEnabled()
        {
            inputState.Activate();
            if (mainCamera.Value != null)
            {
                mainCamera.Value.cullingMask = cullingMask;
            }
            OnCameraStateEnabled();
        }

        protected sealed override void OnStateDisabled()
        {
            inputState.Deactivate();
            OnCameraStateDisabled();
        }

        protected abstract void OnCameraStateEnter(CameraState previousState);

        protected abstract void OnCameraStateExit(CameraState nextState);

        protected virtual void OnCameraStateEnabled()
        {
        }

        protected virtual void OnCameraStateDisabled()
        {
        }
    }
}