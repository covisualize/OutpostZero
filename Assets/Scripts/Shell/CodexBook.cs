namespace OutpostZero.Shell
{
    /// <summary>
    /// Codex pages and the twelve one-time hints. Seen ids are packed as id|id so the save stays schema 1.
    /// </summary>
    public static class CodexBook
    {
        public sealed class Entry
        {
            public string Id;
            public string Title;
            public string Body;
            public bool LockedUntilSeen;
        }

        public sealed class Hint
        {
            public string Id;
            public string Signal;
            public string Text;
        }

        public static readonly Entry[] Entries =
        {
            new Entry { Id = "zombie.walker", Title = "Walker", Body = "Slow, loud, and enough of them to pin you. A scream pulls the ones you have not seen.", LockedUntilSeen = true },
            new Entry { Id = "zombie.runner", Title = "Runner", Body = "Closes the gap with a lunge. Crouch and a corner buy you the reload.", LockedUntilSeen = true },
            new Entry { Id = "zombie.brute", Title = "Brute", Body = "A charge that knocks you down. Barrels and a clear lane do more than a pistol.", LockedUntilSeen = true },
            new Entry { Id = "item.medkit", Title = "Medkit", Body = "Stops bleeding and puts health back. Q uses the one in your hands.", LockedUntilSeen = false },
            new Entry { Id = "module.generator", Title = "Generator", Body = "Keeps the sanctuary lamps lit. It drinks fuel after dusk.", LockedUntilSeen = false },
            new Entry { Id = "module.barricade", Title = "Barricade", Body = "The night raid hits the nearest boards. A guard slows the damage.", LockedUntilSeen = false },
            new Entry { Id = "module.cot", Title = "Medical cot", Body = "Rest and a medic close wounds faster than waiting the night out.", LockedUntilSeen = false },
            new Entry { Id = "faction.market", Title = "Ash Market", Body = "The merchant trades medkits, rifle ammo, and water for camp scrap.", LockedUntilSeen = false },
            new Entry { Id = "mechanic.noise", Title = "Noise", Body = "Shots, sprints, and breaking barrels carry. Crouching cuts the footfall.", LockedUntilSeen = false },
            new Entry { Id = "mechanic.exposure", Title = "Exposure", Body = "Lamplight and the flashlight make you easier to spot. Dark is cover.", LockedUntilSeen = false },
            new Entry { Id = "mechanic.infection", Title = "Infection", Body = "A dirty wound worsens until a medkit or the cot clears it.", LockedUntilSeen = false },
            new Entry { Id = "mechanic.extract", Title = "Extraction", Body = "Finish the quota, then stand in the sanctuary gate. The bag stays if you fall.", LockedUntilSeen = false }
        };

        public static readonly Hint[] Hints =
        {
            new Hint { Id = "hint.move", Signal = "move", Text = "WASD moves. The mouse aims." },
            new Hint { Id = "hint.fire", Signal = "fire", Text = "Left click fires. {key:Reload} reloads." },
            new Hint { Id = "hint.crouch", Signal = "near", Text = "One is close. {key:Crouch} crouches: it cuts exposure, but noise still travels." },
            new Hint { Id = "hint.loot", Signal = "loot", Text = "Scrap and supplies go into the pack." },
            new Hint { Id = "hint.pack", Signal = "pack", Text = "{key:Inventory} opens the pack. Use what you are carrying." },
            new Hint { Id = "hint.reload", Signal = "empty", Text = "The magazine is empty. {key:Reload} reloads. Do it before the click." },
            new Hint { Id = "hint.flashlight", Signal = "dark", Text = "It is dark inside. {key:Flashlight} lights the room, and shows you to it." },
            new Hint { Id = "hint.medkit", Signal = "medkit", Text = "A medkit closes a bleed. It will not refill itself." },
            new Hint { Id = "hint.sprint", Signal = "sprint", Text = "Sprint is loud and spends stamina." },
            new Hint { Id = "hint.aim", Signal = "aim", Text = "Aim tightens the shot and narrows what you can see." },
            new Hint { Id = "hint.extract", Signal = "extract", Text = "The gate is the way home. Unfinished work will not let you through." },
            new Hint { Id = "hint.weight", Signal = "weight", Text = "The pack is near its limit. Drop or use something." }
        };

        public static bool Has(string packed, string id)
        {
            if (string.IsNullOrEmpty(packed) || string.IsNullOrEmpty(id)) return false;
            return ("|" + packed + "|").Contains("|" + id + "|");
        }

        public static string Remember(string packed, string id, out bool added)
        {
            added = false;
            if (string.IsNullOrEmpty(id)) return packed ?? "";
            if (Has(packed, id)) return packed ?? "";
            added = true;
            return string.IsNullOrEmpty(packed) ? id : packed + "|" + id;
        }

        public static bool TryHint(string packed, string signal, out string text, out string next)
        {
            text = null;
            next = packed ?? "";
            if (string.IsNullOrEmpty(signal)) return false;
            for (int i = 0; i < Hints.Length; i++)
            {
                if (Hints[i].Signal != signal) continue;
                next = Remember(next, Hints[i].Id, out bool added);
                if (!added) return false;
                text = Loc.Hint(Hints[i].Id, Hints[i].Text);
                return true;
            }
            return false;
        }

        public static bool Visible(Entry entry, string packed)
        {
            return entry != null && (!entry.LockedUntilSeen || Has(packed, entry.Id));
        }
    }
}
