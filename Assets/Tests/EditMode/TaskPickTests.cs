using NUnit.Framework;
using OutpostZero.Colony;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>
    /// PRO-56: a survivor left to their own call picks work from their skills, traits and the camp's needs.
    /// </summary>
    public class TaskPickTests
    {
        static Survivor Plain() => new Survivor { id = "a", displayName = "A", trait = "", morale = 70f, hunger = 80f, thirst = 80f };

        static TaskPick.Camp Calm() => new TaskPick.Camp { FoodPerHead = 4, Raw = 0, Injured = 0, RaidLikely = false, WorkWaiting = false, Scrap = 40 };

        [Test]
        public void TheWoundedTheWornAndTheMiserableRest()
        {
            var hurt = Plain();
            hurt.combat = 8;
            hurt.injury = 1;
            Assert.AreEqual("Rest", TaskPick.Choose(hurt, Calm()));

            var low = Plain();
            low.combat = 8;
            low.morale = 20f;
            Assert.AreEqual("Rest", TaskPick.Choose(low, Calm()));

            var worn = Plain();
            worn.combat = 8;
            worn.fatigue = 95f;
            Assert.AreEqual("Rest", TaskPick.Choose(worn, Calm()));
        }

        [Test]
        public void AMedicTendsTheInjuredAndIdlesOtherwise()
        {
            var medic = Plain();
            medic.trait = "Field Medic";
            medic.medicine = 4;
            var camp = Calm();
            Assert.AreNotEqual("Medic", TaskPick.Choose(medic, camp));
            camp.Injured = 2;
            Assert.AreEqual("Medic", TaskPick.Choose(medic, camp));
        }

        [Test]
        public void ACookCooksWhenThereIsRawFoodAndMouthsToFeed()
        {
            var cook = Plain();
            cook.trait = "Cook";
            cook.cooking = 4;
            var camp = Calm();
            Assert.AreNotEqual("Cook", TaskPick.Choose(cook, camp));
            camp.Raw = 3;
            camp.FoodPerHead = 1;
            Assert.AreEqual("Cook", TaskPick.Choose(cook, camp));
        }

        [Test]
        public void ARaidPullsFightersToTheWallButNotCowards()
        {
            var camp = Calm();
            camp.RaidLikely = true;
            var brave = Plain();
            brave.trait = "Brave";
            brave.combat = 2;
            Assert.AreEqual("Guard", TaskPick.Choose(brave, camp));

            var coward = Plain();
            coward.trait = "Cowardly";
            coward.combat = 2;
            Assert.AreNotEqual("Guard", TaskPick.Choose(coward, camp));
        }

        [Test]
        public void AnEngineerBuildsOnlyWhenWorkIsWaiting()
        {
            var engineer = Plain();
            engineer.trait = "Engineer";
            engineer.engineering = 4;
            var camp = Calm();
            Assert.AreNotEqual("Build", TaskPick.Choose(engineer, camp));
            camp.WorkWaiting = true;
            Assert.AreEqual("Build", TaskPick.Choose(engineer, camp));
        }

        [Test]
        public void AScroungerGoesOutWhenScrapRunsLow()
        {
            var scrounger = Plain();
            scrounger.trait = "Scrounger";
            scrounger.scavenge = 1;
            var camp = Calm();
            camp.Scrap = 5;
            Assert.AreEqual("Scavenge", TaskPick.Choose(scrounger, camp));
        }

        [Test]
        public void EveryPickIsATaskTheBoardRuns()
        {
            var camp = new TaskPick.Camp { FoodPerHead = 0, Raw = 5, Injured = 1, RaidLikely = true, WorkWaiting = true, Scrap = 0 };
            string[] traits = { "", "Brave", "Cook", "Field Medic", "Engineer", "Scrounger", "Loner", "Cowardly", "Insomniac" };
            foreach (var trait in traits)
            {
                for (int skill = 0; skill <= 8; skill += 4)
                {
                    var s = Plain();
                    s.trait = trait;
                    s.combat = s.cooking = s.medicine = s.engineering = s.scavenge = skill;
                    CollectionAssert.Contains(TaskPick.Tasks, TaskPick.Choose(s, camp));
                }
            }
            Assert.AreEqual("Rest", TaskPick.Choose(null, camp));
        }
    }
}
