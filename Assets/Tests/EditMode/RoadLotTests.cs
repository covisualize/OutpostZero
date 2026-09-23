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

        static int Storeys(KitPlacement[] house) => Mathf.RoundToInt(house.Where(p => p.id == "roof").Max(p => p.y) / KitPlan.Storey);

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
                heights.Add(Storeys(a));
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
                int storeys = Storeys(house);
                Assert.GreaterOrEqual(storeys, 1);
                int well = house.Any(p => p.id == "stairs") ? 2 : 0;
                Assert.AreEqual(wide * deep, house.Count(p => p.id == "roof" && Mathf.Approximately(p.y, storeys * KitPlan.Storey)), map.Id);
                for (int s = 0; s < storeys; s++)
                {
                    float y = s * KitPlan.Storey;
                    Assert.AreEqual(wide * deep - (s > 0 ? well : 0), house.Count(p => p.id == "floor" && Mathf.Approximately(p.y, y)), map.Id);
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

        [Test]
        public void EveryUpperStoreyOfABigBuildingIsReachedByStairsWithHeadRoomAndALanding()
        {
            var book = KitMeshTests.Book();
            var steps = KitPlan.Find(book.pieces, "stairs");
            var flights = 0;
            var fronts = new HashSet<string>();
            var cases = Buildings().Select(b => (b.map.Id, b.block, b.house)).ToList();
            foreach (var front in new[] { "south", "north", "west", "east" })
            {
                foreach (var footprint in new[] { "apartment", "station", "storefront" })
                {
                    var block = new RoadGraph.Block { Cols = 2, Rows = 2, Front = front };
                    cases.Add((footprint + " " + front, block, KitPlan.Building(7, 0f, 0f, 2, 2, footprint, front)));
                }
            }
            foreach (var (label, block, house) in cases)
            {
                var map = new { Id = label };
                int wide = KitPlan.Tiles(block.Cols), deep = KitPlan.Tiles(block.Rows);
                int storeys = Storeys(house);
                var stairs = house.Where(p => p.id == "stairs").ToList();
                if (wide < 3 || deep < 3 || storeys < 2)
                {
                    Assert.IsEmpty(stairs, map.Id);
                    continue;
                }
                Assert.AreEqual(storeys - 1, stairs.Count, map.Id);
                fronts.Add(block.Front);
                foreach (var flight in stairs)
                {
                    flights++;
                    var slabs = house.Where(p => p.id == "floor" && Mathf.Approximately(p.y, flight.y + KitPlan.Storey)).ToList();
                    KitPlan.FlightArea(flight, out var ax0, out var ax1, out var az0, out var az1);
                    Extent(steps, flight, out var ex0, out var ex1, out var ez0, out var ez1);
                    Assert.AreEqual(ax0, ex0, 0.01f); Assert.AreEqual(ax1, ex1, 0.01f);
                    Assert.AreEqual(az0, ez0, 0.01f); Assert.AreEqual(az1, ez1, 0.01f);
                    Assert.GreaterOrEqual(ex0, -0.001f); Assert.LessOrEqual(ex1, wide * KitPlan.LotTile + 0.001f);
                    Assert.GreaterOrEqual(ez0, 0.2f - 0.001f); Assert.LessOrEqual(ez1, deep * KitPlan.LotTile - 0.2f + 0.001f);

                    KitBox top = null;
                    float topX0 = 0, topX1 = 0, topZ0 = 0, topZ1 = 0;
                    foreach (var box in steps.colliders)
                    {
                        var one = new KitPiece { id = "step", colliders = new[] { box } };
                        Extent(one, flight, out var x0, out var x1, out var z0, out var z1);
                        float stepTop = flight.y + box.y + box.h;
                        foreach (var slab in slabs)
                        {
                            bool over = slab.x < x1 - 0.01f && slab.x + KitPlan.LotTile > x0 + 0.01f && slab.z < z1 - 0.01f && slab.z + KitPlan.LotTile > z0 + 0.01f;
                            if (over) Assert.GreaterOrEqual(slab.y - stepTop, 1.9f, map.Id + " the slab above leaves no head room");
                        }
                        if (top == null || box.y > top.y) { top = box; topX0 = x0; topX1 = x1; topZ0 = z0; topZ1 = z1; }
                    }
                    Assert.AreEqual(KitPlan.Storey, top.y + top.h, 0.01f, "the last step meets the slab above");
                    bool landing = slabs.Any(slab =>
                        (Mathf.Abs(slab.x + KitPlan.LotTile - topX0) < 0.01f || Mathf.Abs(slab.x - topX1) < 0.01f) && slab.z <= topZ0 + 0.01f && slab.z + KitPlan.LotTile >= topZ1 - 0.01f
                        || (Mathf.Abs(slab.z + KitPlan.LotTile - topZ0) < 0.01f || Mathf.Abs(slab.z - topZ1) < 0.01f) && slab.x <= topX0 + 0.01f && slab.x + KitPlan.LotTile >= topX1 - 0.01f);
                    Assert.IsTrue(landing, map.Id + " " + block.Front + " the top step has nowhere to land");

                    foreach (var prop in house.Where(p => Props.Contains(p.id)))
                    {
                        Extent(KitPlan.Find(book.pieces, prop.id), prop, out var px0, out var px1, out var pz0, out var pz1);
                        bool hits = px0 < ex1 && px1 > ex0 && pz0 < ez1 && pz1 > ez0;
                        Assert.IsFalse(hits, map.Id + " " + prop.id + " stands on the stairs");
                    }
                }
            }
            Assert.Greater(flights, 12);
            Assert.AreEqual(4, fronts.Count);
        }

        [Test]
        public void EveryUpperStoreyOfASmallBuildingIsReachedByALadder()
        {
            var book = KitMeshTests.Book();
            var rungs = KitPlan.Find(book.pieces, "ladder");
            Assert.IsNotNull(rungs);
            Assert.AreEqual(KitPlan.LadderWidth, rungs.w, 0.001f);
            Assert.AreEqual(KitPlan.LadderDepth, rungs.d, 0.001f);
            Assert.AreEqual(KitPlan.Storey, rungs.h, 0.001f);
            Assert.Greater(rungs.colliders.Min(b => b.y + (b.h < 1f ? 0f : 99f)), 0.35f, "the first rung is above the player's step");

            int ladders = 0;
            var fronts = new HashSet<string>();
            var shapes = new HashSet<string>();
            var cases = Buildings().Select(b => (b.map.Id, b.block, b.house)).ToList();
            foreach (var front in new[] { "south", "north", "west", "east" })
            {
                foreach (var (cols, rows) in new[] { (1, 1), (2, 1), (1, 2) })
                {
                    var block = new RoadGraph.Block { Cols = cols, Rows = rows, Front = front };
                    cases.Add(("apartment " + cols + "x" + rows + " " + front, block, KitPlan.Building(7, 0f, 0f, cols, rows, "apartment", front)));
                }
            }
            foreach (var (label, block, house) in cases)
            {
                int wide = KitPlan.Tiles(block.Cols), deep = KitPlan.Tiles(block.Rows);
                float w = wide * KitPlan.LotTile, d = deep * KitPlan.LotTile;
                int storeys = Storeys(house);
                var climbs = house.Where(p => p.id == "ladder").ToList();
                if (storeys < 2 || (wide >= 3 && deep >= 3))
                {
                    Assert.IsEmpty(climbs, label);
                    continue;
                }
                Assert.AreEqual(storeys - 1, climbs.Count, label);
                fronts.Add(block.Front);
                shapes.Add(wide + "x" + deep);
                foreach (var ladder in climbs)
                {
                    ladders++;
                    Extent(rungs, ladder, out var x0, out var x1, out var z0, out var z1);
                    Assert.GreaterOrEqual(x0, -0.001f, label); Assert.LessOrEqual(x1, w + 0.001f, label);
                    Assert.GreaterOrEqual(z0, 0.2f - 0.001f, label); Assert.LessOrEqual(z1, d - 0.2f + 0.001f, label);
                    float back = block.Front == "south" ? d - 0.2f - z1 : block.Front == "north" ? z0 - 0.2f : block.Front == "west" ? w - x1 : x0;
                    Assert.Less(back, 0.07f, label + " the ladder stands off the back wall");
                    float gap = block.Front == "south" ? z0 - 0.2f : block.Front == "north" ? d - 0.2f - z1 : block.Front == "west" ? x0 : w - x1;
                    Assert.GreaterOrEqual(gap, 1f, label + " the ladder crowds the door");

                    var foot = KitPlan.PointOf(ladder, KitPlan.LadderFoot);
                    var top = KitPlan.PointOf(ladder, KitPlan.LadderTop);
                    Assert.AreEqual(ladder.y, foot.y, 0.001f);
                    Assert.AreEqual(ladder.y + KitPlan.Storey, top.y, 0.001f);
                    foreach (var spot in new[] { foot, top })
                    {
                        Assert.GreaterOrEqual(spot.x, 0.45f - 0.001f, label); Assert.LessOrEqual(spot.x, w - 0.45f + 0.001f, label);
                        Assert.GreaterOrEqual(spot.z, 0.65f - 0.001f, label); Assert.LessOrEqual(spot.z, d - 0.65f + 0.001f, label);
                        bool floored = house.Any(p => p.id == "floor" && Mathf.Approximately(p.y, spot.y)
                            && spot.x >= p.x && spot.x <= p.x + KitPlan.LotTile && spot.z >= p.z && spot.z <= p.z + KitPlan.LotTile);
                        Assert.IsTrue(floored, label + " a climb spot has no floor under it");
                        foreach (var other in climbs)
                        {
                            if (!Mathf.Approximately(other.y, spot.y)) continue;
                            Extent(rungs, other, out var ox0, out var ox1, out var oz0, out var oz1);
                            float dx = Mathf.Max(ox0 - spot.x, 0f, spot.x - ox1), dz = Mathf.Max(oz0 - spot.z, 0f, spot.z - oz1);
                            Assert.GreaterOrEqual(Mathf.Sqrt(dx * dx + dz * dz), 0.44f, label + " a climb spot is inside a ladder");
                        }
                    }

                    KitPlan.ClimbArea(ladder, out var cx0, out var cx1, out var cz0, out var cz1);
                    foreach (var prop in house.Where(p => Props.Contains(p.id)))
                    {
                        Extent(KitPlan.Find(book.pieces, prop.id), prop, out var px0, out var px1, out var pz0, out var pz1);
                        Assert.IsFalse(px0 < cx1 && px1 > cx0 && pz0 < cz1 && pz1 > cz0, label + " " + prop.id + " blocks the ladder");
                    }
                }
            }
            Assert.Greater(ladders, 12);
            Assert.AreEqual(4, fronts.Count);
            CollectionAssert.IsSupersetOf(shapes, new[] { "1x1", "3x1", "1x3" });
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
