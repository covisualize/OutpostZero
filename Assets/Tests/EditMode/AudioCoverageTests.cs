using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.AI;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Graphics;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class AudioCoverageTests
    {
        static string Scripts => Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts");

        static IEnumerable<(string file, string text)> Sources() =>
            Directory.GetFiles(Scripts, "*.cs", SearchOption.AllDirectories)
                .Select(f => (Path.GetFileName(f), File.ReadAllText(f)));

        static void Registered(IEnumerable<string> ids, string where)
        {
            var missing = ids.Where(id => !string.IsNullOrEmpty(id) && !ClipBook.Has(id)).Distinct().ToArray();
            CollectionAssert.IsEmpty(missing, where);
        }

        [Test]
        public void EveryLiteralIdPlayedHasAClip()
        {
            var call = new Regex(@"\b(Play|PlayAt|GetClip|AddBed|AddWorld)\(\s*""([a-z_]+)""");
            var hold = new Regex(@"\bHold\(\s*\w+,\s*""([a-z_]+)""");
            var hits = new List<string>();
            int seen = 0;
            foreach (var (file, text) in Sources())
            {
                foreach (Match m in call.Matches(text))
                {
                    seen++;
                    if (!ClipBook.Has(m.Groups[2].Value)) hits.Add(file + ": " + m.Groups[2].Value);
                }
                foreach (Match m in hold.Matches(text))
                {
                    seen++;
                    if (!ClipBook.Has(m.Groups[1].Value)) hits.Add(file + ": " + m.Groups[1].Value);
                }
            }
            Assert.Greater(seen, 20, "the scan found the play calls");
            CollectionAssert.IsEmpty(hits, "ids played with no clip");
        }

        [Test]
        public void EveryStingMomentHasACue()
        {
            var sting = new Regex(@"\bSting\(\s*""([a-z_]+)""");
            var moments = Sources().SelectMany(s => sting.Matches(s.text).Cast<Match>().Select(m => m.Groups[1].Value)).Distinct().ToArray();
            CollectionAssert.IsSupersetOf(moments, new[] { "kill", "death", "extract", "raid", "dawn" });
            foreach (var moment in moments)
            {
                string cue = MusicStem.Cue(moment);
                Assert.IsNotEmpty(cue, moment);
                Assert.IsTrue(ClipBook.Has(cue), cue);
            }
        }

        [Test]
        public void EveryIdTheMixerNamesIsRegistered()
        {
            var compare = new Regex(@"\bid\s*==\s*""([a-z_]+)""");
            foreach (var name in new[] { "AudioManager.cs", "ClipBook.cs", "AudioMix.cs" })
            {
                string path = Directory.GetFiles(Scripts, name, SearchOption.AllDirectories).Single();
                var ids = compare.Matches(File.ReadAllText(path)).Cast<Match>().Select(m => m.Groups[1].Value);
                Registered(ids, name);
            }
        }

        [Test]
        public void EveryRegisteredIdHasItsOwnTone()
        {
            string path = Directory.GetFiles(Scripts, "ClipBook.cs", SearchOption.AllDirectories).Single();
            string text = File.ReadAllText(path);
            int tone = text.IndexOf("public static float Tone(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(tone, 0);
            var branches = new HashSet<string>(new Regex(@"\bid\s*==\s*""([a-z_]+)""").Matches(text.Substring(tone)).Cast<Match>().Select(m => m.Groups[1].Value));
            var bare = ClipBook.Ids.Where(id => !branches.Contains(id)).ToArray();
            CollectionAssert.IsEmpty(bare, "ids that fall through to plain noise");
            Assert.AreEqual(ClipBook.Ids.Length, ClipBook.Ids.Distinct().Count(), "ids listed twice");
        }

        [Test]
        public void EveryComputedIdHasAClip()
        {
            var ids = new List<string>();
            foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
            {
                ids.Add(ClipBook.Fire(type));
                ids.Add(SwingCue.Sound(type));
                ids.Add(BrassCue.Sound(type));
            }
            foreach (SurfaceKind kind in Enum.GetValues(typeof(SurfaceKind))) ids.Add(SurfaceTag.StepId(kind));
            foreach (HazardKind kind in Enum.GetValues(typeof(HazardKind))) ids.Add(BlastWake.Sound(kind));
            foreach (LootKind kind in Enum.GetValues(typeof(LootKind))) ids.Add(LootTake.Sound(kind));
            foreach (var face in new[] { "flesh", "metal", "wood", "stone", "" }) ids.Add(StrikeFace.Sound(face));
            foreach (var breed in new[] { "walker", "runner", "brute" })
            {
                ids.Add(ZombieVoice.Idle(breed));
                ids.Add(ZombieVoice.Bite(breed));
                ids.Add(ZombieVoice.Hurt(breed));
                ids.Add(ZombieVoice.Death(breed));
            }
            foreach (WeatherKind kind in Enum.GetValues(typeof(WeatherKind)))
            foreach (var district in new[] { "", "ash_market", "old_hospital" })
                ids.Add(AshFall.Bed(kind, district));
            foreach (var surface in new[] { "", "Metal_Grate", "plank", "puddle", "glass", "rubble", "road", "tile" })
                ids.Add(AudioMix.StepId(surface));
            Registered(ids, "computed ids");
        }

        [Test]
        public void BlindCuesStayDistinct()
        {
            var weapons = new[] { "gun", "shotgun", "rifle", "smg", "swing" };
            var steps = Enum.GetValues(typeof(SurfaceKind)).Cast<SurfaceKind>().Select(SurfaceTag.StepId).Distinct().ToArray();
            var breeds = new[] { "walker", "runner", "brute" }.Select(ZombieVoice.Idle).ToArray();
            foreach (var group in new[] { weapons, steps, breeds })
            {
                var marks = group.Select(ClipBook.Mark).ToArray();
                Assert.AreEqual(group.Length, marks.Distinct().Count(), string.Join(",", group));
            }
            Assert.Greater(AudioSpace.MaxDistance("gun_far"), AudioSpace.MaxDistance("clink"));
            Assert.AreEqual(0f, AudioSpace.SpatialBlend("heart"));
            Assert.AreEqual(1f, AudioSpace.SpatialBlend("groan"));
        }

        [Test]
        public void LibraryPicksAVariantOrFallsBack()
        {
            Assert.AreEqual(-1, SfxLibrary.Variant(0, 5));
            Assert.AreEqual(2, SfxLibrary.Variant(3, 5));
            Assert.AreEqual(1, SfxLibrary.Variant(3, -2));
            Assert.AreEqual(0, SfxLibrary.Variant(1, int.MinValue));
            CollectionAssert.IsEmpty(SfxLibrary.Stray(null));
            CollectionAssert.IsEmpty(SfxLibrary.Stray(new[] { "gun", "step_metal" }));
            CollectionAssert.AreEqual(new[] { "nope", "gun" }, SfxLibrary.Stray(new[] { "gun", "nope", "gun" }));
        }

        [Test]
        public void AttributionNamesTheLibrary()
        {
            string doc = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "docs", "AUDIO_ATTRIBUTION.md"));
            StringAssert.Contains("SfxLibrary", doc);
            StringAssert.Contains(SfxLibrary.ResourcePath, doc);
        }
    }
}
