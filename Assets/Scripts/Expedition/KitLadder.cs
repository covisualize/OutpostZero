using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// Gives a kit ladder its two grips: one at the foot that climbs to the floor above, and one beside the top,
    /// on that floor, that climbs back down. The slab above stays whole; the climb is a step through its hatch.
    /// </summary>
    public class KitLadder : MonoBehaviour
    {
        private void Start()
        {
            Grip("LadderFoot", KitPlan.LadderFoot, KitPlan.LadderTop, true);
            Grip("LadderTop", KitPlan.LadderTop, KitPlan.LadderFoot, false);
        }

        private void Grip(string name, Vector3 at, Vector3 to, bool up)
        {
            var grip = new GameObject(name);
            grip.transform.SetParent(transform, false);
            grip.transform.localPosition = at + Vector3.up * 0.6f;
            grip.layer = GameLayers.Interactable;
            var sphere = grip.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.35f;
            grip.AddComponent<LadderGrip>().Configure(transform.TransformPoint(to + Vector3.up * 0.05f), up);
        }
    }

    public class LadderGrip : MonoBehaviour, IInteractable
    {
        public const float Noise = 4f;

        private Vector3 destination;
        private bool up;

        public string Prompt => up ? StreetAsk.ClimbUp(null) : StreetAsk.ClimbDown(null);

        public void Configure(Vector3 target, bool climbsUp)
        {
            destination = target;
            up = climbsUp;
        }

        public bool CanInteract(PlayerInventory inventory) => inventory != null;

        public void Interact(PlayerInventory inventory)
        {
            var player = inventory != null ? inventory.GetComponent<PlayerController>() : null;
            if (player == null) player = PlayerRegistry.Current;
            if (player == null) return;

            var body = player.GetComponent<CharacterController>();
            if (body != null) body.enabled = false;
            player.transform.position = destination;
            if (body != null) body.enabled = true;

            var follower = RescueFollower.Current;
            if (follower != null && follower.Following)
                follower.transform.position = destination + new Vector3(0.4f, 0f, 0f);

            if (Sensory.NoiseManager.Instance != null)
                Sensory.NoiseManager.Instance.EmitNoise(destination, Sensory.NoiseTable.Radius(Sensory.NoiseTable.Ladder), Sensory.NoiseTable.Loud(Sensory.NoiseTable.Ladder), NoiseType.WalkFootstep, player.gameObject);
        }
    }
}
