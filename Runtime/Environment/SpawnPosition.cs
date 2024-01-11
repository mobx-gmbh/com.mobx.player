using Drawing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MobX.Player.Environment
{
    [SelectionBase]
    public class SpawnPosition : MonoBehaviourGizmos
    {
        [SerializeField] [Required] private PlayerCharacterValueAsset playerCharacter;
        [SerializeField] [Required] private InputActionReference resetInput;

        private void Awake()
        {
            resetInput.action.performed += OnResetInput;
        }

        private void OnDestroy()
        {
            resetInput.action.performed -= OnResetInput;
        }

        private void OnResetInput(InputAction.CallbackContext context)
        {
            if (playerCharacter.TryGetValue(out var player))
            {
                var self = transform;
                player.LocomotionController.Teleport(self.position, self.rotation);
            }
        }

        public override void DrawGizmos()
        {
            var self = transform;
            var position = self.position;
            var direction = self.forward;
            var up = self.up;
            Draw.ArrowheadArc(position, direction, .5f);
            Draw.WireCylinder(position, up, 2, .5f, Color.green);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.clear;
            var self = transform;
            var position = self.position;
            Gizmos.DrawCube(position + new Vector3(0, 1, 0), new Vector3(1, 2, 1));
        }
    }
}