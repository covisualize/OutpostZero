namespace OutpostZero.Shell
{
    public enum MixBus
    {
        Music,
        Sfx,
        Ambience,
        Ui
    }

    public enum MixSnapshot
    {
        Normal,
        Paused,
        Toxic,
        Death
    }

    /// <summary>
    /// Bus gains and snapshot ducks. OutpostMixer.mixer is generated from Duck and LowpassHz; without it the sources apply them.
    /// </summary>
    public static class AudioMix
    {
        public const float OpenHz = 22000f;
        public const float PausedHz = 900f;
        public const float ToxicHz = 1400f;
        public const float DeathHz = 480f;

        public static MixBus BusOf(string id)
        {
            if (id == "ambient" || id == "pulse") return MixBus.Music;
            if (id == "ui") return MixBus.Ui;
            if (id == "rain" || id == "storm" || id == "wind" || id == "ash" || id == "hum" || id == "crackle" || id == "buzz" || id == "flies") return MixBus.Ambience;
            return MixBus.Sfx;
        }

        public static MixSnapshot SnapshotFor(Core.GameState state, bool toxic)
        {
            if (state == Core.GameState.GameOver) return MixSnapshot.Death;
            if (state == Core.GameState.Paused
                || state == Core.GameState.MainMenu
                || state == Core.GameState.SuccessionScreen
                || state == Core.GameState.ExpeditionResults
                || state == Core.GameState.Victory)
                return MixSnapshot.Paused;
            if (toxic) return MixSnapshot.Toxic;
            return MixSnapshot.Normal;
        }

        public static float Gain(string id, float clip, float master, float music, float sfx, float ambience, float ui, MixSnapshot snapshot)
        {
            float bus = BusLevel(BusOf(id), music, sfx, ambience, ui);
            float ducked = Unit(clip) * Unit(master) * bus * Duck(BusOf(id), snapshot);
            if (ducked < 0f) return 0f;
            if (ducked > 1f) return 1f;
            return ducked;
        }

        public static float LowpassHz(MixSnapshot snapshot)
        {
            if (snapshot == MixSnapshot.Paused) return PausedHz;
            if (snapshot == MixSnapshot.Toxic) return ToxicHz;
            if (snapshot == MixSnapshot.Death) return DeathHz;
            return OpenHz;
        }

        public static string StepId(string surface)
        {
            if (string.IsNullOrEmpty(surface)) return "step";
            string name = surface.ToLowerInvariant();
            if (name.Contains("metal") || name.Contains("grate") || name.Contains("manhole")) return "step_metal";
            if (name.Contains("wood") || name.Contains("plank") || name.Contains("board")) return "step_wood";
            if (name.Contains("water") || name.Contains("puddle")) return "step_water";
            if (name.Contains("glass")) return "step_glass";
            if (name.Contains("gravel") || name.Contains("rubble") || name.Contains("dirt") || name.Contains("ash")) return "step_gravel";
            if (name.Contains("road") || name.Contains("street") || name.Contains("concrete") || name.Contains("asphalt") || name.Contains("sidewalk")) return "step_hard";
            return "step";
        }

        private static float BusLevel(MixBus bus, float music, float sfx, float ambience, float ui)
        {
            if (bus == MixBus.Music) return Unit(music);
            if (bus == MixBus.Ambience) return Unit(ambience);
            if (bus == MixBus.Ui) return Unit(ui);
            return Unit(sfx);
        }

        public static float Duck(MixBus bus, MixSnapshot snapshot)
        {
            if (snapshot == MixSnapshot.Paused)
            {
                if (bus == MixBus.Music) return 0.35f;
                if (bus == MixBus.Sfx) return 0.45f;
                if (bus == MixBus.Ambience) return 0.5f;
                return 1f;
            }
            if (snapshot == MixSnapshot.Toxic)
            {
                if (bus == MixBus.Music) return 0.7f;
                if (bus == MixBus.Sfx) return 0.75f;
                if (bus == MixBus.Ambience) return 1.15f;
                return 1f;
            }
            if (snapshot == MixSnapshot.Death)
            {
                if (bus == MixBus.Music) return 0.2f;
                if (bus == MixBus.Sfx) return 0.15f;
                if (bus == MixBus.Ambience) return 0.25f;
                return 0.4f;
            }
            return 1f;
        }

        private static float Unit(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
