using System;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Names and decibel conversion shared by OutpostMixer.mixer and MixerRig. Tools/Audio/make_mixer.py writes
    /// the same names and the same dB curve, so the snapshot values match AudioMix.Duck.
    /// </summary>
    public static class MixerDb
    {
        public const string ResourcePath = "Audio/OutpostMixer";
        public const string MasterParam = "MasterVolume";
        public const float Floor = -80f;
        public const float Ceiling = 20f;
        public const float Blend = 0.35f;

        public static readonly MixBus[] Buses = { MixBus.Music, MixBus.Sfx, MixBus.Ambience, MixBus.Ui };
        public static readonly MixSnapshot[] Snapshots = { MixSnapshot.Normal, MixSnapshot.Paused, MixSnapshot.Toxic, MixSnapshot.Death };

        public static string BusName(MixBus bus)
        {
            if (bus == MixBus.Music) return "Music";
            if (bus == MixBus.Ambience) return "Ambience";
            if (bus == MixBus.Ui) return "UI";
            return "SFX";
        }

        public static string Group(MixBus bus)
        {
            return BusName(bus) + " Duck";
        }

        public static string Param(MixBus bus)
        {
            if (bus == MixBus.Music) return "MusicVolume";
            if (bus == MixBus.Ambience) return "AmbienceVolume";
            if (bus == MixBus.Ui) return "UiVolume";
            return "SfxVolume";
        }

        public static string SnapshotName(MixSnapshot snapshot)
        {
            return snapshot.ToString();
        }

        public static float From(float level)
        {
            if (level <= 0.0001f) return Floor;
            double db = 20.0 * Math.Log10(level);
            if (db < Floor) return Floor;
            if (db > Ceiling) return Ceiling;
            return (float)Math.Round(db, 4);
        }
    }
}
