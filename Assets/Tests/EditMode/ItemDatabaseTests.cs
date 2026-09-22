using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Items;

namespace OutpostZero.Tests.EditMode
{
    public class ItemDatabaseTests
    {
        static string Repo => Directory.GetCurrentDirectory();

        static string Guid(string path)
        {
            var match = Regex.Match(File.ReadAllText(path + ".meta"), @"guid: (\w+)");
            return match.Success ? match.Groups[1].Value : "";
        }

        static string Field(string yaml, string name)
        {
            var match = Regex.Match(yaml, @"^  " + name + @": (.*)$", RegexOptions.Multiline);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        static (int, int) PngSize(string path)
        {
            var bytes = File.ReadAllBytes(path);
            int width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
            int height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
            return (width, height);
        }

        /// <summary>Everything an item needs on disk: definition, world model, prefab, and rendered icon, all cross-linked.</summary>
        public static List<string> Problems(string root, IEnumerable<ItemRecord> records)
        {
            var problems = new List<string>();
            string P(string relative) => Path.Combine(root, relative);
            string itemScript = Guid(P("Assets/Scripts/Items/ItemDefinition.cs"));
            string ammoScript = Guid(P("Assets/Scripts/Items/AmmoDefinition.cs"));
            string database = File.Exists(P("Assets/Resources/ItemDatabase.asset")) ? File.ReadAllText(P("Assets/Resources/ItemDatabase.asset")) : "";
            foreach (var record in records)
            {
                string id = record.Id;
                string model = ItemVisuals.ModelFor(id);
                if (string.IsNullOrEmpty(model))
                {
                    problems.Add(id + " no model");
                    continue;
                }
                string assetPath = P(ItemVisuals.AssetPath(id));
                if (!File.Exists(assetPath) || !File.Exists(assetPath + ".meta"))
                {
                    problems.Add(id + " no definition");
                    continue;
                }
                string yaml = File.ReadAllText(assetPath);
                if (Field(yaml, "id") != id) problems.Add(id + " definition id " + Field(yaml, "id"));
                string script = record.Use == ItemUse.Ammo ? ammoScript : itemScript;
                if (!yaml.Contains("m_Script: {fileID: 11500000, guid: " + script + ", type: 3}")) problems.Add(id + " wrong script");
                if (Field(yaml, "worldModel") != model) problems.Add(id + " world model " + Field(yaml, "worldModel"));
                if (!database.Contains("guid: " + Guid(assetPath) + ",")) problems.Add(id + " not in database");

                string fbx = P(ItemVisuals.ModelPath(model));
                if (!File.Exists(fbx) || !File.Exists(fbx + ".meta")) problems.Add(id + " no fbx " + model);

                string icon = P(ItemVisuals.IconPath(model));
                if (!File.Exists(icon) || !File.Exists(icon + ".meta")) problems.Add(id + " no icon");
                else
                {
                    if (PngSize(icon) != (64, 64)) problems.Add(id + " icon size " + PngSize(icon));
                    if (Field(yaml, "icon") != "{fileID: 2800000, guid: " + Guid(icon) + ", type: 3}") problems.Add(id + " icon not linked");
                }

                string prefab = P(ItemVisuals.PrefabPath(model));
                if (!File.Exists(prefab) || !File.Exists(prefab + ".meta")) problems.Add(id + " no prefab");
                else
                {
                    string prefabText = File.ReadAllText(prefab);
                    var root_ = Regex.Match(prefabText, @"--- !u!1 &(\d+) stripped");
                    string link = Field(yaml, "worldPrefab") ?? "";
                    if (!root_.Success || link != "{fileID: " + root_.Groups[1].Value + ", guid: " + Guid(prefab) + ", type: 3}") problems.Add(id + " prefab not linked");
                    if (File.Exists(fbx + ".meta") && !prefabText.Contains("m_SourcePrefab: {fileID: 100100000, guid: " + Guid(fbx) + ", type: 3}")) problems.Add(id + " prefab not from its fbx");
                    if (!prefabText.Contains("BoxCollider:")) problems.Add(id + " prefab has no collider");
                }
            }
            return problems;
        }

        [Test]
        public void EveryItemHasADefinitionAWorldPrefabAndARenderedIcon()
        {
            CollectionAssert.IsEmpty(Problems(Repo, ItemCatalog.All));
            Assert.GreaterOrEqual(ItemCatalog.All.Count, 25);
        }

        [Test]
        public void TheAuditFailsWhenAnArtifactGoesMissing()
        {
            string root = Path.Combine(Path.GetTempPath(), "oz-items-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var bandage = ItemCatalog.Find("bandage");
                string model = ItemVisuals.ModelFor("bandage");
                var copies = new List<string>
                {
                    "Assets/Scripts/Items/ItemDefinition.cs.meta",
                    "Assets/Scripts/Items/AmmoDefinition.cs.meta",
                    "Assets/Resources/ItemDatabase.asset",
                    ItemVisuals.AssetPath("bandage"), ItemVisuals.AssetPath("bandage") + ".meta",
                    ItemVisuals.ModelPath(model), ItemVisuals.ModelPath(model) + ".meta",
                    ItemVisuals.IconPath(model), ItemVisuals.IconPath(model) + ".meta",
                    ItemVisuals.PrefabPath(model), ItemVisuals.PrefabPath(model) + ".meta"
                };
                foreach (var relative in copies)
                {
                    string to = Path.Combine(root, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(to));
                    File.Copy(Path.Combine(Repo, relative), to);
                }
                File.WriteAllText(Path.Combine(root, "Assets/Scripts/Items/ItemDefinition.cs"), "");
                File.WriteAllText(Path.Combine(root, "Assets/Scripts/Items/AmmoDefinition.cs"), "");
                CollectionAssert.IsEmpty(Problems(root, new[] { bandage }));

                File.Delete(Path.Combine(root, ItemVisuals.IconPath(model)));
                CollectionAssert.AreEqual(new[] { "bandage no icon" }, Problems(root, new[] { bandage }));

                File.Delete(Path.Combine(root, ItemVisuals.PrefabPath(model)));
                CollectionAssert.Contains(Problems(root, new[] { bandage }), "bandage no prefab");

                File.Delete(Path.Combine(root, ItemVisuals.AssetPath("bandage")));
                CollectionAssert.AreEqual(new[] { "bandage no definition" }, Problems(root, new[] { bandage }));

                var stranger = new ItemRecord { Id = "moon_rock", DisplayName = "Moon Rock" };
                CollectionAssert.AreEqual(new[] { "moon_rock no model" }, Problems(root, new[] { stranger }));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void EveryWorldModelIsATaggedPipelineItem()
        {
            string manifest = File.ReadAllText(Path.Combine(Repo, "BlenderScripts", "assets.manifest.json"));
            foreach (var id in ItemVisuals.ItemIds)
            {
                string model = ItemVisuals.ModelFor(id);
                var entry = Regex.Match(manifest, "\"id\": \"" + model + "\"[^}]*?\"tags\": \\[([^\\]]*)\\]", RegexOptions.Singleline);
                Assert.IsTrue(entry.Success, model);
                StringAssert.Contains("\"item\"", entry.Groups[1].Value, model);
                Assert.IsNotNull(ItemCatalog.Find(id), id);
            }
        }

        [Test]
        public void LootTableAssetsMatchTheBuiltInTables()
        {
            foreach (var id in new List<string>(LootTables.Ids))
            {
                string yaml = File.ReadAllText(Path.Combine(Repo, "Assets", "Data", "Loot", id + ".asset"));
                Assert.AreEqual(id, Field(yaml, "id"));
                var rows = Regex.Matches(yaml, @"- itemId: (\w*)\s+altItemId: (\w*)\s+roll: (\d+)\s+threshold: ([\d.]+)\s+count: (\d+)\s+spread: (\d+)");
                var entries = LootTables.Entries(id);
                Assert.AreEqual(entries.Length, rows.Count, id);
                for (int i = 0; i < entries.Length; i++)
                {
                    var row = rows[i].Groups;
                    Assert.AreEqual(entries[i].itemId, row[1].Value, id + i);
                    Assert.AreEqual(entries[i].altItemId ?? "", row[2].Value, id + i);
                    Assert.AreEqual((int)entries[i].roll, int.Parse(row[3].Value), id + i);
                    Assert.AreEqual(entries[i].threshold, float.Parse(row[4].Value, System.Globalization.CultureInfo.InvariantCulture), 1e-6f, id + i);
                    Assert.AreEqual(entries[i].count, int.Parse(row[5].Value), id + i);
                    Assert.AreEqual(entries[i].spread, int.Parse(row[6].Value), id + i);
                    Assert.IsNotNull(ItemCatalog.Find(entries[i].itemId), entries[i].itemId);
                    if (entries[i].roll == LootRoll.Pick) Assert.IsNotNull(ItemCatalog.Find(entries[i].altItemId), entries[i].altItemId);
                }
            }
        }

        static LootTables.Grant[] HandWritten(string tableId, int salt)
        {
            var rng = new Random(salt);
            if (tableId == "medical")
            {
                return new[]
                {
                    new LootTables.Grant { ItemId = rng.NextDouble() > 0.45 ? "medkit" : "bandage", Count = 1 },
                    new LootTables.Grant { ItemId = "water", Count = 1 },
                    new LootTables.Grant { ItemId = "antibiotics", Count = rng.NextDouble() > 0.55 ? 1 : 0 }
                };
            }
            if (tableId == "military")
            {
                return new[]
                {
                    new LootTables.Grant { ItemId = rng.NextDouble() > 0.4 ? "ammo_rifle" : "ammo_9mm", Count = 1 },
                    new LootTables.Grant { ItemId = "bandage", Count = rng.NextDouble() > 0.5 ? 1 : 0 },
                    new LootTables.Grant { ItemId = "print_flare", Count = rng.NextDouble() > 0.62 ? 1 : 0 },
                    new LootTables.Grant { ItemId = "ammo_smg", Count = rng.NextDouble() > 0.7 ? 1 : 0 }
                };
            }
            return new[]
            {
                new LootTables.Grant { ItemId = "scrap", Count = 2 + rng.Next(0, 5) },
                new LootTables.Grant { ItemId = rng.NextDouble() > 0.55 ? "canned_food" : "water", Count = 1 },
                new LootTables.Grant { ItemId = "cloth", Count = 1 },
                new LootTables.Grant { ItemId = rng.NextDouble() > 0.6 ? "chemicals" : "tape", Count = 1 },
                new LootTables.Grant { ItemId = "flare", Count = rng.NextDouble() > 0.72 ? 1 : 0 },
                new LootTables.Grant { ItemId = "pipe_bomb", Count = rng.NextDouble() > 0.88 ? 1 : 0 },
                new LootTables.Grant { ItemId = "raw_food", Count = rng.NextDouble() > 0.5 ? 1 : 0 }
            };
        }

        [Test]
        public void DataDrivenRollsMatchTheOldHandWrittenTables()
        {
            LootTables.Reset();
            foreach (var table in new[] { "crate", "medical", "military", "no_such_table" })
            {
                for (int salt = 0; salt < 400; salt++)
                {
                    var expected = HandWritten(table, salt);
                    var actual = LootTables.Roll(table, salt);
                    Assert.AreEqual(expected.Length, actual.Length, table);
                    for (int i = 0; i < expected.Length; i++)
                    {
                        Assert.AreEqual(expected[i].ItemId, actual[i].ItemId, table + salt + ":" + i);
                        Assert.AreEqual(expected[i].Count, actual[i].Count, table + salt + ":" + i);
                    }
                }
            }
        }

        [Test]
        public void DesignerTablesReplaceBuiltInsUntilReset()
        {
            try
            {
                LootTables.Use("medical", new[] { LootEntry.Fixed("medkit", 3) });
                var grants = LootTables.Roll("medical", 1);
                Assert.AreEqual(1, grants.Length);
                Assert.AreEqual("medkit", grants[0].ItemId);
                Assert.AreEqual(3, grants[0].Count);
                LootTables.Use("medical", new LootEntry[0]);
                Assert.AreEqual(1, LootTables.Roll("medical", 1).Length);
                LootTables.Use("", new[] { LootEntry.Fixed("medkit", 1) });
            }
            finally
            {
                LootTables.Reset();
            }
            Assert.AreEqual(3, LootTables.Roll("medical", 1).Length);
        }

        [Test]
        public void DesignerStatsOverwriteTheCodeFallback()
        {
            var original = ItemCatalog.Find("bandage");
            try
            {
                int changed = ItemCatalog.Apply(new[]
                {
                    new ItemRecord { Id = "bandage", DisplayName = "Bandage", Category = original.Category, Weight = 0.2f, Heal = 15, Use = ItemUse.Heal },
                    null,
                    new ItemRecord { Id = "" }
                });
                Assert.AreEqual(1, changed);
                Assert.AreEqual(15, ItemCatalog.Find("bandage").Heal);
                Assert.AreEqual(0.2f, ItemCatalog.Find("bandage").Weight);
            }
            finally
            {
                ItemCatalog.Apply(new[] { original });
            }
            Assert.AreSame(original, ItemCatalog.Find("bandage"));
            Assert.AreEqual(0, ItemCatalog.Apply(null));
        }

        [Test]
        public void GetReturnsTheAmmoDefinitionWithItsIcon()
        {
            ItemDatabase.Ensure();
            var ammo = ItemDatabase.Get("ammo_9mm");
            Assert.IsNotNull(ammo);
            Assert.IsInstanceOf<AmmoDefinition>(ammo);
            Assert.AreEqual(12, ((AmmoDefinition)ammo).rounds);
            Assert.AreEqual(Core.WeaponType.Pistol, ((AmmoDefinition)ammo).weapon);
            Assert.IsTrue(ItemDatabase.FromAsset);
            Assert.IsNotNull(ammo.icon);
            Assert.IsNotNull(ammo.worldPrefab);
            foreach (var record in ItemCatalog.All) Assert.IsNotNull(ItemDatabase.Get(record.Id), record.Id);
            Assert.IsNull(ItemDatabase.Get("moon_rock"));
            Assert.IsNull(ItemDatabase.Get(null));
        }

        [Test]
        public void WithoutTheAssetEveryIdStillResolves()
        {
            ItemDatabase.Use(null);
            try
            {
                Assert.IsFalse(ItemDatabase.FromAsset);
                foreach (var record in ItemCatalog.All)
                {
                    var definition = ItemDatabase.Get(record.Id);
                    Assert.IsNotNull(definition, record.Id);
                    Assert.AreEqual(record.Weight, definition.ToRecord().Weight, record.Id);
                    Assert.AreEqual(ItemVisuals.ModelFor(record.Id), definition.worldModel, record.Id);
                }
                Assert.IsInstanceOf<AmmoDefinition>(ItemDatabase.Get("ammo_rifle"));
                Assert.AreEqual(30, ItemDatabase.Get("ammo_rifle").ToRecord().AmmoAmount);
            }
            finally
            {
                ItemDatabase.Use(UnityEngine.Resources.Load<ItemDatabaseAsset>(ItemDatabase.ResourcePath));
            }
        }
    }
}
