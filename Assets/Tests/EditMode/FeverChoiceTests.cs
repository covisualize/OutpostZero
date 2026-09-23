using System.Collections.Generic;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Graphics;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>PRO-57: stage 3 of the fever in camp is a choice between quarantine and mercy.</summary>
    public class FeverChoiceTests
    {
        [Test]
        public void MercyIsOfferedOnlyAtStageThreeAndNeverForTheLeader()
        {
            Assert.IsFalse(FeverChoice.Offered(true, false, 2));
            Assert.IsTrue(FeverChoice.Offered(true, false, 3));
            Assert.IsFalse(FeverChoice.Offered(true, true, 3), "the leader's death is succession, not a camp choice");
            Assert.IsFalse(FeverChoice.Offered(false, false, 3));
        }

        [Test]
        public void TheCampGrievesAChosenDeathLessThanALoss()
        {
            var camp = new List<ColonistDay>
            {
                new ColonistDay { id = "jonas", bond = "Close to Mara", alive = true, morale = 80f },
                new ColonistDay { id = "priya", bond = "", alive = true, morale = 70f },
                new ColonistDay { id = "ellis", bond = "", alive = true, morale = 5f },
            };
            FeverChoice.Mourn(camp, "Mara Quill", false);
            Assert.AreEqual(50f, camp[0].morale, "a friend loses 40 less 10");
            Assert.AreEqual(55f, camp[1].morale, "the camp loses 25 less 10");
            Assert.AreEqual(0f, camp[2].morale);
            Assert.AreEqual(5f, FeverChoice.Loss(false, true), "a memorial wall eases it further");
        }

        [Test]
        public void TheDayFlagsAFeverThatTurns()
        {
            var camp = new List<ColonistDay>
            {
                new ColonistDay { id = "a", name = "Jonas Reed", task = "Scavenge", bond = "", injury = 3 },
                new ColonistDay { id = "b", name = "Priya Sen", task = "Guard", bond = "" },
            };
            int food = 10, water = 10, raw = 0;
            var notes = ColonyDay.Simulate(camp, ref food, ref water, false, false, "", 0, ref raw, WeatherKind.Clear);
            CollectionAssert.Contains(notes, "stage3");
            Assert.AreEqual("Quarantine", camp[0].task);
            Assert.IsTrue(camp[0].alive, "quarantine keeps them a day, which is the window for the choice");
        }

        [Test]
        public void TheChoiceReadsInBothLanguages()
        {
            foreach (string key in new[] { "note.stage3", "camp.mercy", "camp.released" })
            {
                Assert.AreNotEqual(key, Loc.Raw(key, "en"), key);
                Assert.AreNotEqual(key, Loc.Raw(key, "es"), key);
            }
        }
    }
}
