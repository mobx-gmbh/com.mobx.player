using MobX.Mediator.Settings;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    public class StaminaLocomotionSettings : SettingsAsset
    {
        [SerializeField] private StaminaCost staminaCostSprint = StaminaCost.PerSeconds(10);
        [SerializeField] private StaminaCost staminaCostSlide = StaminaCost.Flat(10);
        [SerializeField] private int staminaPerBar = 25;
        [SerializeField] private int staminaBars = 6;
        [Tooltip("Duration in seconds until stamina regeneration starts after stamina was consumed")]
        [SerializeField] private float staminaRegenerationCooldown = 3f;
        [Tooltip("How much stamina is regenerated per second")]
        [SerializeField] private float staminaRegenerationSpeed = 50f;

        public StaminaCost StaminaCostSprint => staminaCostSprint;
        public StaminaCost StaminaCostSlide => staminaCostSlide;
        public int StaminaPerBar => staminaPerBar;
        public int StaminaBars => staminaBars;
        public float StaminaRegenerationCooldown => staminaRegenerationCooldown;
        public float StaminaRegenerationSpeed => staminaRegenerationSpeed;
    }
}