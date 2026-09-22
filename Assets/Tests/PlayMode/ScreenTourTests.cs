using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using OutpostZero.Utils;

namespace OutpostZero.Tests.PlayMode
{
    public class ScreenTourTests
    {
        /// <summary>CI sets OUTPOST_SCREENSHOTS so the shots land in the uploaded artifacts folder.</summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator TourSavesOneShotPerMarkAndAnIndex()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("No graphics device (-nographics), so there is nothing to capture.");

            string folder = Environment.GetEnvironmentVariable("OUTPOST_SCREENSHOTS");
            if (string.IsNullOrEmpty(folder)) folder = Path.Combine(Application.temporaryCachePath, "ScreenTour");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);

            yield return SceneManager.LoadSceneAsync("PrototypeArena", LoadSceneMode.Single);
            yield return null;

            var capture = UnityEngine.Object.FindFirstObjectByType<AutoScreenCapture>();
            if (capture == null) capture = new GameObject("ScreenCaptureManager").AddComponent<AutoScreenCapture>();
            yield return null;
            capture.Begin(folder);

            float deadline = Time.realtimeSinceStartup + ScreenTour.Length + 20f;
            while (!capture.Finished && Time.realtimeSinceStartup < deadline) yield return null;

            Assert.IsTrue(capture.Finished, "tour did not finish in time");
            foreach (var mark in ScreenTour.Marks)
            {
                string path = Path.Combine(folder, mark.Name + ".png");
                Assert.IsTrue(File.Exists(path), path);
                Assert.Greater(new FileInfo(path).Length, 1024, path);
            }
            StringAssert.Contains("06_night_storm.png", File.ReadAllText(Path.Combine(folder, "index.json")));
        }
    }
}
