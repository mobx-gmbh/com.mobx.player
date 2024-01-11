using MobX.Mediator.Callbacks;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace MobX.Player.Environment
{
    public class LookAtPlayer : MonoBehaviour
    {
        [SerializeField] [Required] private CameraValueAsset cameraAsset;

        private Camera _camera;
        private Action _update;

        private void OnEnable()
        {
            cameraAsset.Changed += OnCameraChanged;
            OnCameraChanged(cameraAsset.Value);
        }

        private void OnDisable()
        {
            cameraAsset.Changed -= OnCameraChanged;
            OnCameraChanged(null);
        }

        private void OnCameraChanged(Camera mainCamera)
        {
            _update ??= OnUpdate;
            _camera = mainCamera;
            if (_camera == null)
            {
                Gameloop.Update -= _update;
            }
            else
            {
                Gameloop.Update -= _update;
                Gameloop.Update += _update;
            }
        }

        private void OnUpdate()
        {
            var target = _camera.transform;
            transform.rotation = Quaternion.LookRotation(transform.position - target.position);
        }
    }
}