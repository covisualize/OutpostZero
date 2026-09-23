using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Graphics;

namespace OutpostZero.Tests.EditMode
{
    public class HeightFogTests
    {
        private const string FullScreenPassScript = "b00045f12942b46c698459096c89274e";

        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        private static string Guid(string relative)
        {
            return Regex.Match(Read(relative + ".meta"), "^guid: ([0-9a-f]{32})$", RegexOptions.Multiline).Groups[1].Value;
        }

        [Test]
        public void FogPoolsLowAndThinsOnTheRooftops()
        {
            float a = HeightFog.Thickness(WeatherKind.Fog, 0f);
            float rise = HeightFog.Rise(WeatherKind.Fog);
            float street = HeightFog.Amount(a, rise, 1.7f, 0.2f, 40f);
            float roof = HeightFog.Amount(a, rise, 12f, 12f, 40f);
            Assert.Greater(street, roof * 2f);
            Assert.LessOrEqual(street, HeightFog.Opacity);
        }

        [Test]
        public void NothingInsideTheStartAndTheEndCapsTheRay()
        {
            float a = HeightFog.Thickness(WeatherKind.Storm, 1f);
            float rise = HeightFog.Rise(WeatherKind.Storm);
            Assert.AreEqual(0f, HeightFog.Amount(a, rise, 1.7f, 0f, HeightFog.Start - 0.5f));
            Assert.AreEqual(HeightFog.Amount(a, rise, 1.7f, 1.7f, HeightFog.End), HeightFog.Amount(a, rise, 1.7f, 1.7f, 500f), 1e-6f);
            Assert.Greater(HeightFog.Amount(a, rise, 1.7f, 1.7f, 30f), HeightFog.Amount(a, rise, 1.7f, 1.7f, 12f));
        }

        [Test]
        public void BadWeatherAndNightThickenIt()
        {
            float clear = HeightFog.Amount(HeightFog.Thickness(WeatherKind.Clear, 0f), HeightFog.Rise(WeatherKind.Clear), 1.7f, 0.5f, 40f);
            float rain = HeightFog.Amount(HeightFog.Thickness(WeatherKind.Rain, 0f), HeightFog.Rise(WeatherKind.Rain), 1.7f, 0.5f, 40f);
            float fog = HeightFog.Amount(HeightFog.Thickness(WeatherKind.Fog, 0f), HeightFog.Rise(WeatherKind.Fog), 1.7f, 0.5f, 40f);
            Assert.Less(clear, rain);
            Assert.Less(rain, fog);
            Assert.Greater(HeightFog.Thickness(WeatherKind.Clear, 1f), HeightFog.Thickness(WeatherKind.Clear, 0f));
        }

        [Test]
        public void TheLevelRayMatchesTheSlopedLimit()
        {
            float a = HeightFog.Thickness(WeatherKind.Rain, 0f);
            float level = HeightFog.Depth(a, 3f, 1.7f, 1.7f, 30f);
            float sloped = HeightFog.Depth(a, 3f, 1.7f, 1.72f, 30f);
            Assert.AreEqual(level, sloped, level * 0.01f);
        }

        [Test]
        public void BothRenderersRunTheHeightFogPassAfterOpaques()
        {
            string material = Guid("Assets/Materials/OutpostHeightFog.mat");
            Assert.AreEqual(32, material.Length);
            foreach (string renderer in new[] { "Assets/Settings/OutpostZero_URP_Renderer.asset", "Assets/Settings/OutpostZero_URP_Renderer_Lite.asset" })
            {
                string text = Read(renderer);
                var block = Regex.Match(text, "^--- !u!114 &(\\d+)\\n((?:(?!^--- ).*\\n)*)", RegexOptions.Multiline);
                string id = null, body = null;
                for (; block.Success; block = block.NextMatch())
                {
                    if (!block.Groups[2].Value.Contains("guid: " + FullScreenPassScript + ",")) continue;
                    id = block.Groups[1].Value;
                    body = block.Groups[2].Value;
                }
                Assert.IsNotNull(id, renderer + " has a full-screen pass");
                StringAssert.Contains("m_Name: HeightFog\n", body);
                StringAssert.Contains("m_Active: 1\n", body);
                StringAssert.Contains("injectionPoint: 450\n", body, "before transparents, so glass and mist sheets stay on top");
                StringAssert.Contains("requirements: 1\n", body, "needs the depth texture");
                StringAssert.Contains("fetchColorBuffer: 0\n", body, "blends over the target without a copy");
                StringAssert.Contains("passMaterial: {fileID: 2100000, guid: " + material + ", type: 2}", body);
                StringAssert.Contains("  - {fileID: " + id + "}\n", text);
                string map = Regex.Match(text, "m_RendererFeatureMap: ([0-9a-f]*)\\n").Groups[1].Value;
                string hex = System.BitConverter.ToString(System.BitConverter.GetBytes(long.Parse(id))).Replace("-", "").ToLowerInvariant();
                Assert.IsTrue(map.Length % 16 == 0 && Regex.IsMatch(map, "^(?:.{16})*" + hex), renderer + " feature map lists the pass");
                Assert.AreEqual(Regex.Matches(text, "^  - \\{fileID: \\d+\\}$", RegexOptions.Multiline).Count * 16, map.Length);
            }
        }

        [Test]
        public void TheMaterialRunsTheHeightFogShader()
        {
            string shader = Guid("Assets/Shaders/OutpostHeightFog.shader");
            StringAssert.Contains("m_Shader: {fileID: 4800000, guid: " + shader + ", type: 3}", Read("Assets/Materials/OutpostHeightFog.mat"));
            string source = Read("Assets/Shaders/OutpostHeightFog.shader");
            StringAssert.Contains("Shader \"OutpostZero/HeightFog\"", source);
            StringAssert.Contains("DeclareDepthTexture.hlsl", source);
            StringAssert.Contains("Blit.hlsl", source);
            foreach (string id in new[] { HeightFog.ColorId, HeightFog.ParamsId, HeightFog.RangeId })
                StringAssert.Contains("float4 " + id + ";", source);
        }
    }
}
