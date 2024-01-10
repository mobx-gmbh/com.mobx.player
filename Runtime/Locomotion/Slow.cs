using System;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    [Serializable]
    public struct Slow
    {
        [Range(0, 100)] public float strength;
        public float duration;
        public AnimationCurve curve;
    }

    public readonly struct SlowEntry
    {
        public readonly float Strength;
        public readonly float StartTimeStamp;
        public readonly float EndTimeStamp;
        public readonly AnimationCurve Curve;

        public SlowEntry(float strength, float startTimeStamp, float endTimeStamp, AnimationCurve curve)
        {
            Strength = strength;
            StartTimeStamp = startTimeStamp;
            EndTimeStamp = endTimeStamp;
            Curve = curve;
        }
    }
}