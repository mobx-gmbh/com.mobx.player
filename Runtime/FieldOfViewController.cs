using Cinemachine;
using MobX.Mediator.Values;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MobX.Player
{
    [ExecuteInEditMode]
    public class FieldOfViewController : MonoBehaviour
    {
        [SerializeField] [Required] private FieldOfViewModifierList fieldOfViewModifier;
        [SerializeField] [Required] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] [Required] private ValueAssetRO<int> fieldOfView;

        private void Update()
        {
            var fieldOfViewValue = (float) fieldOfView.Value;
            var fieldOfViewUnmodified = fieldOfView.Value;
            foreach (var modifier in fieldOfViewModifier)
            {
                modifier.ModifyFieldOfView(ref fieldOfViewValue, fieldOfViewUnmodified);
            }

            virtualCamera.m_Lens.FieldOfView = fieldOfViewValue;
        }
    }
}