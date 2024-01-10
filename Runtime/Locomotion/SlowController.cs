using KinematicCharacterController;
using MobX.Utilities;
using MobX.Utilities.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    [ExecuteAfter(typeof(KinematicCharacterMotor))]
    public class SlowController : MonoBehaviour
    {
        private readonly List<SlowEntry> _slows = new();

        public void SlowSpeedValue(ref float speed)
        {
            var unmodifiedMovementSpeed = speed;

            var time = Time.time;
            foreach (var slowEntry in _slows)
            {
                var slowDelta = Mathf.InverseLerp(slowEntry.StartTimeStamp, slowEntry.EndTimeStamp, time);
                var slowIntensity = slowEntry.Strength * slowEntry.Curve.Evaluate(slowDelta);

                speed -= unmodifiedMovementSpeed.GetPercentage(slowIntensity);
            }
        }

        private void LateUpdate()
        {
            UpdateSlowLifeTimes();
        }

        private void UpdateSlowLifeTimes()
        {
            var time = Time.time;
            for (var index = _slows.Count - 1; index >= 0; index--)
            {
                if (_slows[index].EndTimeStamp <= time)
                {
                    _slows.RemoveAt(index);
                }
            }
        }

        public void AddSlow(Slow slow)
        {
            var time = Time.time;
            var startTimeStamp = time;
            var endTimeStamp = time + slow.duration;

            var slowEntry = new SlowEntry(
                slow.strength,
                startTimeStamp,
                endTimeStamp,
                slow.curve
            );

            _slows.Add(slowEntry);
        }
    }
}