using MobX.DevelopmentTools.Visualization;
using MobX.Mediator.Callbacks;
using MobX.Mediator.Singleton;
using MobX.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace MobX.Player.Locomotion
{
    public class ForceSystem : SingletonAsset<ForceSystem>
    {
        private struct ForceInstance
        {
            public Vector3 Position;
            public ForceSettings Settings;
            public float ShockwaveDistance;
        }

        private readonly List<ForceInstance> _forceInstances = new();
        private readonly Collider[] _collisions = new Collider[128];

        public void AddForceAtPosition(Vector3 position, ForceSettings forceSettings)
        {
            if (forceSettings.type is ForceType.Immediate)
            {
                ApplyForceImmediate(position, in forceSettings);
                return;
            }
            _forceInstances.Add(new ForceInstance
            {
                Position = position,
                Settings = forceSettings,
                ShockwaveDistance = 0
            });
        }

        [CallbackOnUpdate]
        private void OnUpdate()
        {
            var deltaTime = Time.deltaTime;
            for (var index = _forceInstances.Count - 1; index >= 0; index--)
            {
                var instance = _forceInstances[index];
                var settings = instance.Settings;
                var explosionPoint = instance.Position + settings.explosionOffset;
                var distance = instance.ShockwaveDistance + settings.shockwaveSpeed * deltaTime;

                var size = Physics.OverlapSphereNonAlloc(explosionPoint, distance, _collisions, settings.layer);

                for (var collisionIndex = 0; collisionIndex < size; collisionIndex++)
                {
                    var collision = _collisions[collisionIndex];
                    if (collision.TryGetComponent<IForceReceiver>(out var forceReceiver))
                    {
                        var center = forceReceiver.CenterOfGravity;
                        var direction = center - explosionPoint;
                        var distanceDelta = Mathf.InverseLerp(0, settings.radius, direction.magnitude);
                        var forceFactor = settings.forceCurve.Evaluate(distanceDelta);
                        var force = direction.normalized * (settings.force * forceFactor) * deltaTime;
                        forceReceiver.AddForce(force, settings.flags);
                    }
                }

                Visualize.Sphere(explosionPoint, distance.WithMaxLimit(settings.radius), new Color(1f, .5f, 0f, 0.3f));
                Visualize.Sphere(explosionPoint, .1f, new Color(1f, .5f, 0f, 1f));

                instance.ShockwaveDistance = distance;
                if (instance.ShockwaveDistance >= settings.radius)
                {
                    _forceInstances.RemoveAt(index);
                }
                else
                {
                    _forceInstances[index] = instance;
                }
            }
        }

        private void ApplyForceImmediate(Vector3 position, in ForceSettings forceSettings)
        {
            var explosionPoint = position + forceSettings.explosionOffset;
            var distance = forceSettings.radius;

            var size = Physics.OverlapSphereNonAlloc(explosionPoint, distance, _collisions, forceSettings.layer);

            for (var collisionIndex = 0; collisionIndex < size; collisionIndex++)
            {
                var collision = _collisions[collisionIndex];
                if (collision.TryGetComponent<IForceReceiver>(out var forceReceiver))
                {
                    var center = forceReceiver.CenterOfGravity;
                    var direction = center - explosionPoint;
                    var distanceDelta = Mathf.InverseLerp(0, forceSettings.radius, direction.magnitude);
                    var forceFactor = forceSettings.forceCurve.Evaluate(distanceDelta);
                    var force = direction.normalized * (forceSettings.force * forceFactor);
                    forceReceiver.AddForce(force, forceSettings.flags);
                }
            }

            Visualize.Sphere(explosionPoint, distance, new Color(1f, .5f, 0f, 0.3f), .1f);
            Visualize.Sphere(explosionPoint, .1f, new Color(1f, .5f, 0f, 1f), .1f);
        }
    }
}