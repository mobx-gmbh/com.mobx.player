using MobX.Mediator;
using MobX.Mediator.Cooldown;
using MobX.Utilities;
using QFSW.QC;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

namespace MobX.Player.Locomotion
{
    public class StaminaController : MonoBehaviour
    {
        [Header("Mediator")]
        [SerializeField] [Required] private LocomotionSettings settings;
        [SerializeField] [Required] private FloatValueAsset currentStamina;
        [SerializeField] [Required] private FloatValueAsset maximumStamina;
        [SerializeField] [Required] private CooldownAsset staminaRegenerationCooldown;

        public float Stamina => currentStamina.Value;
        public float MaximumStamina => maximumStamina.Value;
        public int StaminaPerBar => settings.StaminaPerBar;

        [Preserve]
        [Command("add-max-stamina")]
        private void IncrementStaminaCommand(int staminaBars = 1)
        {
            maximumStamina.Value += settings.StaminaPerBar * staminaBars;
        }

        [Preserve]
        [Command("remove-max-stamina")]
        private void DecrementStaminaCommand(int staminaBars = 1)
        {
            maximumStamina.Value -= settings.StaminaPerBar * staminaBars;
        }

        private void Start()
        {
            maximumStamina.Value = settings.StaminaPerBar * settings.StaminaBars;
            currentStamina.Value = maximumStamina.Value;
            staminaRegenerationCooldown.Value = settings.StaminaRegenerationCooldown;
        }

        public bool HasEnoughStamina(StaminaCost cost)
        {
            var amount = GetFlatAmount(cost);
            return currentStamina.Value >= amount;
        }

        public void ConsumeStamina(StaminaCost cost)
        {
            var amount = GetFlatAmount(cost);
            ConsumeStaminaInternal(amount);
        }

        public void RestoreStamina(float amount)
        {
            var staminaValue = (currentStamina.Value + amount).WithMaxLimit(maximumStamina.Value);
            currentStamina.Value = staminaValue;
        }

        private void ConsumeStaminaInternal(float amount)
        {
            var staminaValue = (currentStamina.Value - amount).WithMinLimit(0);
            currentStamina.Value = staminaValue;
            staminaRegenerationCooldown.Restart();
        }

        private float GetFlatAmount(StaminaCost cost)
        {
            return cost.Mode switch
            {
                StaminaCostMode.Flat => cost.FlatAmount,
                StaminaCostMode.PerSeconds => cost.PerSecondAmount * Time.deltaTime,
                StaminaCostMode.RemainingBar => GetRemainingStaminaInCurrentBar(),
                StaminaCostMode.Percentage => MaximumStamina.GetPercentage(cost.PercentageAmount),
                var _ => default(float)
            };
        }

        public float GetRemainingStaminaInCurrentBar()
        {
            var subAmount = settings.StaminaPerBar;
            var remainder = currentStamina.Value % subAmount;

            if (remainder == 0)
            {
                return subAmount;
            }

            return remainder;
        }

        private void Update()
        {
            UpdateStamina();
        }

        private void UpdateStamina()
        {
            if (staminaRegenerationCooldown.IsRunning)
            {
                return;
            }
            var value = currentStamina.Value;
            var increase = settings.StaminaRegenerationSpeed * Time.deltaTime;
            var staminaValue = (value + increase).WithMaxLimit(maximumStamina.Value);
            currentStamina.Value = staminaValue;
        }
    }
}