using MobX.Mediator.Generation;
using System;
using UnityEngine;

namespace MobX.Player
{
    [Serializable]
    [GenerateMediator(MediatorTypes.EventAsset)]
    public struct CameraShake
    {
        public AnimationCurve frequency;
        public AnimationCurve amplitude;
        public float duration;
    }
}