using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using OutpostZero.Graphics;

namespace OutpostZero.Utils
{
    /// <summary>
    /// The fixed camera marks of the 30 s screenshot tour CI runs on every PR, so graphics changes can be
    /// reviewed without opening Unity. Positions are in arena space; each mark also pins weather and dusk.
    /// </summary>
    public static class ScreenTour
    {
        public const float Hold = 5f;
        public const float Settle = 1.5f;

        public struct Mark
        {
            public string Name;
            public Vector3 Position;
            public Vector3 Target;
            public WeatherKind Weather;
            public float Night;
        }

        public static readonly Mark[] Marks =
        {
            new Mark { Name = "01_overview", Position = new Vector3(0f, 30f, -26f), Target = new Vector3(0f, 0f, 0f), Weather = WeatherKind.Clear, Night = 0f },
            new Mark { Name = "02_street", Position = new Vector3(-6f, 9f, -18f), Target = new Vector3(0f, 1f, 2f), Weather = WeatherKind.Overcast, Night = 0.2f },
            new Mark { Name = "03_sanctuary", Position = new Vector3(-14f, 14f, -20f), Target = new Vector3(-6f, 0f, -8f), Weather = WeatherKind.Clear, Night = 0.35f },
            new Mark { Name = "04_rain", Position = new Vector3(8f, 7f, -10f), Target = new Vector3(0f, 0.5f, 4f), Weather = WeatherKind.Rain, Night = 0.4f },
            new Mark { Name = "05_fog_dusk", Position = new Vector3(18f, 11f, 6f), Target = new Vector3(0f, 1f, 0f), Weather = WeatherKind.Fog, Night = 0.55f },
            new Mark { Name = "06_night_storm", Position = new Vector3(0f, 16f, -14f), Target = new Vector3(0f, 0f, 2f), Weather = WeatherKind.Storm, Night = 1f },
        };

        public static float Length => Marks.Length * Hold;

        /// <summary>Which mark the camera sits on at this point of the tour, or -1 once it is over.</summary>
        public static int At(float seconds)
        {
            if (seconds < 0f) return 0;
            int index = (int)(seconds / Hold);
            return index < Marks.Length ? index : -1;
        }

        /// <summary>Where the shots go: -screenshotDir, then OUTPOST_SCREENSHOTS, then persistentDataPath/Screenshots.</summary>
        public static string Folder(string[] args, string environment, string fallback)
        {
            for (int i = 0; args != null && i + 1 < args.Length; i++)
            {
                if (args[i] == "-screenshotDir" && !string.IsNullOrEmpty(args[i + 1])) return args[i + 1];
            }
            if (!string.IsNullOrEmpty(environment)) return environment;
            return Path.Combine(fallback ?? "", "Screenshots");
        }

        public static bool Requested(string[] args, string environment)
        {
            if (environment == "1" || environment == "true") return true;
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-screenshotTour") return true;
            }
            return false;
        }

        public static string Index(string[] names, string version)
        {
            var text = new StringBuilder();
            text.Append("{\n  \"version\": \"").Append(version ?? "").Append("\",\n  \"shots\": [");
            for (int i = 0; names != null && i < names.Length; i++)
            {
                if (i > 0) text.Append(',');
                text.Append("\n    \"").Append(names[i]).Append('"');
            }
            return text.Append("\n  ]\n}\n").ToString();
        }
    }

    /// <summary>
    /// F12 saves a screenshot. With <c>-screenshotTour</c> (or OUTPOST_TOUR=1) it flies the <see cref="ScreenTour"/> marks,
    /// saves one PNG per mark plus index.json, and quits a player build when <c>-quitAfterTour</c> is passed.
    /// </summary>
    public class AutoScreenCapture : MonoBehaviour
    {
        private string folder;
        private bool touring;

        public bool Touring => touring;
        public bool Finished { get; private set; }
        public string Folder => folder;

        private void Start()
        {
            var args = Environment.GetCommandLineArgs();
            folder = ScreenTour.Folder(args, Environment.GetEnvironmentVariable("OUTPOST_SCREENSHOTS"), Application.persistentDataPath);
            if (ScreenTour.Requested(args, Environment.GetEnvironmentVariable("OUTPOST_TOUR"))) Begin();
        }

        public void Begin(string overrideFolder = null)
        {
            if (touring) return;
            if (!string.IsNullOrEmpty(overrideFolder)) folder = overrideFolder;
            StartCoroutine(Tour(Array.IndexOf(Environment.GetCommandLineArgs(), "-quitAfterTour") >= 0));
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
                StartCoroutine(SaveAtEnd("manual_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png"));
        }

        private IEnumerator SaveAtEnd(string name)
        {
            yield return new WaitForEndOfFrame();
            Save(name);
        }

        private IEnumerator Tour(bool quit)
        {
            touring = true;
            Finished = false;
            Directory.CreateDirectory(folder);
            var cam = Camera.main;
            Behaviour brain = cam != null ? cam.GetComponent<Unity.Cinemachine.CinemachineBrain>() : null;
            bool brainWas = brain != null && brain.enabled;
            if (brain != null) brain.enabled = false;
            var names = new string[ScreenTour.Marks.Length];
            for (int i = 0; i < ScreenTour.Marks.Length; i++)
            {
                var mark = ScreenTour.Marks[i];
                if (cam != null) cam.transform.SetPositionAndRotation(mark.Position, Quaternion.LookRotation(mark.Target - mark.Position, Vector3.up));
                WeatherController.Instance?.SetFor(mark.Weather, 600f);
                if (DayNightCycle.Instance != null) DayNightCycle.Instance.Hold = mark.Night;
                yield return new WaitForSecondsRealtime(ScreenTour.Settle);
                yield return new WaitForEndOfFrame();
                names[i] = mark.Name + ".png";
                Save(names[i]);
                yield return new WaitForSecondsRealtime(ScreenTour.Hold - ScreenTour.Settle);
            }
            if (DayNightCycle.Instance != null) DayNightCycle.Instance.Hold = -1f;
            if (brain != null) brain.enabled = brainWas;
            File.WriteAllText(Path.Combine(folder, "index.json"), ScreenTour.Index(names, OutpostZero.Shell.BuildStamp.Version));
            Debug.Log("[ScreenTour] " + names.Length + " shots in " + folder);
            touring = false;
            Finished = true;
            if (quit && !Application.isEditor) Application.Quit(0);
        }

        private void Save(string name)
        {
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, name);
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            if (shot == null) return;
            File.WriteAllBytes(path, shot.EncodeToPNG());
            Destroy(shot);
        }
    }
}
