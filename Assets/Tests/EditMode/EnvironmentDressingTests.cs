using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using OutpostZero.Expedition;
using OutpostZero.Graphics;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class EnvironmentDressingTests
    {
        private static string Read(params string[] parts)
        {
            var all = new List<string> { Directory.GetCurrentDirectory() };
            all.AddRange(parts);
            return File.ReadAllText(Path.Combine(all.ToArray())).Replace("\r\n", "\n");
        }

        [Test]
        public void PoissonPointsKeepTheirSpacingInsideTheBox()
        {
            var points = PoissonScatter.Sample(42, -5f, 5f, -3f, 3f, 0.7f, null, null);
            Assert.Greater(points.Count, 60, "a 10 x 6 m box fills well past 60 points at 0.7 m");
            for (int i = 0; i < points.Count; i++)
            {
                Assert.That(points[i].X, Is.InRange(-5f, 5f));
                Assert.That(points[i].Z, Is.InRange(-3f, 3f));
                for (int j = i + 1; j < points.Count; j++)
                {
                    float dx = points[i].X - points[j].X;
                    float dz = points[i].Z - points[j].Z;
                    Assert.GreaterOrEqual(dx * dx + dz * dz, 0.7f * 0.7f - 1e-4f);
                }
            }
        }

        [Test]
        public void TheSameSeedScattersTheSamePoints()
        {
            var a = PoissonScatter.Sample(7, 0f, 8f, 0f, 8f, 0.9f, null, null);
            var b = PoissonScatter.Sample(7, 0f, 8f, 0f, 8f, 0.9f, null, null);
            var c = PoissonScatter.Sample(8, 0f, 8f, 0f, 8f, 0.9f, null, null);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].X, b[i].X);
                Assert.AreEqual(a[i].Z, b[i].Z);
            }
            Assert.AreNotEqual(a[0].X, c[0].X);
        }

        [Test]
        public void TheMaskPeaksAtWallsAndFadesToTheOpenRoad()
        {
            var mask = DebrisMask.Street(new[] { 0f, 0f }, 0.1f, 0.9f, 1.5f);
            Assert.AreEqual(0.9f, mask.At(0f, 0f), 1e-4f);
            Assert.Less(mask.At(1.5f, 0f), 0.5f);
            Assert.AreEqual(0.1f, mask.At(10f, 0f), 1e-3f);
            Assert.AreEqual(0.1f, DebrisMask.Street(null, 0.1f, 0.9f, 1.5f).At(3f, 3f));
        }

        [Test]
        public void LitterGathersAgainstWallsAndCurbs()
        {
            var anchors = new List<float>();
            DressingPlan.Perimeter(anchors, 4f, 6f, 10f, 9f, 1f);
            var row = DebrisProfile.Default("ash_market");
            var marks = DressingPlan.Debris(row, anchors.ToArray());
            var mask = DebrisMask.Street(anchors.ToArray(), row.baseDensity, row.edgeDensity, row.reach);
            int near = 0;
            int far = 0;
            foreach (var mark in marks)
            {
                Assert.IsTrue(DressingPlan.OnTheStreet(mark.X, mark.Z));
                if (mask.Nearest(mark.X, mark.Z) < 1.5f) near++;
                else if (mask.Nearest(mark.X, mark.Z) > 4f) far++;
            }
            float nearArea = 0f;
            float farArea = 0f;
            for (float x = DressingPlan.DebrisMinX; x < DressingPlan.DebrisMaxX; x += 0.25f)
            {
                for (float z = DressingPlan.DebrisMinZ; z < DressingPlan.DebrisMaxZ; z += 0.25f)
                {
                    if (!DressingPlan.OnTheStreet(x, z)) continue;
                    float d = mask.Nearest(x, z);
                    if (d < 1.5f) nearArea += 0.0625f;
                    else if (d > 4f) farArea += 0.0625f;
                }
            }
            Assert.Greater(near / nearArea, 2.5f * far / farArea, "litter per square metre, wall band vs open road");
            Assert.IsTrue(DressingPlan.Spaced(marks, row.spacing));
        }

        [Test]
        public void PerimeterRingsTheFootprint()
        {
            var points = new List<float>();
            DressingPlan.Perimeter(points, 0f, 0f, 4f, 2f, 1f);
            Assert.AreEqual(0, points.Count % 2);
            Assert.AreEqual((5 * 2 + 1 * 2) * 2, points.Count);
            for (int i = 0; i < points.Count; i += 2)
            {
                bool edge = points[i] == 0f || points[i] == 4f || points[i + 1] == 0f || points[i + 1] == 2f;
                Assert.IsTrue(edge, points[i] + "," + points[i + 1]);
            }
        }

        [Test]
        public void EveryDistrictHasACommittedScatterRow()
        {
            string asset = Read("Assets", "Resources", "DebrisProfile.asset");
            string guid = Regex.Match(Read("Assets", "Scripts", "Expedition", "DebrisProfile.cs.meta"), @"guid: (\w+)").Groups[1].Value;
            StringAssert.Contains("guid: " + guid, asset);
            foreach (var node in CampaignBoard.All())
                StringAssert.Contains("- district: " + node.Id + "\n", asset, node.Id);
            foreach (Match spacing in Regex.Matches(asset, @"spacing: ([\d.]+)"))
                Assert.GreaterOrEqual(float.Parse(spacing.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), 0.5f);
        }

        [Test]
        public void LitterIsInstancedExceptKickableBottles()
        {
            Assert.IsFalse(DebrisField.Instanced("bottle"));
            foreach (var role in new[] { "rubble", "paper", "tyre", "brick", "glass" })
            {
                Assert.IsTrue(DebrisField.Instanced(role), role);
                Assert.AreNotEqual(SurfaceFamily.None, DebrisLook.FamilyFor(role), role);
            }
            string street = Read("Assets", "Scripts", "Expedition", "StreetDetail.cs");
            StringAssert.Contains("DebrisProfile.For(districtId)", street);
            StringAssert.Contains("Litter(debris, parent)", street);
            StringAssert.Contains("RenderMeshInstanced", Read("Assets", "Scripts", "Expedition", "DebrisField.cs"));
        }

        [Test]
        public void TheYardEndsAtAChainLinkFence()
        {
            var posts = MapRim.Posts();
            Assert.Greater(posts.Count, 40);
            foreach (var post in posts)
            {
                bool onLine = Mathf.Abs(Mathf.Abs(post.x) - MapRim.Line) < 1e-3f || Mathf.Abs(Mathf.Abs(post.y) - MapRim.Line) < 1e-3f;
                Assert.IsTrue(onLine, post.ToString());
                Assert.IsTrue(MapRim.Inside(post.x, post.y));
            }
            for (int i = 1; i < posts.Count / 4; i++)
                Assert.LessOrEqual(Vector2.Distance(posts[i], posts[i - 1]), MapRim.PostEvery + 1e-3f);
            Assert.AreEqual(SurfaceFamily.ChainLink, MaterialLibrary.FamilyFor("Mat_Fence_ChainLink"));
            Assert.IsFalse(MaterialLibrary.HasTriplanar(SurfaceFamily.ChainLink));
            StringAssert.Contains("renderer.enabled = false", Read("Assets", "Scripts", "Graphics", "MapRim.cs"));
        }

        [Test]
        public void FenceUvsRunInMetres()
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            MapRim.Strip(12f, 0.3f, 2.6f, vertices, uvs, triangles);
            Assert.AreEqual(4, vertices.Count);
            Assert.AreEqual(6, triangles.Count);
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(vertices[i].x + 6f, uvs[i].x, 1e-4f);
                Assert.AreEqual(vertices[i].y, uvs[i].y, 1e-4f);
            }
        }

        [Test]
        public void TheSkylineHasBillboardsAndAWaterTowerOnLegs()
        {
            var horizon = DressingPlan.Horizon();
            int boards = 0;
            int legs = 0;
            int posts = 0;
            DressingPlan.Mark tank = default;
            foreach (var mark in horizon)
            {
                if (mark.Role == "billboard") boards++;
                if (mark.Role == "billboard_post") posts++;
                if (mark.Role == "tower_leg") legs++;
                if (mark.Role == "tower") tank = mark;
                if (mark.Role == "billboard" || mark.Role == "tower") Assert.Greater(mark.Z, 22f);
                Assert.IsTrue(MapRim.Inside(mark.X, mark.Z), mark.Role);
            }
            Assert.AreEqual(2, boards);
            Assert.AreEqual(4, posts);
            Assert.AreEqual(4, legs);
            Assert.Greater(tank.Y, 3f, "the tank stands on its legs");
            Assert.AreEqual(SurfaceFamily.MetalPainted, StreetDetail.FamilyFor("billboard"));
            Assert.AreEqual(SurfaceFamily.MetalRusted, StreetDetail.FamilyFor("tower_leg"));
        }
    }
}
