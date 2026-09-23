using System.Collections.Generic;
using OutpostZero.Core;

namespace OutpostZero.Player
{
    /// <summary>
    /// Each condition's numbers as a row. Empty until <see cref="StatusBook.Ensure"/> fills it from
    /// Resources/StatusBook; until then, and for any kind the book lacks, the built-in rows answer.
    /// </summary>
    public static class StatusTable
    {
        public sealed class Row
        {
            public StatusKind Kind;
            public string Id = "";
            /// <summary>String table key for the HUD pill.</summary>
            public string LabelKey = "";
            /// <summary>Health lost each second while it lasts.</summary>
            public float DamagePerSecond;
            /// <summary>Seconds a fresh case lasts when the source names none; below 0 lasts until treated.</summary>
            public float Seconds;
            /// <summary>Movement multiplier while it lasts (sprint for adrenaline); 1 leaves speed alone.</summary>
            public float Scale = 1f;
        }

        private static readonly Dictionary<StatusKind, Row> rows = new Dictionary<StatusKind, Row>();
        private static List<Row> builtIn;

        public static bool FromAsset => rows.Count > 0;

        public static void Use(IList<Row> list)
        {
            rows.Clear();
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null || row.Kind == StatusKind.None || rows.ContainsKey(row.Kind)) continue;
                rows[row.Kind] = row;
            }
        }

        public static void Clear()
        {
            rows.Clear();
        }

        public static Row Of(StatusKind kind)
        {
            if (rows.TryGetValue(kind, out var row)) return row;
            var list = BuiltInRows();
            for (int i = 0; i < list.Count; i++) if (list[i].Kind == kind) return list[i];
            return new Row { Kind = kind };
        }

        /// <summary>The code values as rows, in kind order: what Sync Status Book writes and what the committed assets must match.</summary>
        public static List<Row> BuiltInRows()
        {
            if (builtIn != null) return builtIn;
            builtIn = new List<Row>
            {
                new Row { Kind = StatusKind.Bleeding, Id = "bleeding", LabelKey = "hud.bleeding", DamagePerSecond = Affliction.BleedPerSecond, Seconds = -1f, Scale = 1f },
                new Row { Kind = StatusKind.Infected, Id = "infected", LabelKey = "hud.infected", DamagePerSecond = 0f, Seconds = -1f, Scale = 0.85f },
                new Row { Kind = StatusKind.Poisoned, Id = "poisoned", LabelKey = "hud.poison", DamagePerSecond = 4f, Seconds = 6f, Scale = 1f },
                new Row { Kind = StatusKind.Adrenaline, Id = "adrenaline", LabelKey = "hud.adrenaline", DamagePerSecond = 0f, Seconds = Affliction.AdrenalineSeconds, Scale = Affliction.AdrenalineSprint },
                new Row { Kind = StatusKind.KnockedDown, Id = "knocked_down", LabelKey = "hud.knocked", DamagePerSecond = 0f, Seconds = 0.7f, Scale = 0f },
                new Row { Kind = StatusKind.Slowed, Id = "slowed", LabelKey = "hud.slowed", DamagePerSecond = 0f, Seconds = 2f, Scale = 0.55f }
            };
            return builtIn;
        }

        /// <summary>Seconds for a new case: the source's own, else the row's.</summary>
        public static float SecondsFor(StatusKind kind, float given)
        {
            return given > 0f ? given : Of(kind).Seconds;
        }
    }
}
