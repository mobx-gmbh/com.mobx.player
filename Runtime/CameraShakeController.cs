using Cinemachine;
using MobX.Utilities.Types;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MobX.Player
{
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class CameraShakeController : MonoBehaviour
    {
        [SerializeField] [Required] private CameraShakeEvent cameraShakeEvent;
        private CinemachineVirtualCamera _virtualCamera;
        private CinemachineBasicMultiChannelPerlin _multiChannelPerlin;

        private Timer _shakeTimer;
        private CameraShake _cameraShake;

        private void Awake()
        {
            _virtualCamera = GetComponent<CinemachineVirtualCamera>();
            _multiChannelPerlin = _virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        }

        private void OnEnable()
        {
            cameraShakeEvent.Add(StartShake);
        }

        private void OnDisable()
        {
            cameraShakeEvent.Remove(StartShake);
        }

        private void StartShake(CameraShake cameraShake)
        {
            _cameraShake = cameraShake;
            _shakeTimer = Timer.FromSeconds(cameraShake.duration);
        }

        private void Update()
        {
            if (_shakeTimer.ExpiredOrNotRunning)
            {
                StopShake();
                return;
            }

            var delta = _shakeTimer.Delta();
            _multiChannelPerlin.m_AmplitudeGain = _cameraShake.amplitude.Evaluate(delta);
            _multiChannelPerlin.m_FrequencyGain = _cameraShake.frequency.Evaluate(delta);
        }

        private void StopShake()
        {
            _multiChannelPerlin.m_AmplitudeGain = 0;
            _multiChannelPerlin.m_FrequencyGain = 0;
            _shakeTimer = Timer.None;
        }
    }
}