using System.Collections.Generic;
using System.Globalization;
using OutpostZero.Core;

namespace OutpostZero.Shell
{
    /// <summary>
    /// A street run caught mid-way, for Merciful saves: the clock on the street, kills and scrap, where the
    /// leader stands and how hurt they are, the objective progress, and each live zombie's type, spot and
    /// health. Permadeath never writes one, so a street save can't undo a death there.
    /// </summary>
    public static class StreetSnapshot
    {
        public const string SaveId = "street_run";
        private const string Version = "s1";

        public struct Body
        {
            public string Variant;
            public float X;
            public float Z;
            public float Health;
        }

        public struct Run
        {
            public float Timer;
            public int Kills;
            public int Scrap;
            public float X;
            public float Z;
            public float Yaw;
            public float Health;
            public string Board;
            public List<Body> Bodies;
        }

        /// <summary>Only a living leader on the street, in a Merciful run, is caught; paused counts as where the pause was opened.</summary>
        public static bool Keeps(bool merciful, GameState state, GameState resume, bool leaderAlive)
        {
            if (!merciful || !leaderAlive) return false;
            var at = state == GameState.Paused ? resume : state;
            return at == GameState.ExpeditionActive;
        }

        public static string Pack(Run run)
        {
            var bodies = new List<string>();
            if (run.Bodies != null)
            {
                foreach (var body in run.Bodies)
                {
                    string variant = Clean(body.Variant);
                    if (variant.Length == 0 || body.Health <= 0f) continue;
                    bodies.Add(variant + ":" + F(body.X) + ":" + F(body.Z) + ":" + F(body.Health));
                }
            }
            return string.Join("|", new[]
            {
                Version, F(run.Timer), I(run.Kills), I(run.Scrap), F(run.X), F(run.Z), F(run.Yaw), F(run.Health),
                Clean(run.Board), string.Join(";", bodies)
            });
        }

        public static bool TryUnpack(string packed, out Run run)
        {
            run = new Run { Board = "", Bodies = new List<Body>() };
            if (string.IsNullOrEmpty(packed)) return false;
            string[] parts = packed.Split('|');
            if (parts.Length != 10 || parts[0] != Version) return false;
            if (!P(parts[1], out run.Timer) || !P(parts[4], out run.X) || !P(parts[5], out run.Z) || !P(parts[6], out run.Yaw) || !P(parts[7], out run.Health)) return false;
            if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out run.Kills)) return false;
            if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out run.Scrap)) return false;
            if (run.Health <= 0f) return false;
            if (run.Timer < 0f) run.Timer = 0f;
            if (run.Kills < 0) run.Kills = 0;
            if (run.Scrap < 0) run.Scrap = 0;
            run.Board = parts[8];
            if (parts[9].Length == 0) return true;
            foreach (string entry in parts[9].Split(';'))
            {
                string[] bits = entry.Split(':');
                if (bits.Length != 4 || bits[0].Length == 0) continue;
                if (!P(bits[1], out float x) || !P(bits[2], out float z) || !P(bits[3], out float health) || health <= 0f) continue;
                run.Bodies.Add(new Body { Variant = bits[0], X = x, Z = z, Health = health });
            }
            return true;
        }

        /// <summary>The prefab a body came from, or its own name without Unity's clone suffix; the spawner matches it as a name fragment.</summary>
        public static string Variant(string prefab, string instance)
        {
            string name = !string.IsNullOrEmpty(prefab) ? prefab : (instance ?? "");
            const string clone = "(Clone)";
            while (name.EndsWith(clone, System.StringComparison.Ordinal)) name = name.Substring(0, name.Length - clone.Length).TrimEnd();
            return Clean(name);
        }

        private static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("|", "").Replace(";", "").Replace(":", "");
        }

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static bool P(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
