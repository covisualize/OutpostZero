using UnityEngine;
using OutpostZero.Colony;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.Items
{
    public class LootContainer : MonoBehaviour, IInteractable
    {
        [SerializeField] private string tableId = "crate";
        [SerializeField] private bool looted;
        private bool rolled;
        private string stamp = "";
        private ContainerHold.Stack[] stacks = new ContainerHold.Stack[0];

        public static LootContainer Open { get; private set; }

        public string Contents => ContainerHold.Signature(stacks);
        public string StampId => stamp ?? "";
        public int HeldCount => stacks == null ? 0 : stacks.Length;
        public string Prompt => looted ? string.Empty : rolled ? "Take from container" : "Search container";

        public string HeldId(int index)
        {
            if (stacks == null || index < 0 || index >= stacks.Length) return "";
            return stacks[index].Id ?? "";
        }

        public string HeldOffer(int index)
        {
            if (stacks == null || index < 0 || index >= stacks.Length) return "";
            return ContainerHold.Offer(stacks[index]);
        }

        public void Configure(string table)
        {
            tableId = string.IsNullOrEmpty(table) ? "crate" : table;
        }

        public void Stamp(string mark)
        {
            stamp = mark ?? "";
        }

        public void MarkEmpty()
        {
            looted = true;
            rolled = true;
            stacks = new ContainerHold.Stack[0];
            if (Open == this) Open = null;
        }

        public void Restore(string body)
        {
            stacks = ContainerHold.Decode(body);
            rolled = true;
            looted = stacks.Length == 0;
            if (looted && Open == this) Open = null;
        }

        public static void Sweep()
        {
            if (OutpostZero.Shell.WorldMapService.Instance == null) return;
            var boxes = Object.FindObjectsByType<LootContainer>(FindObjectsSortMode.None);
            for (int i = 0; i < boxes.Length; i++)
            {
                if (boxes[i] == null || string.IsNullOrEmpty(boxes[i].StampId)) continue;
                string left = OutpostZero.Shell.WorldMapService.Instance.StreetLeft(boxes[i].StampId);
                if (left == null) continue;
                if (left.Length == 0) boxes[i].MarkEmpty();
                else boxes[i].Restore(left);
            }
        }

        public bool CanInteract(PlayerInventory inventory) => !looted && inventory != null;

        public void Interact(PlayerInventory inventory)
        {
            if (!CanInteract(inventory)) return;
            if (!rolled) Roll();
            Open = this;
            PackView.AskOpen();
            GameplayFeedback.Toast(stacks.Length > 0 ? "Container open" : "Empty");
            if (stacks.Length == 0) Finish();
            else Remember();
        }

        public bool Take(string id, PlayerInventory inventory)
        {
            if (inventory == null || looted) return false;
            stacks = ContainerHold.Take(stacks, id, int.MaxValue, out int moved);
            if (moved <= 0) return false;
            if (!Give(inventory, id, moved))
            {
                stacks = PutBack(stacks, id, moved);
                GameplayFeedback.Toast("Pack is too heavy");
                return false;
            }
            if (stacks.Length == 0) Finish();
            else Remember();
            return true;
        }

        public int TakeAll(PlayerInventory inventory)
        {
            if (inventory == null || looted) return 0;
            stacks = ContainerHold.TakeAll(stacks, out var moved);
            int given = 0;
            var left = new ContainerHold.Stack[moved.Length];
            int remain = 0;
            for (int i = 0; i < moved.Length; i++)
            {
                if (Give(inventory, moved[i].Id, moved[i].Count)) given++;
                else left[remain++] = moved[i];
            }
            if (remain > 0)
            {
                var kept = new ContainerHold.Stack[remain];
                for (int i = 0; i < remain; i++) kept[i] = left[i];
                stacks = kept;
                GameplayFeedback.Toast("Left some loot behind");
            }
            if (stacks.Length == 0) Finish();
            else Remember();
            return given;
        }

        private void Roll()
        {
            rolled = true;
            var grants = LootTables.Roll(tableId, GetInstanceID());
            int count = 0;
            for (int i = 0; i < grants.Length; i++)
            {
                if (grants[i].Count > 0 && ItemCatalog.Find(grants[i].ItemId) != null) count++;
            }
            stacks = new ContainerHold.Stack[count];
            int write = 0;
            for (int i = 0; i < grants.Length; i++)
            {
                if (grants[i].Count <= 0) continue;
                if (ItemCatalog.Find(grants[i].ItemId) == null) continue;
                stacks[write++] = new ContainerHold.Stack { Id = grants[i].ItemId, Count = grants[i].Count };
            }
            int scavenge = SurvivorRoster.LeaderPractice("Scavenge");
            int extra = FieldHand.Scrap(scavenge) + HandDepth.Scrap(scavenge);
            if (extra > 0) stacks = Pile(stacks, "scrap", extra);
        }

        private static ContainerHold.Stack[] Pile(ContainerHold.Stack[] held, string id, int extra)
        {
            if (held == null) held = new ContainerHold.Stack[0];
            for (int i = 0; i < held.Length; i++)
            {
                if (held[i].Id == id)
                {
                    held[i].Count += extra;
                    return held;
                }
            }
            var next = new ContainerHold.Stack[held.Length + 1];
            for (int i = 0; i < held.Length; i++) next[i] = held[i];
            next[held.Length] = new ContainerHold.Stack { Id = id, Count = extra };
            return next;
        }

        private static bool Give(PlayerInventory inventory, string id, int count)
        {
            var record = ItemCatalog.Find(id);
            if (record == null || count <= 0) return false;
            if (record.Use == ItemUse.Ammo) return inventory.GrantAmmoPublic(record.AmmoType, record.AmmoAmount * count);
            if (record.Id == "scrap")
            {
                inventory.AddScrap(count);
                return true;
            }
            return inventory.TryAddItem(record.Id, record.DisplayName, record.Category, count, record.Weight);
        }

        private static ContainerHold.Stack[] PutBack(ContainerHold.Stack[] stacks, string id, int count)
        {
            var next = new ContainerHold.Stack[stacks.Length + 1];
            for (int i = 0; i < stacks.Length; i++) next[i] = stacks[i];
            next[stacks.Length] = new ContainerHold.Stack { Id = id, Count = count };
            return next;
        }

        private void Remember()
        {
            if (string.IsNullOrEmpty(stamp)) return;
            OutpostZero.Shell.WorldMapService.Instance?.NoteHold(stamp, ContainerHold.Encode(stacks));
        }

        private void Finish()
        {
            looted = true;
            if (Open == this) Open = null;
            if (!string.IsNullOrEmpty(stamp)) OutpostZero.Shell.WorldMapService.Instance?.NoteStreet(stamp);
        }
    }
}
