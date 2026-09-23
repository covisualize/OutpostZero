using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OutpostZero.Core;
using OutpostZero.Expedition;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>PRO-64: a Merciful save made on the street loads back onto the same street; Permadeath stays camp-only.</summary>
    public class StreetSnapshotTests
    {
        private static string Root => Directory.GetCurrentDirectory();

        private static StreetSnapshot.Run Sample()
        {
            return new StreetSnapshot.Run
            {
                Timer = 184.25f,
                Kills = 6,
                Scrap = 11,
                X = 12.5f,
                Z = -7.75f,
                Yaw = 270f,
                Health = 42.5f,
                Board = "meds=1,nest=2",
                Bodies = new List<StreetSnapshot.Body>
                {
                    new StreetSnapshot.Body { Variant = "Zombie_Walker", X = 3f, Z = 4f, Health = 60f },
                    new StreetSnapshot.Body { Variant = "Zombie_Brute", X = -9.5f, Z = 1.25f, Health = 140f },
                }
            };
        }

        [Test]
        public void OnlyALivingMercifulLeaderOnTheStreetIsKept()
        {
            Assert.IsTrue(StreetSnapshot.Keeps(true, GameState.ExpeditionActive, GameState.ExpeditionActive, true));
            Assert.IsTrue(StreetSnapshot.Keeps(true, GameState.Paused, GameState.ExpeditionActive, true));
            Assert.IsFalse(StreetSnapshot.Keeps(false, GameState.ExpeditionActive, GameState.ExpeditionActive, true));
            Assert.IsFalse(StreetSnapshot.Keeps(true, GameState.ExpeditionActive, GameState.ExpeditionActive, false));
            Assert.IsFalse(StreetSnapshot.Keeps(true, GameState.CampManagement, GameState.CampManagement, true));
            Assert.IsFalse(StreetSnapshot.Keeps(true, GameState.Paused, GameState.CampManagement, true));
            Assert.IsFalse(StreetSnapshot.Keeps(true, GameState.RaidActive, GameState.CampManagement, true));
        }

        [Test]
        public void MercifulOpensManualSavesOnTheStreetButPermadeathDoesNot()
        {
            Assert.IsTrue(SaveSlots.ManualAllowed(GameState.Paused, GameState.ExpeditionActive, true, true));
            Assert.IsFalse(SaveSlots.ManualAllowed(GameState.Paused, GameState.ExpeditionActive, false, true));
            Assert.IsFalse(SaveSlots.ManualAllowed(GameState.Paused, GameState.ExpeditionActive, true, false));
            Assert.IsTrue(SaveSlots.ManualAllowed(GameState.CampManagement, GameState.CampManagement, false, true));
            Assert.IsFalse(SaveSlots.ManualAllowed(GameState.RaidActive, GameState.CampManagement, true, true));
        }

        [Test]
        public void ARunRoundTrips()
        {
            Assert.IsTrue(StreetSnapshot.TryUnpack(StreetSnapshot.Pack(Sample()), out var run));
            Assert.AreEqual(184.25f, run.Timer, 0.001f);
            Assert.AreEqual(6, run.Kills);
            Assert.AreEqual(11, run.Scrap);
            Assert.AreEqual(12.5f, run.X, 0.001f);
            Assert.AreEqual(-7.75f, run.Z, 0.001f);
            Assert.AreEqual(270f, run.Yaw, 0.001f);
            Assert.AreEqual(42.5f, run.Health, 0.001f);
            Assert.AreEqual("meds=1,nest=2", run.Board);
            Assert.AreEqual(2, run.Bodies.Count);
            Assert.AreEqual("Zombie_Brute", run.Bodies[1].Variant);
            Assert.AreEqual(-9.5f, run.Bodies[1].X, 0.001f);
            Assert.AreEqual(140f, run.Bodies[1].Health, 0.001f);
        }

        [Test]
        public void EmptyBrokenOrDeadRunsAreRefused()
        {
            Assert.IsFalse(StreetSnapshot.TryUnpack("", out _));
            Assert.IsFalse(StreetSnapshot.TryUnpack("s0|1|2|3|4|5|6|7||", out _));
            Assert.IsFalse(StreetSnapshot.TryUnpack("s1|x|2|3|4|5|6|7||", out _));
            var dead = Sample();
            dead.Health = 0f;
            Assert.IsFalse(StreetSnapshot.TryUnpack(StreetSnapshot.Pack(dead), out _));
        }

        [Test]
        public void DeadOrNamelessBodiesAreDroppedAndSeparatorsCannotSplitAName()
        {
            var run = Sample();
            run.Bodies.Add(new StreetSnapshot.Body { Variant = "Zombie_Runner", X = 1f, Z = 1f, Health = 0f });
            run.Bodies.Add(new StreetSnapshot.Body { Variant = "", X = 1f, Z = 1f, Health = 30f });
            run.Bodies.Add(new StreetSnapshot.Body { Variant = "Bad|Na;me:", X = 1f, Z = 1f, Health = 30f });
            Assert.IsTrue(StreetSnapshot.TryUnpack(StreetSnapshot.Pack(run), out var back));
            Assert.AreEqual(3, back.Bodies.Count);
            Assert.AreEqual("BadName", back.Bodies[2].Variant);
        }

        [Test]
        public void AVariantIsThePrefabNameWithoutTheCloneSuffix()
        {
            Assert.AreEqual("Zombie_Walker", StreetSnapshot.Variant("Zombie_Walker", "Zombie_Walker(Clone)"));
            Assert.AreEqual("Zombie_Crawler", StreetSnapshot.Variant("", "Zombie_Crawler(Clone)"));
            Assert.AreEqual("Zombie_Crawler", StreetSnapshot.Variant(null, "Zombie_Crawler (Clone)(Clone)"));
        }

        [Test]
        public void BoardProgressRoundTripsByObjectiveId()
        {
            var specs = new[]
            {
                new ObjectiveSpec("meds", ObjectiveKind.Collect, "Medical", 2, true, 4, "", "Find meds"),
                new ObjectiveSpec("nest", ObjectiveKind.ClearNest, "nest", 3, false, 0, "", "Clear the nest"),
            };
            var board = new ObjectiveBoard(specs);
            board.Note(ObjectiveKind.Collect, "Medical", 1);
            board.Note(ObjectiveKind.ClearNest, "nest", 2);
            string packed = board.PackProgress();
            Assert.AreEqual("meds=1,nest=2", packed);

            var fresh = new ObjectiveBoard(new[] { specs[1], specs[0] });
            fresh.RestoreProgress(packed + ",gone=4,nest=x,meds=-1");
            Assert.AreEqual(2, fresh.Progress(0));
            Assert.AreEqual(1, fresh.Progress(1));

            var capped = new ObjectiveBoard(specs);
            capped.RestoreProgress("nest=99");
            Assert.IsTrue(capped.Done(1));
        }

        [Test]
        public void TheSaveHooksAreWired()
        {
            string save = File.ReadAllText(Path.Combine(Root, "Assets/Scripts/Shell/SaveSystem.cs"));
            string installer = File.ReadAllText(Path.Combine(Root, "Assets/Scripts/Core/GameSystemsInstaller.cs"));
            string game = File.ReadAllText(Path.Combine(Root, "Assets/Scripts/Core/GameManager.cs"));
            StringAssert.Contains("StreetRun.Instance?.Resume()", save);
            Assert.Greater(save.IndexOf("StreetRun.Instance?.Resume()"), save.IndexOf("SetState(GameState.CampManagement)"));
            StringAssert.Contains("Add<StreetRun>(", installer);
            StringAssert.Contains("public bool ResumeExpedition(StreetSnapshot.Run run)", game);
            StringAssert.Contains("bool fromCamp = currentState == GameState.CampManagement && !resumed;", game);
        }
    }
}
