using UnityEngine;
using UnityEngine.Audio;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Routes sources into OutpostMixer and drives its exposed volumes and snapshots. When the asset, a duck group
    /// or a snapshot is missing, Live stays false and AudioManager keeps mixing per source with AudioMix.
    /// </summary>
    public static class MixerRig
    {
        private static AudioMixer mixer;
        private static readonly AudioMixerGroup[] groups = new AudioMixerGroup[4];
        private static readonly AudioMixerSnapshot[] snapshots = new AudioMixerSnapshot[4];
        private static readonly float[] sent = new float[5];
        private static bool loaded;
        private static int current = -1;

        public static bool Live
        {
            get
            {
                Load();
                return mixer != null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            mixer = null;
            loaded = false;
            current = -1;
        }

        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            var asset = Resources.Load<AudioMixer>(MixerDb.ResourcePath);
            if (asset == null) return;
            var all = asset.FindMatchingGroups("");
            foreach (var bus in MixerDb.Buses)
            {
                AudioMixerGroup found = null;
                string name = MixerDb.Group(bus);
                if (all != null)
                    foreach (var group in all)
                        if (group != null && group.name == name) found = group;
                if (found == null) return;
                groups[(int)bus] = found;
            }
            foreach (var snapshot in MixerDb.Snapshots)
            {
                var found = asset.FindSnapshot(MixerDb.SnapshotName(snapshot));
                if (found == null) return;
                snapshots[(int)snapshot] = found;
            }
            for (int i = 0; i < sent.Length; i++) sent[i] = float.NaN;
            mixer = asset;
        }

        public static void Route(AudioSource source, MixBus bus)
        {
            if (source == null || !Live) return;
            source.outputAudioMixerGroup = groups[(int)bus];
        }

        public static void Master(float level)
        {
            if (!Live) return;
            Send(4, MixerDb.MasterParam, level);
        }

        public static void Levels(float music, float sfx, float ambience, float ui)
        {
            if (!Live) return;
            Send((int)MixBus.Music, MixerDb.Param(MixBus.Music), music);
            Send((int)MixBus.Sfx, MixerDb.Param(MixBus.Sfx), sfx);
            Send((int)MixBus.Ambience, MixerDb.Param(MixBus.Ambience), ambience);
            Send((int)MixBus.Ui, MixerDb.Param(MixBus.Ui), ui);
        }

        public static void Snapshot(MixSnapshot snapshot)
        {
            if (!Live || (int)snapshot == current) return;
            float blend = current < 0 ? 0f : MixerDb.Blend;
            current = (int)snapshot;
            snapshots[current].TransitionTo(blend);
        }

        private static void Send(int slot, string param, float level)
        {
            float db = MixerDb.From(level);
            if (sent[slot] == db) return;
            sent[slot] = db;
            mixer.SetFloat(param, db);
        }
    }
}
