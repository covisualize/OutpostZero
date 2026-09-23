using NUnit.Framework;
using OutpostZero.Graphics;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>
    /// PRO-65: the brightness calibration strip follows the exposure the slider sets.
    /// </summary>
    public class BrightnessCheckTests
    {
        [Test]
        public void TheDefaultShowsTheTargetMarksAndHidesTheFaintest()
        {
            Assert.AreEqual(BrightnessCheck.Target, BrightnessCheck.Seen(1f));
            Assert.Less(BrightnessCheck.Shade(BrightnessCheck.Marks[0], 1f), BrightnessCheck.SeenFloor);
        }

        [Test]
        public void TooDarkHidesAMarkAndTooBrightShowsThemAll()
        {
            Assert.Less(BrightnessCheck.Seen(0.6f), BrightnessCheck.Target);
            Assert.AreEqual(BrightnessCheck.Marks.Length, BrightnessCheck.Seen(1.4f));
        }

        [Test]
        public void MarksBrightenWithTheSliderAndStayOrdered()
        {
            for (int i = 0; i < BrightnessCheck.Marks.Length; i++)
                Assert.Less(BrightnessCheck.Shade(BrightnessCheck.Marks[i], 0.8f), BrightnessCheck.Shade(BrightnessCheck.Marks[i], 1.2f));
            for (int i = 1; i < BrightnessCheck.Marks.Length; i++)
                Assert.Less(BrightnessCheck.Shade(BrightnessCheck.Marks[i - 1], 1f), BrightnessCheck.Shade(BrightnessCheck.Marks[i], 1f));
        }
    }
}
