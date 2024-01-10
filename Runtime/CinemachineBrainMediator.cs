using Cinemachine;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace MobX.Player
{
    [RequireComponent(typeof(CinemachineBrain))]
    public class CinemachineBrainMediator : MonoBehaviour
    {
        [SerializeField] private bool overrideBlend;
        [ShowIf(nameof(overrideBlend))]
        [SerializeField] private CinemachineBlendDefinition blendDefinitionOverride;
        [SerializeField] [Required] private CinemachineBrain cinemachineBrain;
        [SerializeField] [Required] private CinemachineBrainValueAsset cinemachineBrainBrainAsset;

        private void Awake()
        {
            cinemachineBrainBrainAsset.Value = cinemachineBrain;
        }

        private IEnumerator Start()
        {
            yield return null;
            if (overrideBlend)
            {
                cinemachineBrain.m_DefaultBlend = blendDefinitionOverride;
            }
        }
    }
}