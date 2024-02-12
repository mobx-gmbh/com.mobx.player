using Drawing;
using MobX.Player.Locomotion;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MobX.Player
{
    /// <summary>
    ///     Represents a character that can be controlled by the player.
    ///     This class does not contain logic. It can be used to access multiple player character components.
    /// </summary>
    [SelectionBase]
    [RequireComponent(typeof(LocomotionController))]
    public class PlayerCharacter : MonoBehaviour, IDrawGizmos
    {
        #region Inspector

        [Header("References")]
        [SerializeField] [Required] private PlayerCharacterValueAsset characterAsset;

        [Header("Camera Controller")]
        [SerializeField] [Required] private TopdownCameraController topdownCameraController;
        [SerializeField] [Required] private FirstPersonCameraController firstPersonCameraController;
        [SerializeField] [Required] private ThirdPersonCameraController thirdPersonCameraController;
        [SerializeField] [Required] private Transform freeCameraDefaultPosition;

        [Header("Locomotion")]
        [SerializeField] [Required] private LocomotionController locomotionController;

        #endregion


        #region Properties

        public FirstPersonCameraController FirstPersonCameraController => firstPersonCameraController;
        public ThirdPersonCameraController ThirdPersonCameraController => thirdPersonCameraController;
        public TopdownCameraController TopdownCameraController => topdownCameraController;

        public LocomotionController LocomotionController => locomotionController;

        public Transform FreeCameraDefaultPosition => freeCameraDefaultPosition;
        public Transform ExternalCameraFolder { get; private set; }

        #endregion


        #region Startup

        private void Awake()
        {
            ExternalCameraFolder = new GameObject($"{name}.{nameof(ExternalCameraFolder)}").transform;
            characterAsset.Value = this;
        }

        private void OnDestroy()
        {
            characterAsset.Value = null;
        }

        #endregion


        #region Gizmos

        protected PlayerCharacter()
        {
            DrawingManager.Register(this);
        }

        public void DrawGizmos()
        {
        }

        #endregion
    }
}