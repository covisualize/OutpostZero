using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Graphics;
using OutpostZero.Player;

namespace OutpostZero.Tests.EditMode
{
    public class DecalSystemTests
    {
        private static string Root => Directory.GetCurrentDirectory();

        private static string Read(params string[] parts)
        {
            var all = new List<string> { Root };
            all.AddRange(parts);
            return File.ReadAllText(Path.Combine(all.ToArray())).Replace("\r\n", "\n");
        }

        [Test]
        public void TheAtlasTableMatchesTheGeneratorAndTheIssuesSplatCounts()
        {
            var expected = new Dictionary<string, int>
            {
                { DecalAtlas.Blood, 8 }, { DecalAtlas.Drip, 4 },
                { DecalAtlas.HoleConcrete, 3 }, { DecalAtlas.HoleMetal, 3 }, { DecalAtlas.HoleWood, 3 },
                { DecalAtlas.Scorch, 2 }, { DecalAtlas.Oil, 2 }, { DecalAtlas.Footprint, 2 },
                { DecalAtlas.Blob, 1 }, { DecalAtlas.Aim, 1 },
            };
            foreach (var pair in expected) Assert.AreEqual(pair.Value, DecalAtlas.Count(pair.Key), pair.Key);
            Assert.LessOrEqual(DecalAtlas.Total, DecalAtlas.Columns * DecalAtlas.Rows);

            string generator = Read("BlenderScripts", "decal_atlas.py");
            var rows = Regex.Matches(generator, "\\(\"(\\w+)\", (\\d+)\\),");
            Assert.AreEqual(expected.Count, rows.Count, "one LAYOUT row per kind");
            int first = 0;
            foreach (Match row in rows)
            {
                string kind = row.Groups[1].Value;
                int count = int.Parse(row.Groups[2].Value);
                Assert.AreEqual(count, DecalAtlas.Count(kind), kind);
                Assert.AreEqual(first, DecalAtlas.First(kind), kind + " starts where the generator draws it");
                first += count;
            }
            StringAssert.Contains("COLUMNS = " + DecalAtlas.Columns, generator);
            StringAssert.Contains("ROWS = " + DecalAtlas.Rows, generator);
        }

        [Test]
        public void CellsMapToTheirUvsWithTheTopRowAtTheTopOfTheTexture()
        {
            Assert.AreEqual(0.125f, DecalAtlas.ScaleU, 1e-6f);
            Assert.AreEqual(0.25f, DecalAtlas.ScaleV, 1e-6f);
            Assert.AreEqual(0f, DecalAtlas.BiasU(0), 1e-6f);
            Assert.AreEqual(0.75f, DecalAtlas.BiasV(0), 1e-6f, "PNG row 0 is the top quarter of UV space");
            Assert.AreEqual(0.5f, DecalAtlas.BiasV(8), 1e-6f);
            Assert.AreEqual(0.25f, DecalAtlas.BiasU(26), 1e-6f);
            Assert.AreEqual(0f, DecalAtlas.BiasV(26), 1e-6f);

            for (int seed = -7; seed < 40; seed++)
            {
                int cell = DecalAtlas.Cell(DecalAtlas.HoleMetal, seed);
                Assert.GreaterOrEqual(cell, DecalAtlas.First(DecalAtlas.HoleMetal));
                Assert.Less(cell, DecalAtlas.First(DecalAtlas.HoleMetal) + 3);
            }
            Assert.AreEqual(-1, DecalAtlas.Cell("confetti", 3));
            var blood = new HashSet<int>();
            for (int seed = 0; seed < 8; seed++) blood.Add(DecalAtlas.Cell(DecalAtlas.Blood, seed));
            Assert.AreEqual(8, blood.Count, "consecutive splats use every blood take");
        }

        [Test]
        public void BulletHolesFollowTheSurfaceTheyStruck()
        {
            Assert.AreEqual(DecalAtlas.HoleMetal, DecalAtlas.HoleFor("metal"));
            Assert.AreEqual(DecalAtlas.HoleWood, DecalAtlas.HoleFor("wood"));
            Assert.AreEqual(DecalAtlas.HoleConcrete, DecalAtlas.HoleFor("concrete"));
            Assert.AreEqual("metal", StrikeFace.OfSurface(SurfaceKind.Metal));
            Assert.AreEqual("wood", StrikeFace.OfSurface(SurfaceKind.Wood));
            Assert.AreEqual("concrete", StrikeFace.OfSurface(SurfaceKind.Gravel));
            Assert.IsNull(StrikeFace.OfSurface(SurfaceKind.Default), "an untagged prefab falls back to its name");
            StringAssert.Contains("GetComponentInParent<SurfaceTag>()", Read("Assets", "Scripts", "Combat", "StrikeFace.cs"));

            Assert.AreEqual("hole", ImpactDecalPool.StayKind(DecalAtlas.HoleWood));
            Assert.AreEqual("scorch", ImpactDecalPool.StayKind(DecalAtlas.Scorch));
            Assert.AreEqual("oil", ImpactDecalPool.StayKind(DecalAtlas.Oil));
            Assert.AreEqual("blood", ImpactDecalPool.StayKind(DecalAtlas.Drip));
            Assert.AreEqual("blood", ImpactDecalPool.StayKind(DecalAtlas.Footprint));
        }

        [Test]
        public void TheBudgetRecyclesByFadingNotPopping()
        {
            Assert.AreEqual(400, DecalBudget.Cap(QualityProfile.For(3).Decals), "Ultra holds the issue's 400 projectors");
            Assert.AreEqual(DecalBudget.Ceiling, DecalBudget.Cap(10000));
            Assert.AreEqual(1, DecalBudget.Cap(0));
            Assert.AreEqual(DecalBudget.Fade, 2f);

            Assert.AreEqual(1f, DecalBudget.Retire(0f));
            Assert.AreEqual(0.5f, DecalBudget.Retire(1f), 1e-4f);
            Assert.AreEqual(0f, DecalBudget.Retire(2f));
            float last = 1f;
            for (float t = 0f; t <= 2f; t += 1f / 60f)
            {
                float now = DecalBudget.Retire(t);
                Assert.LessOrEqual(now, last + 1e-6f);
                Assert.Less(last - now, 0.05f, "no frame drops a mark by more than 5% opacity");
                last = now;
            }
            Assert.IsTrue(DecalBudget.Retired(2f));
            Assert.IsFalse(DecalBudget.Retired(1.99f));

            int cap = DecalBudget.Cap(QualityProfile.For(1).Decals);
            Assert.IsTrue(DecalBudget.OverBudget(cap, cap));
            Assert.IsTrue(DecalBudget.CanGrow(cap, cap), "a new mark borrows a slot while the oldest fades");
            Assert.IsFalse(DecalBudget.CanGrow(cap + DecalBudget.Slack(cap), cap));
            Assert.GreaterOrEqual(DecalBudget.Slack(40), 8);
        }

        [Test]
        public void KillsMarkTheWallBehindAndBlastsScorchTheGround()
        {
            Assert.AreEqual(0, BackSplat.Count(0, true), "gore off leaves walls clean");
            Assert.AreEqual(1, BackSplat.Count(1, false));
            Assert.AreEqual(3, BackSplat.Count(1, true), "a reduced-gore shotgun sprays three");
            Assert.AreEqual(5, BackSplat.Count(2, true), "a full-gore shotgun sprays five");
            BackSplat.Spray(0, 5, out float yaw, out float pitch);
            Assert.AreEqual(0f, yaw);
            Assert.AreEqual(0f, pitch);
            var seen = new HashSet<string>();
            for (int i = 1; i < 5; i++)
            {
                BackSplat.Spray(i, 5, out yaw, out pitch);
                Assert.LessOrEqual(yaw * yaw + pitch * pitch, BackSplat.Cone * BackSplat.Cone + 1e-3f);
                seen.Add(yaw.ToString("F1") + "," + pitch.ToString("F1"));
            }
            Assert.AreEqual(4, seen.Count, "the cone's splats don't stack");
            Assert.IsTrue(BackSplat.Drips(0f), "blood runs down a standing wall");
            Assert.IsFalse(BackSplat.Drips(1f), "not across the floor");

            Assert.GreaterOrEqual(BlastScorch.SizeFor(PipeBlast.Radius), 5f);
            Assert.AreEqual(8f, BlastScorch.SizeFor(8f));

            string pool = Read("Assets", "Scripts", "Graphics", "ImpactDecalPool.cs");
            StringAssert.Contains("CombatEvents.OnBlast += OnBlast", pool);
            StringAssert.Contains("SplatterBehind(", pool);
            StringAssert.Contains("AddComponent<DecalProjector>()", pool);
            StringAssert.Contains("CombatEvents.RaiseBlast(blast, PipeBlast.Radius)", Read("Assets", "Scripts", "Player", "PlayerInteractor.cs"));
            StringAssert.Contains("CombatEvents.RaiseBlast(origin, radius)", Read("Assets", "Scripts", "Combat", "DestructibleHazard.cs"));
        }

        [Test]
        public void TheDecalMaterialProjectsTheCommittedAtlas()
        {
            string atlasMeta = Read("Assets", "Textures", "Decals", "DecalAtlas.png.meta");
            string atlasGuid = Regex.Match(atlasMeta, "guid: (\\w+)").Groups[1].Value;
            StringAssert.Contains("alphaIsTransparency: 1", atlasMeta);
            StringAssert.Contains("wrapU: 1", atlasMeta);

            string material = Read("Assets", "Resources", "Decals", "DecalAtlas.mat");
            StringAssert.Contains("guid: 9b4e681081e2b4c469111bb649e2f7ee", material, "URP's Decal shader graph");
            StringAssert.Contains("m_EnableInstancingVariants: 1", material, "URP decals need GPU instancing");
            StringAssert.Contains("- Base_Map:\n        m_Texture: {fileID: 2800000, guid: " + atlasGuid, material);
            StringAssert.EndsWith("Decals/DecalAtlas", DecalAtlas.MaterialPath);

            byte[] png = File.ReadAllBytes(Path.Combine(Root, "Assets", "Textures", "Decals", "DecalAtlas.png"));
            int width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
            int height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
            Assert.AreEqual(1024, width);
            Assert.AreEqual(512, height);
            Assert.AreEqual(6, png[25], "RGBA");

            StringAssert.Contains("technique: 0", Read("Assets", "Settings", "OutpostZero_URP_Renderer.asset"), "Automatic picks D-Buffer where it can");
        }
    }
}
