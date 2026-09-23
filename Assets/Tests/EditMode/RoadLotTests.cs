using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OutpostZero.Expedition;
using OutpostZero.Shell;
using UnityEngine;

namespace OutpostZero.Tests.EditMode
{
    public class RoadLotTests
    {
        static readonly string[] Districts =
        {
            "ash_market", "commercial_strip", "rail_yard", "water_plant", "old_hospital",
            "police_station", "north_gate", "highway_overpass", "downtown_core", "mall"
        };

        static IEnumerable<(RoadGraph.Map map, RoadGraph.Cell cell)> Lots()
        {
            for (int seed = 1; seed <= 12; seed++)
            {
                foreach (var id in Districts)
                {
                    var map = RoadGraph.Build(seed * 97, id);
                    if (map.Cells == null) continue;
                    foreach (var cell in map.Cells.Where(c => c.Kind == "lot")) yield return (map, cell);
                }
            }
        }

        static void Extent(KitPiece piece, KitPlacement placement, out float minX, out float maxX, out float minZ, out float maxZ)
        {
            minX = minZ = float.MaxValue;
            maxX = maxZ = float.MinValue;
            float rad = placement.yaw * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            foreach (var box in piece.colliders)
            {
                foreach (var (x, z) in new[] { (box.x, box.z), (box.x + box.w, box.z), (box.x, box.z + box.d), (box.x + box.w, box.z + box.d) })
                {
                    float wx = placement.x + x * cos + z * sin;
                    float wz = placement.z - x * sin + z * cos;
                    minX = Mathf.Min(minX, wx);
                    maxX = Mathf.Max(maxX, wx);
                    minZ = Mathf.Min(minZ, wz);
                    maxZ = Mathf.Max(maxZ, wz);
                }
            }
        }

        [Test]
        public void EveryLotHouseUsesKnownKitPieces()
        {
            var book = KitMeshTests.Book();
            int count = 0;
            foreach (var (map, cell) in Lots())
            {
                var house = KitPlan.Lot(map.Seed, cell.X, cell.Z, map.Footprint);
                Assert.IsTrue(KitPlan.UsesOnlyKnownPieces(book.pieces, house), map.Id);
                count++;
            }
            Assert.Greater(count, 0);
        }

        [Test]
        public void LotHouseIsDeterministicAndVaried()
        {
            var fronts = new HashSet<string>();
            var heights = new HashSet<int>();
            foreach (var (map, cell) in Lots())
            {
                var a = KitPlan.Lot(map.Seed, cell.X, cell.Z, map.Footprint);
                var b = KitPlan.Lot(map.Seed, cell.X, cell.Z, map.Footprint);
                Assert.AreEqual(a.Length, b.Length);
                for (int i = 0; i < a.Length; i++)
                {
                    Assert.AreEqual(a[i].id, b[i].id);
                    Assert.AreEqual(a[i].yaw, b[i].yaw);
                }
                fronts.Add(a[1].id);
                heights.Add(a.Count(p => p.id == "floor"));
            }
            Assert.GreaterOrEqual(fronts.Count, 3);
            Assert.GreaterOrEqual(heights.Count, 2);
        }

        [Test]
        public void LotHouseHasFloorsWallsAndRoof()
        {
            Assert.AreEqual(2, KitPlan.Lot(5, 24f, 8f, "apartment").Count(p => p.id == "floor"));
            Assert.AreEqual(1, KitPlan.Lot(5, 24f, 8f, "warehouse").Count(p => p.id == "floor"));
            foreach (var (map, cell) in Lots())
            {
                var house = KitPlan.Lot(map.Seed, cell.X, cell.Z, map.Footprint);
                int storeys = house.Count(p => p.id == "floor");
                Assert.AreEqual(storeys * 4, house.Count(p => p.id.StartsWith("wall")), map.Id);
                Assert.IsTrue(house.Any(p => p.id == "roof" && Mathf.Approximately(p.y, storeys * KitPlan.Storey)), map.Id);
                Assert.IsTrue(KitPlan.ReachesStorey(house, 0f));
            }
        }

        [Test]
        public void LotHouseStaysInsideItsLotAndClearOfTheCrate()
        {
            var book = KitMeshTests.Book();
            const float half = 1.25f;
            foreach (var (map, cell) in Lots())
            {
                var house = KitPlan.Lot(map.Seed, cell.X, cell.Z, map.Footprint);
                foreach (var placement in house)
                {
                    var piece = KitPlan.Find(book.pieces, placement.id);
                    Extent(piece, placement, out var minX, out var maxX, out var minZ, out var maxZ);
                    float offset = KitPlan.LotTile * 0.5f;
                    minX -= offset; maxX -= offset; minZ -= offset; maxZ -= offset;
                    Assert.GreaterOrEqual(minX, -half - 0.001f, placement.id);
                    Assert.LessOrEqual(maxX, half + 0.001f, placement.id);
                    Assert.GreaterOrEqual(minZ, -half - 0.001f, placement.id);
                    Assert.LessOrEqual(maxZ, half + 0.001f, placement.id);
                    Assert.Greater(minZ, -1.55f, "the road crate sits 1.9 m south of the lot centre");
                }
            }
        }

        [Test]
        public void LotWallsCloseEveryFace()
        {
            var book = KitMeshTests.Book();
            var house = KitPlan.Lot(3, 12f, 8f, "storefront");
            var faces = new HashSet<string>();
            foreach (var placement in house.Where(p => p.id.StartsWith("wall") && p.y < 0.5f))
            {
                Extent(KitPlan.Find(book.pieces, placement.id), placement, out var minX, out var maxX, out var minZ, out var maxZ);
                if (maxZ - minZ < 0.3f) faces.Add(minZ < 1f ? "south" : "north");
                else if (maxX - minX < 0.3f) faces.Add(minX < 1f ? "west" : "east");
            }
            CollectionAssert.AreEquivalent(new[] { "south", "north", "west", "east" }, faces);
        }
    }
}
