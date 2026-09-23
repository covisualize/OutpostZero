using NUnit.Framework;
using OutpostZero.AI;

namespace OutpostZero.Tests.EditMode
{
    public class TensionLogTests
    {
        private static TensionLog Curve(params float[] tensions)
        {
            var log = new TensionLog();
            for (int i = 0; i < tensions.Length; i++) log.Add(i * 5f, tensions[i], HordeDirector.Evaluate(tensions[i]), i);
            return log;
        }

        [Test]
        public void ARiseAndFallCountsAsOnePeakThatEbbs()
        {
            var log = Curve(20f, 60f, 96f, 90f, 50f, 30f, 10f);
            Assert.AreEqual(1, log.Peaks());
            Assert.IsTrue(log.Ebbs());
            Assert.AreEqual(10f, log.Min());
            Assert.AreEqual(96f, log.Max());
        }

        [Test]
        public void AFlatOrRisingCurveDoesNotEbb()
        {
            Assert.IsFalse(Curve(10f, 12f, 14f).Ebbs());
            Assert.AreEqual(0, Curve(10f, 12f, 14f).Peaks());
            Assert.IsFalse(Curve(30f, 80f, 95f).Ebbs(), "still at the peak");
            Assert.AreEqual(2, Curve(80f, 30f, 90f, 20f).Peaks());
        }

        [Test]
        public void TheCsvHasOneRowPerSampleInInvariantNumbers()
        {
            var old = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-ES");
                string csv = Curve(20f, 96.25f).Csv();
                Assert.AreEqual("seconds,tension,state,alive\n0.0,20.0,Relax,0\n5.0,96.3,Peak,1\n", csv);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = old;
            }
        }

        [Test]
        public void TheScriptedFiveMinutesRiseTwiceAndEase()
        {
            var report = PressureClock.Run(2, 20f, 8f, 2);
            Assert.GreaterOrEqual(report.Peak, 2, "each firefight reaches a peak");
            Assert.GreaterOrEqual(report.Calm, 1);
        }

        [Test]
        public void ASurvivorStreetFeedsAboutOneKillEveryTenSeconds()
        {
            int scavenger = PressureClock.Run(1, 20f, 8f * DifficultyTable.Of(1).IntervalScale, 2).Spawns;
            int survivor = PressureClock.Run(2, 20f, 8f * DifficultyTable.Of(2).IntervalScale, 2).Spawns;
            int nightmare = PressureClock.Run(3, 20f, 8f * DifficultyTable.Of(3).IntervalScale, 2).Spawns;
            Assert.That(survivor, Is.InRange(27, 33), "300 s at one kill per 10 s is 30 bodies");
            Assert.Less(scavenger, survivor);
            Assert.Greater(nightmare, survivor * 3 / 2, "Nightmare is measurably harder");
        }
    }
}
