using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Sensory
{
    /// <summary>The world noise list the noise manager loads from Resources. Tools > Outpost Zero > Sync Noise Book rebuilds it.</summary>
    public class NoiseBook : ScriptableObject
    {
        public const string ResourcePath = "NoiseBook";

        public NoiseDefinition[] noises = new NoiseDefinition[0];

        private static bool loaded;

        public static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            Use(Resources.Load<NoiseBook>(ResourcePath));
        }

        public static void Use(NoiseBook book)
        {
            var rows = new List<NoiseTable.Row>();
            if (book != null && book.noises != null)
            {
                foreach (var noise in book.noises)
                    if (noise != null) rows.Add(noise.ToRow());
            }
            NoiseTable.Use(rows);
        }
    }
}
