using MobX.Utilities.Types;
using UnityEngine;

namespace MobX.Player.Environment
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class DeathPlane : MonoBehaviour
    {
        [SerializeField] private Optional<Transform> positionOverride;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<PlayerCharacter>(out var playerCharacter))
            {
                return;
            }

            var position = positionOverride.TryGetValue(out var value)
                ? value.position
                : transform.position;

            Debug.Log("Death Plane", $"Teleporting Player to position: {position}");

            playerCharacter.LocomotionController.Teleport(position, playerCharacter.transform.rotation);
        }
    }
}