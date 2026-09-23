#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using OutpostZero.Expedition;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Rebuilds Resources/KitPrefabs from the kit catalog and Assets/Prefabs/Kit.
    /// BlenderScripts/kit_prefab_set.py writes the same asset without the editor.
    /// </summary>
    public static class KitPrefabSync
    {
        public const string CatalogPath = "Assets/Resources/KitCatalog.json";
        public const string PrefabDir = "Assets/Prefabs/Kit";
        public const string SetPath = "Assets/Resources/KitPrefabs.asset";

        [MenuItem("Tools/Outpost Zero/Sync Kit Prefabs", false, 4)]
        public static void SyncFromMenu()
        {
            var missing = Sync();
            if (missing.Count == 0) Debug.Log("[KitPrefabSync] Every kit piece has a prefab.");
            foreach (var id in missing) Debug.LogWarning("[KitPrefabSync] No prefab for kit piece " + id);
        }

        public static KitBook Book()
        {
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
            return text != null ? JsonUtility.FromJson<KitBook>(text.text) : null;
        }

        public static GameObject Prefab(string id)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + KitPlan.PrefabName(id) + ".prefab");
        }

        public static List<string> Sync()
        {
            var missing = new List<string>();
            var book = Book();
            if (book == null || book.pieces == null)
            {
                missing.Add(CatalogPath);
                return missing;
            }
            var set = AssetDatabase.LoadAssetAtPath<KitPrefabSet>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<KitPrefabSet>();
                AssetDatabase.CreateAsset(set, SetPath);
            }
            set.entries.Clear();
            foreach (var piece in book.pieces)
            {
                var prefab = Prefab(piece.id);
                if (prefab == null)
                {
                    missing.Add(piece.id);
                    continue;
                }
                set.entries.Add(new KitPrefabSet.Entry { id = piece.id, prefab = prefab });
            }
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            return missing;
        }
    }

    /// <summary>
    /// Snap-grid layout of kit prefabs. Pick a piece, Shift+click in the Scene view to drop it on the grid,
    /// load a catalog recipe to edit it, and export the assembly as a recipe the runtime can raise.
    /// </summary>
    public class KitAssembler : EditorWindow
    {
        public const string RootName = "KitAssembly";
        public const string RecipeDir = "Assets/Data/Kit";

        private KitBook book;
        private string[] ids = new string[0];
        private int piece;
        private float grid = 2f;
        private float storey = 3f;
        private float level;
        private int yaw;
        private string variant = "";
        private string recipeName = "custom";
        private static readonly string[] Recipes = { "storefront", "warehouse", "hospital", "apartment", "edge" };
        private int recipe = 3;

        [MenuItem("Tools/Outpost Zero/Kit Assembler", false, 5)]
        public static void Open() => GetWindow<KitAssembler>("Kit Assembler");

        private void OnEnable()
        {
            book = KitPrefabSync.Book();
            var list = new List<string>();
            if (book != null && book.pieces != null)
                foreach (var p in book.pieces) list.Add(p.id);
            ids = list.ToArray();
            SceneView.duringSceneGui += OnScene;
        }

        private void OnDisable() => SceneView.duringSceneGui -= OnScene;

        private void CreateGUI()
        {
            var ui = rootVisualElement;
            ui.Clear();
            if (ids.Length == 0)
            {
                ui.Add(new HelpBox("No kit catalog at " + KitPrefabSync.CatalogPath, HelpBoxMessageType.Warning));
                return;
            }
            var pieces = new DropdownField("Piece", new List<string>(ids), Mathf.Clamp(piece, 0, ids.Length - 1));
            var looks = new DropdownField("Variant", Looks(ids[Mathf.Clamp(piece, 0, ids.Length - 1)]), 0);
            pieces.RegisterValueChangedCallback(e =>
            {
                piece = System.Array.IndexOf(ids, e.newValue);
                looks.choices = Looks(e.newValue);
                looks.index = 0;
                variant = "";
            });
            looks.RegisterValueChangedCallback(e => variant = e.newValue == BuildingLook ? "" : e.newValue);
            ui.Add(pieces);
            ui.Add(looks);
            ui.Add(new Button(ApplyVariant) { text = "Apply variant to selection" });
            ui.Add(Number("Grid (m)", grid, v => grid = v));
            ui.Add(Number("Storey (m)", storey, v => storey = v));
            ui.Add(Number("Level", level, v => level = v));
            var turn = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var yawLabel = new Label("Yaw " + yaw);
            turn.Add(yawLabel);
            turn.Add(new Button(() => { yaw = KitPlan.SnapYaw(yaw - 90); yawLabel.text = "Yaw " + yaw; }) { text = "-90" });
            turn.Add(new Button(() => { yaw = KitPlan.SnapYaw(yaw + 90); yawLabel.text = "Yaw " + yaw; }) { text = "+90" });
            ui.Add(turn);
            ui.Add(new HelpBox("Shift+click in the Scene view to place on the grid.", HelpBoxMessageType.None));
            ui.Add(new Button(() =>
            {
                if (SceneView.lastActiveSceneView != null) Drop(ids[piece], SceneView.lastActiveSceneView.pivot);
            }) { text = "Place at Scene pivot" });

            var recipes = new DropdownField("Recipe", new List<string>(Recipes), recipe);
            recipes.RegisterValueChangedCallback(e => recipe = System.Array.IndexOf(Recipes, e.newValue));
            ui.Add(recipes);
            ui.Add(new Button(() => Load(Recipes[recipe])) { text = "Load recipe" });
            var export = new TextField("Export name") { value = recipeName };
            export.RegisterValueChangedCallback(e => recipeName = e.newValue);
            ui.Add(export);
            ui.Add(new Button(() => Export(recipeName)) { text = "Export assembly as recipe" });
            ui.Add(new Button(KitPrefabSync.SyncFromMenu) { text = "Sync Kit Prefabs" });
        }

        private const string BuildingLook = "(building)";

        private List<string> Looks(string id)
        {
            var list = new List<string> { BuildingLook };
            var kit = book != null ? KitPlan.Find(book.pieces, id) : null;
            if (kit != null && kit.variants != null && kit.variants.Length > 1) list.AddRange(kit.variants);
            return list;
        }

        private void ApplyVariant()
        {
            foreach (var chosen in Selection.gameObjects)
            {
                if (chosen == null || !KitPlan.ReadHost(chosen.name, out string id, out _)) continue;
                var kit = KitPlan.Find(book.pieces, id);
                string look = KitPlan.VariantFor(kit, variant, "");
                Undo.RecordObject(chosen, "Kit variant");
                chosen.name = KitPlan.HostName(id, look);
                Shade(chosen, kit, look);
            }
        }

        private static void Shade(GameObject host, KitPiece kit, string look)
        {
            if (kit == null) return;
            var shade = KitPlan.Shade(kit, string.IsNullOrEmpty(look) ? "brick" : look);
            foreach (var renderer in host.GetComponentsInChildren<Renderer>(true))
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_Tint", shade);
                renderer.SetPropertyBlock(block);
            }
        }

        private static FloatField Number(string label, float value, System.Action<float> set)
        {
            var field = new FloatField(label) { value = value };
            field.RegisterValueChangedCallback(e => set(e.newValue));
            return field;
        }

        private void OnScene(SceneView view)
        {
            var e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || !e.shift || ids.Length == 0) return;
            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, level * storey, 0f));
            if (!plane.Raycast(ray, out float distance)) return;
            Drop(ids[piece], ray.GetPoint(distance));
            e.Use();
        }

        private static Transform Root()
        {
            var root = GameObject.Find(RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(root, "Kit assembly");
            }
            return root.transform;
        }

        private void Drop(string id, Vector3 world)
        {
            var root = Root();
            var local = root.InverseTransformPoint(world);
            local.y = level * storey;
            var placement = KitPlan.Place(id, local, yaw, grid, storey);
            placement.variant = KitPlan.VariantFor(KitPlan.Find(book.pieces, id), variant, "");
            var host = Spawn(root, placement);
            if (host != null) Selection.activeGameObject = host;
        }

        public static GameObject Spawn(Transform root, KitPlacement placement)
        {
            var prefab = KitPrefabSync.Prefab(placement.id);
            if (prefab == null)
            {
                Debug.LogWarning("[KitAssembler] No prefab for " + placement.id);
                return null;
            }
            var host = new GameObject(KitPlan.HostName(placement.id, placement.variant));
            Undo.RegisterCreatedObjectUndo(host, "Place kit piece");
            host.transform.SetParent(root, false);
            host.transform.localPosition = new Vector3(placement.x, placement.y, placement.z);
            host.transform.localRotation = Quaternion.Euler(0f, placement.yaw, 0f);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, host.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, KitPlan.MeshYaw, 0f);
            var book = KitPrefabSync.Book();
            if (book != null) Shade(host, KitPlan.Find(book.pieces, placement.id), placement.variant);
            return host;
        }

        private void Load(string name)
        {
            if (book == null) return;
            var placements = name == "storefront" ? book.storefront
                : name == "warehouse" ? book.warehouse
                : name == "hospital" ? book.hospital
                : name == "apartment" ? book.apartment
                : book.edge;
            if (placements == null) return;
            var root = Root();
            foreach (var placement in placements) Spawn(root, placement);
            recipeName = name;
        }

        public static KitPlacement[] Collect(Transform root)
        {
            var list = new List<KitPlacement>();
            if (root == null) return list.ToArray();
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (!KitPlan.ReadHost(child.name, out string id, out string look)) continue;
                var placement = KitPlan.Place(id, child.localPosition, child.localEulerAngles.y, 0.01f, 0.01f);
                placement.variant = look;
                list.Add(placement);
            }
            return list.ToArray();
        }

        [System.Serializable]
        private class RecipeFile
        {
            public string name;
            public KitPlacement[] placements;
        }

        private static void Export(string name)
        {
            var root = GameObject.Find(RootName);
            if (root == null)
            {
                Debug.LogWarning("[KitAssembler] Nothing to export; place pieces first.");
                return;
            }
            Directory.CreateDirectory(RecipeDir);
            string path = RecipeDir + "/" + (string.IsNullOrEmpty(name) ? "custom" : name) + ".json";
            var file = new RecipeFile { name = name, placements = Collect(root.transform) };
            File.WriteAllText(path, JsonUtility.ToJson(file, true));
            AssetDatabase.ImportAsset(path);
            Debug.Log("[KitAssembler] Wrote " + file.placements.Length + " pieces to " + path);
        }
    }
}
#endif
