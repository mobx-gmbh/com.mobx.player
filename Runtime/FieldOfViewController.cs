using Cinemachine;
using MobX.Mediator.Values;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MobX.Player
{
    [ExecuteInEditMode]
    public class FieldOfViewController : MonoBehaviour
    {
        [SerializeField] [Required] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] [Required] private ValueAssetRO<int> fieldOfView;

        private void OnEnable()
        {
            fieldOfView.Changed += UpdateFieldOfView;
            UpdateFieldOfView(fieldOfView.Value);
        }

        private void OnDisable()
        {
            fieldOfView.Changed -= UpdateFieldOfView;
        }

        private void UpdateFieldOfView(int fov)
        {
            virtualCamera.m_Lens.FieldOfView = fov;
        }
    }
}