using MobX.Mediator.Callbacks;
using MobX.Utilities.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MobX.Player
{
    [ExecutionOrder(-1000)]
    [RequireComponent(typeof(Camera))]
    public class CameraMediator : MonoBehaviour
    {
        [SerializeField] [Required] private Camera mainCamera;
        [SerializeField] [Required] private CameraValueAsset cameraAsset;

        private void Awake()
        {
            cameraAsset.Value = mainCamera;
        }

        private void OnDestroy()
        {
            if (Gameloop.IsQuitting)
            {
                return;
            }
            if (cameraAsset.Value == mainCamera)
            {
                cameraAsset.Value = null;
            }
        }
    }
}