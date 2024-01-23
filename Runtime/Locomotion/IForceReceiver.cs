using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    [Flags]
    public enum ForceFlags
    {
        None = 0,
        IgnoreWhenGrounded = 1,
        GroundSensitive = 2,
        ForceUnground = 4,
        KillGravity = 8
    }

    public interface IForceReceiver
    {
        public void AddForce(Vector3 force, ForceFlags forceFlags = ForceFlags.None);

        public Vector3 CenterOfGravity { get; }
    }

    public enum ForceType
    {
        Immediate = 0,
        Shockwave = 1
    }

    [Serializable]
    public struct ForceSettings
    {
        public ForceType type;
        public float force;
        public float radius;
        public LayerMask layer;
        public ForceFlags flags;
        public AnimationCurve forceCurve;
        public Vector3 explosionOffset;
        [ShowIf(nameof(type), ForceType.Shockwave)]
        public float shockwaveSpeed;
    }
}