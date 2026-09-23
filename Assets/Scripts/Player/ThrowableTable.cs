using System.Collections.Generic;
using OutpostZero.Combat;

namespace OutpostZero.Player
{
    /// <summary>
    /// Each throwable's numbers as a row, in the order the throw key picks from the pack.
    /// Empty until <see cref="ThrowableBook.Ensure"/> fills it from Resources/ThrowableBook; until then,
    /// and for any id the book lacks, the built-in rows answer.
    /// </summary>
    public static class ThrowableTable
    {
        public sealed class Row
        {
            /// <summary>Item id this row throws.</summary>
            public string Id = "";
            /// <summary>What it does on landing, a <see cref="TossKind"/> value.</summary>
            public int Kind;
            /// <summary>Metres its landing (or each flare pulse) is heard.</summary>
            public float Noise;
            /// <summary>Seconds in the air before it goes off by itself when nothing stops it.</summary>
            public float Fuse;
            /// <summary>Metres of blast, fire burst or flare light; 0 for a plain lure.</summary>
            public float Radius;
            /// <summary>Damage to everything inside the radius when it goes off.</summary>
            public float Damage;
            /// <summary>Seconds a flare burns; 0 for everything else.</summary>
            public float Seconds;
            /// <summary>Metres across the thrown body.</summary>
            public float Size = 0.25f;
        }

        private static readonly List<Row> rows = new List<Row>();
        private static List<Row> builtIn;

        public static bool FromAsset => rows.Count > 0;

        public static void Use(IList<Row> list)
        {
            rows.Clear();
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null || string.IsNullOrEmpty(row.Id) || row.Kind == TossKind.None || Find(rows, row.Id) != null) continue;
                rows.Add(row);
            }
        }

        public static void Clear()
        {
            rows.Clear();
        }

        /// <summary>The rows the throw key tries, in order.</summary>
        public static IList<Row> All => rows.Count > 0 ? rows : BuiltInRows();

        public static Row Of(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return Find(rows, id) ?? Find(BuiltInRows(), id);
        }

        /// <summary>The code values as rows: what Sync Throwable Book writes and what the committed assets must match.</summary>
        public static List<Row> BuiltInRows()
        {
            if (builtIn != null) return builtIn;
            builtIn = new List<Row>
            {
                new Row { Id = "molotov", Kind = TossKind.Fire, Noise = ThrowArc.FireRadius, Fuse = 1.1f, Radius = FirePatch.BurstRadius, Damage = FirePatch.Burst, Seconds = 0f, Size = 0.25f },
                new Row { Id = "street_bottle", Kind = TossKind.Lure, Noise = ThrowArc.LureRadius, Fuse = 0.7f, Radius = 0f, Damage = 0f, Seconds = 0f, Size = 0.25f },
                new Row { Id = "noise_lure", Kind = TossKind.Lure, Noise = ThrowArc.LureRadius, Fuse = 0.7f, Radius = 0f, Damage = 0f, Seconds = 0f, Size = 0.25f },
                new Row { Id = "flare", Kind = TossKind.Flare, Noise = FlareClock.Radius, Fuse = 0.8f, Radius = 9f, Damage = 0f, Seconds = FlareClock.Duration, Size = 0.18f },
                new Row { Id = "pipe_bomb", Kind = TossKind.Bomb, Noise = PipeBlast.Noise, Fuse = PipeBlast.Fuse, Radius = PipeBlast.Radius, Damage = PipeBlast.Damage, Seconds = 0f, Size = 0.22f }
            };
            return builtIn;
        }

        private static Row Find(IList<Row> list, string id)
        {
            for (int i = 0; i < list.Count; i++) if (list[i].Id == id) return list[i];
            return null;
        }
    }
}
