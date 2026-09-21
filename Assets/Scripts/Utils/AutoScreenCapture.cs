using UnityEngine;

namespace OutpostZero.Utils
{
    public class AutoScreenCapture : MonoBehaviour
    {
        private float timer = 0f;
        private int shotsTaken = 0;
        private string captureDir = @"C:\Users\User\.gemini\antigravity\brain\e24cf1f9-24c6-4305-bc0d-830bf97f35b3";

        void Update()
        {
            timer += Time.deltaTime;
            // Take 3 screenshots at 1s, 2.5s, and 4s
            if (shotsTaken == 0 && timer > 1.0f)
            {
                TakeScreenshot("screenshot_overview.png");
                shotsTaken++;
            }
            else if (shotsTaken == 1 && timer > 2.5f)
            {
                TakeScreenshot("screenshot_action.png");
                shotsTaken++;
            }
            else if (shotsTaken == 2 && timer > 4.0f)
            {
                TakeScreenshot("screenshot_base.png");
                shotsTaken++;
            }

            if (Input.GetKeyDown(KeyCode.F12) || Input.GetKeyDown(KeyCode.P))
            {
                string path = System.IO.Path.Combine(captureDir, $"screenshot_manual_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log($"[AutoScreenCapture] Manual screenshot saved: {path}");
            }
        }

        private void TakeScreenshot(string filename)
        {
            string path = System.IO.Path.Combine(captureDir, filename);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[AutoScreenCapture] Screenshot saved to: {path}");
        }
    }
}
