using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Player;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>
    /// PRO-55: the F4 roster sheet shows each survivor's traits, skills, needs, wounds and task.
    /// </summary>
    public class RosterSheetTests
    {
        static Survivor Mara() => new Survivor
        {
            id = "mara_12",
            displayName = "Mara Quill",
            trait = "Brave",
            aside = "Cook",
            leader = true,
            age = 34,
            combat = 2,
            cooking = 4,
            leadership = 3,
            hunger = 77.6f,
            thirst = 60f,
            fatigue = 12f,
            morale = 72f,
            opinion = 18,
            task = "Lead"
        };

        [Test]
        public void ALeaderLineCarriesTraitsSkillsNeedsAndTask()
        {
            string line = RosterSheet.Line(Mara(), "en");
            StringAssert.StartsWith("[Leader] Mara Quill (34) - Brave, Cook", line);
            StringAssert.Contains("Leads 3", line);
            StringAssert.Contains("Hunger 78  Thirst 60  Fatigue 12  Morale 72", line);
            StringAssert.Contains("opinion 18", line);
            StringAssert.DoesNotContain("Hurt", line);
        }

        [Test]
        public void EachSkillShowsExperienceTowardItsNextLevel()
        {
            var mara = Mara();
            mara.combatXp = 0;
            mara.cooking = 5;
            mara.cookingXp = 2;
            mara.medicineXp = 1;
            mara.engineering = Practice.Cap;
            string line = RosterSheet.Line(mara, "en");
            StringAssert.Contains("Guard 2 0/1", line);
            StringAssert.Contains("Cook 5 2/3", line);
            StringAssert.Contains("Medic 0 1/1", line);
            StringAssert.Contains("Build 10  ", line);
            StringAssert.Contains("Leads 3 0/1", line);
            StringAssert.DoesNotContain("Scavenge", line);
        }

        [Test]
        public void AWoundShowsAndTheDeadShowOnlyWhoTheyWere()
        {
            var hurt = Mara();
            hurt.leader = false;
            hurt.injury = 2;
            string line = RosterSheet.Line(hurt, "en");
            StringAssert.StartsWith("Mara Quill", line);
            StringAssert.Contains("Hurt 2", line);

            var dead = Mara();
            dead.alive = false;
            Assert.AreEqual("[Leader] Mara Quill (34) - Brave, Cook | Dead", RosterSheet.Line(dead, "en"));
        }

        [Test]
        public void SpanishSheetUsesTheStringTable()
        {
            string line = RosterSheet.Line(Mara(), "es");
            StringAssert.StartsWith("[Líder] Mara Quill", line);
            StringAssert.Contains("Hambre 78", line);
        }

        [Test]
        public void EmptyRosterGivesNoLines()
        {
            Assert.AreEqual(0, RosterSheet.Lines(null, "en").Length);
            Assert.AreEqual("", RosterSheet.Line(null, "en"));
        }

        [Test]
        public void F4IsReservedForTheSheet()
        {
            CollectionAssert.Contains(ControlBindings.Reserved, UnityEngine.InputSystem.Key.F4);
        }
    }
}
