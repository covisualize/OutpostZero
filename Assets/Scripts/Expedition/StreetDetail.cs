using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Graphics;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// Places the planned debris, skyline, and sanctuary clutter as primitives.
    /// Tiny pieces stay without colliders so they do not jam the street.
    /// </summary>
    public class StreetDetail : MonoBehaviour
    {
        public static void RaiseStreet(string districtId, Transform parent)
        {
            if (parent == null) return;
            var debris = DressingPlan.Debris(DebrisProfile.For(districtId), Anchors(parent));
            Litter(debris, parent);
            Spawn(DressingPlan.Patches(districtId), parent, false);
            Spawn(LanePaint.Marks(districtId), parent, false);
            var horizon = DressingPlan.Horizon();
            Spawn(horizon, parent, true);
            ConnectPoles(horizon, parent);
        }

        public static void RaiseHome()
        {
            if (Object.FindFirstObjectByType<StreetDetail>() != null) return;
            var root = new GameObject("SanctuaryDressing");
            var detail = root.AddComponent<StreetDetail>();
            var marks = DressingPlan.Home();
            Spawn(marks, root.transform, true);
            ConnectLaundry(marks, root.transform);
        }

        /// <summary>Instanced litter; bottles stay objects so they can be kicked.</summary>
        public static DebrisField Litter(DressingPlan.Mark[] marks, Transform parent)
        {
            var host = new GameObject("DebrisField");
            host.transform.SetParent(parent, false);
            var field = host.AddComponent<DebrisField>();
            var loose = new System.Collections.Generic.List<DressingPlan.Mark>();
            for (int i = 0; i < marks.Length; i++)
            {
                var mark = marks[i];
                if (!DebrisField.Instanced(mark.Role)) { loose.Add(mark); continue; }
                field.Add(mark, mark.Role == "tyre" ? PrimitiveType.Cylinder : PrimitiveType.Cube, ColorFor(mark.Role));
            }
            Spawn(loose.ToArray(), parent, false);
            return field;
        }

        /// <summary>Points around every curb and wall already raised under <paramref name="parent"/>.</summary>
        public static float[] Anchors(Transform parent)
        {
            var points = new System.Collections.Generic.List<float>();
            foreach (var collider in parent.GetComponentsInChildren<Collider>())
            {
                if (collider == null || collider.isTrigger) continue;
                var bounds = collider.bounds;
                if (bounds.size.y < 0.1f || bounds.min.y > 0.5f) continue;
                if (bounds.size.x > 30f || bounds.size.z > 30f) continue;
                DressingPlan.Perimeter(points, bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z, 1f);
                if (points.Count > DressingPlan.AnchorLimit * 2) break;
            }
            return points.Count > 0 ? points.ToArray() : DressingPlan.StreetEdges();
        }

        private static void Spawn(DressingPlan.Mark[] marks, Transform parent, bool solidLarge)
        {
            if (marks == null) return;
            for (int i = 0; i < marks.Length; i++)
            {
                var mark = marks[i];
                PrimitiveType shape = PrimitiveType.Cube;
                if (mark.Role == "bulb") shape = PrimitiveType.Sphere;
                else if (mark.Role == "tyre" || mark.Role == "bottle" || mark.Role == "tower" || mark.Role == "pole" || mark.Role == "manhole") shape = PrimitiveType.Cylinder;
                var body = GameObject.CreatePrimitive(shape);
                body.name = "Dress_" + mark.Role;
                body.transform.SetParent(parent, false);
                float lift = mark.Role == "skyline" || mark.Role == "tower" || mark.Role == "pole" || mark.Role == "tower_leg" || mark.Role == "billboard_post" || mark.Role == "tent" || mark.Role == "laundry_a" || mark.Role == "laundry_b" || mark.Role == "sandbag"
                    ? mark.H * 0.5f
                    : 0f;
                body.transform.position = new Vector3(mark.X, mark.Y + lift, mark.Z);
                body.transform.rotation = Quaternion.Euler(mark.Role == "tyre" ? 90f : 0f, mark.Yaw, 0f);
                body.transform.localScale = new Vector3(mark.W, mark.H, mark.D);
                bool plate = mark.Role == "manhole" || mark.Role == "grate";
                bool solid = plate || (solidLarge && (mark.Role == "overpass" || mark.Role == "pole" || mark.Role == "tower" || mark.Role == "tower_leg" || mark.Role == "billboard_post" || mark.Role == "tent" || mark.Role == "sandbag"));
                if (!solid)
                {
                    var collider = body.GetComponent<Collider>();
                    if (collider != null) Object.Destroy(collider);
                }
                else body.layer = GameLayers.Environment;
                if (mark.Role == "bottle")
                {
                    var trigger = body.AddComponent<SphereCollider>();
                    trigger.isTrigger = true;
                    trigger.radius = 1.2f;
                    body.AddComponent<StreetBottle>();
                }
                var renderer = body.GetComponent<Renderer>();
                if (mark.Role == "glass" && PaneGlass.Coat(renderer)) { }
                else Paint(renderer, mark.Role, ColorFor(mark.Role));
                if (mark.Role == "bulb")
                {
                    var lamp = body.AddComponent<Light>();
                    lamp.type = LightType.Point;
                    lamp.range = 4.5f;
                    lamp.intensity = 0.7f;
                    lamp.color = new Color(1f, 0.82f, 0.45f);
                    body.AddComponent<DuskBulb>().Arm(0.7f);
                }
            }
        }

        private static void ConnectPoles(DressingPlan.Mark[] marks, Transform parent)
        {
            DressingPlan.Mark from = default;
            DressingPlan.Mark to = default;
            bool found = false;
            for (int i = 0; i < marks.Length; i++)
            {
                if (marks[i].Role != "pole") continue;
                if (!found)
                {
                    from = marks[i];
                    found = true;
                }
                else
                {
                    to = marks[i];
                    break;
                }
            }
            if (!found || to.Role != "pole") return;
            var wire = new GameObject("Dress_Wire");
            wire.transform.SetParent(parent, false);
            var sway = wire.AddComponent<WireSway>();
            sway.Configure(
                new Vector3(from.X, from.H, from.Z),
                new Vector3(to.X, to.H, to.Z),
                0.85f);
        }

        private static void ConnectLaundry(DressingPlan.Mark[] marks, Transform parent)
        {
            DressingPlan.Mark from = default;
            DressingPlan.Mark to = default;
            bool left = false;
            bool right = false;
            for (int i = 0; i < marks.Length; i++)
            {
                if (marks[i].Role == "laundry_a") { from = marks[i]; left = true; }
                if (marks[i].Role == "laundry_b") { to = marks[i]; right = true; }
            }
            if (!left || !right) return;
            var line = new GameObject("Dress_Laundry");
            line.transform.SetParent(parent, false);
            var sway = line.AddComponent<WireSway>();
            sway.Configure(
                new Vector3(from.X, 2.05f, from.Z),
                new Vector3(to.X, 2.05f, to.Z),
                0.25f);
        }

        private static Color ColorFor(string role)
        {
            switch (role)
            {
                case "paper": return new Color(0.72f, 0.68f, 0.55f);
                case "bottle": return new Color(0.35f, 0.55f, 0.48f);
                case "tyre": return new Color(0.12f, 0.12f, 0.12f);
                case "brick": return new Color(0.48f, 0.24f, 0.18f);
                case "glass": return new Color(0.55f, 0.7f, 0.72f);
                case "patch": return new Color(0.22f, 0.22f, 0.22f);
                case "stripe": return new Color(0.72f, 0.7f, 0.62f);
                case "manhole": return new Color(0.22f, 0.22f, 0.24f);
                case "grate": return new Color(0.16f, 0.17f, 0.18f);
                case "skyline": return new Color(0.12f, 0.13f, 0.16f);
                case "tower": return new Color(0.32f, 0.34f, 0.36f);
                case "tower_leg":
                case "billboard_post": return new Color(0.3f, 0.26f, 0.22f);
                case "billboard": return new Color(0.62f, 0.22f, 0.16f);
                case "pole": return new Color(0.25f, 0.25f, 0.24f);
                case "overpass": return new Color(0.38f, 0.36f, 0.34f);
                case "tent": return new Color(0.42f, 0.38f, 0.28f);
                case "tarp": return new Color(0.22f, 0.32f, 0.38f);
                case "sandbag": return new Color(0.45f, 0.4f, 0.28f);
                case "cable": return new Color(0.15f, 0.15f, 0.15f);
                case "bulb": return new Color(1f, 0.86f, 0.55f);
                default: return new Color(0.34f, 0.3f, 0.26f);
            }
        }

        /// <summary>Library surface for a dressing role; paper, lane paint and bulbs keep flat colour.</summary>
        public static SurfaceFamily FamilyFor(string role)
        {
            switch (role)
            {
                case "bottle": return SurfaceFamily.Glass;
                case "tyre": return SurfaceFamily.Rubber;
                case "brick": return SurfaceFamily.BrickRed;
                case "patch": return SurfaceFamily.Asphalt;
                case "manhole":
                case "grate": return SurfaceFamily.MetalRusted;
                case "skyline": return SurfaceFamily.BrickGrey;
                case "overpass": return SurfaceFamily.ConcreteCracked;
                case "tower":
                case "billboard": return SurfaceFamily.MetalPainted;
                case "tower_leg":
                case "billboard_post": return SurfaceFamily.MetalRusted;
                case "pole": return SurfaceFamily.MetalPainted;
                case "tent":
                case "tarp":
                case "sandbag": return SurfaceFamily.TarpFabric;
                case "cable": return SurfaceFamily.Rubber;
                case "paper":
                case "stripe":
                case "bulb":
                case "glass": return SurfaceFamily.None;
                default: return SurfaceFamily.ConcreteCracked;
            }
        }

        private static void Paint(Renderer renderer, string role, Color color)
        {
            var family = FamilyFor(role);
            if (family != SurfaceFamily.None && MaterialLibrary.Dress(renderer, family, MaterialLibrary.TintFor(color))) return;
            Paint(renderer, color);
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
    }

    public class WireSway : MonoBehaviour
    {
        private Vector3 from;
        private Vector3 to;
        private float sag;
        private LineRenderer line;

        public void Configure(Vector3 start, Vector3 end, float droop)
        {
            from = start;
            to = end;
            sag = droop;
            line = gameObject.AddComponent<LineRenderer>();
            line.positionCount = 8;
            line.widthMultiplier = 0.035f;
            line.useWorldSpace = true;
            var material = Resources.Load<Material>("OutpostTriplanar");
            if (material != null) line.sharedMaterial = material;
            line.startColor = new Color(0.15f, 0.15f, 0.16f);
            line.endColor = line.startColor;
        }

        private void Update()
        {
            if (line == null) return;
            var sky = WeatherController.Instance;
            float wind = sky != null ? GroundMist.Wind(sky.Kind) : WireGust.Clear;
            float sway = WireGust.Side(wind, Time.time);
            int count = line.positionCount;
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : i / (float)(count - 1);
                var point = Vector3.Lerp(from, to, t);
                float bow = Mathf.Sin(t * Mathf.PI);
                point.y -= bow * sag;
                point.x += sway * bow;
                line.SetPosition(i, point);
            }
        }
    }
}
