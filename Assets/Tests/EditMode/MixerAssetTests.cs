using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class MixerAssetTests
    {
        private const string MixerPath = "Assets/Resources/Audio/OutpostMixer.mixer";

        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        private static List<string[]> Blocks()
        {
            var blocks = new List<string[]>();
            foreach (var chunk in Read(MixerPath).Split(new[] { "--- !u!" }, System.StringSplitOptions.RemoveEmptyEntries))
                if (!chunk.StartsWith("%YAML")) blocks.Add(chunk.Split('\n'));
            return blocks;
        }

        private static string Field(string[] block, string name)
        {
            foreach (var line in block)
                if (line.StartsWith("  " + name + ": ")) return line.Substring(name.Length + 4).Trim();
                else if (line == "  " + name + ":") return "";
            return null;
        }

        private static IEnumerable<string[]> OfClass(string cls)
        {
            foreach (var block in Blocks())
                if (block.Length > 1 && block[1] == cls + ":") yield return block;
        }

        [Test]
        public void DecibelsFollowTheGeneratorCurve()
        {
            Assert.AreEqual(0f, MixerDb.From(1f), 0.0001f);
            Assert.AreEqual(-80f, MixerDb.From(0f));
            Assert.AreEqual(-6.0206f, MixerDb.From(0.5f), 0.001f);
            Assert.AreEqual(1.2140f, MixerDb.From(1.15f), 0.001f);
            Assert.AreEqual("Music Duck", MixerDb.Group(MixBus.Music));
            Assert.AreEqual("UI Duck", MixerDb.Group(MixBus.Ui));
            Assert.AreEqual("SfxVolume", MixerDb.Param(MixBus.Sfx));
        }

        [Test]
        public void TheMixerHasEveryBusDuckAndExposedVolume()
        {
            var names = new HashSet<string>();
            foreach (var group in OfClass("AudioMixerGroupController")) names.Add(Field(group, "m_Name"));
            Assert.IsTrue(names.Contains("Master"));
            foreach (var bus in MixerDb.Buses)
            {
                Assert.IsTrue(names.Contains(MixerDb.BusName(bus)), bus.ToString());
                Assert.IsTrue(names.Contains(MixerDb.Group(bus)), bus.ToString());
            }
            string text = Read(MixerPath);
            StringAssert.Contains("    name: " + MixerDb.MasterParam + "\n", text);
            foreach (var bus in MixerDb.Buses) StringAssert.Contains("    name: " + MixerDb.Param(bus) + "\n", text);
            StringAssert.Contains("m_MasterGroup: {fileID: 24300002}", text);
            StringAssert.Contains("m_EffectName: Lowpass Simple", text);
            StringAssert.Contains("mainObjectFileID: 24100000", Read(MixerPath + ".meta"));
            StringAssert.Contains("folderAsset: yes", Read("Assets/Resources/Audio.meta"));
        }

        [Test]
        public void EachSnapshotCarriesTheCodeDucksAndCutoff()
        {
            var volumeOf = new Dictionary<string, string>();
            foreach (var group in OfClass("AudioMixerGroupController")) volumeOf[Field(group, "m_Name")] = Field(group, "m_Volume");
            string cutoff = null;
            foreach (var effect in OfClass("AudioMixerEffectController"))
                if (Field(effect, "m_EffectName") == "Lowpass Simple")
                    for (int i = 0; i < effect.Length - 1; i++)
                        if (effect[i].Trim() == "- m_ParameterName: Cutoff freq") cutoff = effect[i + 1].Trim().Substring("m_GUID: ".Length);
            Assert.IsNotNull(cutoff);

            var seen = new HashSet<string>();
            foreach (var snapshot in OfClass("AudioMixerSnapshotController"))
            {
                string name = Field(snapshot, "m_Name");
                seen.Add(name);
                var values = new Dictionary<string, float>();
                foreach (var line in snapshot)
                {
                    if (!line.StartsWith("    ") || !line.Contains(": ")) continue;
                    var parts = line.Trim().Split(new[] { ": " }, System.StringSplitOptions.None);
                    values[parts[0]] = float.Parse(parts[1], CultureInfo.InvariantCulture);
                }
                var state = (MixSnapshot)System.Enum.Parse(typeof(MixSnapshot), name);
                foreach (var bus in MixerDb.Buses)
                    Assert.AreEqual(MixerDb.From(AudioMix.Duck(bus, state)), values[volumeOf[MixerDb.Group(bus)]], 0.001f, name + " " + bus);
                Assert.AreEqual(AudioMix.LowpassHz(state), values[cutoff], 0.5f, name);
            }
            foreach (var state in MixerDb.Snapshots) Assert.IsTrue(seen.Contains(MixerDb.SnapshotName(state)), state.ToString());
        }

        [Test]
        public void TheAudioRoutesIntoTheMixerAndFallsBackWithoutIt()
        {
            string rig = Read("Assets/Scripts/Shell/MixerRig.cs");
            StringAssert.Contains("Resources.Load<AudioMixer>(MixerDb.ResourcePath)", rig);
            StringAssert.Contains("source.outputAudioMixerGroup = groups[(int)bus];", rig);
            StringAssert.Contains("mixer.SetFloat(param, db);", rig);
            string audio = Read("Assets/Scripts/Shell/AudioManager.cs");
            StringAssert.Contains("MixerRig.Route(source, AudioMix.BusOf(id));", audio);
            StringAssert.Contains("MixerRig.Route(bed, AudioMix.BusOf(id));", audio);
            StringAssert.Contains("MixerRig.Route(ambient, MixBus.Music);", audio);
            StringAssert.Contains("MixerRig.Snapshot(snapshot);\n            return MixSnapshot.Normal;", audio);
            StringAssert.Contains("MixerRig.Levels(music, sfx, ambience, ui);\n            music = sfx = ambience = ui = 1f;", audio);
            string settings = Read("Assets/Scripts/Core/SettingsService.cs");
            StringAssert.Contains("MixerRig.Master(masterVolume);", settings);
            StringAssert.Contains("AudioListener.volume = SettingsDraft.Heard(masterVolume, hasFocus);", settings);
            StringAssert.Contains("DUCK = {", Read("Tools/Audio/make_mixer.py"));
        }
    }
}
