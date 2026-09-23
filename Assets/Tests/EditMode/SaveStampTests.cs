using System;
using System.IO;
using NUnit.Framework;
using OutpostZero.Core;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class SaveStampTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        [Test]
        public void TheClockRunsOnlyWhileARunIsPlayed()
        {
            Assert.AreEqual(10.5f, SaveStamp.Tick(10f, 0.5f, GameState.CampManagement), 1e-5f);
            Assert.AreEqual(10.5f, SaveStamp.Tick(10f, 0.5f, GameState.ExpeditionActive), 1e-5f);
            Assert.AreEqual(10.5f, SaveStamp.Tick(10f, 0.5f, GameState.RaidActive), 1e-5f);
            foreach (var idle in new[] { GameState.MainMenu, GameState.Paused, GameState.GameOver, GameState.Victory })
                Assert.AreEqual(10f, SaveStamp.Tick(10f, 0.5f, idle), idle.ToString());
            Assert.AreEqual(11f, SaveStamp.Tick(10f, 30f, GameState.CampManagement), 1e-5f, "a hitch or a sleep adds at most a second");
            Assert.AreEqual(0f, SaveStamp.Tick(float.NaN, -1f, GameState.CampManagement));
        }

        [Test]
        public void TheSlotLineShowsTimePlayedAndWhenItWasWritten()
        {
            Assert.AreEqual("<1m", SaveStamp.Played(12f));
            Assert.AreEqual("14m", SaveStamp.Played(14f * 60f + 5f));
            Assert.AreEqual("2h 04m", SaveStamp.Played(2f * 3600f + 4f * 60f));

            var when = new DateTime(2026, 9, 23, 6, 10, 42, DateTimeKind.Utc);
            string stamp = SaveStamp.Now(when);
            Assert.AreEqual("2026-09-23T06:10:42Z", stamp);
            Assert.IsTrue(SaveStamp.TryWhen(stamp, out var back));
            Assert.AreEqual(when, back);
            Assert.AreEqual("2h 04m  2026-09-23 06:10", SaveStamp.Line(7440f, stamp, TimeZoneInfo.Utc));
            Assert.AreEqual("14m", SaveStamp.Line(840f, "", TimeZoneInfo.Utc), "a save from before stamps shows only the time played");
            Assert.AreEqual("14m", SaveStamp.Line(840f, "yesterday", TimeZoneInfo.Utc));
        }

        [Test]
        public void AnOversizedThumbnailIsDroppedNotSaved()
        {
            Assert.AreEqual("abc", SaveStamp.Keep("abc"));
            Assert.AreEqual("", SaveStamp.Keep(null));
            Assert.AreEqual("", SaveStamp.Keep(new string('A', SaveStamp.ThumbCap + 1)));
            Assert.AreEqual(128, SaveStamp.ThumbSide);
            var fresh = new SaveGameData();
            Assert.AreEqual("", fresh.thumbnail);
            Assert.AreEqual("", fresh.savedAt);
            Assert.AreEqual(0f, fresh.playtime);
        }

        [Test]
        public void SavesCarryTheStampAndTheLoadScreenShowsIt()
        {
            string save = Read("Assets/Scripts/Shell/SaveSystem.cs");
            StringAssert.Contains("data.thumbnail = SaveThumb.Take();", save);
            StringAssert.Contains("data.savedAt = SaveStamp.Now(", save);
            StringAssert.Contains("data.playtime = playtime;", save);
            StringAssert.Contains("card.Thumbnail = data.thumbnail", save);
            StringAssert.Contains("SaveSystem.Instance?.ResetPlaytime();", Read("Assets/Scripts/Core/GameManager.cs"));
            string ui = Read("Assets/Scripts/UI/OutpostInterface.cs");
            StringAssert.Contains("SlotRow(card, Button(label", ui);
            StringAssert.Contains("SlotRow(auto, Button(autoLabel", ui);
            StringAssert.Contains("SaveThumb.Read(key)", ui);
            string thumb = Read("Assets/Scripts/Shell/SaveThumb.cs");
            StringAssert.Contains("camera.Render();", thumb);
            StringAssert.Contains("SaveStamp.ThumbSide", thumb);
        }
    }
}
