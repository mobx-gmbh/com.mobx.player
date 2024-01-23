using MobX.Mediator.Settings;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    public class GravityLocomotionSettings : SettingsAsset
    {
        [Header("Gravity")]
        [SerializeField] private float gravityForce = 9.81f;
        [SerializeField] private float maxGravityVelocity = 1;
        [SerializeField] private float airResistance = 1;
        [Header("External Force")]
        [SerializeField] private float groundedForceFactor = .25f;
        [SerializeField] private float unsatableForceFactor = .6f;

        public float GravityForce => gravityForce;
        public float MaxGravityVelocity => maxGravityVelocity;
        public float AirResistance => airResistance;
        public float GroundedForceFactor => groundedForceFactor;
        public float UnsatableForceFactor => unsatableForceFactor;
    }
}