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

        static readonly string[] Props = { "shelf", "counter", "fridge", "pallet", "desk", "chair", "filing", "hospital_bed", "lockers", "workbench" };

        static IEnumerable<RoadGraph.Map> Maps()
        {
            for (int seed = 1; seed <= 12; seed++)
            {
                foreach (var id in Districts)
                {
                    var map = RoadGraph.Build(seed * 97, id);
                    if (map.Cells != null) yield return map;
                }
            }
        }

        static IEnumerable<(RoadGraph.Map map, RoadGraph.Block block, KitPlacement[] house)> Buildings()
        {
            foreach (var map in Maps())
            {
                foreach (var block in RoadGraph.Blocks(map))
                    yield return (map, block, KitPlan.Building(map.Seed, block.X, block.Z, block.Cols, block.Rows, map.Footprint, block.Front));
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

        static bool IsWall(string id) => id.StartsWith("wall");

        static string FaceOf(KitPiece piece, KitPlacement placement, int wide, int deep)
        {
            Extent(piece, placement, out var minX, out var maxX, out var minZ, out var maxZ);
            float w = wide * KitPlan.LotTile, d = deep * KitPlan.LotTile;
            if (maxZ - minZ < 0.3f) return minZ < 0.5f ? "south" : Mathf.Abs(maxZ - d) < 0.01f ? "north" : "";
            if (maxX - minX < 0.3f) return maxX < 0.5f ? "west" : Mathf.Abs(minX - w) < 0.01f ? "east" : "";
            return "";
        }

        [Test]
        public void BlocksCoverEveryLotOnceAndMergeNeighbours()
        {
            var shapes = new HashSet<string>();
            foreach (var map in Maps())
            {
                var lots = new HashSet<(int, int)>(map.Cells.Where(c => c.Kind == "lot").Select(c => (Mathf.RoundToInt(c.X), Mathf.RoundToInt(c.Z))));
                var covered = new HashSet<(int, int)>();
                foreach (var block in RoadGraph.Blocks(map))
                {
                    Assert.That(block.Cols, Is.InRange(1, 2), map.Id);
                    Assert.That(block.Rows, Is.InRange(1, 2), map.Id);
                    shapes.Add(block.Cols + "x" + block.Rows);
                    for (int c = 0; c < block.Cols; c++)
                    {
                        for (int r = 0; r < block.Rows; r++)
                        {
                            var key = (Mathf.RoundToInt(block.X + c * RoadGraph.Step), Mathf.RoundToInt(block.Z + r * RoadGraph.Step));
                            Assert.IsTrue(lots.Contains(key), map.Id + " block reaches a cell that is not a lot");
                            Assert.IsTrue(covered.Add(key), map.Id + " two blocks share a lot");
                        }
                    }
                }
                CollectionAssert.AreEquivalent(lots, covered, map.Id);
                Assert.IsTrue(RoadGraph.Navigable(map), map.Id);
            }
            CollectionAssert.IsSupersetOf(shapes, new[] { "1x1", "2x1", "1x2", "2x2" });
        }

        [Test]
        public void EveryBuildingUsesKnownKitPiecesAndPropSizesMatchTheCatalog()
        {
            var book = KitMeshTests.Book();
            int count = 0;
            foreach (var (map, _, house) in Buildings())
            {
                Assert.IsTrue(KitPlan.UsesOnlyKnownPieces(book.pieces, house), map.Id);
                count++;
            }
            Assert.Greater(count, 0);
            foreach (var id in Props)
            {
                var piece = KitPlan.Find(book.pieces, id);
                KitPlan.PropSize(id, out float w, out float d);
                Assert.AreEqual(piece.w, w, 0.001f, id);
                Assert.AreEqual(piece.d, d, 0.001f, id);
            }
        }

        [Test]
        public void BuildingsAreDeterministicAndVaried()
        {
            var fronts = new HashSet<string>();
            var heights = new HashSet<int>();
            var doors = new HashSet<string>();
            foreach (var (map, block, a) in Buildings())
            {
                var b = KitPlan.Building(map.Seed, block.X, block.Z, block.Cols, block.Rows, map.Footprint, block.Front);
                Assert.AreEqual(a.Length, b.Length);
                for (int i = 0; i < a.Length; i++)
                {
                    Assert.AreEqual(a[i].id, b[i].id);
                    Assert.AreEqual(a[i].x, b[i].x);
                    Assert.AreEqual(a[i].z, b[i].z);
                    Assert.AreEqual(a[i].yaw, b[i].yaw);
                }
                fronts.Add(block.Front);
                heights.Add(a.Count(p => p.id == "floor") / (KitPlan.Tiles(block.Cols) * KitPlan.Tiles(block.Rows)));
                foreach (var p in a.Where(p => p.id.Contains("door") || p.id == "wall_garage")) doors.Add(p.id);
            }
            Assert.GreaterOrEqual(fronts.Count, 2, "some blocks open onto a side road");
            CollectionAssert.IsSupersetOf(heights, new[] { 1, 2, 3 });
            CollectionAssert.IsSupersetOf(doors, new[] { "wall_door", "wall_double_door", "wall_garage" });
        }

        [Test]
        public void EveryStoreyHasItsFloorAndClosedWallsAndTheRoofCapsIt()
        {
            var book = KitMeshTests.Book();
            foreach (var (map, block, house) in Buildings())
            {
                int wide = KitPlan.Tiles(block.Cols), deep = KitPlan.Tiles(block.Rows);
                int storeys = house.Count(p => p.id == "floor") / (wide * deep);
                Assert.GreaterOrEqual(storeys, 1);
                Assert.AreEqual(wide * deep, house.Count(p => p.id == "roof" && Mathf.Approximately(p.y, storeys * KitPlan.Storey)), map.Id);
                for (int s = 0; s < storeys; s++)
                {
                    float y = s * KitPlan.Storey;
                    Assert.AreEqual(wide * deep, house.Count(p => p.id == "floor" && Mathf.Approximately(p.y, y)), map.Id);
                    var run = new Dictionary<string, float> { { "south", 0f }, { "north", 0f }, { "west", 0f }, { "east", 0f } };
                    foreach (var p in house.Where(p => IsWall(p.id) && Mathf.Approximately(p.y, y)))
                    {
                        string face = FaceOf(KitPlan.Find(book.pieces, p.id), p, wide, deep);
                        Assert.IsNotEmpty(face, map.Id + " " + p.id + " is off the outline");
                        run[face] += KitPlan.Find(book.pieces, p.id).w;
                    }
                    Assert.AreEqual(wide * KitPlan.LotTile, run["south"], 0.01f, map.Id);
                    Assert.AreEqual(wide * KitPlan.LotTile, run["north"], 0.01f, map.Id);
                    Assert.AreEqual(deep * KitPlan.LotTile, run["west"], 0.01f, map.Id);
                    Assert.AreEqual(deep * KitPlan.LotTile, run["east"], 0.01f, map.Id);
                }
            }
        }

        [Test]
        public void TheDoorOpensOntoTheRoadSide()
        {
            var book = KitMeshTests.Book();
            foreach (var (map, block, house) in Buildings())
            {
                int wide = KitPlan.Tiles(block.Cols), deep = KitPlan.Tiles(block.Rows);
                var doors = house.Where(p => p.y < 0.5f && (p.id.Contains("door") || p.id == "wall_garage")).ToList();
                Assert.IsNotEmpty(doors, map.Id);
                foreach (var door in doors) Assert.AreEqual(block.Front, FaceOf(KitPlan.Find(book.pieces, door.id), door, wide, deep), map.Id);

                bool open = false;
                for (int c = 0; c < block.Cols; c++)
                {
                    for (int r = 0; r < block.Rows; r++)
                    {
                        float x = block.X + c * RoadGraph.Step, z = block.Z + r * RoadGraph.Step;
                        if (block.Front == "south" && r == 0) open |= IsOpen(RoadGraph.KindAt(map, x, z - RoadGraph.Step));
                        if (block.Front == "north" && r == block.Rows - 1) open |= IsOpen(RoadGraph.KindAt(map, x, z + RoadGraph.Step));
                        if (block.Front == "west" && c == 0) open |= IsOpen(RoadGraph.KindAt(map, x - RoadGraph.Step, z));
                        if (block.Front == "east" && c == block.Cols - 1) open |= IsOpen(RoadGraph.KindAt(map, x + RoadGraph.Step, z));
                    }
                }
                bool anyOpen = false;
                foreach (var d in new[] { (0f, -1f), (0f, 1f), (-1f, 0f), (1f, 0f) })
                    for (int c = 0; c < block.Cols; c++)
                        for (int r = 0; r < block.Rows; r++)
                            anyOpen |= IsOpen(RoadGraph.KindAt(map, block.X + (c + d.Item1) * RoadGraph.Step, block.Z + (r + d.Item2) * RoadGraph.Step));
                if (anyOpen) Assert.IsTrue(open, map.Id + " the front faces no road");
            }
        }

        static bool IsOpen(string kind) => kind == "spine" || kind == "road" || kind == "alley" || kind == "poi" || kind == "extract";

        [Test]
        public void BuildingsStayInsideTheirBlockAndClearOfTheCrate()
        {
            var book = KitMeshTests.Book();
            const float margin = 0.75f;
            foreach (var (map, block, house) in Buildings())
            {
                float east = (block.Cols - 1) * RoadGraph.Step + RoadGraph.Step * 0.5f + KitPlan.LotTile * 0.5f;
                float north = (block.Rows - 1) * RoadGraph.Step + RoadGraph.Step * 0.5f + KitPlan.LotTile * 0.5f;
                float west = KitPlan.LotTile * 0.5f - RoadGraph.Step * 0.5f;
                bool crate = map.HasLoot && Mathf.Abs(block.X - map.LootX) < 0.2f && Mathf.Abs(block.Z - map.LootZ) < 0.2f;
                foreach (var placement in house)
                {
                    Extent(KitPlan.Find(book.pieces, placement.id), placement, out var minX, out var maxX, out var minZ, out var maxZ);
                    Assert.GreaterOrEqual(minX, west + margin - 0.001f, map.Id + " " + placement.id);
                    Assert.LessOrEqual(maxX, east - margin + 0.001f, map.Id + " " + placement.id);
                    Assert.GreaterOrEqual(minZ, west + margin - 0.001f, map.Id + " " + placement.id);
                    Assert.LessOrEqual(maxZ, north - margin + 0.001f, map.Id + " " + placement.id);
                    if (crate) Assert.Greater(minZ, -0.55f, "the road crate sits 1.9 m south of its lot centre");
                }
            }
        }

        [Test]
        public void PropsStandInsideAndLeaveTheWayInClear()
        {
            var book = KitMeshTests.Book();
            int furnished = 0;
            foreach (var (map, block, house) in Buildings())
            {
                int wide = KitPlan.Tiles(block.Cols), deep = KitPlan.Tiles(block.Rows);
                float w = wide * KitPlan.LotTile, d = deep * KitPlan.LotTile;
                var props = house.Where(p => Props.Contains(p.id)).ToList();
                if (wide * deep == 1) Assert.IsEmpty(props, "a one-tile house has no room to furnish");
                if (props.Count > 0) furnished++;
                foreach (var p in props)
                {
                    Assert.AreEqual(0f, p.y, map.Id);
                    Extent(KitPlan.Find(book.pieces, p.id), p, out var minX, out var maxX, out var minZ, out var maxZ);
                    Assert.GreaterOrEqual(minX, -0.001f, map.Id + " " + p.id);
                    Assert.LessOrEqual(maxX, w + 0.001f, map.Id + " " + p.id);
                    Assert.GreaterOrEqual(minZ, 0.2f - 0.001f, map.Id + " " + p.id);
                    Assert.LessOrEqual(maxZ, d - 0.2f + 0.001f, map.Id + " " + p.id);
                    float gap = block.Front == "south" ? minZ - 0.2f
                        : block.Front == "north" ? d - 0.2f - maxZ
                        : block.Front == "west" ? minX : w - maxX;
                    Assert.GreaterOrEqual(gap, 0.99f, map.Id + " " + p.id + " crowds the door");
                }
            }
            Assert.Greater(furnished, 10);
        }
    }
}
