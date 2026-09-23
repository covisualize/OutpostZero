using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Graphics;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class IdleDayTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        private static List<ColonistDay> Camp()
        {
            return new List<ColonistDay>
            {
                new ColonistDay { id = "a", trait = "Steady", task = "Guard", morale = 60f, hunger = 80f, thirst = 80f },
                new ColonistDay { id = "b", trait = "Steady", task = "Build", morale = 60f, hunger = 80f, thirst = 80f },
            };
        }

        [Test]
        public void OnlyADayWithNobodyOutIsIdle()
        {
            Assert.IsTrue(IdleDay.Idle(false, 2));
            Assert.IsFalse(IdleDay.Idle(true, 2));
            Assert.IsFalse(IdleDay.Idle(false, 1), "day 1 is spared");
        }

        [Test]
        public void AnIdleDayCostsFiveMoraleAndLeavesANote()
        {
            var busy = Camp();
            var still = Camp();
            int food = 10, water = 10, raw = 0;
            var busyNotes = ColonyDay.Simulate(busy, ref food, ref water, false, false, "", 0, ref raw, WeatherKind.Clear, false);
            food = 10; water = 10; raw = 0;
            var stillNotes = ColonyDay.Simulate(still, ref food, ref water, false, false, "", 0, ref raw, WeatherKind.Clear, true);
            for (int i = 0; i < busy.Count; i++)
                Assert.AreEqual(busy[i].morale - IdleDay.Mood, still[i].morale, 0.001f);
            CollectionAssert.DoesNotContain(busyNotes, "idle");
            CollectionAssert.Contains(stillNotes, "idle");
            Assert.AreEqual("Nadie salió: el campamento está inquieto", Loc.T("note.idle", "es"));
        }

        [Test]
        public void ClosingAnyExpeditionMarksTheDayAndTheSaveKeepsIt()
        {
            StringAssert.Contains("SurvivorRoster.Instance?.MarkOuting();", Read("Assets/Scripts/Core/GameManager.cs"));
            string roster = Read("Assets/Scripts/Colony/SurvivorRoster.cs");
            StringAssert.Contains("bool idle = IdleDay.Idle(wentOut || expeditionWon, endedDay);\n            wentOut = false;", roster);
            string save = Read("Assets/Scripts/Shell/SaveSystem.cs");
            StringAssert.Contains("data.wentOut = SurvivorRoster.Instance.WentOut;", save);
            StringAssert.Contains("SurvivorRoster.Instance?.SetWentOut(data.wentOut);", save);
            var data = new SaveGameData { wentOut = true };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(data), out var back, out var error), error);
            Assert.IsTrue(back.wentOut);
        }
    }
}
