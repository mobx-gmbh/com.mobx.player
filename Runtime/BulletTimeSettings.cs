using System;
using UnityEngine;

namespace MobX.Player
{
    [Serializable]
    public struct BulletTimeSettings
    {
        public AnimationCurve timeScaleOverTime;
        public float duration;
        public bool useUnscaledDuration;
    }
}