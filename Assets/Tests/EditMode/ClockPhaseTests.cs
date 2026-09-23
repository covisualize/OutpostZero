using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Core;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>
    /// PRO-56: the clock runs Morning, Day, Evening and Night, the night matches the raid window,
    /// and only the camp autosaves when a phase turns.
    /// </summary>
    public class ClockPhaseTests
    {
        [TestCase(5f, DayPhase.Morning)]
        [TestCase(9.99f, DayPhase.Morning)]
        [TestCase(10f, DayPhase.Day)]
        [TestCase(16.5f, DayPhase.Day)]
        [TestCase(17f, DayPhase.Evening)]
        [TestCase(19.9f, DayPhase.Evening)]
        [TestCase(20f, DayPhase.Night)]
        [TestCase(2f, DayPhase.Night)]
        [TestCase(30f, DayPhase.Morning)]
        public void HoursFallInTheirPhase(float hour, DayPhase phase)
        {
            Assert.AreEqual(phase, ClockPhase.Of(hour));
        }

        [Test]
        public void NightIsExactlyTheRaidWindow()
        {
            for (float hour = 0f; hour < 24f; hour += 0.25f)
                Assert.AreEqual(RaidWatch.Night(hour), ClockPhase.Of(hour) == DayPhase.Night, "hour " + hour);
        }

        [Test]
        public void OnlyTheCampAutosavesOnATurn()
        {
            Assert.IsTrue(ClockPhase.Autosaves(GameState.CampManagement));
            Assert.IsFalse(ClockPhase.Autosaves(GameState.ExpeditionActive));
            Assert.IsFalse(ClockPhase.Autosaves(GameState.RaidActive));
            Assert.IsFalse(ClockPhase.Autosaves(GameState.MainMenu));
        }

        [Test]
        public void PhaseNamesComeFromTheStringTable()
        {
            Assert.AreEqual("Evening", ClockPhase.Name(DayPhase.Evening, "en"));
            Assert.AreEqual("Noche", ClockPhase.Name(DayPhase.Night, "es"));
        }
    }
}
