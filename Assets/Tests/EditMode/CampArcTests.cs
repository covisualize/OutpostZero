using System.Collections.Generic;
using NUnit.Framework;
using OutpostZero.Colony;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>
    /// PRO-57 acceptance on the day rules alone: a starving, grieving camp breaks within about three days,
    /// while a fed camp with a cook, a cot and a won expedition holds up.
    /// </summary>
    public class CampArcTests
    {
        static List<ColonistDay> Camp(string task)
        {
            return new List<ColonistDay>
            {
                new ColonistDay { id = "lead", name = "Mara Quill", trait = "Watchful", task = "Lead", leader = true, bond = "" },
                new ColonistDay { id = "a", name = "Jonas Reed", trait = "Scrounger", task = task, bond = "Close to Mara" },
                new ColonistDay { id = "b", name = "Priya Sen", trait = "Cook", task = task == "Rest" ? "Rest" : "Cook", bond = "" },
                new ColonistDay { id = "c", name = "Ellis Ward", trait = "Field Medic", task = task == "Rest" ? "Rest" : "Medic", bond = "" },
            };
        }

        static bool Broken(List<ColonistDay> people)
        {
            foreach (var person in people)
                if (!person.alive || person.morale < 10f) return true;
            return false;
        }

        [Test]
        public void AStarvingGrievingCampBreaksWithinThreeDays()
        {
            var people = Camp("Rest");
            int food = 0;
            int water = 0;
            int raw = 0;
            var seen = new List<string>();
            int brokeOn = 0;
            for (int day = 1; day <= 4 && brokeOn == 0; day++)
            {
                string fallen = day == 1 ? "Dell Moss" : "";
                seen.AddRange(ColonyDay.Simulate(people, ref food, ref water, false, false, fallen, 2, ref raw));
                if (Broken(people)) brokeOn = day;
            }
            Assert.That(brokeOn, Is.InRange(1, 3), "the camp should break by day 3");
            CollectionAssert.Contains(seen, "breakdown");
        }

        [Test]
        public void AFedCampWithACotAndACookHoldsForFiveDays()
        {
            var people = Camp("Guard");
            int food = 40;
            int water = 40;
            int raw = 10;
            for (int day = 1; day <= 5; day++)
            {
                ColonyDay.Simulate(people, ref food, ref water, true, day % 2 == 1, "", 0, ref raw);
                Assert.IsFalse(Broken(people), "day " + day);
            }
            float total = 0f;
            foreach (var person in people) total += person.morale;
            Assert.Greater(total / people.Count, 50f);
        }

        [Test]
        public void TheYardCastsTheColonistModel()
        {
            string root = System.IO.Directory.GetCurrentDirectory();
            string Guid(string path) => System.Text.RegularExpressions.Regex.Match(
                System.IO.File.ReadAllText(System.IO.Path.Combine(root, path + ".meta")), @"guid: (\w+)").Groups[1].Value;
            string cast = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets/Resources/CampCast.asset"));
            StringAssert.Contains("guid: " + Guid("Assets/Scripts/Colony/CampCast.cs"), cast);
            StringAssert.Contains("mate: {fileID: 3657165292428549128, guid: " + Guid("Assets/Prefabs/Characters/Colonist_Survivor.prefab") + ", type: 3}", cast);
            StringAssert.Contains("--- !u!1 &3657165292428549128", System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets/Prefabs/Characters/Colonist_Survivor.prefab")));
            Assert.AreEqual("Colonist_Survivor", CampMateBody.ModelId);
        }
    }
}
