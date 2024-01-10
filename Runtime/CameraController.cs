using Cinemachine;
using MobX.Inspector;
using MobX.Mediator.Callbacks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MobX.Player
{
    public abstract class CameraController : MonoBehaviour
    {
        #region Inspector

        [Foldout("References")]
        [SerializeField] [InlineInspector] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] [Required] private PlayerCharacterValueAsset playerCharacter;

        #endregion


        #region Properties

        public CinemachineVirtualCamera VirtualCamera => virtualCamera;
        protected static Camera Camera => Camera.main;
        protected bool IsActive { get; private set; }
        protected PlayerCharacter Character => playerCharacter.Value;

        #endregion


        #region Selection Callbacks

        // ReSharper disable Unity.PerformanceAnalysis
        public void Activate()
        {
            ActivateInternal();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void Deactivate()
        {
            DeactivateInternal();
        }

        private void OnDisable()
        {
            VirtualCamera.Priority = 0;
            IsActive = false;
            Gameloop.LateUpdate -= OnCameraLateUpdate;
            Gameloop.Update -= OnCameraUpdate;
        }

        #endregion


        #region Camera Callbacks

        /// <summary>
        ///     Called on an active camera controller during LateUpdate.
        /// </summary>
        protected abstract void OnCameraLateUpdate();

        /// <summary>
        ///     Called on an active camera controller during Update.
        /// </summary>
        protected abstract void OnCameraUpdate();

        /// <summary>
        ///     Called when the camera controller is enabled and will receive inputs.
        /// </summary>
        protected abstract void OnCameraEnabled();

        /// <summary>
        ///     Called when the camera controller is disabled and will no longer receive inputs.
        /// </summary>
        protected abstract void OnCameraDisabled();

        #endregion


        #region Internal

        private void ActivateInternal()
        {
            if (IsActive)
            {
                return;
            }
            IsActive = true;
            EnableCameraController();
        }

        private void DeactivateInternal()
        {
            if (IsActive is false)
            {
                return;
            }
            IsActive = false;
            DisableCameraController();
        }

        private void EnableCameraController()
        {
            VirtualCamera.Priority = 100;
            OnCameraEnabled();
            Gameloop.LateUpdate += OnCameraLateUpdate;
            Gameloop.Update += OnCameraUpdate;
        }

        private void DisableCameraController()
        {
            VirtualCamera.Priority = 0;
            Gameloop.LateUpdate -= OnCameraLateUpdate;
            Gameloop.Update -= OnCameraUpdate;
            OnCameraDisabled();
        }

        #endregion
    }
}