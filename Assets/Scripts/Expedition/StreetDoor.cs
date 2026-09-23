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
        private int bar;
        private int shotStamp = -1;

        public string Prompt => DoorBar.Holds(bar) ? DoorBar.Face(null) : DoorMap.Prompt(leaving, null);

        public bool Barred => DoorBar.Holds(bar);

        public void Configure(Vector3 target, bool toStreet)
        {
            Configure(target, toStreet, false);
        }

        public void Configure(Vector3 target, bool toStreet, bool barred)
        {
            destination = target;
            leaving = toStreet;
            bar = barred ? DoorBar.Hits : 0;
        }

        public void Strike(GameObject attacker)
        {
            if (!DoorBar.Holds(bar)) return;
            bar = DoorBar.After(bar);
            bool open = !DoorBar.Holds(bar);
            GameplayFeedback.Toast(open ? DoorBar.Gives(null) : DoorBar.Hold(null));
            Ring(open, attacker);
        }

        public void Rake(GameObject attacker)
        {
            if (!DoorBar.Holds(bar)) return;
            bar = BarClaw.Rake(bar);
            bool open = !DoorBar.Holds(bar);
            GameplayFeedback.Toast(open ? DoorBar.Gives(null) : BarClaw.Rattle(null));
            Ring(open, attacker);
        }

        public void Rip(GameObject attacker)
        {
            if (!DoorBar.Holds(bar)) return;
            bar = BarClaw.Rush(bar);
            bool open = !DoorBar.Holds(bar);
            GameplayFeedback.Toast(open ? DoorBar.Gives(null) : DoorBar.Hold(null));
            Ring(open, attacker);
        }

        public void Shoot(GameObject attacker, OutpostZero.Core.WeaponType type, int stamp)
        {
            if (!DoorBar.Holds(bar)) return;
            if (stamp == shotStamp) return;
            shotStamp = stamp;
            bar = BarShot.After(bar, type);
            bool open = !DoorBar.Holds(bar);
            GameplayFeedback.Toast(open ? DoorBar.Gives(null) : BarShot.Line(null));
            Ring(open, attacker);
        }

        private void Ring(bool open, GameObject attacker)
        {
            if (Sensory.NoiseManager.Instance != null)
                Sensory.NoiseManager.Instance.EmitNoise(transform.position, Sensory.NoiseTable.Radius(open ? Sensory.NoiseTable.DoorBreak : Sensory.NoiseTable.DoorBash), Sensory.NoiseTable.Loud(open ? Sensory.NoiseTable.DoorBreak : Sensory.NoiseTable.DoorBash), NoiseType.ObjectBroken, attacker);
        }

        public bool CanInteract(PlayerInventory inventory) => inventory != null;

        public void Interact(PlayerInventory inventory)
        {
            if (DoorBar.Holds(bar))
            {
                GameplayFeedback.Toast(DoorBar.Hold(null));
                return;
            }
            var player = inventory != null ? inventory.GetComponent<PlayerController>() : null;
            if (player == null) player = PlayerRegistry.Current;
            if (player == null) return;

            var body = player.GetComponent<CharacterController>();
            var from = player.transform.position;
            var dest = new Vector3(destination.x, from.y, destination.z);
            DoorCross.Note(from.x, from.z, dest.x, dest.z, Time.time);
            if (Sensory.NoiseManager.Instance != null)
                Sensory.NoiseManager.Instance.EmitNoise(from, Sensory.NoiseTable.Radius(Sensory.NoiseTable.DoorCreak), Sensory.NoiseTable.Loud(Sensory.NoiseTable.DoorCreak), NoiseType.DoorSwing, player.gameObject);
            if (body != null) body.enabled = false;
            player.transform.position = dest;
            if (body != null) body.enabled = true;

            var follower = RescueFollower.Current;
            if (follower != null && follower.Following)
                follower.transform.position = dest + new Vector3(0.8f, 0f, 0f);

            GameplayFeedback.Toast(DoorMap.Cross(leaving, null));
            if (!leaving) Shell.CodexDirector.Hear("dark");
        }
    }
}
