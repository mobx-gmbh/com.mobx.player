using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    [Serializable]
    public struct StaminaCost
    {
        [SerializeField] private StaminaCostMode mode;

        [ShowIf(nameof(mode), StaminaCostMode.Flat)]
        [LabelText("Value")]
        [SerializeField] private float flatAmount;

        [ShowIf(nameof(mode), StaminaCostMode.PerSeconds)]
        [LabelText("Value")]
        [SerializeField] private float perSecondAmount;

        [Range(0, 100)]
        [ShowIf(nameof(mode), StaminaCostMode.Percentage)]
        [SerializeField] private float percentage;

        public StaminaCostMode Mode => mode;
        public float FlatAmount => flatAmount;
        public float PerSecondAmount => perSecondAmount;
        public float PercentageAmount => percentage;

        public static StaminaCost Flat(float value)
        {
            return new StaminaCost(StaminaCostMode.Flat, value, 0, 0);
        }

        public static StaminaCost PerSeconds(float value)
        {
            return new StaminaCost(StaminaCostMode.Flat, 0, 0, value);
        }

        public static StaminaCost Percentage(float value)
        {
            return new StaminaCost(StaminaCostMode.Percentage, 0, value, 0);
        }

        public static StaminaCost Bar()
        {
            return new StaminaCost(StaminaCostMode.Flat, 0, 0, 0);
        }

        private StaminaCost(StaminaCostMode mode, float flatAmount, float percentage, float perSecondAmount)
        {
            this.mode = mode;
            this.flatAmount = flatAmount;
            this.percentage = percentage;
            this.perSecondAmount = perSecondAmount;
        }
    }

    public enum StaminaCostMode
    {
        Flat = 0,
        PerSeconds = 1,
        RemainingBar = 2,
        Percentage = 3
    }
}