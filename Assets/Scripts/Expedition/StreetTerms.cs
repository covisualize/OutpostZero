using System.Collections.Generic;
using OutpostZero.Shell;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// The terms of one district's expedition beyond its objectives: how long the street lasts before overtime,
    /// a difficulty that overrides the run's, and extraction points that replace the seeded gate.
    /// </summary>
    public static class StreetTerms
    {
        /// <summary>Ten minutes: the balance sheet's expedition, one water's worth of thirst.</summary>
        public const float DefaultDuration = 600f;

        public sealed class Terms
        {
            public float Duration = DefaultDuration;
            public int Difficulty;
            public float[] ExtractX = new float[0];
            public float[] ExtractZ = new float[0];
        }

        private static readonly Dictionary<string, Terms> terms = new Dictionary<string, Terms>();
        private static readonly Terms none = new Terms();

        public static Terms Active { get; private set; } = none;

        public static void Use(IEnumerable<KeyValuePair<string, Terms>> list)
        {
            terms.Clear();
            if (list == null) return;
            foreach (var pair in list)
                if (!string.IsNullOrEmpty(pair.Key) && pair.Value != null) terms[pair.Key] = pair.Value;
        }

        public static void Clear()
        {
            terms.Clear();
            Active = none;
        }

        public static Terms For(string districtId)
        {
            return districtId != null && terms.TryGetValue(districtId, out var found) ? found : none;
        }

        public static void Begin(string districtId)
        {
            Active = For(districtId);
        }

        /// <summary>The expedition's own difficulty when it names one (1 to 3), else the run's.</summary>
        public static int Difficulty(string districtId, int run)
        {
            int own = For(districtId).Difficulty;
            return own >= 1 && own <= 3 ? own : run;
        }

        public static float Left(Terms at, float elapsed)
        {
            if (at == null || at.Duration <= 0f) return -1f;
            float left = at.Duration - elapsed;
            return left > 0f ? left : 0f;
        }

        public static bool Overtime(Terms at, float elapsed)
        {
            return at != null && at.Duration > 0f && elapsed >= at.Duration;
        }

        /// <summary>One of the authored extraction points, picked by the world seed; false keeps the seeded gate.</summary>
        public static bool Extraction(string districtId, int seed, out float x, out float z)
        {
            var at = For(districtId);
            int count = at.ExtractX != null && at.ExtractZ != null ? System.Math.Min(at.ExtractX.Length, at.ExtractZ.Length) : 0;
            x = 0f;
            z = 0f;
            if (count == 0) return false;
            int pick = (int)((uint)seed % (uint)count);
            x = at.ExtractX[pick];
            z = at.ExtractZ[pick];
            return true;
        }

        /// <summary>The HUD clock's tail: time left as m:ss, then Overtime; empty with no limit.</summary>
        public static string Line(int secondsLeft, bool limited, string language)
        {
            if (!limited) return "";
            if (secondsLeft <= 0) return Tr("street.overtime", language);
            int minutes = secondsLeft / 60;
            int rest = secondsLeft % 60;
            return string.Format(Tr("street.left", language), minutes + ":" + (rest < 10 ? "0" : "") + rest);
        }

        private static string Tr(string key, string language) => language == null ? Loc.T(key) : Loc.T(key, language);
    }
}
