using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.EditorTools;

namespace OutpostZero.Tests.EditMode
{
    public class SourceHygieneTests
    {
        static string Scripts => Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts");

        static string[] Sources() =>
            Directory.GetFiles(Scripts, "*.cs", SearchOption.AllDirectories);

        static string Rel(string path) =>
            path.Substring(Directory.GetCurrentDirectory().Length + 1).Replace('\\', '/');

        [Test]
        public void GameplayReadsOnlyTheInputSystem()
        {
            var legacy = new Regex(@"(?<![\w.])Input\.(GetKey|GetAxis|GetButton|GetMouseButton|mousePosition|mouseScrollDelta|anyKey|inputString|touches|GetTouch)");
            var hits = Sources().Where(f => legacy.IsMatch(File.ReadAllText(f))).Select(Rel).ToArray();
            CollectionAssert.IsEmpty(hits, "legacy UnityEngine.Input calls");
        }

        [Test]
        public void NoImmediateModeGui()
        {
            var ongui = new Regex(@"void\s+OnGUI\s*\(");
            var hits = Sources().Where(f => ongui.IsMatch(File.ReadAllText(f))).Select(Rel).ToArray();
            CollectionAssert.IsEmpty(hits, "OnGUI methods");
        }

        [Test]
        public void UiTextGoesThroughTheStringTable()
        {
            var literal = new Regex(@"(\b(Body|Button|Title|Label)\(\s*""[A-Za-z]|\.text\s*=\s*""[A-Za-z])");
            string ui = Path.Combine(Scripts, "UI");
            var hits = Directory.GetFiles(ui, "*.cs", SearchOption.AllDirectories)
                .SelectMany(f => File.ReadAllLines(f).Select((line, i) => new { f, line, i }))
                .Where(x => literal.IsMatch(x.line))
                .Select(x => Rel(x.f) + ":" + (x.i + 1) + "  " + x.line.Trim())
                .ToArray();
            CollectionAssert.IsEmpty(hits, "UI text literals; add a Loc key instead");
        }

        [Test]
        public void SceneBuilderPlacesPrefabsNotModelFiles()
        {
            string builder = Path.Combine(Scripts, "Editor", "PrototypeSceneBuilder.cs");
            Assert.IsTrue(File.Exists(builder), builder);
            string text = File.ReadAllText(builder);
            StringAssert.DoesNotContain(".fbx\"", text);
            StringAssert.DoesNotContain("ModelPaths.", text);
        }

        [Test]
        public void EveryBuilderAssetIdHasAPrefabOnDisk()
        {
            string root = Directory.GetCurrentDirectory();
            string text = File.ReadAllText(Path.Combine(Scripts, "Editor", "PrototypeSceneBuilder.cs"));
            var ids = Regex.Matches(text, @"InstantiateModel\(""([A-Za-z0-9_]+)""")
                .Cast<Match>().Select(m => m.Groups[1].Value)
                .Append("Zombie_Walker").Distinct().ToArray();
            Assert.Greater(ids.Length, 25);
            var prefabs = PrefabCatalog.Index(Directory.GetFiles(Path.Combine(root, PrefabCatalog.PrefabRoot), "*.prefab", SearchOption.AllDirectories));
            var missing = ids.Where(id => !prefabs.ContainsKey(id)).ToArray();
            CollectionAssert.IsEmpty(missing, "asset ids without a generated prefab");
        }

        [Test]
        public void CatalogPrefersPrefabsAndFallsBackToModels()
        {
            var prefabs = PrefabCatalog.Index(new[] { "Assets/Prefabs/Props/Prop_Dumpster.prefab" });
            var models = PrefabCatalog.Index(new[] { "Assets/Models/Props/Prop_Dumpster.fbx", "Assets/Models/Props/Prop_Crate_Wood.fbx" });
            Assert.AreEqual("Prop_Dumpster", PrefabCatalog.Id("Assets\\Models\\Props\\Prop_Dumpster.fbx"));
            Assert.AreEqual("Assets/Prefabs/Props/Prop_Dumpster.prefab", PrefabCatalog.Lookup("Prop_Dumpster", prefabs, models));
            Assert.AreEqual("Assets/Prefabs/Props/Prop_Dumpster.prefab", PrefabCatalog.Lookup("Assets/Models/Props/Prop_Dumpster.fbx", prefabs, models));
            Assert.AreEqual("Assets/Models/Props/Prop_Crate_Wood.fbx", PrefabCatalog.Lookup("Prop_Crate_Wood", prefabs, models));
            Assert.IsNull(PrefabCatalog.Lookup("Nope", prefabs, models));
            Assert.AreEqual(string.Empty, PrefabCatalog.Id(null));
        }
    }
}
