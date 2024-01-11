using Drawing;
using MobX.Utilities;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace MobX.Player.Environment
{
    [SelectionBase]
    [RequireComponent(typeof(Rigidbody))]
    public class Teleporter : MonoBehaviourGizmos
    {
        [SerializeField] [Required] private Teleporter destination;
        [SerializeField] private bool isDestinationOnly;
        [HideIf(nameof(isDestinationOnly))]
        [Tooltip("When enabled, the rotation of the character is set to the rotation of the destination teleporter")]
        [SerializeField] private bool overrideRotation;
        [Space]
        [SerializeField] [Required] private MeshRenderer portalRenderer;
        [HideIf(nameof(isDestinationOnly))]
        [SerializeField] [Required] private TMP_Text destinationTextField;

        private bool _disableNextTrigger;

        private void OnTriggerEnter(Collider other)
        {
            if (isDestinationOnly)
            {
                return;
            }

            if (!other.TryGetComponent<PlayerCharacter>(out var playerCharacter))
            {
                return;
            }

            if (_disableNextTrigger)
            {
                _disableNextTrigger = false;
                return;
            }

            destination.Teleport(playerCharacter);
        }

        private void Teleport(PlayerCharacter playerCharacter)
        {
            _disableNextTrigger = true;

            var position = transform.position;
            var rotation = overrideRotation ? transform.rotation : playerCharacter.transform.rotation;

            Debug.Log("Teleporter", $"Teleporting Player to: {position}");

            playerCharacter.LocomotionController.Teleport(position, rotation);
        }

        private void OnValidate()
        {
            if (portalRenderer)
            {
                portalRenderer.enabled = !isDestinationOnly;
            }

            var hasDestination = destination != null;
            if (hasDestination && destination == this)
            {
                Debug.LogError("Destination needs to be another teleporter!", this);
                destination = null;
                hasDestination = false;
            }

            var hasTextField = destinationTextField != null;
            if (hasTextField)
            {
                destinationTextField.SetActive(!isDestinationOnly);
                var destinationName = hasDestination ? destination.name : "No Destination!".Colorize(Color.red);
                var text = $"{name} -> {destinationName}";
                destinationTextField.text = text;
            }
        }

        public override void DrawGizmos()
        {
            if (isDestinationOnly)
            {
                return;
            }
            if (destination == null)
            {
                return;
            }

            var self = transform;
            var position = self.position;
            var direction = (destination.transform.position - position).normalized;
            var center = position + Vector3.up + direction;
            Draw.Arrowhead(center, direction, self.up, 1f);
        }
    }
}