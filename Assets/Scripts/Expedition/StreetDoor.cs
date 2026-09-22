using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// A slab the player can step through. The far side is the matching door.
    /// </summary>
    public class StreetDoor : MonoBehaviour, IInteractable
    {
        private Vector3 destination;
        private bool leaving;

        public string Prompt => DoorMap.Prompt(leaving);

        public void Configure(Vector3 target, bool toStreet)
        {
            destination = target;
            leaving = toStreet;
        }

        public bool CanInteract(PlayerInventory inventory) => inventory != null;

        public void Interact(PlayerInventory inventory)
        {
            var player = inventory != null ? inventory.GetComponent<PlayerController>() : null;
            if (player == null) player = PlayerRegistry.Current;
            if (player == null) return;

            var body = player.GetComponent<CharacterController>();
            var dest = new Vector3(destination.x, player.transform.position.y, destination.z);
            if (body != null) body.enabled = false;
            player.transform.position = dest;
            if (body != null) body.enabled = true;

            var follower = RescueFollower.Current;
            if (follower != null && follower.Following)
                follower.transform.position = dest + new Vector3(0.8f, 0f, 0f);

            GameplayFeedback.Toast(leaving ? "Back on the street" : "Inside");
        }
    }
}
