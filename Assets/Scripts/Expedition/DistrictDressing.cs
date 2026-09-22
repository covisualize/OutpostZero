using UnityEngine;
using OutpostZero.Colony;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Graphics;
using OutpostZero.Items;
using OutpostZero.Shell;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// Builds the district's stalls, barrels, and caches on the street when an expedition opens.
    /// The sanctuary yard is left alone.
    /// </summary>
    public class DistrictDressing : MonoBehaviour
    {
        public static DistrictDressing Instance { get; private set; }

        private Transform root;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void Build(string districtId)
        {
            Clear();
            var anchor = new GameObject("DistrictDressing");
            anchor.transform.SetParent(transform, false);
            root = anchor.transform;
            var pieces = DistrictLayout.For(districtId);
            for (int i = 0; i < pieces.Length; i++) Spawn(pieces[i]);
            int seed = WorldMapService.Instance != null ? WorldMapService.Instance.WorldSeed : DistrictGenerator.DefaultSeed;
            var generated = DistrictGenerator.Scatter(seed, districtId);
            for (int i = 0; i < generated.Length; i++) Spawn(generated[i]);
            var blocks = DistrictBlocks.Build(seed, districtId);
            RaiseBlocks(blocks);
            RaiseGraph(districtId, seed);
            ObjectiveTracker.Instance?.ExpectPoi(blocks.PoiRole);
            ExtractionZone.MoveTo(new Vector3(blocks.ExtractX, 0.5f, blocks.ExtractZ));
            RaiseRescue(districtId, blocks);
            RaiseArmory(districtId);
            RaiseSmg(districtId);
            KitStructure.Raise(districtId, root);
            RaiseCaravan();
            StreetDetail.RaiseStreet(districtId, root);
            StreetNav.Schedule(this);
        }

        private void RaiseBlocks(DistrictBlocks.Plan plan)
        {
            var walls = DistrictBlocks.Walls(plan);
            var tint = BlockTint(plan.Footprint);
            for (int i = 0; i < walls.Length; i++)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "DistrictLot";
                wall.transform.SetParent(root, false);
                wall.transform.position = new Vector3(walls[i].X, 1.3f, walls[i].Z);
                wall.transform.localScale = new Vector3(1.85f, 2.6f, 1.95f);
                wall.layer = GameLayers.Environment;
                Paint(wall.GetComponent<Renderer>(), tint);
            }

            var room = GameObject.CreatePrimitive(PrimitiveType.Cube);
            room.name = "DistrictPoi";
            room.transform.SetParent(root, false);
            room.transform.position = new Vector3(plan.PoiX, 0.7f, plan.PoiZ);
            room.transform.localScale = new Vector3(1.1f, 1.3f, 1.1f);
            room.layer = GameLayers.Interactable;
            Paint(room.GetComponent<Renderer>(), plan.PoiRole == "radio" ? new Color(0.72f, 0.58f, 0.22f) : new Color(0.45f, 0.5f, 0.42f));
            room.AddComponent<DistrictPoi>().Configure(plan.PoiRole);

            var nest = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            nest.name = "DistrictNest";
            nest.transform.SetParent(root, false);
            nest.transform.position = new Vector3(plan.NestX, 0.15f, plan.NestZ);
            nest.transform.localScale = new Vector3(1.2f, 0.08f, 1.2f);
            var nestCollider = nest.GetComponent<Collider>();
            if (nestCollider != null) Destroy(nestCollider);
            Paint(nest.GetComponent<Renderer>(), new Color(0.25f, 0.12f, 0.1f));

            var exit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            exit.name = "DistrictExtract";
            exit.transform.SetParent(root, false);
            exit.transform.position = new Vector3(plan.ExtractX, 0.08f, plan.ExtractZ);
            exit.transform.localScale = new Vector3(2.4f, 0.04f, 2.4f);
            var exitCollider = exit.GetComponent<Collider>();
            if (exitCollider != null) Destroy(exitCollider);
            Paint(exit.GetComponent<Renderer>(), new Color(0.25f, 0.75f, 0.45f));
            ProbeField.Place(root, DistrictBlocks.Open(plan));
            RaiseRoom(plan);
        }

        private void RaiseGraph(string districtId, int seed)
        {
            var map = RoadGraph.Build(seed, districtId);
            var cells = map.Cells;
            if (cells == null) return;
            var tint = BlockTint(map.Footprint);
            for (int i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];
                if (cell.Kind == "hole") continue;
                if (cell.Kind == "lot")
                {
                    var shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shell.name = "RoadLot";
                    shell.transform.SetParent(root, false);
                    shell.transform.position = new Vector3(cell.X, 1.1f, cell.Z);
                    shell.transform.localScale = new Vector3(2.5f, 2.2f, 2.5f);
                    shell.layer = GameLayers.Environment;
                    Paint(shell.GetComponent<Renderer>(), tint);
                    if (map.HasLoot && Close(cell.X, map.LootX) && Close(cell.Z, map.LootZ))
                    {
                        var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        crate.name = "RoadCrate";
                        crate.transform.SetParent(root, false);
                        crate.transform.position = new Vector3(cell.X, 0.35f, cell.Z - 1.9f);
                        crate.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
                        crate.layer = GameLayers.Interactable;
                        Paint(crate.GetComponent<Renderer>(), new Color(0.42f, 0.36f, 0.24f));
                        string table = map.Footprint == "clinic" || map.Footprint == "hospital" ? "medical"
                            : map.Footprint == "warehouse" || map.Footprint == "station" ? "military" : "crate";
                        crate.AddComponent<LootContainer>().Configure(table);
                    }
                    continue;
                }

                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = cell.Kind == "extract" ? "SideGate" : "RoadSlab";
                slab.transform.SetParent(root, false);
                slab.transform.position = new Vector3(cell.X, 0.03f, cell.Z);
                float span = cell.Kind == "alley" ? 1.35f : 1.8f;
                slab.transform.localScale = new Vector3(span, 0.04f, span);
                var slabCollider = slab.GetComponent<Collider>();
                if (slabCollider != null) Destroy(slabCollider);
                Color asphalt = cell.Kind == "extract"
                    ? new Color(0.2f, 0.45f, 0.3f)
                    : cell.Kind == "alley" ? new Color(0.24f, 0.23f, 0.21f) : new Color(0.16f, 0.16f, 0.15f);
                Paint(slab.GetComponent<Renderer>(), asphalt);
                if (cell.Kind == "poi")
                {
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    post.name = "RoadPost";
                    post.transform.SetParent(root, false);
                    post.transform.position = new Vector3(cell.X, 0.7f, cell.Z);
                    post.transform.localScale = new Vector3(0.4f, 1.4f, 0.4f);
                    post.layer = GameLayers.Environment;
                    Paint(post.GetComponent<Renderer>(), new Color(0.72f, 0.58f, 0.22f));
                }
            }

            RaiseEdges(RoadGraph.Edges(map));

            if (!map.HasNest) return;
            var nest = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            nest.name = "EastNest";
            nest.transform.SetParent(root, false);
            nest.transform.position = new Vector3(map.NestX, 0.08f, map.NestZ);
            nest.transform.localScale = new Vector3(1.4f, 0.05f, 1.4f);
            var nestCollider = nest.GetComponent<Collider>();
            if (nestCollider != null) Destroy(nestCollider);
            Paint(nest.GetComponent<Renderer>(), new Color(0.28f, 0.1f, 0.08f));
        }

        private void RaiseEdges(RoadGraph.Edge[] edges)
        {
            if (edges == null) return;
            for (int i = 0; i < edges.Length; i++)
            {
                var edge = edges[i];
                if (edge.Kind == "cross")
                {
                    Mark(edge.X, 0.06f, edge.Z, 1.5f, 0.02f, 0.14f, 0f, "Crosswalk", new Color(0.82f, 0.8f, 0.74f), false);
                    Mark(edge.X, 0.06f, edge.Z, 0.14f, 0.02f, 1.5f, 0f, "Crosswalk", new Color(0.82f, 0.8f, 0.74f), false);
                    continue;
                }
                if (edge.Kind == "dash")
                {
                    Mark(edge.X, 0.06f, edge.Z, 1.15f, 0.02f, 0.12f, edge.Yaw, "LaneDash", new Color(0.72f, 0.62f, 0.22f), false);
                    continue;
                }
                bool curb = edge.Kind == "curb";
                float breadth = curb ? RoadGraph.CurbBreadth : RoadGraph.SidewalkBreadth;
                float height = curb ? RoadGraph.CurbHeight : 0.04f;
                Mark(edge.X, height * 0.5f, edge.Z, 3.05f, height, breadth, edge.Yaw, curb ? "StreetCurb" : "Sidewalk",
                    curb ? new Color(0.55f, 0.54f, 0.5f) : new Color(0.4f, 0.4f, 0.38f), true);
            }
        }

        private void Mark(float x, float y, float z, float width, float height, float depth, float yaw, string name, Color color, bool solid)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = name;
            body.transform.SetParent(root, false);
            body.transform.position = new Vector3(x, y, z);
            body.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            body.transform.localScale = new Vector3(width, height, depth);
            if (!solid)
            {
                var collider = body.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
            }
            else body.layer = GameLayers.Environment;
            Paint(body.GetComponent<Renderer>(), color);
        }

        private static bool Close(float a, float b)
        {
            float d = a - b;
            if (d < 0f) d = -d;
            return d < 0.2f;
        }

        private void RaiseRoom(DistrictBlocks.Plan plan)
        {
            float doorX = plan.PoiX + 2.2f;
            float doorZ = plan.PoiZ;
            DoorMap.Inside(doorX, doorZ, out float insideX, out float insideZ);

            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "StreetDoor";
            slab.transform.SetParent(root, false);
            slab.transform.position = new Vector3(doorX, 1.1f, doorZ);
            slab.transform.localScale = new Vector3(0.18f, 2.2f, 1.05f);
            slab.layer = GameLayers.Interactable;
            Paint(slab.GetComponent<Renderer>(), new Color(0.35f, 0.24f, 0.16f));
            slab.AddComponent<StreetDoor>().Configure(new Vector3(insideX, 0.05f, insideZ), false);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "RoomFloor";
            floor.transform.SetParent(root, false);
            floor.transform.position = new Vector3(insideX, -0.1f, insideZ);
            floor.transform.localScale = new Vector3(8f, 0.2f, 8f);
            floor.layer = GameLayers.Environment;
            Paint(floor.GetComponent<Renderer>(), new Color(0.28f, 0.27f, 0.25f));

            RaiseWall(insideX, insideZ + 3.6f, 8f, 0.3f);
            RaiseWall(insideX, insideZ - 3.6f, 8f, 0.3f);
            RaiseWall(insideX - 3.6f, insideZ, 0.3f, 7.4f);
            var opening = new RoomPlan.Piece[2];
            int openCount = RoomPlan.Opening(insideX, insideZ, opening);
            for (int i = 0; i < openCount; i++) Place(opening[i]);

            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "RoomDoor";
            back.transform.SetParent(root, false);
            back.transform.position = new Vector3(insideX, 1.1f, insideZ);
            back.transform.localScale = new Vector3(0.18f, 2.2f, 1.05f);
            back.layer = GameLayers.Interactable;
            Paint(back.GetComponent<Renderer>(), new Color(0.45f, 0.32f, 0.2f));
            back.AddComponent<StreetDoor>().Configure(new Vector3(doorX, 0.05f, doorZ), true);

            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "RoomCrate";
            crate.transform.SetParent(root, false);
            crate.transform.position = new Vector3(insideX + 1.6f, 0.4f, insideZ + 1.2f);
            crate.transform.localScale = new Vector3(0.8f, 0.7f, 0.8f);
            crate.layer = GameLayers.Interactable;
            Paint(crate.GetComponent<Renderer>(), new Color(0.42f, 0.36f, 0.24f));
            crate.AddComponent<LootContainer>().Configure(plan.Footprint == "clinic" || plan.PoiRole == "radio" ? "medical" : "crate");

            var lamp = new GameObject("RoomLamp");
            lamp.transform.SetParent(root, false);
            lamp.transform.position = new Vector3(insideX, 2.4f, insideZ);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 8f;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.9f, 0.75f);

            var dressed = new RoomPlan.Piece[8];
            int dressedCount = RoomPlan.Dress(plan.Footprint, insideX, insideZ, dressed);
            for (int i = 0; i < dressedCount; i++) Place(dressed[i]);
        }

        private void Place(RoomPlan.Piece piece)
        {
            Color color = new Color(0.32f, 0.3f, 0.28f);
            if (piece.Name == "RoomFloor") color = new Color(0.28f, 0.27f, 0.25f);
            else if (piece.Name == "RoomCot") color = new Color(0.55f, 0.58f, 0.62f);
            else if (piece.Name == "RoomShelf") color = new Color(0.42f, 0.32f, 0.22f);
            else if (piece.Name == "RoomDesk") color = new Color(0.36f, 0.28f, 0.2f);
            else if (piece.Name == "RoomTable") color = new Color(0.4f, 0.3f, 0.22f);
            else if (piece.Name == "RoomCounter") color = new Color(0.48f, 0.4f, 0.28f);
            Mark(piece.X, piece.Y, piece.Z, piece.W, piece.H, piece.D, 0f, piece.Name, color, piece.Solid);
        }

        private void RaiseWall(float x, float z, float width, float depth)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "RoomWall";
            wall.transform.SetParent(root, false);
            wall.transform.position = new Vector3(x, 1.4f, z);
            wall.transform.localScale = new Vector3(width, 2.8f, depth);
            wall.layer = GameLayers.Environment;
            Paint(wall.GetComponent<Renderer>(), new Color(0.32f, 0.3f, 0.28f));
        }

        private void RaiseRescue(string districtId, DistrictBlocks.Plan plan)
        {
            var offer = RescueBook.For(districtId);
            if (string.IsNullOrEmpty(offer.Id)) return;
            if (SurvivorRoster.Instance != null && SurvivorRoster.Instance.Has(offer.Id)) return;

            var person = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            person.name = "Rescue_" + offer.Id;
            person.transform.SetParent(root, false);
            person.transform.position = new Vector3(plan.PoiX - 2f, 1f, plan.PoiZ);
            person.layer = GameLayers.Interactable;
            Paint(person.GetComponent<Renderer>(), new Color(0.72f, 0.48f, 0.28f));
            person.AddComponent<RescueFollower>().Configure(offer.Id, offer.Name);
        }

        private void RaiseArmory(string districtId)
        {
            if (districtId != "police_station") return;
            var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gun.name = "GroundRifle";
            gun.transform.SetParent(root, false);
            gun.transform.position = new Vector3(-4f, 0.25f, 0f);
            gun.transform.localScale = new Vector3(0.7f, 0.12f, 0.18f);
            gun.layer = GameLayers.Interactable;
            gun.AddComponent<GroundWeapon>().Configure("rifle_assault", 12, 30);
        }

        private void RaiseSmg(string districtId)
        {
            if (districtId != "mall") return;
            var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gun.name = "GroundSmg";
            gun.transform.SetParent(root, false);
            gun.transform.position = new Vector3(6f, 0.22f, 8f);
            gun.transform.localScale = new Vector3(0.46f, 0.1f, 0.16f);
            gun.layer = GameLayers.Interactable;
            gun.AddComponent<GroundWeapon>().Configure("smg", 18, 25);
        }

        private static Color BlockTint(string footprint)
        {
            if (footprint == "clinic" || footprint == "hospital") return new Color(0.62f, 0.58f, 0.5f);
            if (footprint == "warehouse") return new Color(0.48f, 0.46f, 0.42f);
            if (footprint == "station") return new Color(0.32f, 0.3f, 0.28f);
            return new Color(0.45f, 0.28f, 0.22f);
        }

        private static void Paint(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        private void RaiseCaravan()
        {
            int day = WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
            bool post = GridBuilder.Instance != null && GridBuilder.Instance.HasKind("TradingPost");
            if (string.IsNullOrEmpty(CaravanBook.Counterparty(day, post))) return;

            var stall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stall.name = "CaravanStall";
            stall.transform.SetParent(root, false);
            stall.transform.position = new Vector3(8f, 0.7f, 4f);
            stall.transform.localScale = new Vector3(1.8f, 1.2f, 0.8f);
            stall.layer = GameLayers.Interactable;
            var stallRenderer = stall.GetComponent<Renderer>();
            if (stallRenderer != null) stallRenderer.material.color = new Color(0.55f, 0.32f, 0.22f);
            stall.AddComponent<CampStation>().Configure(StationKind.Merchant);
            RaiseGuard(new Vector3(6.6f, 0.95f, 3.2f));
            RaiseGuard(new Vector3(9.4f, 0.95f, 3.2f));
        }

        private void RaiseGuard(Vector3 position)
        {
            var guard = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            guard.name = "CaravanGuard";
            guard.transform.SetParent(root, false);
            guard.transform.position = position;
            guard.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            guard.layer = GameLayers.Environment;
            var renderer = guard.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.28f, 0.32f, 0.28f);
        }

        private void Spawn(DistrictLayout.Piece piece)
        {
            bool barrel = piece.Role.StartsWith("barrel");
            var body = GameObject.CreatePrimitive(barrel ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            body.name = "District_" + piece.Role;
            body.transform.SetParent(root, false);
            body.transform.position = new Vector3(piece.X, barrel ? 0.55f : 0.6f, piece.Z);
            body.transform.rotation = Quaternion.Euler(0f, piece.Yaw, 0f);
            body.transform.localScale = Scale(piece.Role);
            body.layer = GameLayers.Environment;
            var renderer = body.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = ColorFor(piece.Role);

            if (barrel)
            {
                var hazard = body.AddComponent<DestructibleHazard>();
                if (piece.Role.Contains("toxic")) hazard.Configure(HazardKind.Toxic);
                else if (piece.Role.Contains("oil")) hazard.Configure(HazardKind.Oil);
                else hazard.Configure(HazardKind.Explosive);
            }
            else if (piece.Role.StartsWith("crate"))
            {
                body.layer = GameLayers.Interactable;
                var container = body.AddComponent<LootContainer>();
                container.Configure(piece.Role == "crate_medical" ? "medical" : piece.Role == "crate_military" ? "military" : "crate");
            }
            else if (piece.Role == "cover")
            {
                body.AddComponent<CoverPost>();
            }
            else if (piece.Role == "lamp")
            {
                var lightObject = new GameObject("DistrictLamp");
                lightObject.transform.SetParent(body.transform, false);
                lightObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                SodiumLamp.Dress(lightObject, piece.X, piece.Z, SodiumLamp.Peak);
            }
        }

        private static Vector3 Scale(string role)
        {
            if (role.StartsWith("barrel")) return new Vector3(0.55f, 0.55f, 0.55f);
            if (role == "stall") return new Vector3(1.6f, 1.1f, 0.7f);
            if (role.StartsWith("crate")) return new Vector3(0.9f, 0.7f, 0.9f);
            if (role == "lamp") return new Vector3(0.25f, 2.2f, 0.25f);
            return new Vector3(1.8f, 1.1f, 0.45f);
        }

        private static Color ColorFor(string role)
        {
            if (role.Contains("toxic")) return new Color(0.35f, 0.7f, 0.25f);
            if (role.Contains("oil")) return new Color(0.12f, 0.12f, 0.12f);
            if (role.Contains("explosive")) return new Color(0.7f, 0.18f, 0.12f);
            if (role == "crate_medical") return new Color(0.75f, 0.82f, 0.78f);
            if (role == "lamp") return new Color(0.35f, 0.32f, 0.28f);
            if (role == "stall") return new Color(0.42f, 0.28f, 0.18f);
            return new Color(0.4f, 0.36f, 0.3f);
        }

        private void Clear()
        {
            if (root == null) return;
            Destroy(root.gameObject);
            root = null;
        }
    }
}
