#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using OutpostZero.Expedition;
using OutpostZero.Shell;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Tunes each district's Poisson-disk litter and commits the seed and densities to
    /// Resources/DebrisProfile.asset. Preview scatters around the selected object's curbs and walls
    /// (or the plain street edges) without entering Play mode.
    /// </summary>
    public class DebrisScatterer : EditorWindow
    {
        public const string AssetPath = "Assets/Resources/DebrisProfile.asset";
        public const string PreviewName = "DebrisPreview";

        private string[] districts = new string[0];
        private int district;
        private DebrisProfile.Row row;
        private Label summary;

        [MenuItem("Tools/Outpost Zero/Debris Scatterer", false, 6)]
        public static void Open() => GetWindow<DebrisScatterer>("Debris Scatterer");

        private void OnEnable()
        {
            var nodes = CampaignBoard.All();
            districts = new string[nodes.Length];
            for (int i = 0; i < nodes.Length; i++) districts[i] = nodes[i].Id;
            Load();
        }

        private void Load()
        {
            string id = districts.Length > 0 ? districts[Mathf.Clamp(district, 0, districts.Length - 1)] : "ash_market";
            var profile = AssetDatabase.LoadAssetAtPath<DebrisProfile>(AssetPath);
            var saved = profile != null ? profile.Find(id) : null;
            row = saved != null ? Copy(saved) : DebrisProfile.Default(id);
        }

        private void CreateGUI()
        {
            var ui = rootVisualElement;
            ui.Clear();
            var pick = new DropdownField("District", new List<string>(districts), Mathf.Clamp(district, 0, districts.Length - 1));
            pick.RegisterValueChangedCallback(e =>
            {
                district = System.Array.IndexOf(districts, e.newValue);
                Load();
                CreateGUI();
            });
            ui.Add(pick);
            var seed = new IntegerField("Seed") { value = row.seed };
            seed.RegisterValueChangedCallback(e => row.seed = e.newValue);
            ui.Add(seed);
            ui.Add(Number("Spacing (m)", row.spacing, v => row.spacing = Mathf.Max(0.2f, v)));
            ui.Add(Number("Open road density", row.baseDensity, v => row.baseDensity = Mathf.Clamp01(v)));
            ui.Add(Number("Wall and curb density", row.edgeDensity, v => row.edgeDensity = Mathf.Clamp01(v)));
            ui.Add(Number("Falloff (m)", row.reach, v => row.reach = Mathf.Max(0.1f, v)));
            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            buttons.Add(new Button(() => { row.seed = Random.Range(int.MinValue, int.MaxValue); CreateGUI(); Preview(); }) { text = "Reseed" });
            buttons.Add(new Button(Preview) { text = "Preview" });
            buttons.Add(new Button(ClearPreview) { text = "Clear" });
            buttons.Add(new Button(Save) { text = "Save" });
            ui.Add(buttons);
            summary = new Label("");
            ui.Add(summary);
            ui.Add(new HelpBox("Select the district root (or anything holding its curbs and walls) to scatter against it.", HelpBoxMessageType.None));
        }

        private void Preview()
        {
            ClearPreview();
            float[] anchors = Selection.activeTransform != null ? StreetDetail.Anchors(Selection.activeTransform) : DressingPlan.StreetEdges();
            var marks = DressingPlan.Debris(row, anchors);
            var root = new GameObject(PreviewName) { hideFlags = HideFlags.DontSave };
            foreach (var mark in marks)
            {
                var body = GameObject.CreatePrimitive(mark.Role == "tyre" ? PrimitiveType.Cylinder : PrimitiveType.Cube);
                body.name = "Debris_" + mark.Role;
                body.hideFlags = HideFlags.DontSave;
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.transform.SetParent(root.transform, false);
                body.transform.SetPositionAndRotation(new Vector3(mark.X, mark.Y, mark.Z), Quaternion.Euler(mark.Role == "tyre" ? 90f : 0f, mark.Yaw, 0f));
                body.transform.localScale = new Vector3(mark.W, mark.H, mark.D);
                OutpostZero.Graphics.MaterialLibrary.Dress(body.GetComponent<Renderer>(), DebrisLook.FamilyFor(mark.Role));
            }
            if (summary != null) summary.text = marks.Length + " pieces, " + (anchors.Length / 2) + " anchors";
        }

        private static void ClearPreview()
        {
            var old = GameObject.Find(PreviewName);
            if (old != null) Object.DestroyImmediate(old);
        }

        private void Save()
        {
            var profile = AssetDatabase.LoadAssetAtPath<DebrisProfile>(AssetPath);
            if (profile == null)
            {
                profile = CreateInstance<DebrisProfile>();
                AssetDatabase.CreateAsset(profile, AssetPath);
            }
            var saved = profile.Find(row.district);
            if (saved == null) profile.rows.Add(Copy(row));
            else
            {
                saved.seed = row.seed;
                saved.spacing = row.spacing;
                saved.baseDensity = row.baseDensity;
                saved.edgeDensity = row.edgeDensity;
                saved.reach = row.reach;
            }
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            if (summary != null) summary.text = "Saved " + row.district + " to " + AssetPath;
        }

        private static DebrisProfile.Row Copy(DebrisProfile.Row from)
        {
            return new DebrisProfile.Row
            {
                district = from.district,
                seed = from.seed,
                spacing = from.spacing,
                baseDensity = from.baseDensity,
                edgeDensity = from.edgeDensity,
                reach = from.reach,
            };
        }

        private static FloatField Number(string label, float value, System.Action<float> set)
        {
            var field = new FloatField(label) { value = value };
            field.RegisterValueChangedCallback(e => set(e.newValue));
            return field;
        }
    }
}
#endif
