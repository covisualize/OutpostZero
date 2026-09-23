using System.IO;
using NUnit.Framework;
using OutpostZero.Core;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class LookSensitivityTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        [Test]
        public void SensitivityRunsFromHalfToDoubleAndDefaultsToOne()
        {
            Assert.AreEqual(1f, PlayOptions.Sensitivity(0f));
            Assert.AreEqual(1f, PlayOptions.Sensitivity(float.NaN));
            Assert.AreEqual(0.5f, PlayOptions.Sensitivity(0.2f));
            Assert.AreEqual(2f, PlayOptions.Sensitivity(3f));
            Assert.AreEqual(1.4f, PlayOptions.Sensitivity(1.4f), 0.0001f);
        }

        [Test]
        public void TheCampPanScalesAndInvertFlipsItsDepth()
        {
            PlayOptions.Pan(1f, 1f, 2f, false, out float x, out float z);
            Assert.AreEqual(2f, x, 0.0001f);
            Assert.AreEqual(2f, z, 0.0001f);
            PlayOptions.Pan(1f, 1f, 1f, true, out x, out z);
            Assert.AreEqual(1f, x, 0.0001f);
            Assert.AreEqual(-1f, z, 0.0001f);
        }

        [Test]
        public void SensitivityIsSavedInTheSettingsFile()
        {
            var snap = SettingsFile.Defaults();
            Assert.AreEqual(1f, snap.sensitivity);
            snap.sensitivity = 1.75f;
            Assert.IsTrue(SettingsFile.TryFromJson(SettingsFile.ToJson(snap), out var back));
            Assert.AreEqual(1.75f, back.sensitivity, 0.0001f);
            Assert.IsTrue(SettingsFile.TryFromJson("{\"shake\":1}", out var old));
            Assert.AreEqual(1f, old.sensitivity);
        }

        [Test]
        public void TheSliderCameraAndStickReadIt()
        {
            StringAssert.Contains("settings.SetSensitivity", Read("Assets/Scripts/UI/OutpostInterface.cs"));
            StringAssert.Contains("PlayOptions.Pan(sx, sz, settings != null ? settings.Sensitivity : 1f, settings != null && settings.InvertLook", Read("Assets/Scripts/Player/CameraTargetDriver.cs"));
            StringAssert.Contains("AimBlend() * sense", Read("Assets/Scripts/Player/PlayerController.cs"));
            Assert.AreEqual("Look sensitivity", Loc.T("set.sensitivity", "en"));
            Assert.AreEqual("Sensibilidad", Loc.T("set.sensitivity", "es"));
        }
    }
}
