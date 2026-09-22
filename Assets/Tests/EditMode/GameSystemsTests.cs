using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Colony;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Expedition;
using OutpostZero.Graphics;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Sensory;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class GameSystemsTests
    {
        [Test]
        public void ItemCatalogCoversMedicalAmmoAndFood()
        {
            Assert.IsNotNull(ItemCatalog.Find("medkit"));
            Assert.AreEqual(50, ItemCatalog.Find("medkit").Heal);
            Assert.AreEqual(ItemUse.Ammo, ItemCatalog.Find("ammo_rifle").Use);
            Assert.AreEqual(30, ItemCatalog.Find("ammo_rifle").AmmoAmount);
            Assert.IsNotNull(ItemCatalog.Find("canned_food"));
            Assert.IsNotNull(ItemCatalog.Find("molotov"));
        }

        [Test]
        public void SaveCodecRoundTripsSchema()
        {
            var data = new SaveGameData
            {
                day = 4,
                hour = 6.5f,
                colonyScrap = 22,
                raw = 4,
                language = "es",
                districtIndex = 2,
                survivors = new[]
                {
                    new SurvivorSave { id = "mara", displayName = "Mara Quill", alive = false, leader = false, morale = 10f, task = "Fallen" }
                },
                modules = new[]
                {
                    new ModuleSave { kind = "Barricade", x = 2f, z = -4f, rotation = 0, age = 3, site = 1, hours = 1 }
                }
            };

            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(data), out var loaded, out var error), error);
            Assert.AreEqual(1, loaded.schemaVersion);
            Assert.AreEqual(4, loaded.day);
            Assert.AreEqual(22, loaded.colonyScrap);
            Assert.AreEqual(4, loaded.raw);
            Assert.AreEqual("es", loaded.language);
            Assert.AreEqual(2, loaded.districtIndex);
            Assert.AreEqual("mara", loaded.survivors[0].id);
            Assert.IsFalse(loaded.survivors[0].alive);
            Assert.AreEqual("Barricade", loaded.modules[0].kind);
            Assert.AreEqual(100, loaded.modules[0].integrity);
            Assert.AreEqual(3, loaded.modules[0].age);
            Assert.AreEqual(1, loaded.modules[0].site);
            Assert.AreEqual(1, loaded.modules[0].hours);
            Assert.IsTrue(SaveCodec.TryDeserialize("{\"schemaVersion\":1,\"modules\":[{\"kind\":\"Farm\",\"age\":3}]}", out var legacy, out var legacyError), legacyError);
            Assert.AreEqual(0, legacy.modules[0].site);
            Assert.AreEqual(0, legacy.modules[0].hours);
            Assert.AreEqual(3, legacy.modules[0].age);
        }

        [Test]
        public void SaveCodecRejectsTheWrongSchema()
        {
            Assert.IsFalse(SaveCodec.TryDeserialize("{\"schemaVersion\":99}", out _, out var error));
            Assert.AreEqual("schema", error);
        }

        [Test]
        public void TensionBandsMatchTheDirector()
        {
            Assert.AreEqual(TensionState.Calm, HordeDirector.Evaluate(0f));
            Assert.AreEqual(TensionState.Relax, HordeDirector.Evaluate(20f));
            Assert.AreEqual(TensionState.BuildUp, HordeDirector.Evaluate(50f));
            Assert.AreEqual(TensionState.Peak, HordeDirector.Evaluate(90f));
        }

        [Test]
        public void NightFactorIsDarkAfterDusk()
        {
            Assert.AreEqual(0f, DayNightCycle.HourToNight(12f));
            Assert.AreEqual(1f, DayNightCycle.HourToNight(23f));
            Assert.Greater(DayNightCycle.HourToNight(18.5f), 0.4f);
        }

        [Test]
        public void EnglishCopyResolvesWithoutASettingsObject()
        {
            Assert.AreEqual("PAUSED", Loc.T("menu.pause"));
            Assert.AreEqual("missing.key", Loc.T("missing.key"));
            Assert.AreEqual("PAUSA", Loc.T("menu.pause", "es"));
            Assert.AreEqual("Entrar al santuario", Loc.T("menu.enter", "es"));
            Assert.AreEqual("AJUSTES", Loc.T("set.title", "es"));
            Assert.AreEqual("Superviviente", Loc.T("diff.survivor", "es"));
            Assert.AreEqual("Survivor", Loc.Difficulty(2));
            Assert.AreEqual("missing.key", Loc.T("missing.key", "es"));
            Assert.AreEqual("Medkit", Loc.Item("medkit"));
            Assert.AreEqual("Botiquín", Loc.Item("medkit", "es"));
            Assert.AreEqual("Flare", Loc.Item("flare"));
            Assert.AreEqual("Bengala", Loc.Item("flare", "es"));
            Assert.AreEqual("Pipe Bomb", Loc.Item("pipe_bomb"));
            Assert.AreEqual("Bomba de tubo", Loc.Item("pipe_bomb", "es"));
            Assert.AreEqual("Ash Market", Loc.District("ash_market"));
            Assert.AreEqual("Mercado de ceniza", Loc.District("ash_market", "es"));
            Assert.AreEqual("Rest", Loc.Task("Rest"));
            Assert.AreEqual("Descansar", Loc.Task("Rest", "es"));
            Assert.AreEqual("Manos firmes", Loc.T("trait.steady", "es"));
            Assert.AreEqual("Caminante", Loc.T("codex.zombie.walker.title", "es"));
        }

        [Test]
        public void ACurbStepsUpFifteenCentimetersAndLeavesTheLaneOpen()
        {
            Assert.AreEqual(0.15f, RoadGraph.CurbHeight, 0.001f);
            Assert.AreEqual(1.6f, RoadGraph.LaneClear, 0.001f);
            Assert.Less(RoadGraph.CurbHeight, 0.3f);

            var edges = RoadGraph.Edges(RoadGraph.Build(1701, "ash_market"));
            Assert.IsTrue(HasEdge(edges, 8f, 0f, "dash"));
            Assert.IsTrue(HasEdge(edges, 32f, 0f, "cross"));
            Assert.IsTrue(HasEdge(edges, 12f, 0.89f, "curb"));
            Assert.IsFalse(HasEdge(edges, 12f, 1.38f, "walk"));
            Assert.IsTrue(HasEdge(edges, 8f, 1.38f, "walk"));
            for (int i = 0; i < edges.Length; i++)
            {
                if (edges[i].Kind != "curb") continue;
                Assert.IsFalse(edges[i].X < 3.5f && edges[i].Z < 0.2f && edges[i].Z > -0.2f);
            }
        }

        [Test]
        public void KeyRebindRejectsADuplicateAndRoundTrips()
        {
            ControlBindings.ResetDefaults();
            try
            {
                Assert.AreEqual("E", ControlBindings.Label(ControlBindings.Action.Interact));
                Assert.IsTrue(ControlBindings.TryRebindNamed(ControlBindings.Action.Interact, "H"));
                Assert.IsFalse(ControlBindings.TryRebindNamed(ControlBindings.Action.Reload, "H"));
                string packed = ControlBindings.Pack();
                ControlBindings.ResetDefaults();
                Assert.AreEqual("E", ControlBindings.Label(ControlBindings.Action.Interact));
                ControlBindings.Unpack(packed);
                Assert.AreEqual("H", ControlBindings.Label(ControlBindings.Action.Interact));
            }
            finally
            {
                ControlBindings.ResetDefaults();
            }
        }

        [Test]
        public void TutorialAdvancesOnlyOnTheMatchingAction()
        {
            bool finished;
            int index = TutorialTrack.Advance(0, "fire", out finished);
            Assert.AreEqual(0, index);
            Assert.IsFalse(finished);

            index = TutorialTrack.Advance(index, "move", out finished);
            Assert.AreEqual(1, index);
            index = TutorialTrack.Advance(index, "fire", out finished);
            index = TutorialTrack.Advance(index, "crouch", out finished);
            index = TutorialTrack.Advance(index, "loot", out finished);
            Assert.AreEqual(4, index);
            Assert.IsFalse(finished);
            index = TutorialTrack.Advance(index, "pack", out finished);
            Assert.IsTrue(finished);
            Assert.AreEqual(TutorialTrack.Gates.Length, index);
        }

        [Test]
        public void DistrictLayoutsStayOnTheStreet()
        {
            string[] ids = { "ash_market", "rail_yard", "old_hospital", "north_gate", "nowhere" };
            foreach (string id in ids)
            {
                var pieces = DistrictLayout.For(id);
                Assert.Greater(pieces.Length, 0);
                for (int i = 0; i < pieces.Length; i++)
                {
                    Assert.IsTrue(DistrictLayout.StaysOnTheStreet(pieces[i]), id + " " + pieces[i].Role);
                }
            }

            Assert.Greater(DistrictLayout.Count("ash_market", "stall"), 0);
            Assert.Greater(DistrictLayout.Count("rail_yard", "barrel_explosive"), DistrictLayout.Count("ash_market", "barrel_explosive"));
            Assert.GreaterOrEqual(DistrictLayout.Count("old_hospital", "crate_medical"), 2);
            Assert.GreaterOrEqual(DistrictLayout.Count("north_gate", "cover"), 3);
        }

        [Test]
        public void BootStreamsTheOutpostAndReadsParkedProgressAsFull()
        {
            Assert.AreEqual("Boot", BootPlan.SceneFor(FlowStep.Boot));
            Assert.AreEqual("PrototypeArena", BootPlan.SceneFor(FlowStep.MainMenu));
            Assert.AreEqual("PrototypeArena", BootPlan.SceneFor(FlowStep.Expedition));
            Assert.AreEqual(0f, BootPlan.Bar(0f));
            Assert.AreEqual(0.5f, BootPlan.Bar(0.45f), 0.0001f);
            Assert.AreEqual(1f, BootPlan.Bar(0.9f));
            Assert.IsFalse(BootPlan.Loaded(0.89f));
            Assert.IsTrue(BootPlan.Loaded(0.9f));
            Assert.IsTrue(BootPlan.InBudget(4.9f));
            Assert.IsFalse(BootPlan.InBudget(5f));
        }

        [Test]
        public void BootSceneIsFirstInBuildAndRunsTheLoader()
        {
            string root = Directory.GetCurrentDirectory();
            string build = File.ReadAllText(Path.Combine(root, "ProjectSettings", "EditorBuildSettings.asset"));
            int boot = build.IndexOf("Assets/Scenes/Boot.unity");
            int arena = build.IndexOf("Assets/Scenes/PrototypeArena.unity");
            Assert.GreaterOrEqual(boot, 0);
            Assert.Greater(arena, boot);
            string scene = File.ReadAllText(Path.Combine(root, "Assets", "Scenes", "Boot.unity"));
            string meta = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "UI", "BootLoader.cs.meta"));
            string guid = meta.Substring(meta.IndexOf("guid: ") + 6, 32);
            StringAssert.Contains("guid: " + guid, scene);
            StringAssert.Contains("SceneRoots", scene);
        }

        [Test]
        public void DevMenuStaysOutOfReleaseAndGodModeShields()
        {
            Assert.IsTrue(DevCheats.Allowed(true, false));
            Assert.IsTrue(DevCheats.Allowed(false, true));
            Assert.IsFalse(DevCheats.Allowed(false, false));
            Assert.IsTrue(DevCheats.Toggle(true, false));
            Assert.IsFalse(DevCheats.Toggle(true, true));
            Assert.IsFalse(DevCheats.Toggle(false, false));
            DevCheats.SetGod(false);
            Assert.IsFalse(DevCheats.Shielded(false));
            Assert.IsTrue(DevCheats.Shielded(true));
            DevCheats.SetGod(true);
            Assert.IsTrue(DevCheats.Shielded(false));
            DevCheats.SetGod(false);
            foreach (var id in DevCheats.Kit) Assert.IsNotNull(ItemCatalog.Find(id), id);
            Assert.AreNotEqual(Loc.T("dev.title", "en"), Loc.T("dev.title", "es"));
        }

        [Test]
        public void KitAssemblesAnEnterableThreeStoreyBlock()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Resources", "KitCatalog.json");
            Assert.IsTrue(File.Exists(path), path);
            var book = JsonUtility.FromJson<KitBook>(File.ReadAllText(path));
            Assert.GreaterOrEqual(book.pieces.Length, 45);
            Assert.AreEqual(2f, book.grid);
            Assert.IsTrue(KitPlan.UsesOnlyKnownPieces(book.pieces, book.apartment));
            Assert.IsTrue(KitPlan.UsesOnlyKnownPieces(book.pieces, book.storefront));
            Assert.IsTrue(KitPlan.UsesOnlyKnownPieces(book.pieces, book.warehouse));
            Assert.IsTrue(KitPlan.UsesOnlyKnownPieces(book.pieces, book.hospital));
            Assert.IsTrue(KitPlan.ReachesStorey(book.apartment, 0f));
            Assert.IsTrue(KitPlan.ReachesStorey(book.apartment, 3f));
            Assert.IsTrue(KitPlan.ReachesStorey(book.apartment, 6f));
            var door = KitPlan.Find(book.pieces, "wall_door");
            Assert.GreaterOrEqual(door.door, 1.2f);
            Assert.IsTrue(KitPlan.ClearAt(door, 1f, 1f, 0.1f));
            Assert.IsFalse(KitPlan.ClearAt(door, 0.05f, 1f, 0.1f));
            Assert.AreEqual("apartment", KitPlan.RecipeName("north_gate"));
            Assert.AreEqual("storefront", KitPlan.RecipeName("ash_market"));
        }

        [Test]
        public void AFallenLeaderIsMournedAndCanBeFoundAgain()
        {
            var camp = new List<ColonistDay>
            {
                new ColonistDay { id = "jonas", bond = "Close to Mara", alive = true, morale = 80f },
                new ColonistDay { id = "priya", bond = "Trusts Ellis", alive = true, morale = 70f },
                new ColonistDay { id = "mara", bond = "", alive = false, morale = 10f }
            };
            SuccessionLedger.Grieve(camp, "Mara Quill");
            Assert.AreEqual(40f, camp[0].morale);
            Assert.AreEqual(45f, camp[1].morale);
            Assert.AreEqual(10f, camp[2].morale);

            Assert.AreEqual("wounded", SuccessionLedger.Outcome(true, 2));
            Assert.AreEqual("succession", SuccessionLedger.Outcome(false, 2));
            Assert.AreEqual("wiped", SuccessionLedger.Outcome(false, 0));
            SuccessionLedger.NextMorning(4, out int day, out float hour);
            Assert.AreEqual(5, day);
            Assert.AreEqual(6.5f, hour);

            string memorial = SuccessionLedger.PackMemorials(new[]
            {
                new SuccessionLedger.Memorial { name = "Mara Quill", day = 4, kills = 12, cause = "infection", district = "ash_market" }
            });
            var remembered = SuccessionLedger.UnpackMemorials(memorial);
            Assert.AreEqual("Mara Quill", remembered[0].name);
            Assert.AreEqual(12, remembered[0].kills);
            Assert.AreEqual("infection", remembered[0].cause);
            Assert.AreEqual(0, SuccessionLedger.UnpackMemorials(null).Count);

            string bodies = SuccessionLedger.PackCorpses(new[]
            {
                new SuccessionLedger.CorpseMark { district = "ash_market", x = 3.5f, y = 0f, z = 8f, name = "Mara Quill", gear = "bandage*1+scrap*4", recovered = false }
            });
            var found = SuccessionLedger.UnpackCorpses(bodies);
            Assert.AreEqual("ash_market", found[0].district);
            Assert.AreEqual(3.5f, found[0].x);
            Assert.AreEqual("bandage*1+scrap*4", found[0].gear);
            Assert.IsFalse(found[0].recovered);
            var saved = new SaveGameData { memorial = memorial, corpses = bodies, mercy = 0 };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(saved), out var loaded, out var error), error);
            Assert.AreEqual(1, loaded.schemaVersion);
            Assert.AreEqual(memorial, loaded.memorial);
            Assert.AreEqual(bodies, loaded.corpses);
            Assert.AreEqual(0, loaded.mercy);
        }

        [Test]
        public void DistrictsChangeTheQuotaAndTheStreet()
        {
            var market = DistrictRules.For("ash_market");
            var yard = DistrictRules.For("rail_yard");
            var hospital = DistrictRules.For("old_hospital");
            var gate = DistrictRules.For("north_gate");
            var unknown = DistrictRules.For("nowhere");

            Assert.AreEqual(8, market.KillGoal);
            Assert.AreEqual(15, market.ScrapGoal);
            Assert.AreEqual(WeatherKind.Clear, market.Weather);
            Assert.AreEqual("", market.LootTable);

            Assert.AreEqual(WeatherKind.Rain, yard.Weather);
            Assert.AreEqual("military", yard.LootTable);
            Assert.AreEqual("Brute", yard.PreferredVariant);

            Assert.AreEqual(WeatherKind.Fog, hospital.Weather);
            Assert.AreEqual("medical", hospital.LootTable);
            Assert.AreEqual("Runner", hospital.PreferredVariant);

            Assert.Greater(gate.KillGoal, market.KillGoal);
            Assert.Greater(gate.OpeningTension, yard.OpeningTension);
            Assert.AreEqual(market.KillGoal, unknown.KillGoal);
        }

        [Test]
        public void DistrictTableBiasesStreetCrates()
        {
            DistrictRules.SetActiveTable("medical");
            try
            {
                var grants = LootTables.Roll("crate", 3);
                Assert.AreEqual("water", grants[1].ItemId);
            }
            finally
            {
                DistrictRules.SetActiveTable("");
            }
        }

        [Test]
        public void MedicalCacheRollsSupplies()
        {
            var grants = LootTables.Roll("medical", 3);
            Assert.AreEqual(3, grants.Length);
            Assert.AreEqual("antibiotics", grants[2].ItemId);
            Assert.LessOrEqual(grants[2].Count, 1);
            Assert.IsTrue(grants[0].ItemId == "medkit" || grants[0].ItemId == "bandage");
            Assert.AreEqual("water", grants[1].ItemId);
            Assert.AreEqual(1, grants[1].Count);
        }

        [Test]
        public void StarvingCampBreaksDownWithinThreeDays()
        {
            var people = new List<ColonistDay>
            {
                new ColonistDay { id = "c", task = "Rest", morale = 40f, hunger = 78f, thirst = 78f }
            };
            int food = 0;
            int water = 0;
            ColonyDay.Simulate(people, ref food, ref water, false, false, "");
            Assert.AreEqual(35f, people[0].morale);
            Assert.AreEqual("Steady", ColonyDay.Mood(people[0].morale));
            ColonyDay.Simulate(people, ref food, ref water, false, false, "");
            Assert.AreEqual(30f, people[0].morale);
            var events = ColonyDay.Simulate(people, ref food, ref water, false, false, "");
            Assert.AreEqual(1f, people[0].morale);
            Assert.AreEqual("Breakdown", ColonyDay.Mood(people[0].morale));
            Assert.IsFalse(people[0].alive);
            Assert.AreEqual("Left", people[0].task);
            Assert.Contains("breakdown", events);
        }

        [Test]
        public void SuppliedCampRisesAndCelebratesTheReturn()
        {
            var people = new List<ColonistDay>
            {
                new ColonistDay { id = "mara", task = "Cook", morale = 72f, hunger = 78f, thirst = 78f, leader = true }
            };
            int food = 10;
            int water = 10;
            var events = ColonyDay.Simulate(people, ref food, ref water, true, true, "");
            Assert.AreEqual(9, food);
            Assert.AreEqual(9, water);
            Assert.AreEqual(100f, people[0].hunger);
            Assert.AreEqual(96f, people[0].thirst);
            Assert.AreEqual(90f, people[0].morale);
            Assert.AreEqual("Inspired", ColonyDay.Mood(people[0].morale));
            Assert.Contains("celebration", events);
            Assert.AreEqual(1.1f, ColonyDay.OutputScale(80f));
            Assert.AreEqual(0.7f, ColonyDay.OutputScale(20f));
            Assert.AreEqual(0f, ColonyDay.OutputScale(5f));

            var tired = new List<ColonistDay> { new ColonistDay { morale = 50f, alive = true } };
            Assert.AreEqual(0, ColonyDay.RewardReturn(tired).Length);
            Assert.AreEqual(60f, tired[0].morale);
            var bright = new List<ColonistDay> { new ColonistDay { morale = 72f, alive = true } };
            Assert.Contains("celebration", ColonyDay.RewardReturn(bright));
            Assert.AreEqual(82f, bright[0].morale);
        }

        [Test]
        public void RelationshipsRecoveryAndGriefFollowTheDay()
        {
            var friends = new List<ColonistDay>
            {
                new ColonistDay { id = "jonas", task = "Guard", morale = 50f, hunger = 78f, thirst = 78f, opinion = 39 },
                new ColonistDay { id = "ellis", task = "Guard", morale = 50f, hunger = 78f, thirst = 78f, opinion = 39 }
            };
            int food = 6;
            int water = 6;
            var friendship = ColonyDay.Simulate(friends, ref food, ref water, false, false, "");
            Assert.AreEqual(41, friends[0].opinion);
            Assert.AreEqual("ellis:2", friends[0].kin);
            Assert.AreEqual("jonas:2", friends[1].kin);
            Assert.Contains("friendship", friendship);

            var ward = new List<ColonistDay>
            {
                new ColonistDay { id = "priya", trait = "Volatile", task = "Rest", morale = 60f, hunger = 78f, thirst = 78f, opinion = 18 },
                new ColonistDay { id = "ellis", task = "Guard", morale = 60f, hunger = 78f, thirst = 78f, opinion = 18 }
            };
            var argument = ColonyDay.Simulate(ward, ref food, ref water, false, false, "");
            Assert.AreEqual(12, ward[0].opinion);
            Assert.Contains("argument", argument);

            var hungry = new List<ColonistDay>
            {
                new ColonistDay { id = "mara", task = "Guard", morale = 40f, hunger = 28f, thirst = 80f }
            };
            food = 4;
            water = 4;
            var recovery = ColonyDay.Simulate(hungry, ref food, ref water, false, false, "");
            Assert.AreEqual(58f, hungry[0].hunger);
            Assert.Contains("recovery", recovery);

            var grieving = new List<ColonistDay>
            {
                new ColonistDay { id = "jonas", task = "Guard", bond = "Close to Mara", morale = 80f, hunger = 78f, thirst = 78f },
                new ColonistDay { id = "ellis", task = "Scavenge", morale = 80f, hunger = 78f, thirst = 78f }
            };
            var grief = ColonyDay.Simulate(grieving, ref food, ref water, false, false, "Mara Quill");
            Assert.AreEqual(44f, grieving[0].morale);
            Assert.AreEqual(59f, grieving[1].morale);
            Assert.Contains("grief", grief);

            var low = new List<ColonistDay>
            {
                new ColonistDay { id = "ellis", task = "Scavenge", morale = 28f, hunger = 78f, thirst = 78f }
            };
            food = 0;
            water = 0;
            ColonyDay.Simulate(low, ref food, ref water, false, false, "");
            Assert.AreEqual("Rest", low[0].task);
            Assert.AreEqual("Depressed", ColonyDay.Mood(low[0].morale));

            var infected = new List<ColonistDay>
            {
                new ColonistDay { id = "jonas", task = "Guard", morale = 50f, hunger = 78f, thirst = 78f, injury = 1 }
            };
            food = 2;
            water = 2;
            var healed = ColonyDay.Simulate(infected, ref food, ref water, true, false, "");
            Assert.AreEqual(0, infected[0].injury);
            Assert.Contains("recovery", healed);

            Assert.IsFalse(FeverSpread.Source(1, "Guard", true));
            Assert.IsTrue(FeverSpread.Source(2, "Guard", true));
            Assert.IsFalse(FeverSpread.Source(3, "Quarantine", true));
            Assert.IsFalse(FeverSpread.Source(2, "Medic", true));
            Assert.IsFalse(FeverSpread.Catches(3, "Guard", true, "ellis", "jonas"));
            Assert.IsFalse(FeverSpread.Catches(0, "Quarantine", true, "ellis", "jonas"));
            Assert.AreEqual(1, FeverSpread.Apply(0));
            Assert.AreEqual(3, FeverSpread.Apply(3));
            var feverWard = new List<ColonistDay>
            {
                new ColonistDay { id = "jonas", task = "Guard", injury = 2, hunger = 78f, thirst = 78f, morale = 60f },
                new ColonistDay { id = "ellis", task = "Scavenge", injury = 0, hunger = 78f, thirst = 78f, morale = 60f }
            };
            int wardFood = 4;
            int wardWater = 4;
            var fever = ColonyDay.Simulate(feverWard, ref wardFood, ref wardWater, false, false, "");
            Assert.AreEqual(2, feverWard[0].injury);
            Assert.AreEqual(1, feverWard[1].injury);
            Assert.Contains("fever", fever);
            var held = new List<ColonistDay>
            {
                new ColonistDay { id = "jonas", task = "Quarantine", injury = 3, hunger = 78f, thirst = 78f, morale = 60f },
                new ColonistDay { id = "ellis", task = "Guard", injury = 0, hunger = 78f, thirst = 78f, morale = 60f }
            };
            Assert.IsFalse(FeverSpread.Try(held));
            Assert.AreEqual(0, held[1].injury);
        }

        [Test]
        public void SceneRouteNamesEveryStepAndEndsOnAFullBar()
        {
            var titles = new System.Collections.Generic.HashSet<string>();
            foreach (FlowStep step in System.Enum.GetValues(typeof(FlowStep)))
            {
                string title = SceneRoute.Title(step);
                Assert.IsFalse(string.IsNullOrEmpty(title));
                Assert.IsTrue(titles.Add(title));
                Assert.IsFalse(string.IsNullOrEmpty(SceneRoute.Tip(step, 0)));
                Assert.AreNotEqual(SceneRoute.Tip(step, 0), SceneRoute.Tip(step, 1));
                var beats = SceneRoute.Beats(step);
                Assert.GreaterOrEqual(beats.Length, 2);
                Assert.AreEqual(1f, beats[beats.Length - 1]);
                for (int i = 1; i < beats.Length; i++) Assert.Greater(beats[i], beats[i - 1]);
            }

            Assert.AreEqual(GameState.MainMenu, SceneRoute.StateFor(FlowStep.Boot));
            Assert.AreEqual(GameState.MainMenu, SceneRoute.StateFor(FlowStep.MainMenu));
            Assert.AreEqual(GameState.CampManagement, SceneRoute.StateFor(FlowStep.Sanctuary));
            Assert.AreEqual(GameState.ExpeditionActive, SceneRoute.StateFor(FlowStep.Expedition));
            Assert.AreEqual(GameState.ExpeditionResults, SceneRoute.StateFor(FlowStep.Results));
            Assert.AreEqual("0.5.0", SceneRoute.Version);
            Assert.GreaterOrEqual(SceneRoute.Tips.Length, 12);
        }

        [Test]
        public void CodexHintsShowOnceAndZombiesStayHiddenUntilAKill()
        {
            Assert.AreEqual(12, CodexBook.Hints.Length);
            Assert.GreaterOrEqual(CodexBook.Entries.Length, 12);

            Assert.IsTrue(CodexBook.TryHint("", "move", out string first, out string packed));
            Assert.IsFalse(string.IsNullOrEmpty(first));
            Assert.IsTrue(CodexBook.Has(packed, "hint.move"));
            Assert.IsFalse(CodexBook.TryHint(packed, "move", out _, out string again));
            Assert.AreEqual(packed, again);

            CodexBook.Entry walker = null;
            for (int i = 0; i < CodexBook.Entries.Length; i++)
            {
                if (CodexBook.Entries[i].Id == "zombie.walker") walker = CodexBook.Entries[i];
            }
            Assert.IsNotNull(walker);
            Assert.IsFalse(CodexBook.Visible(walker, packed));
            packed = CodexBook.Remember(packed, "zombie.walker", out bool added);
            Assert.IsTrue(added);
            Assert.IsTrue(CodexBook.Visible(walker, packed));
            CodexBook.Remember(packed, "zombie.walker", out bool twice);
            Assert.IsFalse(twice);

            CodexBook.Entry noise = null;
            for (int i = 0; i < CodexBook.Entries.Length; i++)
            {
                if (CodexBook.Entries[i].Id == "mechanic.noise") noise = CodexBook.Entries[i];
            }
            Assert.IsTrue(CodexBook.Visible(noise, ""));
        }

        [Test]
        public void WeaponModsAndCampSquaresChangeTheShotAndThePrice()
        {
            var suppressor = WeaponMod.ProfileFor("suppressor");
            var optic = WeaponMod.ProfileFor("optic");
            var mag = WeaponMod.ProfileFor("extended_mag");
            Assert.Less(suppressor.noise, 1f);
            Assert.Less(optic.spread, 1f);
            Assert.AreEqual(10, mag.magazineBonus);
            Assert.AreEqual(1f, WeaponMod.ProfileFor("missing").damage);

            Assert.AreEqual(14, GridBuilder.Cost(ModuleKind.Generator));
            Assert.AreEqual(12, GridBuilder.Cost(ModuleKind.Workbench));
            var yard = new List<PlacedModule>
            {
                new PlacedModule { kind = "Barricade", x = 2f, z = -4f, integrity = 100 }
            };
            Assert.IsTrue(GridBuilder.Occupied(yard, 2f, -4f));
            Assert.IsFalse(GridBuilder.Occupied(yard, 4f, -4f));
            Assert.AreEqual(11, CraftingBench.Priced(12, true));
            Assert.AreEqual(12, CraftingBench.Priced(12, false));
            Assert.AreEqual(1, CraftingBench.Priced(1, true));

            string packed = WeaponMod.JoinSlots(new[]
            {
                WeaponMod.PackFlags(true, true, false),
                "",
                WeaponMod.PackFlags(false, false, true)
            });
            var stacked = WeaponMod.Combine(WeaponMod.SplitSlots(packed)[0]);
            Assert.AreEqual(0.4f, stacked.noise);
            Assert.AreEqual(0.85f * 0.55f, stacked.spread, 0.0001f);
            Assert.AreEqual(0, stacked.magazineBonus);
            Assert.AreEqual(10, WeaponMod.Combine(WeaponMod.SplitSlots(packed)[2]).magazineBonus);
            Assert.AreEqual(1f, WeaponMod.Combine(null).damage);
            var saved = new SaveGameData { weaponMods = packed };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(saved), out var loaded, out var error), error);
            Assert.AreEqual(1, loaded.schemaVersion);
            Assert.AreEqual(packed, loaded.weaponMods);
            Assert.AreEqual(0, WeaponMod.SplitSlots(null).Length);
        }

        [Test]
        public void ACookTurnsRawFoodIntoMealsBeforeTheDayIsServed()
        {
            Assert.AreEqual(0, CookPot.Stew(0, 0, 2, true));
            Assert.AreEqual(0, CookPot.Stew(3, 0, 2, false));
            Assert.AreEqual(2, CookPot.Stew(5, 0, CampRoom.Food, true));
            Assert.AreEqual(1, CookPot.Stew(1, 0, 2, true));
            Assert.AreEqual(0, CookPot.Stew(4, 2, 2, true));
            Assert.AreEqual(1, CookPot.Stew(4, 1, 2, true));
            Assert.AreEqual(0, CookPot.Stew(-1, 0, 2, true));
            var pot = new List<ColonistDay>
            {
                new ColonistDay { id = "cook", task = "Cook", hunger = 40f, thirst = 90f, morale = 50f }
            };
            int food = 0;
            int raw = 3;
            int water = 0;
            var notes = ColonyDay.Simulate(pot, ref food, ref water, false, false, "", 0, ref raw);
            Assert.AreEqual(1, raw);
            Assert.AreEqual(1, food);
            Assert.AreEqual(70f, pot[0].hunger, 0.001f);
            Assert.Contains("stew", notes);
            int plainFood = 0;
            int plainRaw = 3;
            int plainWater = 0;
            var plain = new List<ColonistDay>
            {
                new ColonistDay { id = "ada", task = "Rest", hunger = 40f, thirst = 90f, morale = 50f }
            };
            ColonyDay.Simulate(plain, ref plainFood, ref plainWater, false, false, "", 0, ref plainRaw);
            Assert.AreEqual(2, plainRaw);
            Assert.AreEqual(0, plainFood);
            Assert.AreEqual(44f, plain[0].hunger, 0.001f);
        }

        [Test]
        public void ARestDayTakesTheWearOffAndATiredShiftPaysLess()
        {
            Assert.AreEqual(22f, ShiftWear.After(0f, "Guard", false), 0.001f);
            Assert.AreEqual(8f, ShiftWear.After(0f, "Lead", false), 0.001f);
            Assert.AreEqual(0f, ShiftWear.After(0f, "Fallen", false), 0.001f);
            Assert.AreEqual(0f, ShiftWear.After(30f, "Rest", false), 0.001f);
            Assert.AreEqual(40f, ShiftWear.After(80f, "Rest", false), 0.001f);
            Assert.AreEqual(10f, ShiftWear.After(80f, "Rest", true), 0.001f);
            Assert.AreEqual(100f, ShiftWear.After(90f, "Scavenge", false), 0.001f);
            Assert.AreEqual(4, ShiftWear.Short(4, 75f));
            Assert.AreEqual(3, ShiftWear.Short(4, 76f));
            Assert.AreEqual(1, ShiftWear.Short(1, 90f));
            Assert.AreEqual(0, ShiftWear.Short(0, 90f));
            Assert.AreEqual("Worn out", Loc.T("camp.tired"));
            Assert.AreEqual("Agotado", Loc.T("camp.tired", "es"));
            Assert.AreEqual("Cansancio", Loc.T("camp.wear", "es"));
            var worked = new List<ColonistDay>
            {
                new ColonistDay { id = "ada", task = "Guard", hunger = 90f, thirst = 90f, morale = 50f, fatigue = 80f }
            };
            int food = 2;
            int water = 2;
            int raw = 0;
            ColonyDay.Simulate(worked, ref food, ref water, false, false, "", 0, ref raw);
            Assert.AreEqual(100f, worked[0].fatigue, 0.001f);
            var rested = new List<ColonistDay>
            {
                new ColonistDay { id = "ada", task = "Rest", hunger = 90f, thirst = 90f, morale = 50f, fatigue = 80f }
            };
            ColonyDay.Simulate(rested, ref food, ref water, true, false, "", 0, ref raw);
            Assert.AreEqual(10f, rested[0].fatigue, 0.001f);
        }

        [Test]
        public void ATiredColonistLeavesThePostAndWalksSlower()
        {
            Assert.AreEqual("Guard", CampRoutine.Choose("Guard", 80f, 80f, 60f, 0));
            Assert.AreEqual("Guard", CampRoutine.Choose("Guard", 80f, 80f, 60f, 0, 75f));
            Assert.AreEqual("Rest", CampRoutine.Choose("Guard", 80f, 80f, 60f, 0, 76f));
            Assert.AreEqual("Rest", CampRoutine.Choose("Clear", 80f, 80f, 60f, 0, 90f));
            Assert.AreEqual("Cook", CampRoutine.Choose("Guard", 20f, 80f, 60f, 0, 90f));
            Assert.AreEqual("Medic", CampRoutine.Choose("Scavenge", 80f, 80f, 60f, 2, 90f));
            Assert.AreEqual("Rest", CampRoutine.Choose("Guard", 80f, 80f, 5f, 0, 90f));
            Assert.AreEqual("Resting.", CampRoutine.Bark("Rest", 50f));
            Assert.AreEqual("My legs are done.", CampRoutine.Bark("Rest", 50f, 80f));
            Assert.AreEqual("My legs are done.", Loc.Bark("Rest", 50f, 80f));
            Assert.AreEqual("Las piernas no dan más.", Loc.T("bark.tired", "es"));
            Assert.AreEqual(1.4f, ShiftWear.Stride(0f, false), 0.001f);
            Assert.AreEqual(0.9f, ShiftWear.Stride(76f, false), 0.001f);
            Assert.AreEqual(3.6f, ShiftWear.Stride(75f, true), 0.001f);
            Assert.AreEqual(2.2f, ShiftWear.Stride(80f, true), 0.001f);
        }

        [Test]
        public void ACloseFriendWalksOverWhenBothAreResting()
        {
            var ids = new[] { "ellis", "jonas", "mara" };
            var rest = new[] { "Rest", "Rest", "Rest" };
            var here = new[] { true, true, true };
            Assert.AreEqual("", YardVisit.Host("ellis", "Rest", "jonas:40", 0f, ids, rest, here));
            Assert.AreEqual("ellis", YardVisit.Host("jonas", "Rest", "ellis:40", 0f, ids, rest, here));
            Assert.AreEqual("ellis", YardVisit.Host("jonas", "Rest", "ellis:40|mara:50", 0f, ids, rest, here));
            Assert.AreEqual("ellis", YardVisit.Host("mara", "Rest", "ellis:40|jonas:40", 0f, ids, rest, here));
            Assert.AreEqual("jonas", YardVisit.Host("mara", "Rest", "jonas:40", 0f, ids, rest, here));
            Assert.AreEqual("", YardVisit.Host("jonas", "Guard", "ellis:40", 0f, ids, rest, here));
            Assert.AreEqual("", YardVisit.Host("jonas", "Rest", "ellis:39", 0f, ids, rest, here));
            Assert.AreEqual("", YardVisit.Host("jonas", "Rest", "ellis:40", 76f, ids, rest, here));
            Assert.AreEqual("ellis", YardVisit.Host("jonas", "Rest", "ellis:40", 75f, ids, rest, here));
            var working = new[] { "Guard", "Rest", "Rest" };
            Assert.AreEqual("", YardVisit.Host("jonas", "Rest", "ellis:40", 0f, ids, working, here));
            var gone = new[] { false, true, true };
            Assert.AreEqual("", YardVisit.Host("jonas", "Rest", "ellis:40|mara:40", 0f, ids, rest, gone));
            YardVisit.Stand(-14f, -15f, out float x, out float z);
            Assert.AreEqual(-13.2f, x, 0.001f);
            Assert.AreEqual(-15f, z, 0.001f);
            Assert.AreEqual(0.8f, YardVisit.Beside, 0.001f);
            Assert.AreEqual(6f, YardPose.Lean("Visit", 0f), 0.001f);
            Assert.AreEqual(1f, YardPose.Scale("Visit"), 0.001f);
            Assert.AreEqual(0.72f, YardPose.Scale("Rest"), 0.001f);
            Assert.AreEqual("Good to see you.", CampRoutine.Bark("Visit", 50f));
            Assert.AreEqual("Me alegra verte.", Loc.T("bark.visit", "es"));
            Assert.AreEqual("Visita", Loc.Task("Visit", "es"));
        }

        [Test]
        public void TheExtractScreenNamesTheStreetAndTheHaul()
        {
            Assert.AreEqual("Ash Market  kills 8/8  scrap 15/15", ExtractSlip.Line("Ash Market", 8, 8, 15, 15, "kills", "scrap"));
            Assert.AreEqual("Street  kills 0/1  scrap 0/1", ExtractSlip.Line("", -2, 0, -4, 0, "", ""));
            Assert.AreEqual("Rail Yard  bajas 3/8  chatarra 4/15", ExtractSlip.Line("Rail Yard", 3, 8, 4, 15, "bajas", "chatarra"));
            Assert.AreEqual("bajas", Loc.T("result.kills", "es"));
            Assert.AreEqual("chatarra", Loc.T("result.scrap", "es"));
            Assert.AreEqual("kills", Loc.T("result.kills", "en"));
        }

        [Test]
        public void AStepInAPuddleCarriesFartherUnlessYouCrouch()
        {
            Assert.IsTrue(PuddleStep.Inside(2.2f, 6f, 0.65f));
            Assert.IsFalse(PuddleStep.Inside(2.2f, 6f, 0.64f));
            Assert.IsFalse(PuddleStep.Inside(0f, 0f, 0.85f));
            Assert.IsTrue(PuddleStep.Inside(2.9f, 6f, 0.65f));
            Assert.IsFalse(PuddleStep.Inside(2.91f, 6f, 0.65f));
            Assert.AreEqual(8.7f, PuddleStep.Radius(6f, true, false), 0.001f);
            Assert.AreEqual(6f, PuddleStep.Radius(6f, true, true), 0.001f);
            Assert.AreEqual(18.85f, PuddleStep.Radius(13f, true, false), 0.001f);
            Assert.AreEqual(2f, PuddleStep.Radius(2f, false, false), 0.001f);
            Assert.AreEqual(0f, PuddleStep.Radius(-1f, true, false), 0.001f);
            Assert.AreEqual(1.45f, PuddleStep.Splash, 0.001f);
            Assert.AreEqual(0.65f, WeatherSurface.Wetness(WeatherKind.Rain), 0.001f);
        }

        [Test]
        public void AMetalStepCarriesFartherThanDirt()
        {
            Assert.AreEqual(1.35f, StepReach.Metal, 0.001f);
            Assert.AreEqual(1.12f, StepReach.Hard, 0.001f);
            Assert.AreEqual(0.9f, StepReach.Wood, 0.001f);
            Assert.AreEqual(0.75f, StepReach.Gravel, 0.001f);
            Assert.AreEqual(1.18f, StepReach.Water, 0.001f);
            Assert.AreEqual(8.1f, StepReach.Radius(6f, "step_metal"), 0.001f);
            Assert.AreEqual(6.72f, StepReach.Radius(6f, "step_hard"), 0.001f);
            Assert.AreEqual(5.4f, StepReach.Radius(6f, "step_wood"), 0.001f);
            Assert.AreEqual(4.5f, StepReach.Radius(6f, "step_gravel"), 0.001f);
            Assert.AreEqual(7.08f, StepReach.Radius(6f, "step_water"), 0.001f);
            Assert.AreEqual(6f, StepReach.Radius(6f, "step"), 0.001f);
            Assert.AreEqual(6f, StepReach.Radius(6f, null), 0.001f);
            Assert.AreEqual(17.55f, StepReach.Radius(13f, "step_metal"), 0.001f);
            Assert.AreEqual(1.5f, StepReach.Radius(2f, "step_gravel"), 0.001f);
            Assert.AreEqual(0f, StepReach.Radius(-1f, "step_metal"), 0.001f);
            Assert.AreEqual(8.1f, StepReach.Radius(6f, AudioMix.StepId("Dress_manhole")), 0.001f);
            Assert.AreEqual(4.5f, StepReach.Radius(6f, AudioMix.StepId("gravel_lot")), 0.001f);
        }

        [Test]
        public void StreetWearComesHomeAndARestedCardGoesBackOut()
        {
            Assert.AreEqual(40f, BodyCarry.Clamp(40f), 0.001f);
            Assert.AreEqual(0f, BodyCarry.Clamp(-5f), 0.001f);
            Assert.AreEqual(100f, BodyCarry.Clamp(140f), 0.001f);
            Assert.AreEqual(20f, BodyCarry.Carry(8f, 20f, false), 0.001f);
            Assert.AreEqual(8f, BodyCarry.Carry(8f, 20f, true), 0.001f);
            Assert.AreEqual(0f, BodyCarry.Carry(0f, 20f, true), 0.001f);
            Assert.AreEqual(20f, BodyCarry.Carry(0f, 20f, false), 0.001f);
            Assert.AreEqual(0f, BodyCarry.Carry(-4f, 20f, true), 0.001f);
        }

        [Test]
        public void RainShortensAStepAndAPuddleShortensItAgain()
        {
            Assert.AreEqual(1f, WetStride.Scale(0f, false), 0.001f);
            Assert.AreEqual(1f, WetStride.Scale(0.2f, true), 0.001f);
            Assert.AreEqual(0.9f, WetStride.Scale(0.65f, false), 0.001f);
            Assert.AreEqual(0.75f, WetStride.Scale(0.65f, true), 0.001f);
            Assert.AreEqual(4.5f, WetStride.Pace(4.5f, 0f, false), 0.001f);
            Assert.AreEqual(4.05f, WetStride.Pace(4.5f, 0.65f, false), 0.001f);
            Assert.AreEqual(3.375f, WetStride.Pace(4.5f, 0.65f, true), 0.001f);
            Assert.AreEqual(6.75f, WetStride.Pace(7.5f, 0.85f, false), 0.001f);
            Assert.AreEqual(1.65f, WetStride.Pace(2.2f, 0.65f, true), 0.001f);
            Assert.AreEqual(0f, WetStride.Pace(-1f, 0.85f, true), 0.001f);
        }

        [Test]
        public void ABleedDripCallsNearbyAndGoreOffStaysQuiet()
        {
            Assert.IsTrue(BleedScent.Calls(true, true, 1));
            Assert.IsFalse(BleedScent.Calls(true, true, 0));
            Assert.IsFalse(BleedScent.Calls(false, true, 2));
            Assert.IsFalse(BleedScent.Calls(true, false, 2));
            Assert.AreEqual(4.5f, BleedScent.Radius, 0.001f);
            Assert.AreEqual(0.35f, BleedScent.Loud, 0.001f);
            Assert.Less(BleedScent.Radius, 6f);
            Assert.Greater(BleedScent.Radius, 2f);
            Assert.AreEqual("[Drip, north]", Presentation.Caption(NoiseType.BleedDrip, 0f, 1f, "en"));
            Assert.AreEqual("[Goteo, norte]", Presentation.Caption(NoiseType.BleedDrip, 0f, 1f, "es"));
            Assert.AreEqual("", Presentation.Caption(NoiseType.WalkFootstep, 0f, 1f, "en"));
            Assert.IsTrue(ClipBook.Has("drip"));
            Assert.AreEqual(6f, AudioSpace.MaxDistance("drip"), 0.001f);
        }

        [Test]
        public void OpeningADoorCarriesFartherThanAWalk()
        {
            Assert.AreEqual(9f, DoorCreak.Radius, 0.001f);
            Assert.AreEqual(0.8f, DoorCreak.Loud, 0.001f);
            Assert.Greater(DoorCreak.Radius, 6f);
            Assert.Less(DoorCreak.Radius, 13f);
            Assert.AreEqual("[Door, east]", Presentation.Caption(NoiseType.DoorSwing, 1f, 0f, "en"));
            Assert.AreEqual("[Puerta, este]", Presentation.Caption(NoiseType.DoorSwing, 1f, 0f, "es"));
            Assert.AreEqual("", Presentation.Caption(NoiseType.SneakFootstep, 1f, 0f, "en"));
            Assert.IsTrue(ClipBook.Has("creak"));
            Assert.AreEqual(12f, AudioSpace.MaxDistance("creak"), 0.001f);
        }

        [Test]
        public void ASprintOrAHitDropsAReloadBeforeTheRack()
        {
            Assert.IsFalse(ReloadBreak.Abort(false, false, 0f));
            Assert.IsTrue(ReloadBreak.Abort(true, false, 0f));
            Assert.IsTrue(ReloadBreak.Abort(false, true, 0.5f));
            Assert.IsTrue(ReloadBreak.Abort(false, true, 0.71f));
            Assert.IsFalse(ReloadBreak.Abort(true, false, 0.72f));
            Assert.IsFalse(ReloadBreak.Abort(true, true, 1f));
            Assert.IsTrue(ReloadBreak.Saves(0.72f));
            Assert.IsFalse(ReloadBreak.Saves(0.719f));
            Assert.AreEqual(0.72f, ReloadBreak.Rack, 0.001f);
            Assert.AreEqual("rack", GunCue.Stage(0.72f, 2));
        }

        [Test]
        public void AimingDownSightsShortensAStep()
        {
            Assert.AreEqual(4.5f, AimPace.Pace(4.5f, false), 0.001f);
            Assert.AreEqual(2.475f, AimPace.Pace(4.5f, true), 0.001f);
            Assert.AreEqual(1.21f, AimPace.Pace(2.2f, true), 0.001f);
            Assert.AreEqual(2.2275f, AimPace.Pace(4.05f, true), 0.001f);
            Assert.AreEqual(4.125f, AimPace.Pace(7.5f, true), 0.001f);
            Assert.AreEqual(0f, AimPace.Pace(-1f, true), 0.001f);
            Assert.AreEqual(0.55f, AimPace.Fraction, 0.001f);
            Assert.IsTrue(AimPace.AllowsSprint(false));
            Assert.IsFalse(AimPace.AllowsSprint(true));
        }

        [Test]
        public void SightsPullAShotGroupTighter()
        {
            Assert.AreEqual(2.5f, SightGroup.Angle(2.5f, false), 0.001f);
            Assert.AreEqual(1.55f, SightGroup.Angle(2.5f, true), 0.001f);
            Assert.AreEqual(0f, SightGroup.Angle(-1f, true), 0.001f);
            Assert.AreEqual(0.62f, SightGroup.Tight, 0.001f);
            float open = RecoilBloom.Spread(2.5f, 1f, 80f);
            Assert.AreEqual(5f, open, 0.001f);
            Assert.AreEqual(3.1f, SightGroup.Angle(open, true), 0.001f);
        }

        [Test]
        public void TheStreetBoardSpeaksSpanish()
        {
            Assert.AreEqual("Hunger 40  Thirst 55  Fatigue 80", StreetHud.Needs(40, 55, 80, "en"));
            Assert.AreEqual("Hambre 40  Sed 55  Fatiga 80", StreetHud.Needs(40, 55, 80, "es"));
            Assert.AreEqual("Hunger 0  Thirst 0  Fatigue 0", StreetHud.Needs(-1, -2, -3, "en"));
            Assert.AreEqual("Kills 3/8   Scrap 4/15", StreetHud.Quota(3, 8, 4, 15, "en"));
            Assert.AreEqual("Bajas 3/8   Chatarra 4/15", StreetHud.Quota(3, 8, 4, 15, "es"));
            Assert.AreEqual("Tension 80  Peak", StreetHud.Tension(80, "Peak", "en"));
            Assert.AreEqual("Tensión 50  Subida", StreetHud.Tension(50, "BuildUp", "es"));
            Assert.AreEqual("Hold to extract 3s", StreetHud.Hold(3, "en"));
            Assert.AreEqual("Mantén para extraer 0s", StreetHud.Hold(-4, "es"));
            Assert.AreEqual("Hit from the front", StreetHud.Hit("front", "en"));
            Assert.AreEqual("Golpe por detrás", StreetHud.Hit("back", "es"));
            Assert.AreEqual("No weapon", StreetHud.None("en"));
            Assert.AreEqual("Sin arma", StreetHud.None("es"));
            Assert.AreEqual("Pistol   4 / 20  reload 40%", StreetHud.Ammo("Pistol", 4, 20, true, 40, true, "en"));
            Assert.AreEqual("Pistol   2 / 20  bajo", StreetHud.Ammo("Pistol", 2, 20, false, 0, true, "es"));
            Assert.AreEqual(Affliction.Label(2), StreetHud.Infection(2, "en"));
            Assert.AreEqual("Infección II", StreetHud.Infection(2, "es"));
            Assert.AreEqual("Raid 8s", StreetHud.Raid(8, "en"));
            Assert.AreEqual("Asalto 8s", StreetHud.Raid(8, "es"));
            Assert.IsTrue(Loc.T("hint.aim", "en").Contains("shortens the step"));
            Assert.IsTrue(Loc.T("hint.aim", "es").Contains("acorta el paso"));
        }

        [Test]
        public void TheStallSpeaksSpanish()
        {
            Assert.AreEqual("Use workbench", StallVoice.Prompt(StationKind.Workbench, "en"));
            Assert.AreEqual("Usar el banco", StallVoice.Prompt(StationKind.Workbench, "es"));
            Assert.AreEqual("Trade", StallVoice.Prompt(StationKind.Merchant, "en"));
            Assert.AreEqual("Comerciar", StallVoice.Prompt(StationKind.Merchant, "es"));
            Assert.AreEqual("Need a workbench", StallVoice.Block("Need a workbench", "en"));
            Assert.AreEqual("Hace falta un banco", StallVoice.Block("Need a workbench", "es"));
            Assert.AreEqual("Need a medic on duty", StallVoice.Block("Need a medic on duty", "en"));
            Assert.AreEqual("Hace falta un médico de turno", StallVoice.Block("Need a medic on duty", "es"));
            Assert.AreEqual("The Clinic wants 4 medkits", StallVoice.Quest("clinic", false, "en"));
            Assert.AreEqual("La Clínica pide 4 botiquines", StallVoice.Quest("clinic", false, "es"));
            Assert.AreEqual("Field dressings learned", StallVoice.Quest("clinic", true, "en"));
            Assert.AreEqual("Escort complete", StallVoice.Quest("caravan", true, "en"));
            Assert.AreEqual("Escolta cumplida", StallVoice.Quest("caravan", true, "es"));
            Assert.AreEqual("Iron Militia sells rifle and shell ammo", StallVoice.Quest("militia", true, "en"));
            Assert.AreEqual("Iron Militia", StallVoice.Name("militia", "en"));
            Assert.AreEqual("Milicia de Hierro", StallVoice.Name("militia", "es"));
            Assert.AreEqual(CaravanBook.Display("clinic"), StallVoice.Name("clinic", "en"));
            Assert.AreEqual("La Clínica", StallVoice.Name("clinic", "es"));
            Assert.AreEqual("Buy Medkit (14)", StallVoice.Buy("Medkit", 14, "en"));
            Assert.AreEqual("Comprar Botiquín (14)", StallVoice.Buy("Botiquín", 14, "es"));
            Assert.AreEqual("The Caravan will not trade", StallVoice.Refuse("The Caravan", "en"));
            Assert.AreEqual("La Caravana no comercia", StallVoice.Refuse("La Caravana", "es"));
        }

        [Test]
        public void MistSitsOnTheStreetWhenTheAirIsThick()
        {
            Assert.IsTrue(MistBank.Shows(WeatherKind.Fog, 0f));
            Assert.IsTrue(MistBank.Shows(WeatherKind.Storm, 0f));
            Assert.IsFalse(MistBank.Shows(WeatherKind.Clear, 0f));
            Assert.IsFalse(MistBank.Shows(WeatherKind.Clear, 0.69f));
            Assert.IsTrue(MistBank.Shows(WeatherKind.Clear, 0.7f));
            Assert.IsTrue(MistBank.Shows(WeatherKind.Clear, 1.4f));
            Assert.IsFalse(MistBank.Shows(WeatherKind.Rain, 0f));
            Assert.IsFalse(MistBank.Shows(WeatherKind.Overcast, 0.49f));
            Assert.IsTrue(MistBank.Shows(WeatherKind.Overcast, 0.5f));
            Assert.AreEqual(3, MistBank.Count);
            Assert.AreEqual(0.8f, MistBank.Top, 0.001f);
            Assert.Less(MistBank.Top, 1.6f);
            for (int i = 0; i < MistBank.Count; i++)
            {
                var spot = MistBank.At(i);
                Assert.IsTrue(DressingPlan.OnTheStreet(spot.X, spot.Z), i.ToString());
            }
        }

        [Test]
        public void RainWetsTheGroundAndShotsSitInTheWorld()
        {
            Assert.AreEqual(0.65f, WeatherSurface.Wetness(WeatherKind.Rain));
            Assert.IsTrue(RainPuddle.Shows(WeatherSurface.Wetness(WeatherKind.Rain)));
            Assert.IsTrue(RainPuddle.Shows(WeatherSurface.Wetness(WeatherKind.Storm)));
            Assert.IsFalse(RainPuddle.Shows(WeatherSurface.Wetness(WeatherKind.Fog)));
            Assert.IsFalse(RainPuddle.Shows(WeatherSurface.Wetness(WeatherKind.Clear)));
            Assert.AreEqual(4, RainPuddle.Count);
            for (int i = 0; i < RainPuddle.Count; i++)
            {
                var spot = RainPuddle.At(i);
                Assert.IsTrue(DressingPlan.OnTheStreet(spot.X, spot.Z));
            }
            Assert.AreEqual(0.2f, WeatherSurface.Wetness(WeatherKind.Fog));
            Assert.AreEqual(0f, WeatherSurface.Wetness(WeatherKind.Clear));
            Assert.AreEqual(0.62f, WeatherSurface.Sight(WeatherKind.Fog));
            Assert.AreEqual(0f, AudioSpace.SpatialBlend("pulse"));
            Assert.AreEqual(1f, AudioSpace.SpatialBlend("gun"));
            Assert.AreEqual(0.35f, AudioSpace.SpatialBlend("step"));
            Assert.Greater(AudioSpace.MaxDistance("boom"), AudioSpace.MaxDistance("hit"));
        }

        [Test]
        public void FogHidesTheEdgeAndAshFallsOnTheMarket()
        {
            Assert.AreEqual(0.006f, GroundMist.Air(0f), 0.0001f);
            Assert.AreEqual(0.014f, GroundMist.Air(1f), 0.0001f);
            Assert.AreEqual(0.01f, GroundMist.Air(0.5f), 0.0001f);
            Assert.AreEqual(0.006f, GroundMist.Air(-1f), 0.0001f);
            Assert.AreEqual(0.014f, GroundMist.Air(2f), 0.0001f);
            Assert.AreEqual(0f, GroundMist.Pool(2.4f), 0.0001f);
            Assert.AreEqual(0.01f, GroundMist.Pool(0f), 0.0001f);
            Assert.AreEqual(0.005f, GroundMist.Pool(1.2f), 0.0001f);
            Assert.AreEqual(0f, GroundMist.Pool(8f), 0.0001f);
            Assert.AreEqual(0.028f, GroundMist.Density(WeatherKind.Fog, 0f, 2.4f), 0.0001f);
            Assert.IsTrue(GroundMist.Hides(GroundMist.Density(WeatherKind.Fog, 0f, 2.4f), GroundMist.Edge));
            Assert.IsFalse(GroundMist.Hides(GroundMist.Density(WeatherKind.Clear, 0f, 2.4f), GroundMist.Edge));
            Assert.AreEqual(0.65f, GroundMist.Wind(WeatherKind.Rain), 0.001f);
            Assert.AreEqual(0.08f, GroundMist.Wind(WeatherKind.Clear), 0.001f);
            Color day = GroundMist.Tint(WeatherKind.Clear, 0f);
            Assert.AreEqual(0.55f, day.r, 0.001f);
            Assert.AreEqual(0.62f, day.g, 0.001f);
            Color dark = GroundMist.Tint(WeatherKind.Fog, 1f);
            Assert.AreEqual(0.05f, dark.r, 0.001f);
            Assert.AreEqual(0.12f, dark.b, 0.001f);
            Assert.AreEqual(0.62f, WeatherSurface.Sight(WeatherKind.Fog), 0.001f);
            Assert.AreEqual(0.8f, WeatherSurface.Sight(WeatherKind.Rain), 0.001f);
            Assert.AreEqual(0.65f, WeatherSurface.Wetness(WeatherKind.Rain), 0.001f);
            Assert.IsTrue(AshFall.Falls("ash_market"));
            Assert.IsFalse(AshFall.Falls("rail_yard"));
            Assert.IsFalse(AshFall.Falls(null));
            Assert.IsFalse(AshFall.Falls(""));
            Assert.AreEqual(WeatherKind.Clear, DistrictRules.For("ash_market").Weather);
            Assert.AreEqual("rain", AshFall.Bed(WeatherKind.Rain, "ash_market"));
            Assert.AreEqual("wind", AshFall.Bed(WeatherKind.Fog, "ash_market"));
            Assert.AreEqual("ash", AshFall.Bed(WeatherKind.Clear, "ash_market"));
            Assert.AreEqual("", AshFall.Bed(WeatherKind.Clear, "rail_yard"));
            Assert.AreEqual(MixBus.Ambience, AudioMix.BusOf("ash"));
            Assert.AreEqual(0f, AudioSpace.SpatialBlend("ash"), 0.001f);
            Assert.AreEqual(36, AshFall.Flakes);
        }

        [Test]
        public void AnEvenRainDayBecomesAStorm()
        {
            Assert.AreEqual(WeatherKind.Rain, SkyBand.Cast(WeatherKind.Rain, 1));
            Assert.AreEqual(WeatherKind.Storm, SkyBand.Cast(WeatherKind.Rain, 2));
            Assert.AreEqual(WeatherKind.Rain, SkyBand.Cast(WeatherKind.Rain, 0));
            Assert.AreEqual(WeatherKind.Fog, SkyBand.Cast(WeatherKind.Fog, 2));
            Assert.AreEqual(WeatherKind.Clear, SkyBand.Cast(WeatherKind.Clear, 4));
            Assert.AreEqual(WeatherKind.Clear, DistrictRules.For("ash_market").Weather);
            Assert.AreEqual(WeatherKind.Rain, DistrictRules.For("rail_yard").Weather);
            Assert.AreEqual(0.8f, WeatherSurface.Sight(WeatherKind.Rain), 0.001f);
            Assert.AreEqual(0.62f, WeatherSurface.Sight(WeatherKind.Fog), 0.001f);
            Assert.AreEqual(1f, WeatherSurface.Sight(WeatherKind.Clear), 0.001f);
            Assert.AreEqual(0.7f, WeatherSurface.Sight(WeatherKind.Storm), 0.001f);
            Assert.AreEqual(0.9f, WeatherSurface.Sight(WeatherKind.Overcast), 0.001f);
            Assert.AreEqual(0.65f, WeatherSurface.Wetness(WeatherKind.Rain), 0.001f);
            Assert.AreEqual(0.2f, WeatherSurface.Wetness(WeatherKind.Fog), 0.001f);
            Assert.AreEqual(0f, WeatherSurface.Wetness(WeatherKind.Clear), 0.001f);
            Assert.AreEqual(0.85f, WeatherSurface.Wetness(WeatherKind.Storm), 0.001f);
            Assert.AreEqual(0.1f, WeatherSurface.Wetness(WeatherKind.Overcast), 0.001f);
            Assert.IsTrue(SkyBand.Rains(WeatherKind.Rain));
            Assert.IsTrue(SkyBand.Rains(WeatherKind.Storm));
            Assert.IsFalse(SkyBand.Rains(WeatherKind.Fog));
            Assert.IsFalse(FlashCap.Due(false, 0f, 20f));
            Assert.IsFalse(SkyBand.BoltDue(WeatherKind.Rain, 0f, 20f));
            Assert.IsFalse(SkyBand.BoltDue(WeatherKind.Storm, 0f, 4.4f));
            Assert.IsTrue(SkyBand.BoltDue(WeatherKind.Storm, 0f, 4.5f));
            Assert.IsTrue(SkyBand.BoltDue(WeatherKind.Storm, 4.5f, 9f));
            Assert.IsFalse(SkyBand.BoltDue(WeatherKind.Storm, 4.5f, 8.9f));
            Assert.AreEqual(0.022f, GroundMist.Sheet(WeatherKind.Fog), 0.0001f);
            Assert.AreEqual(0.012f, GroundMist.Sheet(WeatherKind.Rain), 0.0001f);
            Assert.AreEqual(0.02f, GroundMist.Sheet(WeatherKind.Storm), 0.0001f);
            Assert.AreEqual(0.65f, GroundMist.Wind(WeatherKind.Rain), 0.001f);
            Assert.AreEqual(0.9f, GroundMist.Wind(WeatherKind.Storm), 0.001f);
            Assert.AreEqual("rain", AshFall.Bed(WeatherKind.Rain, "ash_market"));
            Assert.AreEqual("storm", AshFall.Bed(WeatherKind.Storm, "rail_yard"));
            Assert.AreEqual("wind", AshFall.Bed(WeatherKind.Fog, "ash_market"));
            Assert.AreEqual("wind", AshFall.Bed(WeatherKind.Overcast, "old_hospital"));
            Assert.IsTrue(ClipBook.Has("storm"));
            Assert.AreNotEqual(ClipBook.Mark("storm"), ClipBook.Mark("rain"));
            Assert.AreEqual(MixBus.Ambience, AudioMix.BusOf("storm"));
        }

        [Test]
        public void EveryPlayedSoundHasATone()
        {
            Assert.IsFalse(ClipBook.Has(null));
            Assert.IsFalse(ClipBook.Has(""));
            Assert.IsFalse(ClipBook.Has("nope"));
            for (int i = 0; i < ClipBook.Ids.Length; i++)
                Assert.IsTrue(ClipBook.Has(ClipBook.Ids[i]), ClipBook.Ids[i]);
            Assert.AreEqual("gun", ClipBook.Fire(WeaponType.Pistol));
            Assert.AreEqual("shotgun", ClipBook.Fire(WeaponType.Shotgun));
            Assert.AreEqual("rifle", ClipBook.Fire(WeaponType.Rifle));
            Assert.AreEqual("smg", ClipBook.Fire(WeaponType.SMG));
            Assert.AreEqual("swing", ClipBook.Fire(WeaponType.Melee));
            Assert.AreNotEqual(ClipBook.Mark("gun"), ClipBook.Mark("shotgun"));
            Assert.AreNotEqual(ClipBook.Mark("gun"), ClipBook.Mark("rifle"));
            Assert.AreNotEqual(ClipBook.Mark("rifle"), ClipBook.Mark("smg"));
            Assert.AreNotEqual(ClipBook.Mark("shotgun"), ClipBook.Mark("smg"));
            Assert.AreNotEqual(ClipBook.Mark("boom"), ClipBook.Mark("kill"));
            Assert.AreNotEqual(ClipBook.Mark("step"), ClipBook.Mark("step_hard"));
            Assert.AreNotEqual(ClipBook.Mark("step_metal"), ClipBook.Mark("step_wood"));
            Assert.AreNotEqual(ClipBook.Mark("step_water"), ClipBook.Mark("step_gravel"));
            Assert.AreNotEqual(ClipBook.Mark("step"), ClipBook.Mark("step_gravel"));
            Assert.AreNotEqual(ClipBook.Mark("groan"), ClipBook.Mark("shriek"));
            Assert.AreNotEqual(ClipBook.Mark("shriek"), ClipBook.Mark("roar"));
            Assert.AreNotEqual(ClipBook.Mark("roar"), ClipBook.Mark("groan"));
            Assert.AreEqual(32f, AudioSpace.MaxDistance("rifle"), 0.001f);
            Assert.AreEqual(32f, AudioSpace.MaxDistance("smg"), 0.001f);
            Assert.AreEqual(1f, ClipBook.Mark("nope"), 0.001f);
            Assert.AreNotEqual(ClipBook.Mark("gun"), ClipBook.Mark("nope"));
        }

        [Test]
        public void CaravansVisitOnACalendarAndPricesFollowStanding()
        {
            Assert.IsTrue(CaravanBook.Visits(3));
            Assert.IsTrue(CaravanBook.Visits(7));
            Assert.IsTrue(CaravanBook.Visits(12));
            Assert.IsFalse(CaravanBook.Visits(1));
            Assert.IsFalse(CaravanBook.Visits(4));
            Assert.AreEqual("caravan", CaravanBook.Visitor(3));
            Assert.AreEqual("militia", CaravanBook.Visitor(7));
            Assert.AreEqual("clinic", CaravanBook.Visitor(12));
            Assert.AreEqual("farmers", CaravanBook.Visitor(15));

            Assert.AreEqual(14, CaravanBook.Price("medkit", 0, false));
            Assert.AreEqual(10, CaravanBook.Price("medkit", 100, false));
            Assert.AreEqual(18, CaravanBook.Price("medkit", -100, false));
            Assert.AreEqual(13, CaravanBook.Price("medkit", 0, true));
            Assert.IsTrue(CaravanBook.Refuses("militia", -21));
            Assert.IsFalse(CaravanBook.Refuses("militia", -20));
            Assert.IsFalse(CaravanBook.Refuses("caravan", -100));
            Assert.IsFalse(CaravanBook.Ambush(-40));
            Assert.IsTrue(CaravanBook.Ambush(-41));

            var standing = new[] { 5, -3, 0, 1 };
            CaravanBook.Decay(standing);
            Assert.AreEqual(4, standing[0]);
            Assert.AreEqual(-2, standing[1]);
            Assert.AreEqual(0, standing[2]);
            Assert.AreEqual(0, standing[3]);

            var legacy = new int[4];
            CaravanBook.Unpack("", 10, legacy);
            Assert.AreEqual(10, legacy[0]);
            Assert.AreEqual(0, legacy[1]);
            CaravanBook.Unpack(null, 10, legacy);
            Assert.AreEqual(10, legacy[0]);
            Assert.AreEqual(0, legacy[3]);

            string packed = CaravanBook.Pack(new[] { 12, -8, 4, 1 });
            var round = new int[4];
            CaravanBook.Unpack(packed, 0, round);
            Assert.AreEqual(12, round[0]);
            Assert.AreEqual(-8, round[1]);
            Assert.AreEqual(4, round[2]);
            Assert.AreEqual(1, round[3]);
            Assert.AreEqual("caravan", CaravanBook.Counterparty(3, false));
            Assert.AreEqual("caravan", CaravanBook.Counterparty(1, true));
            Assert.AreEqual("", CaravanBook.Counterparty(1, false));

            string quests = CaravanBook.MarkQuest("", "clinic");
            Assert.IsTrue(CaravanBook.QuestDone(quests, "clinic"));
            Assert.IsFalse(CaravanBook.QuestDone(quests, "farmers"));
            Assert.AreEqual(20, GridBuilder.Cost(ModuleKind.TradingPost));

            var saved = new SaveGameData { factionStanding = 12, factions = packed, quests = quests };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(saved), out var loaded, out var error), error);
            Assert.AreEqual(1, loaded.schemaVersion);
            Assert.AreEqual(packed, loaded.factions);
            Assert.AreEqual(quests, loaded.quests);
            Assert.AreEqual(12, loaded.factionStanding);
        }

        [Test]
        public void DressingStaysOffTheSpawnAndReadsAsAPlace()
        {
            var market = DressingPlan.Debris("ash_market");
            var again = DressingPlan.Debris("ash_market");
            var yard = DressingPlan.Debris("rail_yard");
            Assert.GreaterOrEqual(market.Length, 12);
            Assert.AreEqual(market.Length, again.Length);
            Assert.AreEqual(market[0].X, again[0].X);
            Assert.AreEqual(market[0].Z, again[0].Z);
            Assert.AreNotEqual(market[0].X, yard[0].X);
            Assert.IsTrue(DressingPlan.Spaced(market, 0.85f));
            for (int i = 0; i < market.Length; i++)
            {
                Assert.IsTrue(DressingPlan.OnTheStreet(market[i].X, market[i].Z));
            }

            var patches = DressingPlan.Patches("old_hospital");
            Assert.GreaterOrEqual(patches.Length, 3);
            for (int i = 0; i < patches.Length; i++) Assert.IsTrue(DressingPlan.OnTheStreet(patches[i].X, patches[i].Z));

            var home = DressingPlan.Home();
            Assert.GreaterOrEqual(home.Length, 8);
            for (int i = 0; i < home.Length; i++) Assert.IsTrue(DressingPlan.ClearsHome(home[i].X, home[i].Z));

            var horizon = DressingPlan.Horizon();
            int poles = 0;
            bool gap = true;
            for (int i = 0; i < horizon.Length; i++)
            {
                if (horizon[i].Role == "skyline" || horizon[i].Role == "tower") Assert.Greater(horizon[i].Z, 22f);
                if (horizon[i].Role == "pole") poles++;
                if (horizon[i].Role == "overpass" && System.Math.Abs(horizon[i].X) < 3f) gap = false;
            }
            Assert.AreEqual(2, poles);
            Assert.IsTrue(gap);

            var paint = LanePaint.Marks("ash_market");
            var painted = LanePaint.Marks("ash_market");
            var slid = LanePaint.Marks("rail_yard");
            Assert.AreEqual(6, paint.Length);
            Assert.AreEqual(0f, LanePaint.Shift("ash_market"), 0.001f);
            Assert.AreEqual(0f, LanePaint.Shift(""), 0.001f);
            Assert.Greater(LanePaint.Shift("rail_yard"), 0f);
            Assert.AreEqual("stripe", paint[0].Role);
            Assert.AreEqual("stripe", paint[3].Role);
            Assert.AreEqual("manhole", paint[4].Role);
            Assert.AreEqual("grate", paint[5].Role);
            Assert.AreEqual(8f, paint[0].Z, 0.001f);
            Assert.AreEqual(-1.2f, paint[0].X, 0.001f);
            Assert.AreEqual(1.2f, paint[3].X, 0.001f);
            Assert.AreEqual(paint[0].Z, painted[0].Z, 0.001f);
            Assert.AreNotEqual(paint[0].Z, slid[0].Z);
            for (int i = 0; i < paint.Length; i++) Assert.IsTrue(DressingPlan.OnTheStreet(paint[i].X, paint[i].Z));
            for (int i = 0; i < slid.Length; i++) Assert.IsTrue(DressingPlan.OnTheStreet(slid[i].X, slid[i].Z));
            Assert.AreEqual("step_metal", AudioMix.StepId("Dress_manhole"));
            Assert.AreEqual("step_metal", AudioMix.StepId("Dress_grate"));
        }

        [Test]
        public void ASharedMealLiftsBothSidesOfTheBook()
        {
            Assert.IsFalse(GiftBond.Can("ada", "ada", true, true, 1));
            Assert.IsFalse(GiftBond.Can("ada", "ellis", true, true, 0));
            Assert.IsFalse(GiftBond.Can("ada", "ellis", false, true, 1));
            Assert.IsFalse(GiftBond.Can("", "ellis", true, true, 1));
            Assert.IsTrue(GiftBond.Can("ada", "ellis", true, true, 1));
            Assert.AreEqual(1, GiftBond.Cost);
            Assert.AreEqual(8, GiftBond.Lift);
            Assert.AreEqual(26, GiftBond.Score(18));
            Assert.AreEqual(100, GiftBond.Score(96));
            Assert.AreEqual(100, GiftBond.Score(100));
            Assert.AreEqual(-32, GiftBond.Score(-40));
            Assert.AreEqual(-92, GiftBond.Score(-100));
            Assert.AreEqual("ellis:8", GiftBond.Give("", "ellis"));
            Assert.AreEqual("ellis:100", GiftBond.Give("ellis:96", "ellis"));
            Assert.AreEqual("ellis:-32", GiftBond.Give("ellis:-40", "ellis"));
            Assert.AreEqual("Regalar comida", Loc.T("camp.gift", "es"));
            Assert.AreEqual("No hay comida para regalar", Loc.T("camp.gift_none", "es"));
            Assert.AreEqual("Comida compartida", Loc.T("camp.gift_ok", "es"));
        }

        [Test]
        public void AnAimingRailThrowsAShortBeamAndLiftsExposure()
        {
            Assert.IsFalse(RailLamp.Lit(false, true, true));
            Assert.IsFalse(RailLamp.Lit(true, false, true));
            Assert.IsFalse(RailLamp.Lit(true, true, false));
            Assert.IsTrue(RailLamp.Lit(true, true, true));
            Assert.AreEqual(0.57f, RailLamp.Exposure(0.22f, true, false), 0.001f);
            Assert.AreEqual(0.85f, RailLamp.Exposure(0.8f, true, false), 0.001f);
            Assert.AreEqual(1f, RailLamp.Exposure(1f, true, true), 0.001f);
            float dark = SpotRange.Exposure(true, false, false, 1f, 0f);
            Assert.AreEqual(dark, RailLamp.Exposure(dark, false, false), 0.001f);
            Assert.AreEqual(1f, SpotRange.Exposure(true, false, true, 1f, 0f), 0.001f);
            Assert.IsTrue(RailLamp.Beam(8f, 12f, true));
            Assert.IsFalse(RailLamp.Beam(8.01f, 0f, true));
            Assert.IsFalse(RailLamp.Beam(5f, 12.1f, true));
            Assert.IsFalse(RailLamp.Beam(5f, 0f, false));
            Assert.IsTrue(SpotRange.Beam(5f, 10f, true));
            Assert.IsFalse(SpotRange.Beam(5f, 10f, false));
            var rail = WeaponMod.ProfileFor("rail");
            Assert.AreEqual(1f, rail.noise, 0.001f);
            Assert.AreEqual(1f, rail.spread, 0.001f);
            Assert.AreEqual(0, rail.magazineBonus);
            Assert.AreEqual(0.4f, WeaponMod.Combine(WeaponMod.PackFlags(true, true, false)).noise, 0.001f);
            Assert.AreEqual("rail", WeaponMod.PackFlags(false, false, false, true));
            Assert.IsTrue(WeaponMod.RailOn("suppressor+rail"));
            Assert.IsFalse(WeaponMod.RailOn("suppressor"));
            Assert.AreEqual(0.4f, WeaponMod.Combine("suppressor+rail").noise, 0.001f);
            Assert.IsTrue(CraftBill.TryOf("rail", out var bill));
            Assert.AreEqual(5, bill.Scrap);
            Assert.AreEqual(CraftBill.Workbench, bill.Station);
            Assert.AreEqual("Riel de linterna", Loc.T("recipe.rail", "es"));
        }

        [Test]
        public void TheFlashlightCookieKeepsABrightCenterAndADirtRing()
        {
            Assert.AreEqual(256, LampCookie.Size);
            Assert.AreEqual(40f, LampCookie.Inner, 0.001f);
            Assert.AreEqual(62f, LampCookie.Outer, 0.001f);
            Assert.AreEqual(1f, LampCookie.Shade(0.5f, 0.5f), 0.001f);
            Assert.AreEqual(0f, LampCookie.Shade(0f, 0f), 0.001f);
            Assert.AreEqual(0f, LampCookie.Shade(1f, 1f), 0.001f);
            float ring = LampCookie.Shade(0.5f + LampCookie.Ring * 0.5f, 0.5f);
            float inside = LampCookie.Shade(0.5f + 0.30f, 0.5f);
            Assert.AreEqual(0.182f, ring, 0.001f);
            Assert.AreEqual(0.4f, inside, 0.001f);
            Assert.Less(ring, inside);
            Assert.AreEqual(LampCookie.Shade(0.2f, 0.5f), LampCookie.Shade(0.8f, 0.5f), 0.001f);
            Assert.AreEqual(LampCookie.Shade(0.5f, 0.2f), LampCookie.Shade(0.5f, 0.8f), 0.001f);
        }

        [Test]
        public void CharactersKeepDistinctEyesAndAGoreLine()
        {
            var walker = CharacterLook.Eye("walker");
            var runner = CharacterLook.Eye("Zombie_Runner");
            var brute = CharacterLook.Eye("brute");
            Assert.Greater(walker.G, walker.R);
            Assert.Greater(walker.R, walker.B);
            Assert.Greater(runner.R, runner.G);
            Assert.Greater(brute.R, brute.G);
            Assert.Greater(brute.G, brute.B);
            Assert.AreEqual(2.5f, CharacterLook.Strength("walker"));
            Assert.Less(CharacterLook.Strength("brute"), CharacterLook.Strength("walker"));
            Assert.IsTrue(CharacterLook.Glows("walker"));
            Assert.IsFalse(CharacterLook.Glows("survivor"));
            Assert.AreEqual("walker", CharacterLook.RoleOf("Zombie_Walker(Clone)"));
            var first = CharacterLook.Clothing(1);
            var second = CharacterLook.Clothing(2);
            Assert.AreNotEqual(first.R, second.R);
            Assert.Greater(first.R, 0.4f);
            Assert.Less(first.R, 2f);
            Assert.IsTrue(CharacterLook.Wounded(39f, 100f));
            Assert.IsFalse(CharacterLook.Wounded(40f, 100f));
            Assert.IsFalse(CharacterLook.Wounded(0f, 0f));
            Assert.Greater(CharacterLook.EyeHeight("brute"), CharacterLook.EyeHeight("runner"));
        }

        [Test]
        public void QualityTiersCapTheHordeAndStaggerSight()
        {
            var low = QualityProfile.For(0);
            var medium = QualityProfile.For(1);
            var high = QualityProfile.For(2);
            var ultra = QualityProfile.For(4);
            Assert.AreEqual("Low", low.Name);
            Assert.AreEqual(16, low.Zombies);
            Assert.IsFalse(low.Ssao);
            Assert.IsFalse(low.DepthOfField);
            Assert.Greater(low.FrameMs, 30f);
            Assert.AreEqual(32, medium.Zombies);
            Assert.IsTrue(medium.Ssao);
            Assert.IsFalse(medium.DepthOfField);
            Assert.IsTrue(high.DepthOfField);
            Assert.AreEqual("Ultra", ultra.Name);
            Assert.AreEqual(40, ultra.Zombies);
            Assert.Less(low.ShadowDistance, medium.ShadowDistance);
            Assert.Less(medium.Decals, high.Decals);

            int due = 0;
            for (int token = 0; token < 32; token++)
            {
                if (QualityProfile.SightDue(token, 0)) due++;
            }
            Assert.AreEqual(QualityProfile.SightPerFrame, due);
            Assert.IsTrue(QualityProfile.SightDue(1, 1));
            Assert.IsFalse(QualityProfile.SightDue(1, 0));

            Assert.AreEqual(0, QualityProfile.Lod(10f));
            Assert.AreEqual(1, QualityProfile.Lod(40f));
            Assert.AreEqual(-1, QualityProfile.Lod(60f));
            Assert.IsTrue(QualityProfile.Culls("KitBlock", 70f));
            Assert.IsTrue(QualityProfile.Culls("Dress_rubble", 70f));
            Assert.IsFalse(QualityProfile.Culls("Dress_rubble", 10f));
            Assert.IsFalse(QualityProfile.Culls("Dress_skyline", 90f));
            Assert.IsFalse(QualityProfile.Culls("Player", 90f));
        }

        [Test]
        public void AudioBusesDuckWhenPausedPoisonedOrDead()
        {
            Assert.AreEqual(MixBus.Music, AudioMix.BusOf("ambient"));
            Assert.AreEqual(MixBus.Music, AudioMix.BusOf("pulse"));
            Assert.AreEqual(MixBus.Ui, AudioMix.BusOf("ui"));
            Assert.AreEqual(MixBus.Ambience, AudioMix.BusOf("rain"));
            Assert.AreEqual(MixBus.Ambience, AudioMix.BusOf("wind"));
            Assert.AreEqual(MixBus.Sfx, AudioMix.BusOf("gun"));
            Assert.AreEqual(MixBus.Sfx, AudioMix.BusOf("step_metal"));

            Assert.AreEqual(0.8f, AudioMix.Gain("gun", 0.8f, 1f, 0.7f, 1f, 0.8f, 1f, MixSnapshot.Normal), 0.001f);
            Assert.AreEqual(0.4f, AudioMix.Gain("gun", 0.8f, 1f, 0.7f, 0.5f, 0.8f, 1f, MixSnapshot.Normal), 0.001f);
            Assert.AreEqual(0.4f, AudioMix.Gain("gun", 0.8f, 0.5f, 0.7f, 1f, 0.8f, 1f, MixSnapshot.Normal), 0.001f);
            Assert.AreEqual(0f, AudioMix.Gain("gun", 0.8f, 0f, 1f, 1f, 1f, 1f, MixSnapshot.Normal), 0.001f);
            Assert.AreEqual(0.07f, AudioMix.Gain("ambient", 0.1f, 1f, 0.7f, 0f, 1f, 1f, MixSnapshot.Normal), 0.001f);
            Assert.AreEqual(0.0245f, AudioMix.Gain("ambient", 0.1f, 1f, 0.7f, 1f, 1f, 1f, MixSnapshot.Paused), 0.001f);
            Assert.AreEqual(0.36f, AudioMix.Gain("gun", 0.8f, 1f, 1f, 1f, 1f, 1f, MixSnapshot.Paused), 0.001f);
            Assert.AreEqual(0.5f, AudioMix.Gain("ui", 0.5f, 1f, 0.2f, 0.2f, 0.2f, 1f, MixSnapshot.Paused), 0.001f);
            Assert.AreEqual(0.12f, AudioMix.Gain("gun", 0.8f, 1f, 1f, 1f, 1f, 1f, MixSnapshot.Death), 0.001f);
            Assert.AreEqual(0.4025f, AudioMix.Gain("rain", 0.35f, 1f, 1f, 1f, 1f, 1f, MixSnapshot.Toxic), 0.001f);
            Assert.AreEqual(1f, AudioMix.Gain("rain", 1f, 1f, 1f, 1f, 1f, 1f, MixSnapshot.Toxic), 0.001f);

            Assert.AreEqual(AudioMix.OpenHz, AudioMix.LowpassHz(MixSnapshot.Normal));
            Assert.AreEqual(AudioMix.PausedHz, AudioMix.LowpassHz(MixSnapshot.Paused));
            Assert.AreEqual(AudioMix.ToxicHz, AudioMix.LowpassHz(MixSnapshot.Toxic));
            Assert.AreEqual(AudioMix.DeathHz, AudioMix.LowpassHz(MixSnapshot.Death));

            Assert.AreEqual(MixSnapshot.Death, AudioMix.SnapshotFor(GameState.GameOver, false));
            Assert.AreEqual(MixSnapshot.Paused, AudioMix.SnapshotFor(GameState.Paused, true));
            Assert.AreEqual(MixSnapshot.Paused, AudioMix.SnapshotFor(GameState.MainMenu, false));
            Assert.AreEqual(MixSnapshot.Paused, AudioMix.SnapshotFor(GameState.Victory, false));
            Assert.AreEqual(MixSnapshot.Paused, AudioMix.SnapshotFor(GameState.SuccessionScreen, false));
            Assert.AreEqual(MixSnapshot.Toxic, AudioMix.SnapshotFor(GameState.ExpeditionActive, true));
            Assert.AreEqual(MixSnapshot.Normal, AudioMix.SnapshotFor(GameState.ExpeditionActive, false));
            Assert.AreEqual(MixSnapshot.Normal, AudioMix.SnapshotFor(GameState.RaidActive, false));

            Assert.AreEqual("step_metal", AudioMix.StepId("Kit_manhole"));
            Assert.AreEqual("step_wood", AudioMix.StepId("wall_boarded"));
            Assert.AreEqual("step_water", AudioMix.StepId("puddle"));
            Assert.AreEqual("step_hard", AudioMix.StepId("Road_Straight"));
            Assert.AreEqual("step_hard", AudioMix.StepId("sidewalk_corner"));
            Assert.AreEqual("step", AudioMix.StepId("Ground"));
            Assert.AreEqual("step", AudioMix.StepId(null));
            Assert.AreEqual("step_gravel", AudioMix.StepId("gravel_lot"));
            Assert.AreEqual("step_gravel", AudioMix.StepId("rubble"));
            Assert.AreEqual("step_gravel", AudioMix.StepId("dirt_path"));
            Assert.AreEqual("step_hard", AudioMix.StepId("Road_Straight"));
            Assert.AreEqual(0.35f, AudioSpace.SpatialBlend("step_gravel"), 0.001f);
        }

        [Test]
        public void RaidsComeFromASideAndTowersSlowThem()
        {
            Assert.IsFalse(RaidPlan.Due(1, 0));
            Assert.IsTrue(RaidPlan.Due(2, 0));
            Assert.IsFalse(RaidPlan.Due(2, 8));
            Assert.IsFalse(RaidPlan.Due(3, 0));
            Assert.IsTrue(RaidPlan.Due(4, 7));

            var first = RaidPlan.Opening(1, 0);
            Assert.AreEqual("gate", first.Approach);
            Assert.AreEqual(6, first.Pressure);
            Assert.AreEqual(1.2f, first.Interval, 0.001f);
            Assert.AreEqual("alley", RaidPlan.Opening(2, 0).Approach);
            Assert.AreEqual("yard", RaidPlan.Opening(3, 0).Approach);
            Assert.AreEqual("fence", RaidPlan.Opening(4, 0).Approach);
            Assert.AreEqual("gate", RaidPlan.Opening(5, 0).Approach);

            var towered = RaidPlan.Opening(1, 1);
            Assert.AreEqual("alley", towered.Approach);
            Assert.AreEqual(4, towered.Pressure);
            Assert.AreEqual(1.6f, towered.Interval, 0.001f);
            Assert.AreEqual(2, RaidPlan.Opening(1, 4).Pressure);
            Assert.AreEqual(2.4f, RaidPlan.Opening(1, 4).Interval, 0.001f);
            Assert.AreEqual(8, RaidPlan.Opening(4, 0).Pressure);

            Assert.AreEqual(8, RaidPlan.SpawnCount(1, 0));
            Assert.AreEqual(3, RaidPlan.SpawnCount(1, 2));
            Assert.AreEqual(11, RaidPlan.SpawnCount(9, 0));
            Assert.AreEqual(16, RaidPlan.SpawnCount(30, 0));

            Assert.AreEqual(6, RaidPlan.Strike(6, 0, 0));
            Assert.AreEqual(2, RaidPlan.Strike(9, 2, 1));
            Assert.AreEqual(1, RaidPlan.Strike(6, 3, 0));

            RaidPlan.AnchorOf("gate", out float gx, out float gz);
            Assert.AreEqual(-6f, gx);
            Assert.AreEqual(-8f, gz);
            Assert.IsTrue(RaidPlan.Covers(gx, gz, -6f, -8f));
            Assert.IsFalse(RaidPlan.Covers(gx, gz, -12f, -12f));
            RaidPlan.AnchorOf("alley", out float ax, out float az);
            Assert.AreEqual(-20f, ax);
            Assert.AreEqual(-12f, az);
            RaidPlan.AnchorOf("mystery", out float ux, out float uz);
            Assert.AreEqual(-6f, ux);
            Assert.AreEqual(-8f, uz);
        }

        [Test]
        public void AFarScreamStaysOffTheSubtitleLine()
        {
            Assert.IsTrue(CaptionGate.Show(0f, 10f, false, NoiseType.ZombieScream, false));
            Assert.IsFalse(CaptionGate.Show(11f, 10f, false, NoiseType.ZombieScream, false));
            Assert.IsFalse(CaptionGate.Show(0f, 10f, true, NoiseType.ZombieScream, false));
            Assert.IsTrue(CaptionGate.Show(0f, 10f, true, NoiseType.GunshotLoud, false));
            Assert.IsFalse(CaptionGate.Show(9.5f, 10f, false, NoiseType.ZombieScream, false));
            Assert.IsTrue(CaptionGate.Show(8f, 10f, false, NoiseType.ZombieScream, false));
            Assert.IsTrue(CaptionGate.Show(9.45f, 10f, false, NoiseType.ZombieScream, false));
            Assert.IsFalse(CaptionGate.Show(9.45f, 10f, false, NoiseType.ZombieScream, true));
            Assert.IsTrue(CaptionGate.Show(0f, 40f, false, NoiseType.Thunder, false));
            Assert.IsFalse(CaptionGate.Show(41f, 40f, false, NoiseType.Thunder, false));
            Assert.IsFalse(CaptionGate.Show(0f, 0f, false, NoiseType.Thunder, false));
            Assert.AreEqual(0f, HearGate.Perceived(0f, 40f, 1f, false, NoiseType.Thunder), 0.001f);
        }

        [Test]
        public void TheYardWallKeepsTheStreetOnThePlane()
        {
            Assert.AreEqual(33.4f, MapRim.Open, 0.001f);
            Assert.AreEqual(3.2f, MapRim.Height, 0.001f);
            Assert.IsTrue(MapRim.Inside(0f, 0f));
            Assert.IsTrue(MapRim.Inside(20f, 16f));
            Assert.IsTrue(MapRim.Inside(-18f, -16f));
            Assert.IsTrue(MapRim.Inside(33.4f, 0f));
            Assert.IsTrue(MapRim.Inside(-33.4f, -33.4f));
            Assert.IsFalse(MapRim.Inside(33.5f, 0f));
            Assert.IsFalse(MapRim.Inside(-33.5f, 0f));
            Assert.IsFalse(MapRim.Inside(0f, 34f));
            Assert.IsFalse(MapRim.Inside(0f, -34f));
        }

        [Test]
        public void APartlyEmptiedCrateKeepsWhatIsLeft()
        {
            var stacks = new[]
            {
                new ContainerHold.Stack { Id = "scrap", Count = 4 },
                new ContainerHold.Stack { Id = "bandage", Count = 1 }
            };
            Assert.AreEqual("scrap*4;bandage*1", ContainerHold.Encode(stacks));
            var back = ContainerHold.Decode("scrap*4;bandage*1");
            Assert.AreEqual(2, back.Length);
            Assert.AreEqual("scrap", back[0].Id);
            Assert.AreEqual(4, back[0].Count);
            Assert.AreEqual("bandage", back[1].Id);
            Assert.AreEqual(1, back[1].Count);
            Assert.AreEqual(0, ContainerHold.Decode("").Length);
            Assert.AreEqual(0, ContainerHold.Decode(null).Length);
            string mark = StreetLedger.Mark("crate", 2.2f, 12f);
            Assert.AreEqual("crate@22,120", mark);
            string packed = StreetLedger.Hold(null, "ash_market", mark, "scrap*4;bandage*1");
            Assert.AreEqual("ash_market=crate@22,120~scrap*4;bandage*1", packed);
            Assert.IsFalse(StreetLedger.Has(packed, "ash_market", mark));
            Assert.AreEqual("scrap*4;bandage*1", StreetLedger.Read(packed, "ash_market", mark));
            Assert.AreEqual(1, StreetLedger.Count(packed, "ash_market"));
            Assert.IsNull(StreetLedger.Read(packed, "rail_yard", mark));
            packed = StreetLedger.Hold(packed, "ash_market", mark, "scrap*1");
            Assert.AreEqual("scrap*1", StreetLedger.Read(packed, "ash_market", mark));
            Assert.AreEqual(1, StreetLedger.Count(packed, "ash_market"));
            packed = StreetLedger.Note(packed, "ash_market", mark);
            Assert.AreEqual("", StreetLedger.Read(packed, "ash_market", mark));
            Assert.IsTrue(StreetLedger.Has(packed, "ash_market", mark));
            Assert.AreEqual(1, StreetLedger.Count(packed, "ash_market"));
            Assert.IsNull(StreetLedger.Read("", "ash_market", mark));
            Assert.IsNull(StreetLedger.Read(null, "ash_market", mark));
        }

        [Test]
        public void ABurstBarrelStaysGoneOnTheNextTrip()
        {
            string barrel = StreetLedger.Mark("barrel_explosive", 1.6f, 4f);
            string crate = StreetLedger.Mark("crate", 1.6f, 4f);
            Assert.AreEqual("barrel_explosive@16,40", barrel);
            Assert.AreNotEqual(barrel, crate);
            string packed = StreetLedger.Note("", "rail_yard", barrel);
            Assert.IsTrue(StreetLedger.Has(packed, "rail_yard", barrel));
            Assert.IsFalse(StreetLedger.Has(packed, "rail_yard", crate));
            Assert.IsFalse(StreetLedger.Has(packed, "ash_market", barrel));
            Assert.AreEqual(1, StreetLedger.Count(packed, "rail_yard"));
            packed = StreetLedger.Note(packed, "rail_yard", crate);
            Assert.AreEqual(2, StreetLedger.Count(packed, "rail_yard"));
            Assert.AreEqual(0, StreetLedger.Count(packed, "ash_market"));
        }

        [Test]
        public void AnEmptiedCrateStaysEmptyOnTheSameStreet()
        {
            Assert.AreEqual("crate@16,40", StreetLedger.Mark("crate", 1.6f, 4f));
            Assert.AreEqual("poi@-32,60", StreetLedger.Mark("poi", -3.2f, 6f));
            Assert.AreEqual("", StreetLedger.Note(null, "", "road"));
            Assert.AreEqual("", StreetLedger.Note("", "ash_market", ""));
            string packed = StreetLedger.Note(null, "ash_market", "road@16,40");
            Assert.AreEqual("ash_market=road@16,40", packed);
            Assert.AreEqual(packed, StreetLedger.Note(packed, "ash_market", "road@16,40"));
            packed = StreetLedger.Note(packed, "rail_yard", "poi@0,0");
            Assert.AreEqual("ash_market=road@16,40|rail_yard=poi@0,0", packed);
            Assert.IsTrue(StreetLedger.Has(packed, "ash_market", "road@16,40"));
            Assert.IsTrue(StreetLedger.Has(packed, "rail_yard", "poi@0,0"));
            Assert.IsFalse(StreetLedger.Has(packed, "rail_yard", "road@16,40"));
            Assert.IsFalse(StreetLedger.Has(null, "ash_market", "road@16,40"));
            Assert.AreEqual(1, StreetLedger.Count(packed, "ash_market"));
            Assert.AreEqual(0, StreetLedger.Count(packed, "old_hospital"));
            Assert.AreEqual(0, StreetLedger.Count("", "ash_market"));
            var data = new SaveGameData { street = packed };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(data), out var loaded, out var error), error);
            Assert.AreEqual(1, loaded.schemaVersion);
            Assert.AreEqual(packed, loaded.street);
            Assert.IsTrue(SaveCodec.TryDeserialize("{\"schemaVersion\":1}", out var legacy, out var legacyError), legacyError);
            Assert.IsTrue(string.IsNullOrEmpty(legacy.street));
            Assert.AreEqual("searched", Loc.T("camp.searched"));
            Assert.AreEqual("registrado", Loc.T("camp.searched", "es"));
        }

        [Test]
        public void UnmappedRoadsStayOffTheBoardUntilANeighborIsCleared()
        {
            Assert.IsFalse(MapVeil.Seen("", new string[0]));
            Assert.IsFalse(MapVeil.Seen(null, new string[0]));
            Assert.IsFalse(MapVeil.Seen("nowhere", null));
            Assert.IsTrue(MapVeil.Seen("ash_market", new string[0]));
            Assert.IsTrue(MapVeil.Seen("rail_yard", null));
            Assert.IsTrue(MapVeil.Seen("commercial_strip", new string[0]));
            Assert.IsFalse(MapVeil.Seen("old_hospital", new string[0]));
            Assert.IsFalse(MapVeil.Seen("downtown_core", new string[0]));
            Assert.IsFalse(MapVeil.Seen("old_hospital", new[] { "ash_market" }));
            Assert.IsTrue(MapVeil.Seen("police_station", new[] { "rail_yard" }));
            Assert.IsFalse(MapVeil.Seen("north_gate", new[] { "rail_yard" }));
            Assert.IsTrue(MapVeil.Seen("north_gate", new[] { "water_plant" }));
            Assert.IsTrue(MapVeil.Seen("downtown_core", new[] { "mall" }));
            Assert.AreEqual(7, MapVeil.Hidden(null));
            Assert.AreEqual(7, MapVeil.Hidden(new string[0]));
            Assert.AreEqual(5, MapVeil.Hidden(new[] { "rail_yard" }));
            Assert.AreEqual("clear", MapVeil.Forecast("ash_market", new string[0], 1));
            Assert.AreEqual("rain", MapVeil.Forecast("rail_yard", null, 1));
            Assert.AreEqual("storm", MapVeil.Forecast("rail_yard", null, 2));
            Assert.AreEqual("", MapVeil.Forecast("old_hospital", new string[0], 3));
            Assert.AreEqual("fog", MapVeil.Forecast("old_hospital", new[] { "commercial_strip" }, 1));
            Assert.AreEqual("overcast", MapVeil.Forecast("old_hospital", new[] { "commercial_strip" }, 3));
            Assert.AreEqual("Unmapped roads", Loc.T("camp.fog"));
            Assert.AreEqual("Caminos sin mapa", Loc.T("camp.fog", "es"));
            Assert.AreEqual("Niebla", Loc.T("sky.fog", "es"));
            Assert.AreEqual("Tormenta", Loc.T("sky.storm", "es"));
            Assert.AreEqual("cache", MapVeil.Site("ash_market", new string[0]));
            Assert.AreEqual("cache", MapVeil.Site("rail_yard", null));
            Assert.AreEqual("", MapVeil.Site("old_hospital", new string[0]));
            Assert.AreEqual("", MapVeil.Site("downtown_core", null));
            Assert.AreEqual("radio", MapVeil.Site("old_hospital", new[] { "commercial_strip" }));
            Assert.AreEqual("radio", MapVeil.Site("police_station", new[] { "rail_yard" }));
            Assert.AreEqual("cache", MapVeil.Site("north_gate", new[] { "water_plant" }));
            Assert.AreEqual("", MapVeil.Site("nowhere", new[] { "ash_market" }));
            Assert.AreEqual(0.5f, FuelTank.TripRate, 0.001f);
            Assert.AreEqual(1f, FuelTank.TripCost(2f), 0.001f);
            Assert.AreEqual(4f, FuelTank.TripCost(8f), 0.001f);
            Assert.AreEqual(0f, FuelTank.TripCost(0f), 0.001f);
            Assert.AreEqual(0f, FuelTank.TripCost(-1f), 0.001f);
            Assert.AreEqual(9f, FuelTank.Trip(10f, 2f), 0.001f);
            Assert.AreEqual(6f, FuelTank.Trip(10f, 8f), 0.001f);
            Assert.AreEqual(0f, FuelTank.Trip(1f, 8f), 0.001f);
            Assert.AreEqual(10f, FuelTank.Trip(10f, 0f), 0.001f);
            Assert.AreEqual(0f, FuelTank.Trip(-2f, 2f), 0.001f);
            Assert.AreEqual("Fuel burned", Loc.T("camp.trip"));
            Assert.AreEqual("Combustible gastado", Loc.T("camp.trip", "es"));
            Assert.AreEqual("Pieza de radio", Loc.T("poi.radio", "es"));
            Assert.AreEqual("Alijo", Loc.T("poi.cache", "es"));
        }

        [Test]
        public void TenDistrictsOpenByRoadAndTheTowerEndsTheRun()
        {
            var board = CampaignBoard.All();
            Assert.AreEqual(10, board.Length);
            Assert.AreEqual("ash_market", board[0].Id);
            Assert.AreEqual("rail_yard", board[1].Id);
            Assert.AreEqual("old_hospital", board[2].Id);
            Assert.AreEqual("north_gate", board[3].Id);

            var seen = new HashSet<string>();
            for (int i = 0; i < board.Length; i++)
            {
                Assert.IsFalse(seen.Contains(board[i].Id));
                seen.Add(board[i].Id);
                var pieces = DistrictLayout.For(board[i].Id);
                Assert.Greater(pieces.Length, 0);
                for (int p = 0; p < pieces.Length; p++)
                {
                    Assert.IsTrue(DistrictLayout.StaysOnTheStreet(pieces[p]), board[i].Id);
                }
            }

            Assert.IsTrue(CampaignBoard.Reachable("ash_market", new string[0]));
            Assert.IsFalse(CampaignBoard.Reachable("rail_yard", new string[0]));
            Assert.IsTrue(CampaignBoard.Reachable("rail_yard", new[] { "ash_market" }));
            Assert.IsTrue(CampaignBoard.Reachable("commercial_strip", new[] { "ash_market" }));
            Assert.IsFalse(CampaignBoard.Reachable("downtown_core", new[] { "ash_market", "rail_yard", "old_hospital" }));
            Assert.IsFalse(CampaignBoard.Reachable("north_gate", new[] { "ash_market", "rail_yard", "old_hospital" }));
            Assert.IsTrue(CampaignBoard.Reachable("north_gate", new[] { "water_plant" }));
            Assert.IsTrue(CampaignBoard.Reachable("downtown_core", new[] { "mall" }));
            Assert.AreEqual(2f, CampaignBoard.TravelHours("ash_market"));
            Assert.AreEqual(8f, CampaignBoard.TravelHours("downtown_core"));

            Assert.AreEqual("hospital", CampaignBoard.PartFor("old_hospital"));
            Assert.AreEqual("police", CampaignBoard.PartFor("police_station"));
            Assert.AreEqual("downtown", CampaignBoard.PartFor("downtown_core"));
            Assert.AreEqual("", CampaignBoard.PartFor("ash_market"));
            string parts = CampaignBoard.AddPart("", "hospital");
            parts = CampaignBoard.AddPart(parts, "downtown");
            parts = CampaignBoard.AddPart(parts, "hospital");
            Assert.AreEqual("downtown,hospital", parts);
            Assert.IsFalse(CampaignBoard.PartsComplete(parts));
            parts = CampaignBoard.AddPart(parts, "police");
            Assert.AreEqual("downtown,hospital,police", parts);
            Assert.IsTrue(CampaignBoard.PartsComplete(parts));
            Assert.IsTrue(CampaignBoard.Ready(parts, true, false));
            Assert.IsFalse(CampaignBoard.Ready(parts, false, false));
            Assert.IsFalse(CampaignBoard.Won(parts, true, false));
            Assert.IsTrue(CampaignBoard.Won(parts, true, true));
            Assert.IsFalse(CampaignBoard.Won(parts, false, true));

            Assert.AreEqual("warehouse", KitPlan.RecipeName("water_plant"));
            Assert.AreEqual("hospital", KitPlan.RecipeName("police_station"));
            Assert.AreEqual("apartment", KitPlan.RecipeName("downtown_core"));
            Assert.AreEqual("storefront", KitPlan.RecipeName("commercial_strip"));
            var downtown = DistrictRules.For("downtown_core");
            Assert.AreEqual(18, downtown.KillGoal);
            Assert.Greater(downtown.OpeningTension, DistrictRules.For("north_gate").OpeningTension);

            Assert.AreEqual(2, DifficultyProfile.Resolve(0));
            Assert.AreEqual(1, DifficultyProfile.Resolve(1));
            Assert.AreEqual(3, DifficultyProfile.Resolve(9));
            Assert.AreEqual("Survivor", DifficultyProfile.Name(0));
            Assert.AreEqual("Nightmare", DifficultyProfile.Name(3));
            var easy = DifficultyProfile.For(1, 1, 2);
            Assert.AreEqual(0f, easy.Tension, 0.001f);
            Assert.AreEqual(1f, easy.Interval, 0.001f);
            Assert.AreEqual(0, easy.ExtraKills);
            Assert.AreEqual("", easy.Prefer);
            var mid = DifficultyProfile.For(2, 1, 2);
            Assert.AreEqual(4f, mid.Tension, 0.001f);
            Assert.AreEqual(1, mid.ExtraKills);
            Assert.AreEqual("Runner", mid.Prefer);
            var hard = DifficultyProfile.For(3, 1, 3);
            Assert.AreEqual(16.5f, hard.Tension, 0.001f);
            Assert.AreEqual(0.75f, hard.Interval, 0.001f);
            Assert.AreEqual(4, hard.ExtraKills);
            Assert.AreEqual("Brute", hard.Prefer);
            var scavenger = DifficultyProfile.For(4, 1, 1);
            Assert.AreEqual(6f, scavenger.Tension, 0.001f);
            Assert.AreEqual(1.15f, scavenger.Interval, 0.001f);
            Assert.AreEqual(0, scavenger.ExtraKills);
            Assert.AreEqual("", scavenger.Prefer);
            Assert.AreEqual(4, DifficultyProfile.Batch((int)TensionState.Peak, 2));
            Assert.AreEqual(6, DifficultyProfile.Batch((int)TensionState.Peak, 3));
            Assert.AreEqual(1, DifficultyProfile.Batch((int)TensionState.BuildUp, 1));
            Assert.AreEqual(0, DifficultyProfile.Batch((int)TensionState.Calm, 3));

            Assert.IsFalse(SpawnRing.Allowed(10f, 0f, 0f, 0f));
            Assert.IsTrue(SpawnRing.Allowed(20f, 0f, 0f, 0f));
            Assert.IsFalse(SpawnRing.Allowed(-10f, -10f, 0f, 0f));
            Assert.IsTrue(SpawnRing.Allowed(-20f, 5f, 0f, 0f));
            Assert.IsTrue(SpawnRing.InFront(0f, 0f, 0f, 1f, 0f, 10f));
            Assert.IsFalse(SpawnRing.InFront(0f, 0f, 0f, 1f, 0f, -10f));
            Assert.IsFalse(SpawnRing.InFront(0f, 0f, 0f, 0.1f, 0f, 10f));
        }

        [Test]
        public void FiveSlotsKeepTheNewestAndABrokenSealFallsBack()
        {
            Assert.AreEqual("slot_0.json", SaveSlots.FileName(0));
            Assert.AreEqual("slot_4.json", SaveSlots.FileName(4));
            Assert.AreEqual("slot_4.json", SaveSlots.FileName(9));
            Assert.AreEqual("slot_auto.json", SaveSlots.FileName(SaveSlots.AutoSlot));
            Assert.AreEqual(SaveSlots.LegacyFile, SaveSlots.FileName(SaveSlots.LegacySlot));
            Assert.AreEqual(0, SaveSlots.Manual(-1));
            Assert.AreEqual(4, SaveSlots.Manual(8));

            var cards = new[]
            {
                new SaveSlots.Card { Slot = 0, Day = 2, Hour = 8f, Occupied = true },
                new SaveSlots.Card { Slot = 1, Day = 4, Hour = 6f, Occupied = true },
                new SaveSlots.Card { Slot = SaveSlots.AutoSlot, Day = 4, Hour = 6f, Occupied = true, Auto = true },
                new SaveSlots.Card { Slot = SaveSlots.LegacySlot, Occupied = false }
            };
            Assert.AreEqual(2, SaveSlots.Newest(cards));
            cards[2].Day = 3;
            Assert.AreEqual(1, SaveSlots.Newest(cards));
            Assert.AreEqual(-1, SaveSlots.Newest(new[] { new SaveSlots.Card { Occupied = false } }));
            Assert.AreEqual(-1, SaveSlots.Newest(null));

            var data = new SaveGameData { day = 4, hour = 6.5f, slot = 2 };
            string json = SaveCodec.Serialize(data);
            Assert.IsTrue(SaveCodec.TryDeserialize(json, out var loaded, out var error), error);
            Assert.AreEqual(1, loaded.schemaVersion);
            Assert.AreEqual(4, loaded.day);
            Assert.AreEqual(2, loaded.slot);
            Assert.IsFalse(string.IsNullOrEmpty(loaded.seal));

            var tampered = JsonUtility.FromJson<SaveGameData>(json);
            tampered.day = 9;
            string broken = JsonUtility.ToJson(tampered, true);
            Assert.IsFalse(SaveCodec.TryDeserialize(broken, out _, out error));
            Assert.AreEqual("seal", error);

            var legacy = new SaveGameData { day = 2, schemaVersion = 1 };
            string old = JsonUtility.ToJson(legacy);
            Assert.IsTrue(SaveCodec.TryDeserialize(old, out var kept, out error), error);
            Assert.AreEqual(2, kept.day);
            Assert.AreEqual("", kept.seal);

            var future = new SaveGameData { schemaVersion = 2 };
            Assert.IsFalse(SaveCodec.TryDeserialize(JsonUtility.ToJson(future), out _, out error));
            Assert.AreEqual("schema", error);
        }

        [Test]
        public void GoreCaptionsAndBrightnessKeepTheOldDefaults()
        {
            Assert.IsTrue(CharacterLook.Wounded(39f, 100f));
            Assert.IsFalse(CharacterLook.Wounded(40f, 100f));
            Assert.IsFalse(CharacterLook.Wounded(10f, 100f, 0));
            Assert.IsTrue(CharacterLook.Wounded(60f, 100f, 2));
            Assert.IsFalse(CharacterLook.Wounded(70f, 100f, 2));
            Assert.IsFalse(CharacterLook.Wounded(60f, 100f, 1));

            Assert.AreEqual(1, Presentation.Gore(0));
            Assert.AreEqual("Standard", Presentation.GoreName(0));
            Assert.AreEqual("Heavy", Presentation.GoreName(2));
            Assert.AreEqual("Off", Presentation.GoreName(3));
            Assert.AreEqual(2, Presentation.NextGore(1));
            Assert.AreEqual(3, Presentation.NextGore(2));
            Assert.AreEqual(1, Presentation.NextGore(3));
            Assert.IsTrue(Presentation.HitStop(0));
            Assert.IsFalse(Presentation.HitStop(2));
            Assert.IsTrue(Presentation.DamageNumbers(1));
            Assert.AreEqual(1f, Presentation.Opacity(0f), 0.001f);
            Assert.AreEqual(0.45f, Presentation.Opacity(0.2f), 0.001f);
            Assert.AreEqual(1f, Presentation.Brightness(0f), 0.001f);
            Assert.AreEqual(0.15f, Presentation.Exposure(1f), 0.001f);
            Assert.AreEqual(0.47f, Presentation.Exposure(1.4f), 0.001f);
            Assert.IsFalse(Presentation.MotionBlur(0));
            Assert.IsTrue(Presentation.MotionBlur(1));

            Assert.AreEqual("north", Presentation.Compass(0f, 10f, "en"));
            Assert.AreEqual("south", Presentation.Compass(0f, -8f, "en"));
            Assert.AreEqual("east", Presentation.Compass(9f, 1f, "en"));
            Assert.AreEqual("west", Presentation.Compass(-9f, 0f, "en"));
            Assert.AreEqual("northeast", Presentation.Compass(6f, 6f, "en"));
            Assert.AreEqual("here", Presentation.Compass(0f, 0f, "en"));
            Assert.AreEqual("[Grito, norte]", Presentation.Caption(NoiseType.ZombieScream, 0f, 4f, "es"));
            Assert.AreEqual("[Gunshot, west]", Presentation.Caption(NoiseType.GunshotLoud, -5f, 0f, "en"));
            Assert.AreEqual("[Explosion, here]", Presentation.Caption(NoiseType.Explosion, 0f, 0f, "en"));
            Assert.AreEqual("", Presentation.Caption(NoiseType.WalkFootstep, 1f, 0f, "en"));
            Assert.AreEqual("", Presentation.Caption(NoiseType.SprintFootstep, 1f, 0f, "en"));
            Assert.AreEqual("", Presentation.Caption(NoiseType.SneakFootstep, 1f, 0f, "es"));
            Assert.AreEqual("[Something broke, east]", Presentation.Caption(NoiseType.ObjectBroken, 4f, 0f, "en"));
            Assert.AreEqual("[Rotura, este]", Presentation.Caption(NoiseType.ObjectBroken, 4f, 0f, "es"));
            Assert.AreEqual("[Blade, north]", Presentation.Caption(NoiseType.MeleeSwing, 0f, 4f, "en"));
            Assert.AreEqual("[Corte, norte]", Presentation.Caption(NoiseType.MeleeSwing, 0f, 4f, "es"));
        }

        [Test]
        public void ASeedRebuildsTheSameStreetAndTheHordeKeepsTime()
        {
            Assert.AreEqual(DistrictGenerator.DefaultSeed, DistrictGenerator.Resolve(0));
            var first = DistrictGenerator.Scatter(1701, "mall");
            var again = DistrictGenerator.Scatter(1701, "mall");
            Assert.GreaterOrEqual(first.Length, 6);
            Assert.AreEqual(first.Length, again.Length);
            Assert.AreEqual(first[0].X, again[0].X);
            Assert.AreEqual(first[0].Z, again[0].Z);
            Assert.AreEqual(first[0].Role, again[0].Role);
            var other = DistrictGenerator.Scatter(99991, "mall");
            Assert.IsTrue(first[0].X != other[0].X || first[0].Role != other[0].Role);

            var ids = CampaignBoard.All();
            for (int seed = 1; seed <= 100; seed++)
            {
                for (int d = 0; d < ids.Length; d++)
                {
                    var pieces = DistrictGenerator.Scatter(seed, ids[d].Id);
                    Assert.IsTrue(DistrictGenerator.Spaced(pieces, 1.05f), ids[d].Id + " " + seed);
                    for (int i = 0; i < pieces.Length; i++)
                    {
                        Assert.IsTrue(DistrictLayout.StaysOnTheStreet(pieces[i]), ids[d].Id);
                        Assert.IsTrue(DistrictGenerator.LaneClear(pieces[i]), ids[d].Id);
                    }
                }
            }

            float cursor = HordeSchedule.Advance(400f, 0f, 1, out string kind);
            Assert.AreEqual("alley", kind);
            Assert.AreEqual(8, HordeSchedule.Count(kind));
            Assert.AreEqual(45f, cursor);
            cursor = HordeSchedule.Advance(400f, cursor, 1, out kind);
            Assert.AreEqual("", kind);
            Assert.AreEqual(90f, cursor);
            cursor = HordeSchedule.Advance(400f, cursor, 1, out kind);
            Assert.AreEqual("", kind);
            Assert.AreEqual(300f, cursor);

            cursor = HordeSchedule.Advance(400f, 45f, 3, out kind);
            Assert.AreEqual("runners", kind);
            Assert.AreEqual(4, HordeSchedule.Count(kind));
            Assert.AreEqual("Runner", HordeSchedule.Prefer(kind));
            cursor = HordeSchedule.Advance(400f, cursor, 3, out kind);
            Assert.AreEqual("brute", kind);
            Assert.AreEqual(1, HordeSchedule.Count(kind));
            Assert.AreEqual("Brute", HordeSchedule.Prefer(kind));
            Assert.AreEqual(300f, cursor);
            Assert.AreEqual(45f, HordeSchedule.Advance(60f, 45f, 3, out kind));
            Assert.AreEqual("", kind);
        }

        [Test]
        public void ABlockStaysWalkableFromTheSpawnToTheWayOut()
        {
            var ash = DistrictBlocks.Build(1701, "ash_market");
            Assert.AreEqual("storefront", ash.Footprint);
            Assert.AreEqual("cache", ash.PoiRole);
            Assert.AreEqual("gate", ash.ExtractKind);
            Assert.AreEqual(-5.5f, ash.ExtractX);
            Assert.AreEqual(-10f, ash.ExtractZ);
            Assert.AreEqual("Find the cache", ObjectiveTracker.LineFor("cache", false));
            Assert.AreEqual("Radio part stowed", ObjectiveTracker.LineFor("radio", true));
            Assert.AreEqual("", ObjectiveTracker.LineFor("", false));
            Assert.IsTrue(DistrictBlocks.Navigable(ash));

            var hospital = DistrictBlocks.Build(1701, "old_hospital");
            Assert.AreEqual("clinic", hospital.Footprint);
            Assert.AreEqual("radio", hospital.PoiRole);
            Assert.AreEqual("alley", hospital.ExtractKind);
            Assert.AreEqual(8f, hospital.PoiZ);

            var downtown = DistrictBlocks.Build(4, "downtown_core");
            Assert.AreEqual("station", downtown.Footprint);
            Assert.AreEqual("plaza", downtown.ExtractKind);
            Assert.AreEqual(14f, downtown.PoiZ);
            Assert.AreEqual(-12f, downtown.ExtractX);
            Assert.AreEqual(16f, downtown.ExtractZ);

            var mall = DistrictBlocks.Build(1701, "mall");
            var mallAgain = DistrictBlocks.Build(1701, "mall");
            Assert.AreEqual(mall.PoiZ, mallAgain.PoiZ);
            Assert.AreEqual(mall.NestZ, mallAgain.NestZ);
            Assert.AreEqual("plaza", mall.ExtractKind);
            var other = DistrictBlocks.Build(99991, "mall");
            Assert.IsTrue(mall.PoiZ != other.PoiZ || mall.NestZ != other.NestZ);

            var ids = CampaignBoard.All();
            for (int seed = 1; seed <= 100; seed++)
            {
                for (int d = 0; d < ids.Length; d++)
                {
                    var plan = DistrictBlocks.Build(seed, ids[d].Id);
                    Assert.IsTrue(DistrictBlocks.Navigable(plan), ids[d].Id + " " + seed);
                    var obstacles = new List<DistrictLayout.Piece>();
                    obstacles.AddRange(DistrictLayout.For(ids[d].Id));
                    obstacles.AddRange(DistrictGenerator.Scatter(seed, ids[d].Id));
                    var pieces = obstacles.ToArray();
                    Assert.IsTrue(DistrictBlocks.ClearOf(DistrictBlocks.Open(plan), pieces, 1.05f), ids[d].Id + " open " + seed);
                    Assert.IsTrue(DistrictBlocks.ClearOf(DistrictBlocks.Walls(plan), pieces, 1.05f), ids[d].Id + " wall " + seed);
                    Assert.GreaterOrEqual(DistrictBlocks.Walls(plan).Length, 12);
                }
            }
        }

        [Test]
        public void SpawnsStayOffScreenAndTheHordeEbbsBeforeItPeaksAgain()
        {
            Assert.IsTrue(ViewVolume.Seen(0f, 0f, 0f, 0f, 0f, 1f, 1f, 0f, 0f, 0f, 1f, 0f, 90f, 1f, 0.3f, 40f, 0f, 0f, 10f, 0.4f, 0.9f, 0.4f));
            Assert.IsFalse(ViewVolume.Seen(0f, 0f, 0f, 0f, 0f, 1f, 1f, 0f, 0f, 0f, 1f, 0f, 90f, 1f, 0.3f, 40f, 0f, 0f, -8f, 0.4f, 0.9f, 0.4f));
            Assert.IsFalse(ViewVolume.Seen(0f, 0f, 0f, 0f, 0f, 1f, 1f, 0f, 0f, 0f, 1f, 0f, 90f, 1f, 0.3f, 40f, 15f, 0f, 10f, 0.4f, 0.9f, 0.4f));
            Assert.IsFalse(ViewVolume.Seen(0f, 0f, 0f, 0f, 0f, 1f, 1f, 0f, 0f, 0f, 1f, 0f, 90f, 1f, 0.3f, 40f, 0f, 0f, 50f, 0.4f, 0.9f, 0.4f));
            Assert.IsFalse(ViewVolume.Seen(0f, 0f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f, 0f, 90f, 1f, 0.3f, 40f, 0f, 0f, 10f, 0.4f, 0.9f, 0.4f));

            var survivor = PressureClock.Run(2, 20f, 8f, 2);
            var nightmare = PressureClock.Run(3, 20f, 6f, 2);
            Assert.Less(survivor.Min, 1f);
            Assert.Greater(survivor.Max, 75f);
            Assert.GreaterOrEqual(survivor.Calm, 1);
            Assert.GreaterOrEqual(survivor.Peak, 1);
            Assert.AreEqual(35, survivor.Spawns);
            Assert.AreEqual(56, nightmare.Spawns);
            Assert.Greater(nightmare.Spawns, survivor.Spawns);

            Assert.AreEqual(1f, ExtractWatch.Advance(0f, 1f, true, false));
            Assert.AreEqual(ExtractWatch.HoldSeconds, ExtractWatch.Advance(2f, 2f, true, false));
            Assert.AreEqual(0f, ExtractWatch.Advance(2f, 1f, true, true));
            Assert.AreEqual(0f, ExtractWatch.Advance(2f, 0.5f, false, false));
            Assert.IsTrue(ExtractWatch.Ready(3f));
            Assert.IsFalse(ExtractWatch.Ready(2.5f));
            Assert.IsTrue(ExtractWatch.Threatened(0f, 0f, new[] { 20f }, new[] { 0f }));
            Assert.IsFalse(ExtractWatch.Threatened(0f, 0f, new[] { 21f }, new[] { 0f }));
            Assert.IsFalse(ExtractWatch.Threatened(0f, 0f, null, null));
        }

        [Test]
        public void CoverShortensSightAndAWonRunKeepsTheNights()
        {
            Assert.AreEqual(0.4f, CoverSight.Scale(0f, -1f, 0f, 4f, 0f, 0f, 0f, true), 0.001f);
            Assert.AreEqual(0.62f, CoverSight.Scale(0f, -1f, 0f, 4f, 0f, 0f, 0f, false), 0.001f);
            Assert.AreEqual(1f, CoverSight.Scale(0f, -1f, 0f, -4f, 0f, 0f, 0f, true), 0.001f);
            Assert.AreEqual(1f, CoverSight.Scale(5f, 0f, 0f, 4f, 0f, 0f, 0f, true), 0.001f);
            Assert.AreEqual(0.4f, CoverSight.Scale(-1f, 0f, 4f, 0f, 0f, 0f, 90f, true), 0.001f);
            Assert.AreEqual(0.4f, CoverSight.Best(0f, -1f, 0f, 4f, new[] { 8f, 0f }, new[] { 0f, 0f }, new[] { 0f, 0f }, true), 0.001f);

            Assert.IsFalse(RaidPlan.Due(1, 0));
            Assert.IsTrue(RaidPlan.Due(1, 0, true));
            Assert.IsFalse(RaidPlan.Due(4, 8, true));
            Assert.IsTrue(RaidPlan.Due(2, 0, false));
            Assert.AreEqual(0f, EndlessShift.Tension(0), 0.001f);
            Assert.AreEqual(8f, EndlessShift.Tension(2), 0.001f);
            Assert.AreEqual(24f, EndlessShift.Tension(9), 0.001f);
            Assert.AreEqual(1f, EndlessShift.IntervalScale(0), 0.001f);
            Assert.Less(EndlessShift.IntervalScale(6), 0.6f);
        }

        [Test]
        public void AimPullAndTheFrameCapKeepTheOldDefaults()
        {
            float light = PlayOptions.Yaw(0f, 1f, 1f, 4f, 1, 1f);
            float strong = PlayOptions.Yaw(0f, 1f, 1f, 4f, 2, 1f);
            Assert.Greater(light, 3f);
            Assert.Less(light, 5f);
            Assert.Greater(strong, light);
            Assert.AreEqual(0f, PlayOptions.Yaw(0f, 1f, 0f, -4f, 2, 1f));
            Assert.AreEqual(0f, PlayOptions.Yaw(0f, 1f, 1f, 4f, 0, 1f));
            Assert.AreEqual(-1.5f, PlayOptions.StickY(1.5f, true));
            Assert.AreEqual(1.5f, PlayOptions.StickY(1.5f, false));

            Assert.IsTrue(PlayOptions.Stance(true, false, false, 0));
            Assert.IsFalse(PlayOptions.Stance(false, false, true, 0));
            Assert.IsTrue(PlayOptions.Stance(true, true, false, 1));
            Assert.IsTrue(PlayOptions.Stance(true, false, true, 1));
            Assert.IsFalse(PlayOptions.Stance(true, true, true, 1));

            Assert.AreEqual(-1, PlayOptions.FrameTarget(0, true));
            Assert.AreEqual(60, PlayOptions.FrameTarget(0, false));
            Assert.AreEqual(30, PlayOptions.FrameTarget(1, true));
            Assert.AreEqual(-1, PlayOptions.FrameTarget(4, false));
            Assert.AreEqual(0, PlayOptions.NextFrame(4));
            Assert.AreEqual("Auto", PlayOptions.FrameName(0));
            Assert.AreEqual("120 fps", PlayOptions.FrameName(3));
            Assert.AreEqual("Uncapped", PlayOptions.FrameName(4));
        }

        [Test]
        public void FinishedRunsRankByDaysAndKeepEight()
        {
            var first = RunBoard.Make("Ada", 3, 4, 1, 1, false);
            var longer = RunBoard.Make("Bo", 9, 2, 0, 3, true);
            var sameDay = RunBoard.Make("Cy", 9, 8, 0, 1, false);
            var board = RunBoard.Insert(null, first);
            board = RunBoard.Insert(board, longer);
            board = RunBoard.Insert(board, sameDay);
            Assert.AreEqual("Cy", board[0].Name);
            Assert.AreEqual("Bo", board[1].Name);
            Assert.AreEqual("Ada", board[2].Name);
            Assert.AreEqual("Held  day 9  kills 2  lost 0  streets 3", RunBoard.Line(longer));
            Assert.AreEqual("Fell  day 3  kills 4  lost 1  streets 1", RunBoard.Line(first));

            var packed = RunBoard.Pack(board);
            var again = RunBoard.Unpack(packed);
            Assert.AreEqual(3, again.Length);
            Assert.AreEqual(board[0].Kills, again[0].Kills);
            Assert.AreEqual(board[0].Won, again[0].Won);
            Assert.AreEqual(0, RunBoard.Unpack("").Length);
            Assert.AreEqual(0, RunBoard.Unpack("nope").Length);

            var many = new RunBoard.Run[0];
            for (int day = 1; day <= 9; day++) many = RunBoard.Insert(many, RunBoard.Make("N", day, 1, 0, 0, false));
            Assert.AreEqual(RunBoard.Limit, many.Length);
            Assert.AreEqual(9, many[0].Day);
            Assert.AreEqual(2, many[many.Length - 1].Day);
        }

        [Test]
        public void AStrandedSurvivorFollowsToTheGateAndJoinsOnce()
        {
            var hospital = RescueBook.For("old_hospital");
            Assert.AreEqual("rescue_hospital", hospital.Id);
            Assert.AreEqual("Imani Cole", hospital.Name);
            Assert.AreEqual("Field Medic", hospital.Trait);
            Assert.AreEqual("Dell Orth", RescueBook.For("police_station").Name);
            Assert.AreEqual("Nia Pell", RescueBook.For("mall").Name);
            Assert.IsTrue(string.IsNullOrEmpty(RescueBook.For("ash_market").Id));

            Assert.IsFalse(RescueBook.CanJoin("", null, 4));
            Assert.IsFalse(RescueBook.CanJoin("mara", null, 4));
            Assert.IsFalse(RescueBook.CanJoin("rescue_hospital", null, 8));
            Assert.IsFalse(RescueBook.CanJoin("rescue_hospital", new[] { "rescue_hospital" }, 4));
            Assert.IsTrue(RescueBook.CanJoin("rescue_hospital", null, 4));

            RescueBook.Step(0f, 0f, 0f, 6f, 4f, 1f, out float stepX, out float stepZ);
            Assert.AreEqual(0f, stepX, 0.001f);
            Assert.AreEqual(4f, stepZ, 0.001f);
            RescueBook.Step(0f, 4.8f, 0f, 6f, 4f, 1f, out float stayX, out float stayZ);
            Assert.AreEqual(0f, stayX, 0.001f);
            Assert.AreEqual(4.8f, stayZ, 0.001f);
            RescueBook.Step(0f, 0f, 0f, 20f, 4f, 1f, out float snapX, out float snapZ);
            Assert.AreEqual(0f, snapX, 0.001f);
            Assert.AreEqual(17f, snapZ, 0.001f);

            Assert.IsTrue(RescueBook.AtGate(3f, 0f, 0f, 0f, 3.2f));
            Assert.IsFalse(RescueBook.AtGate(4f, 0f, 0f, 0f, 3.2f));
        }

        [Test]
        public void ARunnerWindsUpBeforeTheLungeAndABruteBeforeTheCharge()
        {
            Assert.IsTrue(SpecialBeat.InReach(2.2f, false));
            Assert.IsFalse(SpecialBeat.InReach(2.1f, false));
            Assert.IsTrue(SpecialBeat.InReach(5.5f, false));
            Assert.IsFalse(SpecialBeat.InReach(5.6f, false));
            Assert.IsTrue(SpecialBeat.InReach(3f, true));
            Assert.IsTrue(SpecialBeat.InReach(8f, true));
            Assert.IsFalse(SpecialBeat.InReach(2.5f, true));
            Assert.AreEqual(8f, SpecialBeat.Speed(false));
            Assert.AreEqual(6.5f, SpecialBeat.Speed(true));
            Assert.AreEqual(30f, SpecialBeat.LungeDamage);

            var idle = SpecialBeat.Advance(new SpecialBeat.Clock(), false, 10f, 1f);
            Assert.AreEqual(0, idle.Phase);

            var wind = SpecialBeat.Advance(new SpecialBeat.Clock(), true, 10f, 0.1f);
            Assert.AreEqual(1, wind.Phase);
            Assert.Greater(wind.Left, 0.2f);

            var dash = SpecialBeat.Advance(wind, true, 10.1f, 0.5f);
            Assert.AreEqual(2, dash.Phase);
            Assert.IsFalse(dash.Struck);
            Assert.IsTrue(SpecialBeat.Hits(dash, 1.6f, 1.9f));
            Assert.IsFalse(SpecialBeat.Hits(dash, 4f, 1.9f));
            Assert.IsFalse(SpecialBeat.Hits(wind, 1.6f, 1.9f));

            var done = SpecialBeat.Advance(dash, true, 11f, 0.5f);
            Assert.AreEqual(0, done.Phase);
            Assert.Greater(done.Ready, 11f);
            var held = SpecialBeat.Advance(done, true, done.Ready - 0.1f, 0.1f);
            Assert.AreEqual(0, held.Phase);
            var again = SpecialBeat.Advance(done, true, done.Ready, 0.1f);
            Assert.AreEqual(1, again.Phase);

            var brute = SpecialBeat.Advance(new SpecialBeat.Clock(), true, 10f, 0.05f, true);
            Assert.AreEqual(1, brute.Phase);
            Assert.AreEqual(SpecialBeat.ChargeWindup, brute.Left, 0.001f);
            Assert.AreEqual(1.5f, SpecialBeat.WallStun);
            SpecialBeat.Commit(0f, 4f, out float headX, out float headZ);
            Assert.AreEqual(0f, headX, 0.001f);
            Assert.AreEqual(1f, headZ, 0.001f);
            SpecialBeat.Commit(3f, 0f, out float sideX, out float sideZ);
            Assert.AreEqual(1f, sideX, 0.001f);
            Assert.AreEqual(0f, sideZ, 0.001f);
        }

        [Test]
        public void LosingSightStartsASearchAndAChargeBreaksBoards()
        {
            Assert.IsFalse(SearchMemory.Forgotten(SearchMemory.Lose(0f, false, 0.7f)));
            Assert.IsTrue(SearchMemory.Forgotten(SearchMemory.Lose(0.7f, false, 0.06f)));
            Assert.AreEqual(0f, SearchMemory.Lose(0.5f, true, 1f));

            var sweep = SearchMemory.Start();
            Assert.AreEqual(0, sweep.Index);
            Assert.Greater(sweep.Left, 2.6f);
            Assert.Less(sweep.Left, 2.7f);
            sweep = SearchMemory.Tick(sweep, false, 1f);
            Assert.AreEqual(0, sweep.Index);
            sweep = SearchMemory.Tick(sweep, true, 0f);
            Assert.AreEqual(1, sweep.Index);
            sweep = SearchMemory.Tick(sweep, true, 0f);
            sweep = SearchMemory.Tick(sweep, true, 0f);
            Assert.IsTrue(SearchMemory.Done(sweep));

            for (int i = 0; i < SearchMemory.Points; i++)
            {
                SearchMemory.Offset(i, out float x, out float z);
                Assert.LessOrEqual(x * x + z * z, SearchMemory.Radius * SearchMemory.Radius);
            }

            Assert.AreEqual(0f, BoardBreak.Apply(BoardBreak.Wood, BoardBreak.ChargeHit));
            Assert.IsTrue(BoardBreak.GivesWay(BoardBreak.Apply(BoardBreak.Wood, BoardBreak.ChargeHit)));
            Assert.AreEqual(20f, BoardBreak.Apply(BoardBreak.Wood, 20f));
            Assert.IsFalse(BoardBreak.GivesWay(20f));
        }

        [Test]
        public void ARifleHoldsTheTriggerAndAPistolDoesNot()
        {
            Assert.IsTrue(TriggerGate.ShouldFire(true, true, false));
            Assert.IsFalse(TriggerGate.ShouldFire(true, false, false));
            Assert.IsTrue(TriggerGate.ShouldFire(false, true, true));
            Assert.IsFalse(TriggerGate.ShouldFire(false, true, false));

            Assert.IsTrue(WeaponCard.FiresAutomatic(WeaponType.Rifle, false));
            Assert.IsTrue(WeaponCard.FiresAutomatic(WeaponType.Pistol, true));
            Assert.IsFalse(WeaponCard.FiresAutomatic(WeaponType.Pistol, false));
            Assert.IsFalse(WeaponCard.FiresAutomatic(WeaponType.Shotgun, false));
            Assert.IsTrue(WeaponCard.FiresProjectile(WeaponType.Shotgun, false));
            Assert.IsTrue(WeaponCard.FiresProjectile(WeaponType.Rifle, false));
            Assert.IsFalse(WeaponCard.FiresProjectile(WeaponType.Pistol, false));

            float heat = 0f;
            for (int i = 0; i < 10; i++) heat = RecoilBloom.AfterShot(heat);
            Assert.AreEqual(70f, heat, 0.001f);
            Assert.AreEqual(5.625f, RecoilBloom.Spread(3f, 1f, heat), 0.001f);
            Assert.AreEqual(3f, RecoilBloom.Spread(3f, 1f, 0f), 0.001f);
            Assert.AreEqual(0f, RecoilBloom.Cool(28f, 1f), 0.001f);
            Assert.AreEqual(100f, RecoilBloom.AfterShot(98f), 0.001f);

            var rifle = WeaponCard.Find("rifle_assault");
            Assert.AreEqual(26f, rifle.Damage, 0.001f);
            Assert.AreEqual(30, rifle.Magazine);
            Assert.IsTrue(rifle.Automatic);
            Assert.IsTrue(rifle.Projectile);
            Assert.AreEqual(7, WeaponCard.Find("shotgun_pump").Pellets);
            Assert.IsFalse(WeaponCard.Find("shotgun_pump").Automatic);
            Assert.IsTrue(string.IsNullOrEmpty(WeaponCard.Find("nope").Id));

            Assert.AreEqual(NoiseType.GunshotQuiet, WeaponMod.Report(NoiseType.GunshotLoud, true));
            Assert.AreEqual(NoiseType.GunshotLoud, WeaponMod.Report(NoiseType.GunshotLoud, false));
            Assert.AreEqual(NoiseType.Explosion, WeaponMod.Report(NoiseType.Explosion, true));
        }

        [Test]
        public void ThePackSplitsAStackAndACrateKeepsWhatYouLeave()
        {
            Assert.AreEqual(3f, PackOps.Weight(1f, 10, 2), 0.001f);
            Assert.IsFalse(PackOps.Heavy(31f, 35f));
            Assert.IsTrue(PackOps.Heavy(32f, 35f));
            Assert.IsTrue(PackOps.Fits(34f, 35f, 1f));
            Assert.IsFalse(PackOps.Fits(34f, 35f, 2f));
            Assert.AreEqual(0, PackOps.SplitOff(1));
            Assert.AreEqual(2, PackOps.SplitOff(5));
            Assert.AreEqual(2, PackOps.SplitOff(4));
            Assert.AreEqual(1, PackOps.Tier(0));
            Assert.AreEqual(1, PackOps.Tier(1));
            Assert.AreEqual(2, PackOps.Tier(2));
            Assert.AreEqual(35f, PackOps.Limit(0), 0.001f);
            Assert.AreEqual(50f, PackOps.Limit(2), 0.001f);
            Assert.IsTrue(PackOps.Fits(48f, PackOps.RaisedLimit, 2f));
            Assert.IsFalse(PackOps.Fits(49f, PackOps.RaisedLimit, 2f));
            Assert.IsTrue(PackOps.CanRaise(1, 2, 12, 3, 1));
            Assert.IsFalse(PackOps.CanRaise(1, 1, 12, 3, 1));
            Assert.IsFalse(PackOps.CanRaise(2, 2, 12, 3, 1));
            Assert.IsFalse(PackOps.CanRaise(1, 2, 11, 3, 1));
            Assert.IsFalse(PackOps.CanRaise(1, 2, 12, 2, 1));
            Assert.IsFalse(PackOps.CanRaise(1, 2, 12, 3, 0));
            var packSave = new SaveGameData { packTier = 2 };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(packSave), out var packLoaded, out var packError), packError);
            Assert.AreEqual(2, packLoaded.packTier);
            Assert.IsTrue(SaveCodec.TryDeserialize("{\"schemaVersion\":1}", out var packLegacy, out var packLegacyError), packLegacyError);
            Assert.AreEqual(0, packLegacy.packTier);
            Assert.AreEqual(1, PackOps.Tier(packLegacy.packTier));
            Assert.AreEqual("La mochila carga 50", Loc.T("camp.pack_t2", "es"));
            Assert.AreEqual("Mochila de campo", Loc.T("camp.pack_raise", "es"));

            var crate = new[]
            {
                new ContainerHold.Stack { Id = "bandage", Count = 4 },
                new ContainerHold.Stack { Id = "water", Count = 1 }
            };
            crate = ContainerHold.Take(crate, "bandage", 1, out int moved);
            Assert.AreEqual(1, moved);
            Assert.AreEqual("bandage*3|water*1", ContainerHold.Signature(crate));
            var empty = ContainerHold.TakeAll(crate, out var all);
            Assert.AreEqual(0, empty.Length);
            Assert.AreEqual(2, all.Length);
            Assert.AreEqual(3, all[0].Count);
            Assert.AreEqual("", ContainerHold.Signature(null));
        }

        [Test]
        public void AHungryColonistLeavesThePostAndACollapseWillNotWork()
        {
            Assert.AreEqual("Rest", CampRoutine.Choose("Guard", 80f, 80f, 5f, 0));
            Assert.AreEqual("I can't do this.", CampRoutine.Bark("Guard", 5f));
            Assert.AreEqual("Cook", CampRoutine.Choose("Guard", 20f, 80f, 60f, 0));
            Assert.AreEqual("Fire's lit.", CampRoutine.Bark("Cook", 60f));
            Assert.AreEqual("Medic", CampRoutine.Choose("Scavenge", 80f, 80f, 60f, 2));
            Assert.AreEqual("Hold still.", CampRoutine.Bark("Medic", 60f));
            Assert.AreEqual("Rest", CampRoutine.Choose("Scavenge", 80f, 80f, 25f, 0));
            Assert.AreEqual("Guard", CampRoutine.Choose("Guard", 80f, 80f, 25f, 0));
            Assert.AreEqual("Watching the gate.", CampRoutine.Bark("Guard", 50f));
            Assert.AreEqual("We'll hold.", CampRoutine.Bark("Rest", 80f));
            Assert.AreEqual("Resting.", CampRoutine.Bark("Rest", 50f));

            CampRoutine.Nudge(0, out float ax, out float az);
            CampRoutine.Nudge(1, out float bx, out float bz);
            Assert.AreNotEqual(ax, bx);
            Assert.AreNotEqual(az, bz);
            CampRoutine.Nudge(6, out float cx, out float cz);
            Assert.AreEqual(ax, cx);
            Assert.AreEqual(az, cz);
        }

        [Test]
        public void AHordePushesApartInsteadOfStandingInOneSpot()
        {
            CrowdSpace.Push(0f, 0f, new[] { 0.5f }, new[] { 0f }, 1, out float awayX, out float awayZ);
            Assert.Less(awayX, -0.6f);
            Assert.Greater(awayX, -0.7f);
            Assert.AreEqual(0f, awayZ, 0.001f);

            CrowdSpace.Push(0f, 0f, new[] { 3f }, new[] { 0f }, 1, out float farX, out float farZ);
            Assert.AreEqual(0f, farX, 0.001f);
            Assert.AreEqual(0f, farZ, 0.001f);

            CrowdSpace.Push(0f, 0f, new[] { 0f }, new[] { 0f }, 1, out float sameX, out float sameZ);
            Assert.AreEqual(0f, sameX, 0.001f);
            Assert.AreEqual(0f, sameZ, 0.001f);

            var packedX = new float[20];
            var packedZ = new float[20];
            for (int i = 0; i < packedX.Length; i++) packedX[i] = 0.2f;
            CrowdSpace.Push(0f, 0f, packedX, packedZ, packedX.Length, out float clampX, out float clampZ);
            float clamp = (float)System.Math.Sqrt(clampX * clampX + clampZ * clampZ);
            Assert.AreEqual(CrowdSpace.MaxPush, clamp, 0.001f);

            Assert.AreEqual(1, CrowdSpace.Neighbors(0f, 0f, new[] { 0.5f, 3f }, new[] { 0f, 0f }, 2));
            Assert.AreEqual(30, CrowdSpace.Priority(0));
            Assert.AreEqual(99, CrowdSpace.Priority(10));
        }

        [Test]
        public void ABiteConnectsPartwayThroughTheSwing()
        {
            Assert.AreEqual(0.5f, SwingClock.Advance(0f, 0.5f, 1f), 0.001f);
            Assert.AreEqual(0.7f, SwingClock.Advance(0.5f, 0.2f, 1f), 0.001f);
            Assert.AreEqual(1f, SwingClock.Advance(0.9f, 0.5f, 1f), 0.001f);
            Assert.IsTrue(SwingClock.Connects(0.5f, 0.7f));
            Assert.IsFalse(SwingClock.Connects(0.6f, 0.9f));
            Assert.IsFalse(SwingClock.Connects(0.2f, 0.5f));
            Assert.AreEqual(SwingClock.HitAt, 0.6f, 0.001f);
        }

        [Test]
        public void AResolutionChoiceCyclesAndNativeLeavesTheWindowAlone()
        {
            Assert.AreEqual(1, DisplayModes.Next(0));
            Assert.AreEqual(0, DisplayModes.Next(5));
            Assert.AreEqual(1, DisplayModes.Next(-1));
            Assert.AreEqual("Native", DisplayModes.Name(0));
            Assert.AreEqual("1920 x 1080", DisplayModes.Name(3));
            Assert.IsFalse(DisplayModes.Size(0, out int nativeW, out int nativeH));
            Assert.AreEqual(0, nativeW);
            Assert.AreEqual(0, nativeH);
            Assert.IsTrue(DisplayModes.Size(3, out int width, out int height));
            Assert.AreEqual(1920, width);
            Assert.AreEqual(1080, height);
            Assert.IsTrue(DisplayModes.Size(5, out int ultraW, out int ultraH));
            Assert.AreEqual(3840, ultraW);
            Assert.AreEqual(2160, ultraH);
        }

        [Test]
        public void ACrateRowTakesOneStackAndLeavesTheRest()
        {
            var crate = new[]
            {
                new ContainerHold.Stack { Id = "bandage", Count = 3 },
                new ContainerHold.Stack { Id = "scrap", Count = 2 }
            };
            Assert.AreEqual("Bandage x3", ContainerHold.Offer(crate[0]));
            Assert.AreEqual("Scrap x2", ContainerHold.Offer(crate[1]));
            Assert.AreEqual("mystery x2", ContainerHold.Offer(new ContainerHold.Stack { Id = "mystery", Count = 2 }));
            Assert.AreEqual("", ContainerHold.Offer(new ContainerHold.Stack { Id = "", Count = 4 }));
            crate = ContainerHold.Take(crate, "bandage", int.MaxValue, out int moved);
            Assert.AreEqual(3, moved);
            Assert.AreEqual("scrap*2", ContainerHold.Signature(crate));
        }

        [Test]
        public void ABeltPocketTakesAConsumableAndRefusesAFifth()
        {
            var slots = ItemBelt.Fresh();
            Assert.AreEqual(-1, ItemBelt.Toggle(slots, "ammo_rifle"));
            Assert.AreEqual(-1, ItemBelt.Toggle(slots, "scrap"));
            Assert.AreEqual(0, ItemBelt.Toggle(slots, "bandage"));
            Assert.AreEqual("5", ItemBelt.Mark(slots, "bandage"));
            Assert.AreEqual("bandage", ItemBelt.IdAt(slots, 0));
            Assert.AreEqual(0, ItemBelt.Toggle(slots, "bandage"));
            Assert.AreEqual("", ItemBelt.IdAt(slots, 0));
            Assert.AreEqual(0, ItemBelt.Toggle(slots, "bandage"));
            Assert.AreEqual(1, ItemBelt.Toggle(slots, "water"));
            Assert.AreEqual(2, ItemBelt.Toggle(slots, "canned_food"));
            Assert.AreEqual(3, ItemBelt.Toggle(slots, "molotov"));
            Assert.AreEqual(-2, ItemBelt.Toggle(slots, "noise_lure"));
            Assert.AreEqual("5 Bandage   6 Water Bottle   7 Canned Food   8 Molotov", ItemBelt.Line(slots));
            Assert.AreEqual("5 -   6 -   7 -   8 -", ItemBelt.Line(ItemBelt.Fresh()));
        }

        [Test]
        public void ACompassPutsTheGateOnTheRightAndAHitFromTheEast()
        {
            Assert.AreEqual("N   Gate right", StreetHeading.Readout(0f, 1f, 0f, 0f, false, 0f, 0f, true, 10f, 0f));
            Assert.AreEqual("N   Gate behind", StreetHeading.Readout(0f, 1f, 0f, 0f, false, 0f, 0f, true, 0f, -10f));
            Assert.AreEqual("E   POI left", StreetHeading.Readout(1f, 0f, 0f, 0f, true, 0f, 12f, false, 0f, 0f));
            Assert.IsTrue(StreetHeading.OnStrip(0f, 80f, out float center));
            Assert.AreEqual(0f, center, 0.01f);
            Assert.IsTrue(StreetHeading.OnStrip(-45f, 80f, out float left));
            Assert.AreEqual(-40f, left, 0.01f);
            Assert.IsFalse(StreetHeading.OnStrip(120f, 80f, out _));
            Assert.AreEqual("front", StreetHeading.Sector(StreetHeading.Incoming(0f, 1f, 0f, -1f)));
            Assert.AreEqual("right", StreetHeading.Sector(StreetHeading.Incoming(0f, 1f, -1f, 0f)));
            Assert.AreEqual(0.825f, HealthGhost.Follow(1f, 0f, 0.5f), 0.001f);
            Assert.AreEqual(0.8f, HealthGhost.Follow(0.2f, 0.8f, 0.1f), 0.001f);
            Assert.AreEqual(0.2f, HealthGhost.Follow(0.2f, 0.2f, 0.5f), 0.001f);
            var tape = new KillTape();
            tape.Note(KillTape.Name("walker"));
            tape.Note(KillTape.Name("runner"));
            tape.Note(KillTape.Name("brute"));
            tape.Note(KillTape.Name("walker"));
            Assert.AreEqual("Runner\nBrute\nWalker", tape.Text());
        }

        [Test]
        public void ABleedLastsAndAntibioticsStopTheFeverBeforeItKills()
        {
            Assert.AreEqual(90f, Affliction.BleedLoss(90f), 0.01f);
            Assert.AreEqual(0, Affliction.Stage(0f));
            Assert.AreEqual(1, Affliction.Stage(1f));
            Assert.AreEqual(1, Affliction.Stage(89f));
            Assert.AreEqual(2, Affliction.Stage(90f));
            Assert.AreEqual(2, Affliction.Stage(179f));
            Assert.AreEqual(3, Affliction.Stage(180f));
            Assert.IsFalse(Affliction.Fatal(179f));
            Assert.IsTrue(Affliction.Fatal(180f));
            Assert.AreEqual(0f, Affliction.Advance(0f, 1f), 0.001f);
            Assert.AreEqual(91f, Affliction.Advance(90f, 1f), 0.001f);
            Assert.AreEqual(181f, Affliction.Advance(179f, 5f), 0.001f);
            Assert.AreEqual(181f, Affliction.Advance(181f, 4f), 0.001f);
            Assert.AreEqual(0.05f, Affliction.Bite(0f), 0.001f);
            Assert.AreEqual(25.05f, Affliction.Bite(0.05f), 0.001f);
            Assert.AreEqual(181f, Affliction.Bite(170f), 0.001f);
            Assert.IsTrue(Affliction.AntibioticsWork(1));
            Assert.IsTrue(Affliction.AntibioticsWork(2));
            Assert.IsFalse(Affliction.AntibioticsWork(0));
            Assert.IsFalse(Affliction.AntibioticsWork(3));
            Assert.AreEqual(10f, Affliction.PainHeal(10f), 0.01f);
            Assert.AreEqual(20f, Affliction.PainHeal(20f), 0.01f);
            Assert.AreEqual("Infection II", Affliction.Label(2));
            Assert.AreEqual(10, ItemCatalog.Find("bandage").Heal);
            Assert.AreEqual(ItemUse.Cure, ItemCatalog.Find("antibiotics").Use);
            Assert.AreEqual(ItemUse.Relief, ItemCatalog.Find("painkillers").Use);
        }

        [Test]
        public void FatigueBleedAndFeverStayOnTheExistingSave()
        {
            Assert.AreEqual(200, BodyState.PackFatigue(20f));
            Assert.AreEqual(0, BodyState.PackFatigue(-3f));
            Assert.AreEqual(1000, BodyState.PackFatigue(140f));
            Assert.AreEqual(20f, BodyState.UnpackFatigue(200, 1), 0.001f);
            Assert.AreEqual(0f, BodyState.UnpackFatigue(0, 1), 0.001f);
            Assert.AreEqual(-1f, BodyState.UnpackFatigue(0, 0), 0.001f);
            Assert.AreEqual(1, BodyState.PackBleed(true));
            Assert.AreEqual(0, BodyState.PackBleed(false));
            Assert.IsTrue(BodyState.UnpackBleed(1));
            Assert.IsFalse(BodyState.UnpackBleed(0));
            Assert.AreEqual(0, BodyState.PackInfection(0f));
            Assert.AreEqual(900, BodyState.PackInfection(90f));
            Assert.AreEqual(1810, BodyState.PackInfection(400f));
            Assert.AreEqual(90f, BodyState.UnpackInfection(900), 0.001f);
            Assert.AreEqual(0f, BodyState.UnpackInfection(0), 0.001f);

            var data = new SaveGameData { fatigue = 200, fatigueSet = 1, bleed = 1, infection = 900 };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(data), out var loaded, out var error), error);
            Assert.AreEqual(200, loaded.fatigue);
            Assert.AreEqual(1, loaded.fatigueSet);
            Assert.AreEqual(1, loaded.bleed);
            Assert.AreEqual(900, loaded.infection);
            Assert.IsTrue(SaveCodec.TryDeserialize("{\"schemaVersion\":1}", out var legacy, out var legacyError), legacyError);
            Assert.AreEqual(0, legacy.fatigueSet);
            Assert.AreEqual(0, legacy.bleed);
            Assert.AreEqual(0, legacy.infection);
            Assert.AreEqual(-1f, BodyState.UnpackFatigue(legacy.fatigue, legacy.fatigueSet), 0.001f);
        }

        [Test]
        public void HungerSlowsRecoveryAndThirstShrinksStamina()
        {
            Assert.AreEqual(55f, NeedsPressure.HungerPerSecond * 600f, 0.01f);
            Assert.AreEqual(50f, NeedsPressure.ThirstPerSecond * 600f, 0.01f);
            Assert.AreEqual(55f, NeedsPressure.FatiguePerSecond * 600f, 0.01f);
            Assert.AreEqual(0.85f, NeedsPressure.Regen(24f), 0.001f);
            Assert.AreEqual(1f, NeedsPressure.Regen(25f), 0.001f);
            Assert.AreEqual(80f, NeedsPressure.StaminaCap(24f, 100f), 0.01f);
            Assert.AreEqual(100f, NeedsPressure.StaminaCap(25f, 100f), 0.01f);
            Assert.AreEqual(100f, NeedsPressure.Pool(0, 100f), 0.01f);
            Assert.AreEqual(116f, NeedsPressure.Pool(4, 100f), 0.01f);
            Assert.AreEqual(132f, NeedsPressure.Pool(8, 100f), 0.01f);
            Assert.AreEqual(132f, NeedsPressure.Pool(12, 100f), 0.01f);
            Assert.AreEqual(105.6f, NeedsPressure.StaminaCap(24f, NeedsPressure.Pool(8, 100f)), 0.01f);
            Assert.AreEqual(0.62f, NeedsPressure.Aim(76f), 0.001f);
            Assert.AreEqual(1f, NeedsPressure.Aim(75f), 0.001f);
            Assert.IsTrue(NeedsPressure.Hungry(24f));
            Assert.IsTrue(NeedsPressure.Dry(10f));
            Assert.IsFalse(NeedsPressure.Tired(75f));
            Assert.IsTrue(NeedsPressure.Tired(76f));
        }

        [Test]
        public void APackBriefNamesWeightAndWhatTheItemDoes()
        {
            string medkit = ItemBrief.Text(ItemCatalog.Find("medkit"));
            Assert.IsTrue(medkit.Contains("Medkit"));
            Assert.IsTrue(medkit.Contains("0.50 kg"));
            Assert.IsTrue(medkit.Contains("+50 health"));
            Assert.IsTrue(medkit.Contains("Stops bleeding and breaks a fever."));
            string pills = ItemBrief.Text(ItemCatalog.Find("antibiotics"));
            Assert.IsTrue(pills.Contains("Clears infection"));
            Assert.IsTrue(pills.Contains("Works before the fever turns lethal."));
            string water = ItemBrief.Text(ItemCatalog.Find("water"));
            Assert.IsTrue(water.Contains("0.50 kg"));
            Assert.IsTrue(water.Contains("+40 thirst"));
            Assert.AreEqual("0.08 kg", ItemBrief.Weight(0.08f));
            Assert.AreEqual("", ItemBrief.Text(null));
            Assert.AreEqual("", ItemBrief.Blurb("nope"));
        }

        [Test]
        public void ADarkCrouchHidesFiveMetersUntilTheFlashlightOrABottle()
        {
            float dark = SpotRange.Exposure(true, false, false, 1f, 0f);
            Assert.IsFalse(SpotRange.Notices(5f, 16f, dark, true, 1f, 1f, 0f, 110f));
            Assert.Less(SpotRange.Meters(16f, dark, true, 1f, 1f), 5f);
            float lit = SpotRange.Exposure(true, false, true, 1f, 0f);
            Assert.AreEqual(1f, lit, 0.001f);
            Assert.IsTrue(SpotRange.Notices(5f, 16f, lit, true, 1f, 1f, 0f, 110f));
            Assert.IsTrue(SpotRange.Beam(5f, 10f, true));
            Assert.IsFalse(SpotRange.Beam(5f, 10f, false));
            Assert.IsFalse(SpotRange.Beam(13f, 0f, true));
            Assert.Greater(ThrowArc.Flight(ThrowArc.Height, ThrowArc.Forward, ThrowArc.Lift, ThrowArc.Gravity), 15f);
            Assert.AreEqual(18f, ThrowArc.NoiseRadius(false), 0.01f);
        }

        [Test]
        public void AShotgunStaggersLongerAndABruteShrugsMostOfItOff()
        {
            Assert.AreEqual(0.25f, HitStun.Seconds(WeaponType.Pistol), 0.001f);
            Assert.AreEqual(0.25f, HitStun.Seconds(WeaponType.Rifle), 0.001f);
            Assert.AreEqual(0.6f, HitStun.Seconds(WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(0.4f, HitStun.Seconds(WeaponType.Melee), 0.001f);
            Assert.AreEqual(0.18f, HitStun.Resist(0.6f, true), 0.001f);
            Assert.AreEqual(0.075f, HitStun.Resist(0.25f, true), 0.001f);
            Assert.AreEqual(0.4f, HitStun.Resist(0.4f, false), 0.001f);
            Assert.AreEqual(0.05f, HitStun.Resist(0.01f, true), 0.001f);
        }

        [Test]
        public void AChaseIsABangAndASearchIsAQuestion()
        {
            Assert.AreEqual("!", ThreatMark.Glyph(ZombieAI.ZombieState.Chase));
            Assert.AreEqual("!", ThreatMark.Glyph(ZombieAI.ZombieState.Attack));
            Assert.AreEqual("?", ThreatMark.Glyph(ZombieAI.ZombieState.InvestigateNoise));
            Assert.AreEqual("?", ThreatMark.Glyph(ZombieAI.ZombieState.Searching));
            Assert.AreEqual("", ThreatMark.Glyph(ZombieAI.ZombieState.Wander));
            Assert.AreEqual("", ThreatMark.Glyph(ZombieAI.ZombieState.Idle));
            Assert.IsTrue(ThreatMark.Near(28f));
            Assert.IsFalse(ThreatMark.Near(28.1f));
            Assert.IsFalse(ThreatMark.Near(-1f));
            Assert.AreEqual("! ! ?", ThreatMark.Line(2, 1));
            Assert.AreEqual("? ?", ThreatMark.Line(0, 2));
            Assert.AreEqual("", ThreatMark.Line(0, 0));
            Assert.AreEqual("! ! ! ! ? ? +", ThreatMark.Line(5, 3));
        }

        [Test]
        public void AMagazineReadsLowAtOneThirdAndTheReloadFills()
        {
            Assert.IsTrue(MagPulse.Low(4, 12, false));
            Assert.IsFalse(MagPulse.Low(5, 12, false));
            Assert.IsTrue(MagPulse.Low(0, 30, false));
            Assert.IsFalse(MagPulse.Low(0, 30, true));
            Assert.IsTrue(MagPulse.Low(2, 6, false));
            Assert.IsFalse(MagPulse.Low(10, 0, false));
            Assert.AreEqual(0f, MagPulse.Fill(0f, 2.1f), 0.001f);
            Assert.AreEqual(0.5f, MagPulse.Fill(1.05f, 2.1f), 0.001f);
            Assert.AreEqual(1f, MagPulse.Fill(3f, 2.1f), 0.001f);
            Assert.AreEqual(1f, MagPulse.Alpha(0f, false), 0.001f);
            Assert.AreEqual(0.45f, MagPulse.Alpha(0f, true), 0.001f);
            float mid = MagPulse.Alpha(0.4f, true);
            Assert.Greater(mid, 0.7f);
            Assert.Less(mid, 0.95f);
        }

        [Test]
        public void AWeaponWheelPutsTheFirstGunUpAndIgnoresAQuietStick()
        {
            Assert.AreEqual(0, WeaponWheel.Pick(0f, 1f));
            Assert.AreEqual(1, WeaponWheel.Pick(1f, 0f));
            Assert.AreEqual(2, WeaponWheel.Pick(0f, -1f));
            Assert.AreEqual(3, WeaponWheel.Pick(-1f, 0f));
            Assert.AreEqual(0, WeaponWheel.Pick(0.3f, 0.9f));
            Assert.AreEqual(1, WeaponWheel.Pick(0.9f, 0.3f));
            Assert.AreEqual(-1, WeaponWheel.Pick(0.2f, 0.2f));
            Assert.IsFalse(WeaponWheel.Shown(true, 0.15f));
            Assert.IsTrue(WeaponWheel.Shown(true, 0.16f));
            Assert.IsFalse(WeaponWheel.Shown(false, 1f));
            Assert.AreEqual(2, WeaponWheel.Release(-1, 2));
            Assert.AreEqual(1, WeaponWheel.Release(1, 2));
            Assert.AreEqual("> 1  Pistol", WeaponWheel.Row(0, "Pistol", true));
            Assert.AreEqual("  3  empty", WeaponWheel.Row(2, "", false));
        }

        [Test]
        public void SettingsFileKeepsAZeroAndPadRebindRejectsADuplicate()
        {
            var snap = SettingsFile.Defaults();
            snap.language = "es";
            snap.resolution = 3;
            snap.music = 0f;
            string json = SettingsFile.ToJson(snap);
            Assert.IsTrue(SettingsFile.TryFromJson(json, out var loaded));
            Assert.AreEqual("es", loaded.language);
            Assert.AreEqual(3, loaded.resolution);
            Assert.AreEqual(0f, loaded.music, 0.001f);
            Assert.AreEqual(0.7f, SettingsFile.Defaults().music, 0.001f);
            Assert.IsTrue(SettingsFile.TryFromJson("{\"shake\":0.5}", out var partial));
            Assert.AreEqual(0.5f, partial.shake, 0.001f);
            Assert.AreEqual(0.7f, partial.music, 0.001f);
            Assert.AreEqual("en", partial.language);
            Assert.IsFalse(SettingsFile.TryFromJson("", out _));

            PadBindings.ResetDefaults();
            try
            {
                Assert.AreEqual("South", PadBindings.Label(PadBindings.Action.Interact));
                Assert.AreEqual("RightTrigger", PadBindings.Label(PadBindings.Action.Fire));
                Assert.AreEqual("LeftShoulder", PadBindings.Label(PadBindings.Action.Wheel));
                Assert.AreEqual("LeftTrigger", PadBindings.Label(PadBindings.Action.Aim));
                Assert.AreEqual("RightShoulder", PadBindings.Label(PadBindings.Action.Dodge));
                Assert.IsFalse(PadBindings.TryRebindNamed(PadBindings.Action.Interact, "North"));
                Assert.IsFalse(PadBindings.TryRebindNamed(PadBindings.Action.Interact, "RightShoulder"));
                string opened = PadBindings.Pack();
                int cut = opened.LastIndexOf(',');
                PadBindings.Unpack(opened.Substring(0, cut + 1));
                Assert.AreEqual("None", PadBindings.Label(PadBindings.Action.Dodge));
                Assert.IsTrue(PadBindings.TryRebindNamed(PadBindings.Action.Interact, "RightShoulder"));
                string packed = PadBindings.Pack();
                PadBindings.ResetDefaults();
                Assert.AreEqual("South", PadBindings.Label(PadBindings.Action.Interact));
                Assert.AreEqual("RightShoulder", PadBindings.Label(PadBindings.Action.Dodge));
                PadBindings.Unpack(packed);
                Assert.AreEqual("RightShoulder", PadBindings.Label(PadBindings.Action.Interact));
                Assert.AreEqual("North", PadBindings.Label(PadBindings.Action.Medkit));
                Assert.AreEqual("None", PadBindings.Label(PadBindings.Action.Dodge));
            }
            finally
            {
                PadBindings.ResetDefaults();
            }
        }

        [Test]
        public void AMedkitNeedsACotAndAMedicAndScavengeBringsCloth()
        {
            Assert.IsTrue(CraftBill.TryOf("medkit", out var medkit));
            Assert.AreEqual(CraftBill.Cot, medkit.Station);
            Assert.AreEqual("Medic", medkit.Skill);
            Assert.AreEqual(1, medkit.Cloth);
            Assert.AreEqual(1, medkit.Chemicals);
            Assert.AreEqual(1, medkit.Tape);
            Assert.AreEqual("Need a medical cot", CraftBill.Block(medkit.Station, medkit.Skill, false, true));
            Assert.AreEqual("Need a medic on duty", CraftBill.Block(medkit.Station, medkit.Skill, true, false));
            Assert.AreEqual("", CraftBill.Block(medkit.Station, medkit.Skill, true, true));
            Assert.IsTrue(CraftBill.OnDuty("Field Medic", "Rest", true, "Medic"));
            Assert.IsTrue(CraftBill.OnDuty("Steady Hands", "Medic", true, "Medic"));
            Assert.IsFalse(CraftBill.OnDuty("Field Medic", "Medic", false, "Medic"));
            Assert.IsFalse(CraftBill.OnDuty("Scrounger", "Scavenge", true, "Medic"));

            Assert.IsTrue(CraftBill.TryOf("ammo_9mm", out var ammo));
            Assert.AreEqual(CraftBill.Workbench, ammo.Station);
            Assert.AreEqual(3, CraftBill.ScrapDue(ammo.Scrap, true));
            Assert.AreEqual(4, CraftBill.ScrapDue(ammo.Scrap, false));
            Assert.AreEqual("Need a workbench", CraftBill.Block(ammo.Station, ammo.Skill, false, true));
            Assert.IsFalse(CraftBill.Afford(3, 0, 1, 0, 3, 0, 0, 0));
            Assert.IsTrue(CraftBill.Afford(3, 0, 1, 0, 3, 0, 1, 0));
            Assert.AreEqual("9mm (12)   scrap 3   chem 1", CraftBill.Line("9mm (12)", 3, 0, 1, 0));

            CraftBill.Salvage(1, false, out int cloth, out int chemicals, out int tape);
            Assert.AreEqual(1, cloth);
            Assert.AreEqual(1, chemicals);
            Assert.AreEqual(0, tape);
            CraftBill.Salvage(4, true, out cloth, out chemicals, out tape);
            Assert.AreEqual(2, cloth);
            Assert.AreEqual(0, chemicals);
            Assert.AreEqual(1, tape);
        }

        [Test]
        public void ARaidSwingsToANewSideAndAGuardTakesTheBreach()
        {
            Assert.AreEqual(0, RaidPlan.PhaseAt(0f, 75f));
            Assert.AreEqual(0, RaidPlan.PhaseAt(24f, 75f));
            Assert.AreEqual(1, RaidPlan.PhaseAt(25f, 75f));
            Assert.AreEqual(2, RaidPlan.PhaseAt(50f, 75f));
            Assert.AreEqual(2, RaidPlan.PhaseAt(80f, 75f));

            var first = RaidPlan.WaveAt(1, 0, 0);
            Assert.AreEqual("gate", first.Approach);
            Assert.AreEqual(6, first.Pressure);
            Assert.AreEqual(1.2f, first.Interval, 0.001f);
            var second = RaidPlan.WaveAt(1, 0, 1);
            Assert.AreEqual("alley", second.Approach);
            Assert.AreEqual(8, second.Pressure);
            Assert.AreEqual(1f, second.Interval, 0.001f);
            var third = RaidPlan.WaveAt(1, 0, 2);
            Assert.AreEqual("yard", third.Approach);
            Assert.AreEqual(10, third.Pressure);
            Assert.AreEqual(0.8f, third.Interval, 0.001f);
            Assert.AreEqual("yard", RaidPlan.WaveAt(1, 1, 1).Approach);
            Assert.AreEqual(0, RaidPlan.Reinforcements(0));
            Assert.AreEqual(3, RaidPlan.Reinforcements(1));
            Assert.AreEqual(4, RaidPlan.Reinforcements(2));

            Assert.AreEqual(1, RaidPlan.Pick(new[] { true, true, false }, new[] { false, true, false }, 0));
            Assert.AreEqual(2, RaidPlan.Pick(new[] { true, true, true }, new[] { true, false, true }, 1));
            Assert.AreEqual(1, RaidPlan.Pick(new[] { true, true, false }, new[] { false, false, false }, 1));
            Assert.AreEqual(-1, RaidPlan.Pick(new[] { false, false }, new[] { true, true }, 0));
            Assert.AreEqual(1, RaidPlan.Hurt(0));
            Assert.AreEqual(3, RaidPlan.Hurt(2));
            Assert.AreEqual(3, RaidPlan.Hurt(3));
        }

        [Test]
        public void AStreetDoorOpensARoomNinetyMetersEast()
        {
            DoorMap.Inside(-7.8f, 4f, out float insideX, out float insideZ);
            Assert.AreEqual(82.2f, insideX, 0.001f);
            Assert.AreEqual(4f, insideZ, 0.001f);
            DoorMap.Outside(insideX, insideZ, out float backX, out float backZ);
            Assert.AreEqual(-7.8f, backX, 0.001f);
            Assert.AreEqual(4f, backZ, 0.001f);
            Assert.IsFalse(DoorMap.IsInside(-7.8f));
            Assert.IsTrue(DoorMap.IsInside(insideX));
            Assert.AreEqual("Step inside", DoorMap.Prompt(false));
            Assert.AreEqual("Step outside", DoorMap.Prompt(true));

            DoorCross.Clear();
            DoorCross.Note(-7.8f, 4f, 82.2f, 4f, 10f);
            Assert.IsTrue(DoorCross.ShouldFollow(-1, -7.8f, 4f, 10f, out int crossed));
            Assert.IsFalse(DoorCross.ShouldFollow(crossed, -7.8f, 4f, 10.2f, out _));
            Assert.IsTrue(DoorCross.ShouldFollow(-1, 0.2f, 4f, 12.5f, out _));
            Assert.IsFalse(DoorCross.ShouldFollow(-1, 0.3f, 4f, 10f, out _));
            Assert.IsFalse(DoorCross.ShouldFollow(-1, -7.8f, 4f, 12.51f, out _));
            DoorCross.Slot(0, out float slotX, out float slotZ);
            Assert.AreEqual(-0.7f, slotX, 0.001f);
            Assert.AreEqual(0f, slotZ, 0.001f);
            DoorCross.Clear();
        }

        [Test]
        public void AColorblindNoiseMeterChangesShapeWithLoudness()
        {
            Assert.AreEqual(6f, NoiseCue.Height(0, 1f), 0.001f);
            Assert.AreEqual(6f, NoiseCue.Height(1, 0.5f), 0.001f);
            Assert.AreEqual(8f, NoiseCue.Height(2, 0f), 0.001f);
            Assert.AreEqual(13f, NoiseCue.Height(2, 0.5f), 0.001f);
            Assert.AreEqual(18f, NoiseCue.Height(2, 1f), 0.001f);
            Assert.AreEqual(8f, NoiseCue.Height(2, -1f), 0.001f);
            Assert.AreEqual(18f, NoiseCue.Height(2, 2f), 0.001f);

            Assert.AreEqual("", NoiseCue.Mark(0, 1f));
            Assert.AreEqual("", NoiseCue.Mark(3, 1f));
            Assert.AreEqual(".", NoiseCue.Mark(1, 0f));
            Assert.AreEqual("-", NoiseCue.Mark(1, 0.25f));
            Assert.AreEqual("=", NoiseCue.Mark(1, 0.55f));
            Assert.AreEqual("#", NoiseCue.Mark(1, 0.8f));
            Assert.AreEqual("#", NoiseCue.Mark(1, 4f));
            Assert.AreEqual("o", NoiseCue.Mark(2, -0.2f));
            Assert.AreEqual("^", NoiseCue.Mark(2, 0.549f));
            Assert.AreEqual("[]", NoiseCue.Mark(2, 0.799f));
            Assert.AreEqual("*", NoiseCue.Mark(2, 0.8f));
            Assert.AreNotEqual(NoiseCue.Mark(1, 0.9f), NoiseCue.Mark(2, 0.9f));
            Color health = HudPalette.Health(0);
            Assert.AreEqual(0.75f, health.r, 0.001f);
            Assert.AreEqual(0.2f, health.g, 0.001f);
            Assert.AreEqual(0.16f, health.b, 0.001f);
            Assert.AreEqual(health, HudPalette.Health(-1));
            Assert.AreEqual(health, HudPalette.Health(3));
            Color blue = HudPalette.Health(1);
            Assert.Greater(blue.b, blue.r);
            Color mono = HudPalette.Health(2);
            Assert.AreEqual(mono.r, mono.g, 0.001f);
            Assert.AreEqual(mono.g, mono.b, 0.001f);
            Color safe = HudPalette.Safe(0);
            Assert.AreEqual(0.35f, safe.r, 0.001f);
            Assert.AreEqual(0.62f, safe.g, 0.001f);
            Assert.AreEqual(0.38f, safe.b, 0.001f);
            Assert.Greater(HudPalette.Safe(1).r, HudPalette.Safe(1).b);
            Color warn = HudPalette.Warn(0);
            Assert.AreEqual(0.95f, warn.r, 0.001f);
            Assert.AreEqual(0.55f, warn.g, 0.001f);
            Assert.AreEqual(0.25f, warn.b, 0.001f);
            Color alarm = HudPalette.Alarm(0);
            Assert.AreEqual(0.95f, alarm.r, 0.001f);
            Assert.AreEqual(0.35f, alarm.g, 0.001f);
            Color ask = HudPalette.Ask(0);
            Assert.AreEqual(0.95f, ask.r, 0.001f);
            Assert.AreEqual(0.8f, ask.g, 0.001f);
            Assert.AreEqual("set.vision0", HudPalette.Name(0));
            Assert.AreEqual("set.vision1", HudPalette.Name(1));
            Assert.AreEqual("set.vision2", HudPalette.Name(2));
            Assert.AreEqual("Blue-yellow", Loc.T("set.vision1"));
            Assert.AreEqual("Azul-amarillo", Loc.T("set.vision1", "es"));
            Assert.AreEqual("Apagado", Loc.T("set.vision0", "es"));
            Assert.AreEqual("", NoiseCue.Mark(0, 1f));
        }

        [Test]
        public void AnInteriorAnnexStaysOpenThroughATwoMeterGap()
        {
            RoomPlan.Annex(82.2f, 4f, out float annexX, out float annexZ);
            Assert.AreEqual(90.2f, annexX, 0.001f);
            Assert.AreEqual(4f, annexZ, 0.001f);

            var opening = new RoomPlan.Piece[2];
            Assert.AreEqual(2, RoomPlan.Opening(82.2f, 4f, opening));
            Assert.IsFalse(RoomPlan.Covers(opening[0], 85.8f, 4f));
            Assert.IsFalse(RoomPlan.Covers(opening[1], 85.8f, 4f));
            Assert.IsTrue(RoomPlan.Covers(opening[0], opening[0].X, opening[0].Z));

            string[] kinds = { "clinic", "hospital", "warehouse", "station", "apartment", "storefront", "" };
            string[] names = { "RoomCot", "RoomCot", "RoomShelf", "RoomDesk", "RoomTable", "RoomCounter", "RoomCounter" };
            var dressed = new RoomPlan.Piece[8];
            for (int i = 0; i < kinds.Length; i++)
            {
                int count = RoomPlan.Dress(kinds[i], 82.2f, 4f, dressed);
                Assert.GreaterOrEqual(count, 5);
                bool found = false;
                bool floor = false;
                for (int p = 0; p < count; p++)
                {
                    if (dressed[p].Name == names[i]) found = true;
                    if (dressed[p].Name == "RoomFloor") floor = true;
                    Assert.IsFalse(RoomPlan.BlocksLane(dressed[p], 4f), kinds[i] + " " + dressed[p].Name);
                }
                Assert.IsTrue(found, kinds[i]);
                Assert.IsTrue(floor);
                Assert.IsTrue(RoomPlan.Covers(dressed[0], annexX, annexZ));
            }
        }

        [Test]
        public void AScreamStopsAtAWallAndTheWatchListsTheChase()
        {
            Assert.AreEqual(0f, HearGate.Perceived(0f, 10f, 1f, true, NoiseType.ZombieScream), 0.001f);
            Assert.AreEqual(1f, HearGate.Perceived(0f, 10f, 1f, false, NoiseType.ZombieScream), 0.001f);
            Assert.AreEqual(0.2f, HearGate.Perceived(8f, 10f, 1f, false, NoiseType.ZombieScream), 0.001f);
            Assert.AreEqual(0f, HearGate.Perceived(9.5f, 10f, 1f, false, NoiseType.ZombieScream), 0.001f);
            Assert.AreEqual(0f, HearGate.Perceived(11f, 10f, 1f, false, NoiseType.ZombieScream), 0.001f);
            Assert.AreEqual(0.09f, HearGate.Perceived(8f, 10f, 1f, true, NoiseType.GunshotLoud), 0.001f);
            Assert.AreEqual(0f, HearGate.Perceived(9f, 10f, 1f, true, NoiseType.GunshotLoud), 0.001f);
            Assert.AreEqual(0f, HearGate.Perceived(1f, 0f, 1f, false, NoiseType.ZombieScream), 0.001f);

            Assert.AreEqual("Chase  Player  1.3", AiWatch.Line("Chase", "Player", 1.26f));
            Assert.AreEqual("Chase  -  0.0", AiWatch.Line("Chase", "", -1f));
            AiWatch.Close();
            try
            {
                Assert.AreEqual("", AiWatch.Page(new[] { "Chase  Player  1.3" }, 6));
                AiWatch.Toggle();
                Assert.IsTrue(AiWatch.Open);
                Assert.AreEqual("watch", AiWatch.Page(null, 6));
                Assert.AreEqual("watch\nChase  Player  1.3", AiWatch.Page(new[] { "Chase  Player  1.3" }, 6));
                var many = new[] { "a", "b", "c", "d", "e", "f", "g" };
                Assert.AreEqual("watch\na\nb\nc\nd\ne\nf\n+", AiWatch.Page(many, 6));
            }
            finally
            {
                AiWatch.Close();
            }
        }

        [Test]
        public void AFlareBurnsForTwentySecondsAndPulsesTheSearch()
        {
            Assert.IsTrue(FlareClock.Lit(0f));
            Assert.IsTrue(FlareClock.Lit(19.9f));
            Assert.IsFalse(FlareClock.Lit(20f));
            Assert.IsFalse(FlareClock.Lit(-0.1f));
            Assert.IsTrue(FlareClock.PulseDue(-1f, 0f));
            Assert.IsFalse(FlareClock.PulseDue(0f, 0.4f));
            Assert.IsTrue(FlareClock.PulseDue(2.4f, 2.6f));
            Assert.IsFalse(FlareClock.PulseDue(19.9f, 20.1f));
            Assert.AreEqual(ItemUse.Flare, ItemCatalog.Find("flare").Use);
            Assert.AreEqual("Pulls a search for 20s", ItemBrief.Effect(ItemCatalog.Find("flare")));
        }

        [Test]
        public void APipeBombBlastsAnyoneInsideTheRadius()
        {
            Assert.IsTrue(PipeBlast.Inside(0f));
            Assert.IsTrue(PipeBlast.Inside(4.2f));
            Assert.IsFalse(PipeBlast.Inside(4.21f));
            Assert.IsTrue(PipeBlast.Inside(-1f));
            Assert.AreEqual(42f, PipeBlast.Damage, 0.001f);
            Assert.AreEqual(24f, PipeBlast.Noise, 0.001f);
            Assert.AreEqual(1.2f, PipeBlast.Fuse, 0.001f);
            Assert.AreEqual(ItemUse.Bomb, ItemCatalog.Find("pipe_bomb").Use);
            Assert.AreEqual(0.8f, ItemCatalog.Find("pipe_bomb").Weight, 0.001f);
            Assert.AreEqual("Blast on impact", ItemBrief.Effect(ItemCatalog.Find("pipe_bomb")));
            Assert.AreEqual("Estalla al impacto", Loc.T("unit.pipe_bomb", "es"));
            Assert.IsTrue(CraftBill.TryOf("pipe_bomb", out var bill));
            Assert.AreEqual(8, bill.Scrap);
            Assert.AreEqual(0, bill.Cloth);
            Assert.AreEqual(1, bill.Chemicals);
            Assert.AreEqual(1, bill.Tape);
            Assert.AreEqual(CraftBill.Workbench, bill.Station);
            Assert.AreEqual("", bill.Skill);
            Assert.AreEqual(7, CraftBill.ScrapDue(bill.Scrap, true));
            var street = LootTables.Roll("street", 2);
            Assert.AreEqual(7, street.Length);
            Assert.AreEqual("pipe_bomb", street[5].ItemId);
        }

        [Test]
        public void AStorageCrateRaisesTheCampRoom()
        {
            Assert.AreEqual(80, CampRoom.Room(0));
            Assert.AreEqual(160, CampRoom.Room(2));
            Assert.AreEqual(80, CampRoom.Room(-1));
            Assert.AreEqual(18, CampRoom.Bulk(4, 1, 1, 1, 1, 1));
            Assert.AreEqual(0, CampRoom.Bulk(-3, 0, 0, 0, 0, 0));
            Assert.AreEqual(10, CampRoom.Fit(70, CampRoom.Scrap, 20, 80));
            Assert.AreEqual(0, CampRoom.Fit(80, CampRoom.Scrap, 5, 80));
            Assert.AreEqual(1, CampRoom.Fit(76, CampRoom.Chemicals, 2, 80));
            Assert.AreEqual(0, CampRoom.Fit(78, CampRoom.Chemicals, 1, 80));
            Assert.AreEqual(0, CampRoom.Fit(0, CampRoom.Scrap, 0, 80));
            Assert.AreEqual(0, CampRoom.Fit(0, 0, 5, 80));
            Assert.AreEqual(10, GridBuilder.Cost(ModuleKind.Crate));
            Assert.AreEqual("Crate", Loc.T("camp.crate"));
            Assert.AreEqual("Caja", Loc.T("camp.crate", "es"));
            Assert.AreEqual("Almacén", Loc.T("camp.room", "es"));
        }

        [Test]
        public void ABuildSiteStaysQuietUntilTheHoursAreIn()
        {
            Assert.AreEqual(1, BuildSite.Need("Barricade"));
            Assert.AreEqual(4, BuildSite.Need("Farm"));
            Assert.AreEqual(5, BuildSite.Need("Turret"));
            Assert.AreEqual(2, BuildSite.Need(""));
            Assert.AreEqual(1, BuildSite.Shift(null, 50f));
            Assert.AreEqual(1, BuildSite.Shift("Steady Hands", 40f));
            Assert.AreEqual(2, BuildSite.Shift("Field Engineer", 40f));
            Assert.AreEqual(0, BuildSite.Shift("Steady Hands", 5f));
            Assert.IsTrue(BuildSite.Ready(0, 100));
            Assert.IsFalse(BuildSite.Ready(1, 100));
            Assert.IsFalse(BuildSite.Ready(0, 0));
            Assert.AreEqual(0.45f, BuildSite.Bulk(0), 0.001f);
            Assert.AreEqual(0.75f, BuildSite.Bulk(2), 0.001f);
            BuildSite.Work(1, 0, 2, 1, out int site, out int hours, out bool finished);
            Assert.AreEqual(1, site);
            Assert.AreEqual(1, hours);
            Assert.IsFalse(finished);
            BuildSite.Work(site, hours, 2, 1, out site, out hours, out finished);
            Assert.AreEqual(0, site);
            Assert.AreEqual(2, hours);
            Assert.IsTrue(finished);
            BuildSite.Work(0, 4, 2, 1, out site, out hours, out finished);
            Assert.AreEqual(0, site);
            Assert.AreEqual(4, hours);
            Assert.IsFalse(finished);
            Assert.AreEqual("Build", CampRoutine.Choose("Build", 80f, 80f, 60f, 0));
            Assert.AreEqual("I'll raise it.", CampRoutine.Bark("Build", 50f));
            Assert.AreEqual("Construir", Loc.Task("Build", "es"));
        }

        [Test]
        public void ATakedownReachesJustPastAnArmAndThenMakesASmallNoise()
        {
            Assert.AreEqual(1.2f, QuietKill.Reach, 0.001f);
            Assert.AreEqual(1.5f, QuietKill.Noise, 0.001f);
            Assert.AreEqual(1.1f, QuietKill.Windup, 0.001f);
            Assert.IsTrue(QuietKill.InReach(1.2f));
            Assert.IsFalse(QuietKill.InReach(1.21f));
            Assert.IsTrue(QuietKill.InReach(-0.2f));
            Assert.IsTrue(QuietKill.FromBehind(0.35f));
            Assert.IsFalse(QuietKill.FromBehind(0.34f));
            Assert.IsFalse(QuietKill.Lands(1.09f));
            Assert.IsTrue(QuietKill.Lands(1.1f));
            Assert.IsFalse(QuietKill.Lands(-1f));
            Assert.IsTrue(QuietKill.Victim(true, false, false, 1.2f, 0.35f));
            Assert.IsFalse(QuietKill.Victim(false, false, false, 1f, 1f));
            Assert.IsFalse(QuietKill.Victim(true, true, false, 1f, 1f));
            Assert.IsFalse(QuietKill.Victim(true, false, true, 1f, 1f));
            Assert.IsFalse(QuietKill.Victim(true, false, false, 1.21f, 1f));
            Assert.IsFalse(QuietKill.Victim(true, false, false, 1f, 0.34f));
        }

        [Test]
        public void AFloodlightBreaksACrouchInsideItsCircle()
        {
            Assert.AreEqual(1f, FloodBeam.Strength(0f, true), 0.001f);
            Assert.AreEqual(0.5f, FloodBeam.Strength(7f, true), 0.001f);
            Assert.AreEqual(0f, FloodBeam.Strength(14f, true), 0.001f);
            Assert.AreEqual(0f, FloodBeam.Strength(14.1f, true), 0.001f);
            Assert.AreEqual(0f, FloodBeam.Strength(0f, false), 0.001f);
            Assert.AreEqual(1f, FloodBeam.Strength(-2f, true), 0.001f);
            float lit = SpotRange.Exposure(true, false, false, 1f, FloodBeam.Strength(0f, true));
            Assert.IsTrue(SpotRange.Notices(5f, 16f, lit, true, 1f, 1f, 0f, 110f));
            Assert.IsTrue(SpotRange.Notices(12f, 16f, lit, true, 1f, 1f, 0f, 110f));
            Assert.IsFalse(SpotRange.Notices(13f, 16f, lit, true, 1f, 1f, 0f, 110f));
            float edge = SpotRange.Exposure(true, false, false, 1f, FloodBeam.Strength(14f, true));
            Assert.IsFalse(SpotRange.Notices(5f, 16f, edge, true, 1f, 1f, 0f, 110f));
            Assert.AreEqual(2, BuildSite.Need("Lamp"));
            Assert.AreEqual(13, GridBuilder.Cost(ModuleKind.Lamp));
            Assert.AreEqual("Foco", Loc.T("camp.lamp", "es"));
            Assert.AreEqual(2, YardFlood.Count);
            Assert.IsTrue(YardFlood.Lit(true));
            Assert.IsFalse(YardFlood.Lit(false));
            Assert.AreEqual(-1.6f, YardFlood.Local(0).x, 0.001f);
            Assert.AreEqual(3.2f, YardFlood.Local(0).y, 0.001f);
            Assert.AreEqual(1.6f, YardFlood.Local(1).x, 0.001f);
            Assert.AreEqual(55f, YardFlood.Aim(0).x, 0.001f);
            Assert.AreEqual(-35f, YardFlood.Aim(0).y, 0.001f);
            Assert.AreEqual(35f, YardFlood.Aim(1).y, 0.001f);
            Assert.AreEqual(FloodBeam.Radius, 14f, 0.001f);
            Assert.AreEqual(70f, YardFlood.Spread, 0.001f);
            Assert.AreEqual(3.4f, YardFlood.Intensity, 0.001f);
        }

        [Test]
        public void ACampfireTurnsRawFoodIntoAMeal()
        {
            CookPot.Serve(false, 4, 2, out int coldSpent, out int coldFood, out int coldMorale);
            Assert.AreEqual(0, coldSpent);
            Assert.AreEqual(1, coldFood);
            Assert.AreEqual(2, coldMorale);
            CookPot.Serve(true, 0, 2, out int emptySpent, out int emptyFood, out int emptyMorale);
            Assert.AreEqual(0, emptySpent);
            Assert.AreEqual(1, emptyFood);
            Assert.AreEqual(2, emptyMorale);
            CookPot.Serve(true, 4, 2, out int spent, out int food, out int morale);
            Assert.AreEqual(2, spent);
            Assert.AreEqual(6, food);
            Assert.AreEqual(8, morale);
            CookPot.Serve(true, 1, 2, out int shortSpent, out int shortFood, out int shortMorale);
            Assert.AreEqual(1, shortSpent);
            Assert.AreEqual(3, shortFood);
            Assert.AreEqual(8, shortMorale);
            CookPot.Serve(true, 3, 0, out int idleSpent, out int idleFood, out int idleMorale);
            Assert.AreEqual(0, idleSpent);
            Assert.AreEqual(0, idleFood);
            Assert.AreEqual(0, idleMorale);
            Assert.AreEqual(18, CampRoom.Bulk(4, 1, 1, 1, 1, 1));
            Assert.AreEqual(22, CampRoom.Bulk(4, 1, 1, 1, 1, 1, 2));
            Assert.AreEqual(1, BuildSite.Need("Campfire"));
            Assert.AreEqual(7, GridBuilder.Cost(ModuleKind.Campfire));
            Assert.AreEqual(12, ItemCatalog.Find("raw_food").Hunger);
            var street = LootTables.Roll("street", 2);
            Assert.AreEqual("raw_food", street[6].ItemId);
            Assert.AreEqual("Fogata", Loc.T("camp.fire", "es"));
            Assert.AreEqual("Comida cruda", Loc.Item("raw_food", "es"));
        }

        [Test]
        public void ABuilderMendsTheWorstBoardBeforeAWholeOne()
        {
            Assert.IsTrue(MendBoard.Needs(0, 40));
            Assert.IsFalse(MendBoard.Needs(1, 40));
            Assert.IsFalse(MendBoard.Needs(0, 0));
            Assert.IsFalse(MendBoard.Needs(0, 100));
            Assert.AreEqual(65, MendBoard.Mend(40, 1));
            Assert.AreEqual(100, MendBoard.Mend(80, 1));
            Assert.AreEqual(100, MendBoard.Mend(90, 2));
            Assert.AreEqual(0, MendBoard.Mend(0, 1));
            Assert.AreEqual(40, MendBoard.Mend(40, 0));
            Assert.AreEqual(0, MendBoard.Mend(-5, 1));
            Assert.AreEqual(1, MendBoard.Pick(new[] { 0, 0 }, new[] { 80, 40 }));
            Assert.AreEqual(1, MendBoard.Pick(new[] { 1, 0 }, new[] { 20, 90 }));
            Assert.AreEqual(-1, MendBoard.Pick(new[] { 0 }, new[] { 100 }));
            Assert.AreEqual(-1, MendBoard.Pick(null, new[] { 40 }));
            Assert.AreEqual("Reparar", Loc.T("camp.mend_module", "es"));
        }

        [Test]
        public void AClearShiftHaulsTwoBodiesAndTheRestCostMorale()
        {
            Assert.AreEqual(4, YardDead.Dropped(4, true));
            Assert.AreEqual(2, YardDead.Dropped(4, false));
            Assert.AreEqual(0, YardDead.Dropped(0, true));
            Assert.AreEqual(0, YardDead.Dropped(1, false));
            Assert.AreEqual(1, YardDead.Dropped(3, false));
            Assert.AreEqual(2, YardDead.Hands(70f));
            Assert.AreEqual(2, YardDead.Hands(30f));
            Assert.AreEqual(1, YardDead.Hands(20f));
            Assert.AreEqual(0, YardDead.Hands(5f));
            Assert.AreEqual(2, YardDead.Left(4, 2));
            Assert.AreEqual(4, YardDead.Left(4, 0));
            Assert.AreEqual(0, YardDead.Left(2, 5));
            Assert.AreEqual(0, YardDead.Left(-1, 1));
            Assert.AreEqual(12, YardDead.MoodHit(2));
            Assert.AreEqual(18, YardDead.MoodHit(4));
            Assert.AreEqual(0, YardDead.MoodHit(0));
            Assert.AreEqual("Clear", CampRoutine.Choose("Clear", 80f, 80f, 60f, 0));
            Assert.AreEqual("Cook", CampRoutine.Choose("Clear", 20f, 80f, 60f, 0));
            Assert.AreEqual("Medic", CampRoutine.Choose("Clear", 80f, 80f, 60f, 2));
            Assert.AreEqual("I'll haul them.", CampRoutine.Bark("Clear", 60f));

            var people = new List<ColonistDay>
            {
                new ColonistDay { id = "ada", morale = 60f, hunger = 90f, thirst = 90f, task = "Clear" }
            };
            int food = 0;
            int water = 0;
            ColonyDay.Simulate(people, ref food, ref water, false, false, "", 2);
            Assert.AreEqual(48f, people[0].morale);
            Assert.AreEqual("Clear", people[0].task);
            people[0].morale = 60f;
            people[0].hunger = 90f;
            people[0].thirst = 90f;
            ColonyDay.Simulate(people, ref food, ref water, false, false, "", 0);
            Assert.AreEqual(60f, people[0].morale);

            var data = new SaveGameData { bodies = 3 };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(data), out var loaded, out var error), error);
            Assert.AreEqual(3, loaded.bodies);
            Assert.IsTrue(SaveCodec.TryDeserialize("{\"schemaVersion\":1}", out var legacy, out var legacyError), legacyError);
            Assert.AreEqual(0, legacy.bodies);
            Assert.AreEqual("Retirar", Loc.T("task.clear", "es"));
            Assert.AreEqual("Cuerpos", Loc.T("camp.bodies", "es"));
            Assert.AreEqual("Los retiro.", Loc.T("bark.clear", "es"));
        }

        [Test]
        public void ARaisedBenchOpensThePrintedRecipes()
        {
            Assert.AreEqual("", CraftGate.PrintOf("bandage"));
            Assert.AreEqual("dressing", CraftGate.PrintOf("dressing"));
            Assert.AreEqual(1, CraftGate.TierOf("dressing"));
            Assert.AreEqual(2, CraftGate.TierOf("flare"));
            Assert.AreEqual(2, CraftGate.TierOf("radio_spare"));
            Assert.IsTrue(CraftGate.Open("bandage", 0, ""));
            Assert.IsFalse(CraftGate.Open("dressing", 1, ""));
            Assert.IsTrue(CraftGate.Open("dressing", 1, "dressing"));
            Assert.IsFalse(CraftGate.Open("flare", 1, "flare"));
            Assert.IsTrue(CraftGate.Open("flare", 2, "flare"));
            Assert.AreEqual("tier", CraftGate.Deny("repair_kit", 1, "repair"));
            Assert.AreEqual("print", CraftGate.Deny("repair_kit", 2, ""));
            Assert.AreEqual("dressing,flare", CraftGate.Learn("flare", "dressing"));
            Assert.AreEqual("dressing,flare", CraftGate.Learn("dressing,flare", "flare"));
            Assert.AreEqual("repair", CraftGate.Sheet("rail_yard"));
            Assert.AreEqual("wall", CraftGate.Sheet("police_station"));
            Assert.AreEqual("flare", CraftGate.Sheet("mall"));
            Assert.AreEqual("radio", CraftGate.Sheet("downtown_core"));
            Assert.AreEqual("", CraftGate.Sheet("ash_market"));
            Assert.IsFalse(CraftGate.Ordered(0));
            Assert.IsTrue(CraftGate.Ordered(1));
            Assert.IsTrue(CraftGate.Ordered(3));
            Assert.IsFalse(CraftGate.Ordered(4));
            Assert.AreEqual(0, CraftGate.Worked(1));
            Assert.AreEqual(2, CraftGate.Worked(3));
            CraftGate.Advance(1, 0, out int idle, out bool idleDone);
            Assert.AreEqual(1, idle);
            Assert.IsFalse(idleDone);
            CraftGate.Advance(1, 2, out int mid, out bool midDone);
            Assert.AreEqual(3, mid);
            Assert.IsFalse(midDone);
            CraftGate.Advance(3, 1, out int raised, out bool raisedDone);
            Assert.AreEqual(4, raised);
            Assert.IsTrue(raisedDone);
            CraftGate.Advance(0, 2, out int quiet, out bool quietDone);
            Assert.AreEqual(0, quiet);
            Assert.IsFalse(quietDone);
            Assert.AreEqual(90, CraftGate.MendGenerator(40));
            Assert.AreEqual(100, CraftGate.MendGenerator(80));
            Assert.AreEqual(0, CraftGate.MendGenerator(0));
            Assert.AreEqual(70, CraftGate.BraceWall(30));
            Assert.AreEqual(100, CraftGate.BraceWall(90));
            Assert.AreEqual(1, CraftGate.PickWorn(new[] { 100, 40, 70 }));
            Assert.AreEqual(-1, CraftGate.PickWorn(new[] { 100, 0 }));
            Assert.AreEqual(-1, CraftGate.PickWorn(null));
            Assert.AreEqual(16, CraftGate.UpgradeScrap);
            Assert.AreEqual(2, CraftGate.UpgradeCloth);
            Assert.AreEqual(2, CraftGate.UpgradeTape);
            Assert.AreEqual(3, CraftGate.Hours);
            Assert.AreEqual(14, CraftingBench.Priced(16, true, 2));
            Assert.AreEqual(15, CraftingBench.Priced(16, true, 1));
            Assert.AreEqual(1, CraftingBench.Priced(2, true, 2));
            Assert.IsTrue(CraftBill.TryOf("dressing", out var dressing));
            Assert.AreEqual(2, dressing.Cloth);
            int dressings = 0;
            foreach (var recipe in CraftingBench.Recipes)
            {
                if (recipe.Id == "dressing") dressings = recipe.OutputCount;
            }
            Assert.AreEqual(3, dressings);
            Assert.IsFalse(CampaignBoard.PartsComplete("downtown,hospital"));
            Assert.IsTrue(CampaignBoard.PartsComplete(CampaignBoard.AddPart("downtown,hospital", "spare")));
            Assert.IsTrue(CampaignBoard.PartsComplete("downtown,hospital,police"));
            var data = new SaveGameData { prints = "dressing,flare" };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(data), out var loaded, out var error), error);
            Assert.AreEqual("dressing,flare", loaded.prints);
            Assert.IsTrue(SaveCodec.TryDeserialize("{\"schemaVersion\":1}", out var legacy, out var legacyError), legacyError);
            Assert.AreEqual("", legacy.prints ?? "");
            Assert.AreEqual(0, legacy.modules == null || legacy.modules.Length == 0 ? 0 : legacy.modules[0].job);
            Assert.AreEqual("Subir el banco", Loc.T("camp.bench_raise", "es"));
            Assert.AreEqual("Hace falta un banco de nivel 2", Loc.T("gate.tier", "es"));
            Assert.AreEqual("Plano de bengala", Loc.Item("print_flare", "es"));
            Assert.AreEqual("Bengala", Loc.T("recipe.flare", "es"));
        }

        [Test]
        public void AColonistStandsBesideTheModuleForTheTask()
        {
            var kinds = new[] { "Workbench", "Campfire", "Campfire", "Cot" };
            var sites = new[] { 0, 1, 0, 0 };
            var integrity = new[] { 100, 100, 80, 100 };
            var jobs = new[] { 2, 0, 0, 0 };
            Assert.AreEqual(2, CampPost.Pick("Cook", kinds, sites, integrity, jobs));
            Assert.AreEqual(1, CampPost.Pick("Build", kinds, sites, integrity, jobs));
            Assert.AreEqual(3, CampPost.Pick("Medic", kinds, sites, integrity, jobs));
            Assert.AreEqual(-1, CampPost.Pick("Guard", kinds, sites, integrity, jobs));
            Assert.AreEqual(-1, CampPost.Pick("Clear", kinds, sites, integrity, jobs));
            Assert.AreEqual(-1, CampPost.Pick("Cook", null, sites, integrity, jobs));
            var raised = new[] { "Workbench" };
            Assert.AreEqual(0, CampPost.Pick("Build", raised, new[] { 0 }, new[] { 100 }, new[] { 2 }));
            Assert.AreEqual(-1, CampPost.Pick("Build", raised, new[] { 0 }, new[] { 100 }, new[] { 0 }));
            Assert.AreEqual(-1, CampPost.Pick("Build", raised, new[] { 0 }, new[] { 100 }, new[] { 4 }));
            CampPost.Place("Cook", 0, 4f, 6f, true, out float cookX, out float cookZ);
            Assert.AreEqual(4f, cookX, 0.001f);
            Assert.AreEqual(6f, cookZ, 0.001f);
            CampPost.Place("Clear", 0, 0f, 0f, false, out float clearX, out float clearZ);
            Assert.AreEqual(-7.1f, clearX, 0.001f);
            Assert.AreEqual(-8f, clearZ, 0.001f);
            CampPost.Place("Guard", 0, 0f, 0f, false, out float guardX, out float guardZ);
            Assert.AreEqual(-9.1f, guardX, 0.001f);
            Assert.AreEqual(-12f, guardZ, 0.001f);
            CampPost.Place("Build", 1, 2f, 3f, true, out float buildX, out float buildZ);
            Assert.AreEqual(2.55f, buildX, 0.001f);
            Assert.AreEqual(3.4f, buildZ, 0.001f);
        }

        [Test]
        public void ALitApproachLosesItsSneak()
        {
            RaidPlan.AnchorOf("gate", out float gateX, out float gateZ);
            var on = new[] { gateX, gateX, gateX + 20f };
            var z = new[] { gateZ, gateZ, gateZ };
            var sites = new[] { 0, 0, 0 };
            var integrity = new[] { 100, 100, 100 };
            Assert.AreEqual(0, FloodBeam.Covering("gate", on, z, sites, integrity, false));
            Assert.AreEqual(2, FloodBeam.Covering("gate", on, z, sites, integrity, true));
            Assert.AreEqual(1, FloodBeam.Covering("gate", on, z, new[] { 1, 0, 0 }, integrity, true));
            Assert.AreEqual(1, FloodBeam.Covering("gate", on, z, sites, new[] { 0, 100, 100 }, true));
            Assert.AreEqual(0, FloodBeam.Covering("gate", null, z, sites, integrity, true));
            var edge = new[] { gateX };
            var far = new[] { gateZ + FloodBeam.Radius };
            Assert.AreEqual(0, FloodBeam.Covering("gate", edge, far, new[] { 0 }, new[] { 100 }, true));
            var inside = new[] { gateZ + FloodBeam.Radius - 0.1f };
            Assert.AreEqual(1, FloodBeam.Covering("gate", edge, inside, new[] { 0 }, new[] { 100 }, true));
            Assert.AreEqual(6, FloodBeam.ApproachPressure(8, 1));
            Assert.AreEqual(4, FloodBeam.ApproachPressure(8, 2));
            Assert.AreEqual(4, FloodBeam.ApproachPressure(8, 3));
            Assert.AreEqual(1, FloodBeam.ApproachPressure(2, 2));
            Assert.AreEqual(1.55f, FloodBeam.ApproachGap(1.2f, 1), 0.001f);
            Assert.AreEqual(1.9f, FloodBeam.ApproachGap(1.2f, 2), 0.001f);
            Assert.AreEqual(3.1f, FloodBeam.ApproachGap(2.9f, 2), 0.001f);
            Assert.AreEqual("Las luces los descubren", Loc.T("camp.lamps", "es"));
        }

        [Test]
        public void ALeaderCalmsTheFeudAndRawFoodSoursTheMeal()
        {
            int food = 2;
            int raw = 2;
            float hunger = 40f;
            float morale = 70f;
            MealTable.Serve(ref food, ref raw, ref hunger, ref morale);
            Assert.AreEqual(1, food);
            Assert.AreEqual(2, raw);
            Assert.AreEqual(88f, hunger);
            Assert.AreEqual(70f, morale);
            food = 0;
            hunger = 40f;
            MealTable.Serve(ref food, ref raw, ref hunger, ref morale);
            Assert.AreEqual(1, raw);
            Assert.AreEqual(62f, hunger);
            Assert.AreEqual(66f, morale);
            Assert.AreEqual(-5f, MealTable.RestMood(false, true));
            Assert.AreEqual(1f, MealTable.RestMood(true, true));
            Assert.AreEqual(6f, MealTable.RestMood(true, false));
            Assert.AreEqual(6, MealTable.FeudShift(6, false));
            Assert.AreEqual(3, MealTable.FeudShift(6, true));
            Assert.IsTrue(MealTable.Argument(true, 2, false));
            Assert.IsFalse(MealTable.Argument(true, 2, true));
            Assert.IsFalse(MealTable.Argument(true, 1, false));
            Assert.IsFalse(KinBoard.Quarrel(null, false));
            var cold = new List<ColonistDay>
            {
                new ColonistDay { id = "ada", task = "Rest", kin = "ben:-20", morale = 50f, hunger = 90f, thirst = 90f },
                new ColonistDay { id = "ben", task = "Scavenge", morale = 50f, hunger = 90f, thirst = 90f }
            };
            Assert.IsTrue(KinBoard.Quarrel(cold, false));
            Assert.IsFalse(KinBoard.Quarrel(cold, true));
            cold[0].kin = "ben:-19";
            Assert.IsFalse(KinBoard.Quarrel(cold, false));
            cold[0].kin = "ben:-20";
            int coldFood = 0;
            int coldWater = 0;
            var coldNotes = ColonyDay.Simulate(cold, ref coldFood, ref coldWater, false, false, "");
            Assert.Contains("argument", coldNotes);
            Assert.AreEqual(6, MealTable.FeudShift(6, false));
            Assert.AreEqual(3, MealTable.FeudShift(6, true));

            var ward = new List<ColonistDay>
            {
                new ColonistDay { id = "v", trait = "Volatile", task = "Rest", morale = 60f, hunger = 40f, thirst = 80f, opinion = 18 },
                new ColonistDay { id = "lead", task = "Lead", leader = true, morale = 70f, hunger = 90f, thirst = 90f, opinion = 20 }
            };
            food = 0;
            raw = 2;
            int water = 0;
            var notes = ColonyDay.Simulate(ward, ref food, ref water, true, false, "", 0, ref raw);
            Assert.AreEqual(0, raw);
            Assert.AreEqual(15, ward[0].opinion);
            Assert.AreEqual(57f, ward[0].morale);
            Assert.IsFalse(System.Array.Exists(notes, note => note == "argument"));

            var idle = new List<ColonistDay>
            {
                new ColonistDay { id = "ada", task = "Rest", morale = 50f, hunger = 90f, thirst = 90f }
            };
            food = 0;
            raw = 0;
            water = 0;
            ColonyDay.Simulate(idle, ref food, ref water, true, false, "", 0, ref raw);
            Assert.AreEqual(51f, idle[0].morale);
            idle[0].morale = 50f;
            idle[0].hunger = 90f;
            idle[0].thirst = 90f;
            idle[0].injury = 2;
            ColonyDay.Simulate(idle, ref food, ref water, true, false, "", 0, ref raw);
            Assert.AreEqual(56f, idle[0].morale);
            Assert.AreEqual(1, idle[0].injury);
        }

        [Test]
        public void AWatchtowerWarnsBeforeTheRaidStarts()
        {
            Assert.AreEqual(0f, RaidWarn.Seconds(0, 3));
            Assert.AreEqual(0f, RaidWarn.Seconds(-1, 1));
            Assert.AreEqual(8f, RaidWarn.Seconds(1, 0));
            Assert.AreEqual(8f, RaidWarn.Seconds(1, -2));
            Assert.AreEqual(14f, RaidWarn.Seconds(1, 1));
            Assert.AreEqual(16f, RaidWarn.Seconds(1, 2));
            Assert.AreEqual(22f, RaidWarn.Seconds(2, 1));
            Assert.AreEqual(28f, RaidWarn.Seconds(3, 1));
            Assert.AreEqual(28f, RaidWarn.Seconds(5, 4));
            Assert.AreEqual("La torre los ve", Loc.T("camp.warn", "es"));
        }

        [Test]
        public void AFarmFeedsTheCampAfterThreeMornings()
        {
            var plots = new[]
            {
                new CampYield.Plot { Kind = "Farm", Age = 0, Integrity = 100 },
                new CampYield.Plot { Kind = "Purifier", Age = 0, Integrity = 100 },
                new CampYield.Plot { Kind = "Water", Age = 0, Integrity = 100 },
                new CampYield.Plot { Kind = "Farm", Age = 9, Integrity = 0 }
            };
            CampYield.Produce(plots, false, out int earlyFood, out int dryWater);
            Assert.AreEqual(0, earlyFood);
            Assert.AreEqual(3, dryWater);
            CampYield.Produce(plots, true, out _, out int wetWater);
            Assert.AreEqual(4, wetWater);

            for (int morning = 0; morning < 2; morning++) CampYield.Advance(plots);
            CampYield.Produce(plots, false, out int waiting, out _);
            Assert.AreEqual(0, waiting);
            Assert.AreEqual(2, plots[0].Age);
            CampYield.Advance(plots);
            CampYield.Produce(plots, false, out int harvest, out _);
            Assert.AreEqual(2, harvest);
            Assert.AreEqual(3, plots[0].Age);
            Assert.AreEqual(9, plots[3].Age);

            Assert.AreEqual(6, CampYield.RaidPressure(6, false, 0));
            Assert.AreEqual(8, CampYield.RaidPressure(6, true, 0));
            Assert.AreEqual(3, CampYield.RaidPressure(6, false, 3));
            Assert.AreEqual(5, CampYield.RaidPressure(6, true, 3));
            Assert.AreEqual(1, CampYield.RaidPressure(2, false, 8));
            Assert.AreEqual(18, GridBuilder.Cost(ModuleKind.Farm));
            Assert.AreEqual(15, GridBuilder.Cost(ModuleKind.Purifier));
        }

        [Test]
        public void ATurretFiresWhenTheGeneratorIsUpAndARoundIsReady()
        {
            Assert.IsFalse(TurretBeat.Ready(0.84f, true, 1, 4));
            Assert.IsTrue(TurretBeat.Ready(0.85f, true, 1, 1));
            Assert.IsFalse(TurretBeat.Ready(2f, false, 1, 5));
            Assert.IsFalse(TurretBeat.Ready(2f, true, 0, 5));
            Assert.IsFalse(TurretBeat.Ready(2f, true, 1, 0));
            Assert.AreEqual(1, TurretBeat.Pick(new[] { 30f, 10f, 22f }, TurretBeat.Range));
            Assert.AreEqual(0, TurretBeat.Pick(new[] { 22f }, TurretBeat.Range));
            Assert.AreEqual(-1, TurretBeat.Pick(new[] { 22.1f }, TurretBeat.Range));
            Assert.AreEqual(-1, TurretBeat.Pick(null, TurretBeat.Range));
            Assert.AreEqual(2, TurretBeat.Prefer(new[] { false, false, true }, new[] { 0, 4, 2 }));
            Assert.AreEqual(1, TurretBeat.Prefer(new[] { false, false }, new[] { 0, 3 }));
            Assert.AreEqual(-1, TurretBeat.Prefer(null, new[] { 0, 0 }));
            Assert.AreEqual(22, GridBuilder.Cost(ModuleKind.Turret));
            Assert.IsFalse(TurretBeat.Fed(true, 1, 2, 0));
            Assert.IsFalse(TurretBeat.Fed(false, 1, 2, 4));
            Assert.IsFalse(TurretBeat.Fed(true, 0, 2, 4));
            Assert.IsFalse(TurretBeat.Fed(true, 1, 1, 4));
            Assert.IsTrue(TurretBeat.Fed(true, 1, 2, 1));
            Assert.AreEqual(1, TurretBeat.Draw(4));
            Assert.AreEqual(1, TurretBeat.Draw(1));
            Assert.AreEqual(0, TurretBeat.Draw(0));
            Assert.AreEqual(0, TurretBeat.Draw(-2));
            Assert.AreEqual("La torreta quiere un banco de nivel 2", Loc.T("camp.turret_tier", "es"));
        }

        [Test]
        public void SpikesCutTheNearestZombieAndThenWearDown()
        {
            Assert.IsFalse(TrapHit.Due(0.59f));
            Assert.IsTrue(TrapHit.Due(0.6f));
            Assert.IsFalse(TrapHit.Due(-1f));
            Assert.AreEqual(2, TrapHit.Victim(new[] { 2f, 1.4f, 0.2f }, TrapHit.Radius));
            Assert.AreEqual(-1, TrapHit.Victim(new[] { 1.41f }, TrapHit.Radius));
            Assert.AreEqual(-1, TrapHit.Victim(null, TrapHit.Radius));
            Assert.AreEqual(92, TrapHit.WearDown(100, TrapHit.Wear));
            Assert.AreEqual(0, TrapHit.WearDown(8, TrapHit.Wear));
            Assert.AreEqual(0, TrapHit.WearDown(3, TrapHit.Wear));
            Assert.AreEqual(8, GridBuilder.Cost(ModuleKind.Spikes));
        }

        [Test]
        public void OilStaysDarkUntilFireReachesIt()
        {
            Assert.IsFalse(OilBurn.Burning(-1f, 0f));
            Assert.IsTrue(OilBurn.Burning(0f, 0f));
            Assert.IsTrue(OilBurn.Burning(0f, 7.9f));
            Assert.IsFalse(OilBurn.Burning(0f, 8f));
            Assert.IsFalse(OilBurn.Burning(5f, 4f));
            Assert.IsTrue(OilBurn.Ignites(3.5f, 0f));
            Assert.IsFalse(OilBurn.Ignites(0f, 3.51f));
            Assert.AreEqual(2, OilBurn.Victim(new[] { 3f, 2.2f, 0.4f }));
            Assert.AreEqual(-1, OilBurn.Victim(new[] { 2.21f }));
            Assert.AreEqual(-1, OilBurn.Victim(null));
            Assert.AreEqual(9, GridBuilder.Cost(ModuleKind.Oil));
        }

        [Test]
        public void GuardsOnDutyFireTogetherDuringARaid()
        {
            Assert.IsFalse(GuardVolley.Ready(1.39f, 1));
            Assert.IsTrue(GuardVolley.Ready(1.4f, 1));
            Assert.IsFalse(GuardVolley.Ready(3f, 0));
            Assert.IsFalse(GuardVolley.Ready(-1f, 2));
            Assert.AreEqual(0, GuardVolley.Crew(0));
            Assert.AreEqual(0, GuardVolley.Crew(-2));
            Assert.AreEqual(2, GuardVolley.Crew(2));
            Assert.AreEqual(3, GuardVolley.Crew(5));
            Assert.AreEqual(16f, GuardVolley.Hit(2), 0.001f);
            Assert.AreEqual(24f, GuardVolley.Hit(4), 0.001f);
            Assert.AreEqual(2, GuardVolley.Pick(new[] { 20f, 16f, 4f }));
            Assert.AreEqual(0, GuardVolley.Pick(new[] { 16f }));
            Assert.AreEqual(-1, GuardVolley.Pick(new[] { 16.1f }));
            Assert.AreEqual(-1, GuardVolley.Pick(null));
        }

        [Test]
        public void GuardsSpendStoredRoundsWhenTheyFire()
        {
            Assert.AreEqual(2, GuardVolley.Rounds(2, 5));
            Assert.AreEqual(3, GuardVolley.Rounds(4, 10));
            Assert.AreEqual(1, GuardVolley.Rounds(3, 1));
            Assert.AreEqual(0, GuardVolley.Rounds(2, 0));
            Assert.AreEqual(0, GuardVolley.Rounds(0, 4));
            Assert.AreEqual(0, GuardVolley.Rounds(2, -3));
            Assert.AreEqual(8f, GuardVolley.Fired(3, 1), 0.001f);
            Assert.AreEqual(16f, GuardVolley.Fired(2, 5), 0.001f);
            Assert.AreEqual(0f, GuardVolley.Fired(2, 0), 0.001f);
            Assert.AreEqual(2, GuardVolley.Brought(false));
            Assert.AreEqual(4, GuardVolley.Brought(true));

            var data = new SaveGameData { rounds = 6 };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(data), out var loaded, out var error), error);
            Assert.AreEqual(6, loaded.rounds);
            Assert.IsTrue(SaveCodec.TryDeserialize("{\"schemaVersion\":1}", out var legacy, out var legacyError), legacyError);
            Assert.AreEqual(0, legacy.rounds);
            Assert.AreEqual("Balas", Loc.T("camp.rounds", "es"));
        }

        [Test]
        public void TheGeneratorKeepsItsFuelOnTheExistingSave()
        {
            Assert.IsTrue(FuelTank.Lit(true, 0.1f));
            Assert.IsFalse(FuelTank.Lit(true, 0f));
            Assert.IsFalse(FuelTank.Lit(false, 10f));
            Assert.AreEqual(10f, FuelTank.Drink(10f, 200f, false, 0.8f), 0.001f);
            Assert.AreEqual(10f, FuelTank.Drink(10f, 200f, true, 0.45f), 0.001f);
            Assert.AreEqual(5f, FuelTank.Drink(10f, 200f, true, 0.46f), 0.001f);
            Assert.AreEqual(0f, FuelTank.Drink(1f, 400f, true, 1f), 0.001f);
            Assert.AreEqual(10f, FuelTank.Pour(2f, 8f), 0.001f);
            Assert.AreEqual(2f, FuelTank.Pour(2f, 0f), 0.001f);
            Assert.AreEqual(100, FuelTank.Pack(10f));
            Assert.AreEqual(45, FuelTank.Pack(4.5f));
            Assert.AreEqual(4.5f, FuelTank.Unpack(45, 1), 0.001f);
            Assert.AreEqual(0f, FuelTank.Unpack(0, 1), 0.001f);
            Assert.AreEqual(10f, FuelTank.Unpack(0, 0), 0.001f);
            Assert.AreEqual("4.5", FuelTank.Label(4.5f));

            var data = new SaveGameData { fuel = 45, fuelSet = 1 };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(data), out var loaded, out var error), error);
            Assert.AreEqual(45, loaded.fuel);
            Assert.AreEqual(1, loaded.fuelSet);
            Assert.IsTrue(SaveCodec.TryDeserialize("{\"schemaVersion\":1}", out var legacy, out var legacyError), legacyError);
            Assert.AreEqual(0, legacy.fuel);
            Assert.AreEqual(0, legacy.fuelSet);
            Assert.AreEqual(FuelTank.Start, FuelTank.Unpack(legacy.fuel, legacy.fuelSet), 0.001f);
            Assert.AreEqual("Combustible", Loc.T("camp.fuel", "es"));
        }

        [Test]
        public void ALoudDayOrARunningGeneratorPullsAnOddNight()
        {
            Assert.IsTrue(RaidCall.Likely(2, 0, false, 0, false, 0, 2));
            Assert.IsFalse(RaidCall.Likely(2, 8, false, 9, true, 0, 3));
            Assert.IsFalse(RaidCall.Likely(1, 0, false, 9, true, 0, 3));
            Assert.IsTrue(RaidCall.Likely(1, 0, true, 0, false, 5, 1));
            Assert.IsFalse(RaidCall.Likely(3, 0, false, 0, false, 0, 2));
            Assert.IsTrue(RaidCall.Likely(3, 0, false, 3, false, 0, 2));
            Assert.IsFalse(RaidCall.Likely(3, 0, false, 3, false, 2, 2));
            Assert.IsTrue(RaidCall.Likely(3, 0, false, 0, true, 0, 2));
            Assert.IsFalse(RaidCall.Likely(3, 0, false, 0, true, 2, 2));
            Assert.IsTrue(RaidCall.Likely(3, 0, false, 1, false, 0, 3));
            Assert.IsFalse(RaidCall.Likely(3, 0, false, 5, false, 0, 1));
            Assert.IsTrue(RaidCall.Likely(3, 0, false, 6, false, 0, 1));
            Assert.IsFalse(RaidCall.Likely(3, 0, false, 0, true, 0, 1));
            Assert.IsTrue(RaidCall.Likely(3, 0, false, 3, false, 2, 3));
            Assert.IsFalse(RaidCall.Likely(3, 0, false, 3, false, 3, 3));
            Assert.IsTrue(RaidCall.Likely(3, 0, false, 0, true, 2, 3));
            Assert.IsTrue(RaidCall.Likely(3, 0, false, 3, false, 0, 0));
            Assert.IsTrue(RaidCall.Likely(3, 0, true, 0, false, 9, 1));

            RaidPlan.AnchorOf("gate", out float gx, out float gz);
            Assert.AreEqual(2, RaidCall.Hear(0, gx, gz, true, true));
            Assert.AreEqual(1, RaidCall.Hear(0, gx + 10f, gz, true, false));
            Assert.AreEqual(0, RaidCall.Hear(0, gx + 18.1f, gz, true, true));
            Assert.AreEqual(4, RaidCall.Hear(4, gx + 40f, gz, false, true));
            Assert.AreEqual(24, RaidCall.Hear(23, gx, gz, true, true));
            Assert.AreEqual(0, RaidCall.Carry(6, 2, 3));
            Assert.AreEqual(6, RaidCall.Carry(6, 2, 2));
            Assert.AreEqual(0, RaidCall.Carry(-2, 1, 1));

            var data = new SaveGameData { shots = 4 };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(data), out var loaded, out var error), error);
            Assert.AreEqual(4, loaded.shots);
            Assert.IsTrue(SaveCodec.TryDeserialize("{\"schemaVersion\":1}", out var legacy, out var legacyError), legacyError);
            Assert.AreEqual(0, legacy.shots);
            Assert.AreEqual("Disparos", Loc.T("camp.shots", "es"));
        }

        [Test]
        public void DuskHoldsTheWatchUntilTheRaidIsMet()
        {
            Assert.IsFalse(RaidWatch.Night(19.9f));
            Assert.IsTrue(RaidWatch.Night(20f));
            Assert.IsTrue(RaidWatch.Night(23.5f));
            Assert.IsTrue(RaidWatch.Night(0f));
            Assert.IsTrue(RaidWatch.Night(4.9f));
            Assert.IsFalse(RaidWatch.Night(5f));
            Assert.IsFalse(RaidWatch.Night(12f));
            Assert.IsFalse(RaidWatch.Night(-1f));
            Assert.IsTrue(RaidWatch.Crosses(22f, 0f));
            Assert.IsFalse(RaidWatch.Crosses(8f, 0f));
            Assert.IsFalse(RaidWatch.Crosses(12f, 6f));
            Assert.IsTrue(RaidWatch.Crosses(18.5f, 6f));
            Assert.IsFalse(RaidWatch.Crosses(19f, 0.5f));
            Assert.IsTrue(RaidWatch.Crosses(19f, 1f));
            Assert.IsTrue(RaidWatch.Crosses(16f, 8f));
            Assert.AreEqual("El anochecer los trae", Loc.T("camp.dusk", "es"));
        }

        [Test]
        public void ABruteBreachKillsADefenderAlreadyAtThreeWounds()
        {
            Assert.IsFalse(RaidBreach.Brute(0));
            Assert.IsFalse(RaidBreach.Brute(1));
            Assert.IsTrue(RaidBreach.Brute(2));
            Assert.IsTrue(RaidBreach.Brute(4));
            Assert.IsFalse(RaidBreach.Brute(-1));
            Assert.AreEqual(6, RaidBreach.Blow(6, false));
            Assert.AreEqual(1, RaidBreach.Blow(0, false));
            Assert.AreEqual(1, RaidBreach.Blow(-4, false));
            Assert.AreEqual(12, RaidBreach.Blow(6, true));
            Assert.AreEqual(2, RaidBreach.Blow(0, true));
            Assert.AreEqual(40, RaidBreach.Blow(30, true));

            Assert.AreEqual(1, RaidBreach.Wound(0, true, out bool fallen));
            Assert.IsFalse(fallen);
            Assert.AreEqual(3, RaidBreach.Wound(2, true, out fallen));
            Assert.IsFalse(fallen);
            Assert.AreEqual(3, RaidBreach.Wound(3, true, out fallen));
            Assert.IsTrue(fallen);
            Assert.AreEqual(1, RaidBreach.Wound(1, false, out fallen));
            Assert.IsFalse(fallen);
            Assert.AreEqual(3, RaidPlan.Hurt(3));
            Assert.AreEqual("cayó", Loc.T("camp.fell", "es"));
            Assert.AreEqual("está herido", Loc.T("camp.hit", "es"));
            Assert.AreEqual("Un bruto está en las tablas", Loc.T("camp.brute", "es"));
        }

        [Test]
        public void PressingAmmoAlsoStocksTheCamp()
        {
            Assert.AreEqual(0, AmmoPress.Rounds("bandage", 1));
            Assert.AreEqual(0, AmmoPress.Rounds(null, 1));
            Assert.AreEqual(0, AmmoPress.Rounds("ammo_rifle", 0));
            Assert.AreEqual(4, AmmoPress.Rounds("ammo_9mm", 1));
            Assert.AreEqual(6, AmmoPress.Rounds("ammo_shells", 1));
            Assert.AreEqual(8, AmmoPress.Rounds("ammo_rifle", 1));
            Assert.AreEqual(16, AmmoPress.Rounds("ammo_rifle", 2));
            Assert.AreEqual(24, AmmoPress.Rounds("ammo_rifle", 5));
            Assert.AreEqual("Prensadas", Loc.T("camp.press", "es"));
        }

        [Test]
        public void AnEmptyCampIsOverAndBoardsBlockTheWalk()
        {
            Assert.IsTrue(CampEnd.Wiped(0));
            Assert.IsTrue(CampEnd.Wiped(-1));
            Assert.IsFalse(CampEnd.Wiped(1));
            Assert.IsFalse(CampEnd.Wiped(4));
            Assert.IsTrue(StreetNav.HonorObstacles);
            Assert.AreEqual("El campamento cayó", Loc.T("camp.wiped", "es"));
        }

        [Test]
        public void ABreachSpillsTheStores()
        {
            Assert.AreEqual(0, RaidSpoil.Scrap(false, 40));
            Assert.AreEqual(0, RaidSpoil.Scrap(true, 0));
            Assert.AreEqual(3, RaidSpoil.Scrap(true, 3));
            Assert.AreEqual(4, RaidSpoil.Scrap(true, 8));
            Assert.AreEqual(10, RaidSpoil.Scrap(true, 40));
            Assert.AreEqual(12, RaidSpoil.Scrap(true, 80));
            Assert.AreEqual(0, RaidSpoil.Meals(false, 9));
            Assert.AreEqual(0, RaidSpoil.Meals(true, 0));
            Assert.AreEqual(1, RaidSpoil.Meals(true, 2));
            Assert.AreEqual(2, RaidSpoil.Meals(true, 6));
            Assert.AreEqual(3, RaidSpoil.Meals(true, 12));
            Assert.AreEqual("Se derramaron las reservas", Loc.T("camp.spoiled", "es"));
        }

        [Test]
        public void ARaidStepsPastTheBoardsAndChewsTheWeakOne()
        {
            RaidDrop.Point("gate", 0, 1, out float x, out float z);
            Assert.AreEqual(-10.8f, x, 0.05f);
            Assert.AreEqual(-14.4f, z, 0.05f);
            RaidDrop.Point("alley", 0, 1, out float alleyX, out float alleyZ);
            Assert.Less(alleyX, -20f);
            Assert.Less(alleyZ, -12f);
            Assert.AreEqual(-1, BoardBite.Assign(0, 0));
            Assert.AreEqual(0, BoardBite.Assign(0, 2));
            Assert.AreEqual(1, BoardBite.Assign(1, 2));
            Assert.AreEqual(0, BoardBite.Assign(2, 2));
            var integrity = new[] { 80, 20, 50 };
            var order = new int[3];
            BoardBite.Rank(integrity, order);
            Assert.AreEqual(1, order[0]);
            Assert.AreEqual(2, order[1]);
            Assert.AreEqual(0, order[2]);
            Assert.IsTrue(BoardBite.InReach(-6f, -8f, -6f, -9.5f));
            Assert.IsFalse(BoardBite.InReach(0f, 0f, -6f, -8f));
            Assert.IsFalse(BoardBite.CrowdChews(0));
            Assert.IsTrue(BoardBite.CrowdChews(3));
            Assert.AreEqual(2, BoardBite.Chip);
            Assert.AreEqual("Muerden las tablas", Loc.T("camp.chew", "es"));
        }

        [Test]
        public void GuardsStandOnTheLineThatIsBeingHit()
        {
            Assert.AreEqual("Rest", GuardStand.Face("Guard", 5f, 0));
            Assert.AreEqual("Medic", GuardStand.Face("Guard", 60f, 2));
            Assert.AreEqual("Guard", GuardStand.Face("Guard", 60f, 0));
            Assert.AreEqual("Rest", GuardStand.Face("Cook", 60f, 0));
            Assert.AreEqual("Medic", GuardStand.Face("Medic", 60f, 0));
            var kinds = new[] { "Barricade", "Barricade", "Watchtower" };
            var x = new[] { -6f, -8f, -4f };
            var z = new[] { -8f, -6f, -4f };
            var sites = new[] { 0, 0, 0 };
            var integrity = new[] { 40, 90, 100 };
            Assert.AreEqual(0, GuardStand.Pick("gate", kinds, x, z, sites, integrity, 0));
            Assert.AreEqual(1, GuardStand.Pick("gate", kinds, x, z, sites, integrity, 1));
            Assert.AreEqual(0, GuardStand.Pick("gate", kinds, x, z, sites, integrity, 2));
            integrity[0] = 0;
            integrity[1] = 0;
            Assert.AreEqual(2, GuardStand.Pick("gate", kinds, x, z, sites, integrity, 0));
            GuardStand.Mark("gate", 0, kinds, x, z, sites, integrity, out float px, out float pz);
            Assert.AreEqual(-4f, px, 0.01f);
            Assert.AreEqual(-4f, pz, 0.01f);
            kinds[2] = "Crate";
            Assert.AreEqual(-1, GuardStand.Pick("gate", kinds, x, z, sites, integrity, 0));
            GuardStand.Mark("gate", 0, kinds, x, z, sites, integrity, out float fx, out float fz);
            Assert.AreEqual(-9.1f, fx, 0.01f);
            Assert.AreEqual(-12f, fz, 0.01f);
            Assert.AreEqual("Los guardias sostienen la línea", Loc.T("camp.line", "es"));
        }

        [Test]
        public void ARaidHitsTwoSidesAndThreeWhenTheNightIsWorse()
        {
            Assert.AreEqual(1, RaidPlan.Fronts(1, 2));
            Assert.AreEqual(2, RaidPlan.Fronts(2, 2));
            Assert.AreEqual(2, RaidPlan.Fronts(7, 0));
            Assert.AreEqual(3, RaidPlan.Fronts(2, 3));
            Assert.AreEqual(3, RaidPlan.Fronts(8, 1));
            Assert.AreEqual(3, RaidPlan.Fronts(10, 2));
            Assert.AreEqual("alley", RaidPlan.Side(2, 0, 0));
            Assert.AreEqual("yard", RaidPlan.Side(2, 0, 1));
            Assert.AreEqual("fence", RaidPlan.Side(2, 0, 2));
            Assert.AreEqual(4, RaidPlan.Share(8, 2, 0));
            Assert.AreEqual(4, RaidPlan.Share(8, 2, 1));
            Assert.AreEqual(3, RaidPlan.Share(8, 3, 0));
            Assert.AreEqual(3, RaidPlan.Share(8, 3, 1));
            Assert.AreEqual(2, RaidPlan.Share(8, 3, 2));
            Assert.AreEqual(0, RaidPlan.Share(8, 3, 3));
            Assert.AreEqual("lados", Loc.T("camp.sides", "es"));
        }

        [Test]
        public void ARaidPullsTheYardOffTheDaylightGrade()
        {
            Assert.AreEqual(Presentation.Exposure(1f), RaidGrade.Exposure(1f, false), 0.001f);
            Assert.AreEqual(-0.2f, RaidGrade.Exposure(1f, true), 0.001f);
            Assert.AreEqual(-0.45f, RaidGrade.Exposure(0.6f, true), 0.001f);
            RaidGrade.Filter(false, out float dayR, out float dayG, out float dayB);
            Assert.AreEqual(1f, dayR, 0.001f);
            Assert.AreEqual(0.96f, dayG, 0.001f);
            Assert.AreEqual(0.9f, dayB, 0.001f);
            RaidGrade.Filter(true, out float red, out float green, out float blue);
            Assert.AreEqual(0.72f, red, 0.001f);
            Assert.AreEqual(0.58f, green, 0.001f);
            Assert.AreEqual(0.78f, blue, 0.001f);
            Assert.AreEqual(0.28f, RaidGrade.Vignette(false, 1), 0.001f);
            Assert.AreEqual(0.16f, RaidGrade.Vignette(false, 0), 0.001f);
            Assert.AreEqual(0.46f, RaidGrade.Vignette(true, 1), 0.001f);
            Assert.AreEqual(1.4f, RaidGrade.Pulse(0f), 0.001f);
            Assert.Greater(RaidGrade.Pulse(0.4f), 1.4f);
            Assert.AreEqual("El patio se oscurece", Loc.T("camp.dark", "es"));
        }

        [Test]
        public void TheMorningAfterARaidHaulsBodiesAndMendsBoards()
        {
            var tasks = new[] { "Guard", "Cook", "Rest", "Medic" };
            var alive = new[] { true, true, true, false };
            var leader = new[] { false, true, false, false };
            int flags = MorningBoard.Apply(tasks, alive, leader, true, true);
            Assert.AreEqual(3, flags);
            Assert.AreEqual("Clear", tasks[0]);
            Assert.AreEqual("Cook", tasks[1]);
            Assert.AreEqual("Build", tasks[2]);
            Assert.AreEqual("Medic", tasks[3]);
            Assert.AreEqual("camp.morning", MorningBoard.Key(flags));
            var held = new[] { "Clear", "Build", "Guard" };
            var living = new[] { true, true, true };
            var leads = new[] { false, false, true };
            Assert.AreEqual(0, MorningBoard.Apply(held, living, leads, true, true));
            Assert.AreEqual("Guard", held[2]);
            var cooks = new[] { "Cook", "Medic" };
            var up = new[] { true, true };
            var notLead = new[] { false, false };
            Assert.AreEqual(0, MorningBoard.Apply(cooks, up, notLead, true, true));
            Assert.AreEqual("camp.haul", MorningBoard.Key(1));
            Assert.AreEqual("camp.mend", MorningBoard.Key(2));
            Assert.AreEqual("Recoge el patio y repara las tablas", Loc.T("camp.morning", "es"));
        }

        [Test]
        public void RaidDeadLeaveTheYardAndTheStreetHordeStays()
        {
            Assert.IsTrue(RaidRecall.Leaves(true, false));
            Assert.IsFalse(RaidRecall.Leaves(true, true));
            Assert.IsFalse(RaidRecall.Leaves(false, false));
            Assert.AreEqual(2, RaidRecall.Count(new[] { true, false, true, true }, new[] { false, false, true, false }));
            Assert.AreEqual(0, RaidRecall.Count(null, new[] { false }));
            Assert.AreEqual("Los muertos retroceden", Loc.T("camp.quiet", "es"));
        }

        [Test]
        public void AThinDayThreeLosesAndABuiltDayTenHolds()
        {
            Assert.AreEqual(2, RaidOutcome.Need(3));
            Assert.AreEqual(4, RaidOutcome.Need(10));
            Assert.AreEqual(6, RaidOutcome.Need(20));
            Assert.IsFalse(RaidOutcome.Holds(3, 1, 0, 0, false));
            Assert.IsTrue(RaidOutcome.Holds(3, 2, 0, 0, false));
            Assert.IsTrue(RaidOutcome.Holds(3, 0, 1, 0, false));
            Assert.IsFalse(RaidOutcome.Holds(3, 4, 2, 2, true));
            Assert.IsFalse(RaidOutcome.Holds(10, 1, 0, 0, false));
            Assert.IsTrue(RaidOutcome.Holds(10, 2, 1, 0, false));
            Assert.IsTrue(RaidOutcome.Holds(10, 1, 0, 3, false));
            Assert.AreEqual(4, RaidOutcome.Strength(2, 1, 0));
        }

        [Test]
        public void ASleptNightTakesTheExhaustionOffAndACotTakesMore()
        {
            Assert.AreEqual(40f, NightRest.Amount(false), 0.001f);
            Assert.AreEqual(70f, NightRest.Amount(true), 0.001f);
            Assert.AreEqual(40f, NightRest.Wake(80f, false), 0.001f);
            Assert.AreEqual(10f, NightRest.Wake(80f, true), 0.001f);
            Assert.AreEqual(0f, NightRest.Wake(20f, false), 0.001f);
            Assert.AreEqual(0f, NightRest.Wake(-4f, true), 0.001f);
            Assert.AreEqual(30f, NightRest.Wake(100f, true), 0.001f);
            Assert.AreEqual("Duermes", Loc.T("camp.slept", "es"));
            Assert.AreEqual("La cama sostiene la noche", Loc.T("camp.cot_sleep", "es"));
        }

        [Test]
        public void TheFourthShiftPaysAndAnOldSaveStaysAtZero()
        {
            Assert.AreEqual(4, Practice.Gain(3));
            Assert.AreEqual(8, Practice.Gain(8));
            Assert.AreEqual(1, Practice.Gain(-1));
            Assert.AreEqual(0, Practice.Bonus(3));
            Assert.AreEqual(1, Practice.Bonus(4));
            Assert.AreEqual(1, Practice.Bonus(8));
            Practice.Unpack(Practice.Pack(4, 0, 1, 0, 2), out int combat, out int medicine, out int engineering, out int cooking, out int scavenge);
            Assert.AreEqual(4, combat);
            Assert.AreEqual(0, medicine);
            Assert.AreEqual(1, engineering);
            Assert.AreEqual(0, cooking);
            Assert.AreEqual(2, scavenge);
            Practice.Unpack(null, out combat, out medicine, out engineering, out cooking, out scavenge);
            Assert.AreEqual(0, combat);
            Assert.AreEqual(0, medicine);
            Assert.AreEqual(0, engineering);
            Assert.AreEqual(0, cooking);
            Assert.AreEqual(0, scavenge);
            Practice.Unpack("", out combat, out medicine, out engineering, out cooking, out scavenge);
            Assert.AreEqual(0, combat);
            Practice.Unpack("4", out combat, out medicine, out engineering, out cooking, out scavenge);
            Assert.AreEqual(4, combat);
            Assert.AreEqual(0, scavenge);
            Assert.AreEqual("Vigilar 4  Construir 1  Rebuscar 2", Practice.Line(4, 0, 1, 0, 2, "es"));
            Assert.AreEqual("", Practice.Line(0, 0, 0, 0, 0, "es"));
        }

        [Test]
        public void APracticedLeaderChangesTheStreetAndANewOneDoesNot()
        {
            Assert.AreEqual(1f, FieldHand.Spread(0), 0.001f);
            Assert.AreEqual(1f, FieldHand.Spread(3), 0.001f);
            Assert.AreEqual(0.85f, FieldHand.Spread(4), 0.001f);
            Assert.AreEqual(1f, FieldHand.Reload(0), 0.001f);
            Assert.AreEqual(0.8f, FieldHand.Reload(8), 0.001f);
            Assert.AreEqual(0, FieldHand.Scrap(3));
            Assert.AreEqual(1, FieldHand.Scrap(4));
            Assert.AreEqual(50, FieldHand.Medkit(0));
            Assert.AreEqual(50, FieldHand.Medkit(3));
            Assert.AreEqual(56, FieldHand.Medkit(4));
            Assert.AreEqual("Medkit used  +50 HP", FieldHand.Dose(0));
            Assert.AreEqual("Medkit used  +56 HP", FieldHand.Dose(4));
            Assert.AreEqual("Medkit used  +60 HP", FieldHand.Dose(8));
            Assert.AreEqual(1f, HandDepth.Spread(4), 0.001f);
            Assert.AreEqual(0.88f, HandDepth.Spread(8), 0.001f);
            Assert.AreEqual(1f, HandDepth.Reload(4), 0.001f);
            Assert.AreEqual(0.9f, HandDepth.Reload(8), 0.001f);
            Assert.AreEqual(0, HandDepth.Heal(4));
            Assert.AreEqual(4, HandDepth.Heal(8));
            Assert.AreEqual(0, HandDepth.Scrap(4));
            Assert.AreEqual(4, HandDepth.Scrap(8));
            Assert.AreEqual(8, Practice.Gain(8));
            Assert.AreEqual(2.5f, RecoilBloom.Spread(2.5f, FieldHand.Spread(0), 0f), 0.001f);
            Assert.AreEqual(2.125f, RecoilBloom.Spread(2.5f, FieldHand.Spread(4), 0f), 0.001f);
        }

        [Test]
        public void TheYardKeepsThreeBedsAndARaidIsAlreadyLoud()
        {
            Assert.AreEqual(MusicTheme.Camp, MusicStem.Theme(GameState.CampManagement));
            Assert.AreEqual(MusicTheme.Camp, MusicStem.Theme(GameState.MainMenu));
            Assert.AreEqual(MusicTheme.Street, MusicStem.Theme(GameState.ExpeditionActive));
            Assert.AreEqual(MusicTheme.Raid, MusicStem.Theme(GameState.RaidActive));
            MusicStem.Gains(MusicTheme.Street, 0f, out float drone, out float perc, out float fight);
            Assert.AreEqual(0.35f, drone, 0.001f);
            Assert.AreEqual(0f, perc, 0.001f);
            Assert.AreEqual(0f, fight, 0.001f);
            MusicStem.Gains(MusicTheme.Street, 50f, out drone, out perc, out fight);
            Assert.AreEqual(0.5f, perc, 0.001f);
            Assert.AreEqual(0f, fight, 0.001f);
            MusicStem.Gains(MusicTheme.Street, 90f, out drone, out perc, out fight);
            Assert.AreEqual(1f, perc, 0.001f);
            Assert.AreEqual(1f, fight, 0.001f);
            MusicStem.Gains(MusicTheme.Camp, 100f, out drone, out perc, out fight);
            Assert.AreEqual(0.22f, drone, 0.001f);
            Assert.AreEqual(0.25f, perc, 0.001f);
            Assert.AreEqual(0f, fight, 0.001f);
            MusicStem.Gains(MusicTheme.Raid, 0f, out drone, out perc, out fight);
            Assert.AreEqual(0.55f, drone, 0.001f);
            Assert.AreEqual(0f, perc, 0.001f);
            Assert.AreEqual(0.45f, fight, 0.001f);
            Assert.AreEqual(1, MusicStem.Tally(0, 0f, 10f, 8f));
            Assert.AreEqual(2, MusicStem.Tally(1, 10f, 12f, 8f));
            Assert.AreEqual(1, MusicStem.Tally(2, 1f, 12f, 8f));
            Assert.IsFalse(MusicStem.Streak(2));
            Assert.IsTrue(MusicStem.Streak(3));
            Assert.AreEqual("stinger_raid", MusicStem.Cue("raid"));
            Assert.AreEqual("stinger_dawn", MusicStem.Cue("dawn"));
            Assert.AreEqual("", MusicStem.Cue("idle"));
        }

        [Test]
        public void TheNextLeaderIsTheSteadiestHealthyHand()
        {
            Assert.AreEqual(6, MealTable.FeudShift(6, false, 8));
            Assert.AreEqual(3, MealTable.FeudShift(6, true, 0));
            Assert.AreEqual(3, MealTable.FeudShift(6, true, 3));
            Assert.AreEqual(2, MealTable.FeudShift(6, true, 4));
            Assert.AreEqual(3, MealTable.FeudShift(6, true));
            var alive = new[] { true, true, true, false };
            var injury = new[] { 2, 0, 0, 0 };
            var leadership = new[] { 6, 1, 4, 8 };
            var morale = new[] { 90f, 50f, 50f, 100f };
            Assert.AreEqual(2, Heir.Pick(alive, injury, leadership, morale));
            morale[2] = 40f;
            Assert.AreEqual(1, Heir.Pick(alive, injury, leadership, morale));
            injury[1] = 1;
            injury[2] = 1;
            Assert.AreEqual(0, Heir.Pick(alive, injury, leadership, morale));
            Assert.AreEqual(-1, Heir.Pick(new[] { false }, new[] { 0 }, new[] { 4 }, new[] { 50f }));
            Assert.AreEqual("Lidera 4", Heir.Line(4, "es"));
            Assert.AreEqual("", Heir.Line(0, "es"));
        }

        [Test]
        public void TwoSeedsOpenDifferentCampsAndAFieldMedicStartsReady()
        {
            var a = SurvivorDraw.Open(1701);
            var b = SurvivorDraw.Open(1888);
            Assert.AreEqual(4, a.Length);
            Assert.AreEqual(4, b.Length);
            Assert.IsTrue(a[0].Leader);
            Assert.IsFalse(a[1].Leader);
            Assert.AreNotEqual(SurvivorDraw.Signature(a), SurvivorDraw.Signature(b));
            Assert.AreEqual(SurvivorDraw.Signature(a), SurvivorDraw.Signature(SurvivorDraw.Open(1701)));
            Assert.AreNotEqual(a[0].Name, a[1].Name);
            Assert.AreNotEqual(a[0].Trait, a[1].Trait);
            Assert.IsTrue(a[0].Bond.Contains(a[1].Name.Split(' ')[0]));
            bool medic = false;
            for (int seed = 1; seed <= 40; seed++)
            {
                var camp = SurvivorDraw.Open(seed);
                for (int i = 0; i < camp.Length; i++)
                {
                    if (camp[i].Trait != "Field Medic") continue;
                    Assert.AreEqual(4, camp[i].Medicine);
                    medic = true;
                }
            }
            Assert.IsTrue(medic);
            Assert.AreEqual("Volátil", Loc.T("trait.volatile", "es"));
        }

        [Test]
        public void AWatchfulCampHearsTheRaidSoonerAndAGluttonEatsMore()
        {
            Assert.AreEqual(0f, TraitHook.Warning(0, 3, 2, 2), 0.001f);
            Assert.AreEqual(8f, TraitHook.Warning(1, 0, 0, 0), 0.001f);
            Assert.AreEqual(14f, TraitHook.Warning(1, 1, 0, 0), 0.001f);
            Assert.AreEqual(18f, TraitHook.Warning(1, 1, 1, 0), 0.001f);
            Assert.AreEqual(21f, TraitHook.Warning(1, 1, 1, 1), 0.001f);
            Assert.AreEqual(28f, TraitHook.Warning(3, 1, 2, 2), 0.001f);
            Assert.AreEqual(18f, TraitHook.HungerDrop(null), 0.001f);
            Assert.AreEqual(18f, TraitHook.HungerDrop("Watchful"), 0.001f);
            Assert.AreEqual(23.4f, TraitHook.HungerDrop("Glutton"), 0.001f);
            Assert.AreEqual("Glotón", Loc.T("trait.glutton", "es"));
        }

        [Test]
        public void AnEngineerBuildsACookPlatesAndACowardFlinches()
        {
            Assert.AreEqual(2, BuildSite.Shift("Engineer", 40f));
            Assert.AreEqual(4, TraitHook.CookPlate("Cook", true));
            Assert.AreEqual(0, TraitHook.CookPlate("Cook", false));
            Assert.AreEqual(0, TraitHook.CookPlate("Glutton", true));
            Assert.AreEqual(0.8f, TraitHook.Aim("Sharpshooter"), 0.001f);
            Assert.AreEqual(1f, TraitHook.Aim("Steady Hands"), 0.001f);
            Assert.AreEqual(1f, TraitHook.Aim(null), 0.001f);
            Assert.AreEqual(0, TraitHook.WatchCost("Brave"));
            Assert.AreEqual(2, TraitHook.WatchCost("Watchful"));
            Assert.AreEqual(2, TraitHook.WatchCost(null));
            Assert.AreEqual(6, TraitHook.WatchCost("Cowardly"));
            Assert.AreEqual(0, TraitHook.WatchPay("Cowardly", 2));
            Assert.AreEqual(2, TraitHook.WatchPay("Brave", 2));
            Assert.AreEqual(0, TraitHook.WatchPay("Brave", 0));
            bool engineer = false;
            bool cook = false;
            bool sharp = false;
            for (int seed = 1; seed <= 40; seed++)
            {
                var camp = SurvivorDraw.Open(seed);
                for (int i = 0; i < camp.Length; i++)
                {
                    if (camp[i].Trait == "Engineer")
                    {
                        Assert.AreEqual(4, camp[i].Engineering);
                        engineer = true;
                    }
                    if (camp[i].Trait == "Cook")
                    {
                        Assert.AreEqual(4, camp[i].Cooking);
                        cook = true;
                    }
                    if (camp[i].Trait == "Sharpshooter")
                    {
                        Assert.AreEqual(4, camp[i].Combat);
                        sharp = true;
                    }
                }
            }
            Assert.IsTrue(engineer);
            Assert.IsTrue(cook);
            Assert.IsTrue(sharp);
            Assert.AreEqual("Ingeniero", Loc.T("trait.engineer", "es"));
            Assert.AreEqual("Cocinero", Loc.T("trait.cook", "es"));
            Assert.AreEqual("Tirador", Loc.T("trait.sharp", "es"));
            Assert.AreEqual("Valiente", Loc.T("trait.brave", "es"));
            Assert.AreEqual("Cobarde", Loc.T("trait.coward", "es"));
        }

        [Test]
        public void AnInsomniacRestsLessAndANightOwlStretchesTheWarning()
        {
            Assert.AreEqual(8, TraitHook.RestGain(null, 8));
            Assert.AreEqual(10, TraitHook.RestGain("Cook", 10));
            Assert.AreEqual(5, TraitHook.RestGain("Insomniac", 8));
            Assert.AreEqual(7, TraitHook.RestGain("Insomniac", 10));
            Assert.AreEqual(2, TraitHook.RestGain("Insomniac", 5));
            Assert.AreEqual(1, TraitHook.RestGain("Insomniac", 1));
            Assert.AreEqual("Steady", ColonyDay.Mood(65f));
            Assert.AreEqual("Inspired", ColonyDay.Mood(65f, "Optimist"));
            Assert.AreEqual("Inspired", ColonyDay.Mood(80f, "Optimist"));
            Assert.AreEqual("Breakdown", ColonyDay.Mood(5f, "Optimist"));
            Assert.AreEqual(1f, ColonyDay.OutputScale(65f), 0.001f);
            Assert.AreEqual(1.1f, ColonyDay.OutputScale(65f, "Optimist"), 0.001f);
            Assert.AreEqual(0f, ColonyDay.OutputScale(5f, "Optimist"), 0.001f);
            Assert.AreEqual(1.1f, ColonyDay.OutputScale(80f), 0.001f);
            Assert.AreEqual(14f, TraitHook.NightStretch(14f, 0), 0.001f);
            Assert.AreEqual(0f, TraitHook.NightStretch(0f, 2), 0.001f);
            Assert.AreEqual(16f, TraitHook.NightStretch(14f, 1), 0.001f);
            Assert.AreEqual(28f, TraitHook.NightStretch(28f, 3), 0.001f);
            int food = 4;
            int water = 4;
            int raw = 0;
            var pair = new[]
            {
                new ColonistDay { id = "ada", trait = "Loner", task = "Guard", morale = 50f, hunger = 78f, thirst = 78f, opinion = 18 },
                new ColonistDay { id = "ben", task = "Guard", morale = 50f, hunger = 78f, thirst = 78f, opinion = 18 }
            };
            ColonyDay.Simulate(pair, ref food, ref water, true, false, "", 0, ref raw);
            Assert.AreEqual(18, pair[0].opinion);
            Assert.AreEqual(20, pair[1].opinion);
            Assert.AreEqual("", pair[0].kin);
            Assert.AreEqual("ada:2", pair[1].kin);
            bool loner = false;
            for (int seed = 1; seed <= 40; seed++)
            {
                var camp = SurvivorDraw.Open(seed);
                for (int i = 0; i < camp.Length; i++)
                {
                    if (camp[i].Trait != "Loner") continue;
                    Assert.AreEqual(camp[i].Aside == "Scrounger" ? 3 : 2, camp[i].Scavenge);
                    loner = true;
                }
            }
            Assert.IsTrue(loner);
            Assert.AreEqual("Insomne", Loc.T("trait.insomniac", "es"));
            Assert.AreEqual("Optimista", Loc.T("trait.optimist", "es"));
            Assert.AreEqual("Solitario", Loc.T("trait.loner", "es"));
            Assert.AreEqual("Noctámbulo", Loc.T("trait.owl", "es"));
        }

        [Test]
        public void EachHandCarriesASecondTraitThatDoesNotClash()
        {
            Assert.IsTrue(SurvivorDraw.Clashes("Brave", "Cowardly"));
            Assert.IsTrue(SurvivorDraw.Clashes("Cowardly", "Brave"));
            Assert.IsTrue(SurvivorDraw.Clashes("Insomniac", "Light Sleeper"));
            Assert.IsTrue(SurvivorDraw.Clashes("Optimist", "Volatile"));
            Assert.IsTrue(SurvivorDraw.Clashes("Cook", "Cook"));
            Assert.IsFalse(SurvivorDraw.Clashes("Cook", "Engineer"));
            Assert.IsFalse(SurvivorDraw.Clashes("Brave", ""));
            Assert.IsFalse(SurvivorDraw.Clashes(null, "Loner"));
            for (int seed = 1; seed <= 40; seed++)
            {
                var camp = SurvivorDraw.Open(seed);
                var again = SurvivorDraw.Open(seed);
                Assert.AreEqual(SurvivorDraw.Signature(camp), SurvivorDraw.Signature(again));
                for (int i = 0; i < camp.Length; i++)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(camp[i].Aside));
                    Assert.IsFalse(SurvivorDraw.Clashes(camp[i].Trait, camp[i].Aside));
                    Assert.AreEqual(camp[i].Aside, again[i].Aside);
                    if (camp[i].Trait == "Field Medic") Assert.AreEqual(4, camp[i].Medicine);
                    if (camp[i].Aside == "Engineer") Assert.AreEqual(4, camp[i].Engineering);
                    if (camp[i].Aside == "Cook") Assert.AreEqual(4, camp[i].Cooking);
                    if (camp[i].Aside == "Sharpshooter") Assert.AreEqual(4, camp[i].Combat);
                }
            }
            Assert.AreEqual(23.4f, TraitHook.HungerDrop("Watchful", "Glutton"), 0.001f);
            Assert.AreEqual(18f, TraitHook.HungerDrop("Watchful", null), 0.001f);
            Assert.AreEqual(6, TraitHook.WatchCost("Cook", "Cowardly"));
            Assert.AreEqual(0, TraitHook.WatchCost("Cook", "Brave"));
            Assert.AreEqual(2, TraitHook.WatchCost("Watchful", null));
            Assert.AreEqual(0, TraitHook.WatchPay("Cook", "Cowardly", 2));
            Assert.AreEqual(2, TraitHook.WatchPay("Cook", "Brave", 2));
            Assert.AreEqual(0.8f, TraitHook.Aim("Cook", "Sharpshooter"), 0.001f);
            Assert.AreEqual(1f, TraitHook.Aim("Cook", null), 0.001f);
            Assert.AreEqual(4, TraitHook.CookPlate("Guard", "Cook", true));
            Assert.AreEqual(0, TraitHook.CookPlate("Guard", "Cook", false));
            Assert.AreEqual(5, TraitHook.RestGain("Cook", "Insomniac", 8));
            Assert.AreEqual(8, TraitHook.RestGain("Cook", null, 8));
            Assert.AreEqual("Inspired", ColonyDay.Mood(65f, "Cook", "Optimist"));
            Assert.AreEqual("Steady", ColonyDay.Mood(65f, "Cook", "Loner"));
            Assert.AreEqual(1.1f, ColonyDay.OutputScale(65f, "Cook", "Optimist"), 0.001f);
            var saved = new SaveGameData { survivors = new[] { new SurvivorSave { id = "ada", trait = "Cook", aside = "Loner" } } };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(saved), out var loaded, out var error), error);
            Assert.AreEqual(1, loaded.schemaVersion);
            Assert.AreEqual("Loner", loaded.survivors[0].aside);
            Assert.IsTrue(SaveCodec.TryDeserialize("{\"schemaVersion\":1,\"survivors\":[{\"id\":\"ada\",\"trait\":\"Cook\"}]}", out var old, out error), error);
            Assert.IsNull(old.survivors[0].aside);
            Assert.IsNull(old.survivors[0].mark);
        }

        [Test]
        public void EachHandCarriesAThirdTrait()
        {
            Assert.AreEqual(23.4f, TraitHook.HungerDrop("Watchful", "Loner", "Glutton"), 0.001f);
            Assert.AreEqual(18f, TraitHook.HungerDrop("Watchful", "Loner", null), 0.001f);
            Assert.AreEqual(18f, TraitHook.HungerDrop("Watchful", null), 0.001f);
            Assert.AreEqual(23.4f, TraitHook.HungerDrop("Watchful", "Glutton"), 0.001f);
            Assert.AreEqual(0.8f, TraitHook.Aim("Cook", "Loner", "Sharpshooter"), 0.001f);
            Assert.AreEqual(1f, TraitHook.Aim("Cook", null), 0.001f);
            Assert.AreEqual(5, TraitHook.RestGain("Cook", "Loner", "Insomniac", 8));
            Assert.AreEqual(8, TraitHook.RestGain("Cook", null, 8));
            Assert.AreEqual(4, TraitHook.CookPlate("Guard", "Loner", "Cook", true));
            Assert.AreEqual(0, TraitHook.CookPlate("Guard", "Cook", false));
            Assert.AreEqual(6, TraitHook.WatchCost("Cook", "Loner", "Cowardly"));
            Assert.AreEqual(0, TraitHook.WatchCost("Cook", "Loner", "Brave"));
            Assert.AreEqual(2, TraitHook.WatchCost("Watchful", null));
            Assert.AreEqual("Inspired", ColonyDay.Mood(65f, "Cook", "Loner", "Optimist"));
            Assert.AreEqual("Steady", ColonyDay.Mood(65f, "Cook", "Loner", null));
            Assert.AreEqual(1.1f, ColonyDay.OutputScale(65f, "Cook", "Loner", "Optimist"), 0.001f);
            var plain = new[]
            {
                new ColonistDay { id = "ada", trait = "Watchful", aside = "Loner", task = "Rest", morale = 50f, hunger = 78f, thirst = 78f, opinion = 18 }
            };
            var marked = new[]
            {
                new ColonistDay { id = "ada", trait = "Watchful", aside = "Loner", mark = "Glutton", task = "Rest", morale = 50f, hunger = 78f, thirst = 78f, opinion = 18 }
            };
            int food = 0;
            int water = 0;
            int raw = 0;
            int foodB = 0;
            int waterB = 0;
            int rawB = 0;
            ColonyDay.Simulate(plain, ref food, ref water, false, false, "", 0, ref raw);
            ColonyDay.Simulate(marked, ref foodB, ref waterB, false, false, "", 0, ref rawB);
            Assert.AreEqual(60f, plain[0].hunger, 0.01f);
            Assert.AreEqual(54.6f, marked[0].hunger, 0.01f);
            for (int seed = 1; seed <= 40; seed++)
            {
                var camp = SurvivorDraw.Open(seed);
                var again = SurvivorDraw.Open(seed);
                Assert.AreEqual(SurvivorDraw.Signature(camp), SurvivorDraw.Signature(again));
                for (int i = 0; i < camp.Length; i++)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(camp[i].Mark));
                    Assert.AreEqual(camp[i].Mark, again[i].Mark);
                    Assert.IsFalse(SurvivorDraw.Clashes(camp[i].Trait, camp[i].Mark));
                    Assert.IsFalse(SurvivorDraw.Clashes(camp[i].Aside, camp[i].Mark));
                    Assert.AreNotEqual(camp[i].Trait, camp[i].Mark);
                    Assert.AreNotEqual(camp[i].Aside, camp[i].Mark);
                    if (camp[i].Trait == "Field Medic") Assert.AreEqual(4, camp[i].Medicine);
                    if (camp[i].Trait == "Cook") Assert.AreEqual(4, camp[i].Cooking);
                    if (camp[i].Trait == "Loner") Assert.AreEqual(camp[i].Aside == "Scrounger" ? 3 : 2, camp[i].Scavenge);
                }
            }
            var saved = new SaveGameData { survivors = new[] { new SurvivorSave { id = "ada", trait = "Cook", aside = "Loner", mark = "Optimist" } } };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(saved), out var loaded, out var error), error);
            Assert.AreEqual("Optimist", loaded.survivors[0].mark);
            Assert.AreEqual(1, loaded.schemaVersion);
        }

        [Test]
        public void ADeepCookPlatesExtraAndAFourthShiftDoesNot()
        {
            Assert.AreEqual(0, PotDepth.Plate(4));
            Assert.AreEqual(0, PotDepth.Plate(0));
            Assert.AreEqual(0, PotDepth.Plate(-1));
            Assert.AreEqual(1, PotDepth.Plate(5));
            Assert.AreEqual(4, PotDepth.Plate(8));
            Assert.AreEqual(4, PotDepth.Plate(12));
            Assert.AreEqual(1, Practice.Bonus(4));
            Assert.AreEqual(1, Practice.Bonus(8));
            CookPot.Serve(true, 4, 2, out int spent, out int food, out int morale);
            Assert.AreEqual(2, spent);
            Assert.AreEqual(6, food);
            Assert.AreEqual(8, morale);
            Assert.AreEqual(7, food + Practice.Bonus(4) + PotDepth.Plate(4));
            Assert.AreEqual(11, food + Practice.Bonus(8) + PotDepth.Plate(8));
        }

        [Test]
        public void AThirdFogDayOpensOvercastAndTheMarketStaysClear()
        {
            Assert.AreEqual(WeatherKind.Fog, SkyBand.Cast(WeatherKind.Fog, 3));
            Assert.AreEqual(WeatherKind.Fog, CloudDeck.Lay(WeatherKind.Fog, 2));
            Assert.AreEqual(WeatherKind.Fog, CloudDeck.Lay(WeatherKind.Fog, 0));
            Assert.AreEqual(WeatherKind.Overcast, CloudDeck.Lay(WeatherKind.Fog, 3));
            Assert.AreEqual(WeatherKind.Overcast, CloudDeck.Lay(WeatherKind.Fog, 6));
            Assert.AreEqual(WeatherKind.Rain, CloudDeck.Lay(WeatherKind.Rain, 1));
            Assert.AreEqual(WeatherKind.Storm, CloudDeck.Lay(WeatherKind.Rain, 2));
            Assert.AreEqual(WeatherKind.Clear, CloudDeck.Lay(WeatherKind.Clear, 3));
            Assert.AreEqual(WeatherKind.Clear, CloudDeck.Lay(DistrictRules.For("ash_market").Weather, 3));
            Assert.AreEqual(WeatherKind.Fog, DistrictRules.For("old_hospital").Weather);
            Assert.AreEqual(WeatherKind.Overcast, CloudDeck.Lay(DistrictRules.For("old_hospital").Weather, 3));
            Assert.AreEqual(0.9f, WeatherSurface.Sight(WeatherKind.Overcast), 0.001f);
            Assert.AreEqual(0.1f, WeatherSurface.Wetness(WeatherKind.Overcast), 0.001f);
            Assert.AreEqual("wind", AshFall.Bed(WeatherKind.Overcast, "old_hospital"));
        }

        [Test]
        public void EveryToneHasACreditAndAMissingOneDoesNot()
        {
            Assert.AreEqual("", SoundCredit.Line(null));
            Assert.AreEqual("", SoundCredit.Line(""));
            Assert.AreEqual("", SoundCredit.Line("nope"));
            Assert.IsFalse(SoundCredit.Covers("nope"));
            Assert.AreEqual(ClipBook.Ids.Length, SoundCredit.Count);
            for (int i = 0; i < ClipBook.Ids.Length; i++)
            {
                string id = ClipBook.Ids[i];
                Assert.IsTrue(SoundCredit.Covers(id), id);
                Assert.AreEqual(id + " — generated", SoundCredit.Line(id));
            }
            Assert.AreEqual("Every tone is generated in the game.", Loc.T("menu.tones", "en"));
            Assert.AreEqual("Cada tono se genera en el juego.", Loc.T("menu.tones", "es"));
            Assert.AreEqual("tonos generados", Loc.T("menu.tones_n", "es"));
        }

        [Test]
        public void ALeaderWithoutARigStillFiresAndFalls()
        {
            Assert.AreEqual(GaitSheet.Beat.Idle, GaitSheet.Pick(false, false, false, false, false, false, 0f));
            Assert.AreEqual(GaitSheet.Beat.Walk, GaitSheet.Pick(false, false, false, false, false, false, 1f));
            Assert.AreEqual(GaitSheet.Beat.CrouchStill, GaitSheet.Pick(false, false, false, false, true, false, 0f));
            Assert.AreEqual(GaitSheet.Beat.Crouch, GaitSheet.Pick(false, false, false, false, true, true, 1f));
            Assert.AreEqual(GaitSheet.Beat.Sprint, GaitSheet.Pick(false, false, false, false, false, true, 2f));
            Assert.AreEqual(GaitSheet.Beat.Attack, GaitSheet.Pick(false, false, true, true, true, true, 2f));
            Assert.AreEqual(GaitSheet.Beat.Hit, GaitSheet.Pick(false, true, true, true, false, true, 2f));
            Assert.AreEqual(GaitSheet.Beat.Dead, GaitSheet.Pick(true, true, true, true, true, true, 2f));
            Assert.AreEqual(GaitSheet.Beat.Aim, GaitSheet.Pick(false, false, false, false, false, true, 2f, true));
            Assert.AreEqual(GaitSheet.Beat.Aim, GaitSheet.Pick(false, false, false, false, true, false, 1f, true));
            Assert.AreEqual(GaitSheet.Beat.Reload, GaitSheet.Pick(false, false, false, true, false, false, 0f, true));
            Assert.AreEqual(GaitSheet.Beat.Walk, GaitSheet.Pick(false, false, false, false, false, false, 1f, false));
            Assert.AreEqual(8f, GaitSheet.Lean(GaitSheet.Beat.Aim, 0f), 0.001f);
            Assert.AreEqual(0.02f, GaitSheet.Hop(GaitSheet.Beat.Aim, 1.5708f), 0.001f);
            Assert.AreEqual(0.72f, GaitSheet.Scale(true, false), 0.001f);
            Assert.AreEqual(1f, GaitSheet.Scale(true, true), 0.001f);
            Assert.AreEqual(1f, GaitSheet.Scale(false, false), 0.001f);
            Assert.AreEqual(0.04f, GaitSheet.Hop(GaitSheet.Beat.Walk, 1.5707963f), 0.001f);
            Assert.AreEqual(0.07f, GaitSheet.Hop(GaitSheet.Beat.Sprint, 1.5707963f), 0.001f);
            Assert.AreEqual(0f, GaitSheet.Hop(GaitSheet.Beat.Idle, 1.5707963f), 0.001f);
            Assert.AreEqual(0f, GaitSheet.Lean(GaitSheet.Beat.Idle, 1f), 0.001f);
            Assert.AreEqual(0f, GaitSheet.Lean(GaitSheet.Beat.CrouchStill, 1f), 0.001f);
            Assert.AreEqual(12f, GaitSheet.Lean(GaitSheet.Beat.Sprint, 0f), 0.001f);
            Assert.AreEqual(0f, GaitSheet.Swing(0f), 0.001f);
            Assert.AreEqual(1f, GaitSheet.Swing(0.12f), 0.001f);
            Assert.AreEqual(0f, GaitSheet.Swing(0.34f), 0.001f);
            Assert.AreEqual(26f, GaitSheet.Lean(GaitSheet.Beat.Attack, 0.12f), 0.001f);
            Assert.AreEqual(1f, GaitSheet.Flail(0.08f), 0.001f);
            Assert.AreEqual(0f, GaitSheet.Flail(0.28f), 0.001f);
            Assert.AreEqual(-18f, GaitSheet.Lean(GaitSheet.Beat.Hit, 0.08f), 0.001f);
            Assert.AreEqual(1f, GaitSheet.Dip(0.4f), 0.001f);
            Assert.AreEqual(0f, GaitSheet.Dip(0f), 0.001f);
            Assert.AreEqual(10f, GaitSheet.Lean(GaitSheet.Beat.Reload, 0.4f), 0.001f);
            Assert.AreEqual(-0.06f, GaitSheet.Sink(GaitSheet.Beat.Reload, 0.4f), 0.001f);
            Assert.AreEqual(0f, GaitSheet.Fall(0f), 0.001f);
            Assert.AreEqual(1f, GaitSheet.Fall(0.7f), 0.001f);
            Assert.AreEqual(1f, GaitSheet.Fall(2f), 0.001f);
            Assert.AreEqual(76f, GaitSheet.Lean(GaitSheet.Beat.Dead, 0.7f), 0.001f);
            Assert.AreEqual(-0.45f, GaitSheet.Sink(GaitSheet.Beat.Dead, 0.7f), 0.001f);
            Assert.AreEqual(0f, GaitSheet.Sink(GaitSheet.Beat.Walk, 1f), 0.001f);
            Assert.AreEqual(14f, PoseSheet.Lean(ZombieAI.ZombieState.Chase, 0f), 0.001f);
        }

        [Test]
        public void SaveSlotsAndKeyRowsSpeakSpanish()
        {
            Assert.AreEqual("SAVES", MenuLine.Title("en"));
            Assert.AreEqual("PARTIDAS", MenuLine.Title("es"));
            Assert.AreEqual("Slot 1  day 4  Mara", MenuLine.Slot(1, 4, "Mara", true, "en"));
            Assert.AreEqual("Ranura 1  día 4  Mara", MenuLine.Slot(1, 4, "Mara", true, "es"));
            Assert.AreEqual("Slot 2  empty", MenuLine.Slot(2, 0, "", false, "en"));
            Assert.AreEqual("Ranura 2  vacía", MenuLine.Slot(2, 9, "Mara", false, "es"));
            Assert.AreEqual("Autosave  day 7  Ellis", MenuLine.Auto(7, "Ellis", "en"));
            Assert.AreEqual("Autoguardado  día 7  Ellis", MenuLine.Auto(7, "Ellis", "es"));
            Assert.AreEqual("Press a key for Reload", MenuLine.KeyWait("Reload", "en"));
            Assert.AreEqual("Pulsa una tecla para Recargar", MenuLine.KeyWait("Reload", "es"));
            Assert.AreEqual("Reload: R", MenuLine.KeyBound("Reload", "R", "en"));
            Assert.AreEqual("Recargar: R", MenuLine.KeyBound("Reload", "R", "es"));
            Assert.AreEqual("Press a button for Interact", MenuLine.PadWait("Interact", "en"));
            Assert.AreEqual("Pulsa un botón para Interactuar", MenuLine.PadWait("Interact", "es"));
            Assert.AreEqual("Pad Dodge: South", MenuLine.PadBound("Dodge", "South", "en"));
            Assert.AreEqual("Mando Esquivar: South", MenuLine.PadBound("Dodge", "South", "es"));
        }

        [Test]
        public void ARunnerClawOpensABleed()
        {
            Assert.IsTrue(ClawCut.Opens(true, 0.34f));
            Assert.IsFalse(ClawCut.Opens(true, 0.35f));
            Assert.IsFalse(ClawCut.Opens(false, 0f));
            Assert.IsTrue(ClawCut.Opens(true, -1f));
            Assert.IsFalse(ClawCut.Opens(true, 1f));
            Assert.AreEqual(0.35f, ClawCut.RunnerBleed, 0.001f);
            Assert.IsTrue(ClawCut.Infects(0.19f));
            Assert.IsFalse(ClawCut.Infects(0.2f));
            Assert.IsFalse(ClawCut.Infects(1f));
            Assert.AreEqual(0.2f, ClawCut.BiteInfect, 0.001f);
        }

        [Test]
        public void ASpentCasingCallsAnythingClose()
        {
            Assert.AreEqual(3.2f, ShellRing.Radius, 0.001f);
            Assert.AreEqual(0.28f, ShellRing.Loud, 0.001f);
            Assert.IsTrue(ShellRing.Calls(WeaponType.Pistol));
            Assert.IsTrue(ShellRing.Calls(WeaponType.Shotgun));
            Assert.IsTrue(ShellRing.Calls(WeaponType.Rifle));
            Assert.IsTrue(ShellRing.Calls(WeaponType.SMG));
            Assert.IsFalse(ShellRing.Calls(WeaponType.Melee));
            Assert.Greater(ShellRing.Radius, 2f);
            Assert.Less(ShellRing.Radius, 6f);
            Assert.Less(ShellRing.Radius, BleedScent.Radius);
            Assert.AreEqual("[Shell, east]", Presentation.Caption(NoiseType.ShellClink, 1f, 0f, "en"));
            Assert.AreEqual("[Casquillo, este]", Presentation.Caption(NoiseType.ShellClink, 1f, 0f, "es"));
            Assert.AreEqual("", Presentation.Caption(NoiseType.WalkFootstep, 1f, 0f, "en"));
            Assert.IsFalse(StormCover.Masks(10f, 10.6f, NoiseType.ShellClink));
        }

        [Test]
        public void ACraftRowAndAMoodSpeakSpanish()
        {
            Assert.AreEqual(CraftBill.Line("9mm (12)", 3, 0, 1, 0), CraftSay.Line("9mm (12)", 3, 0, 1, 0, "en"));
            Assert.AreEqual("9mm (12)   chatarra 3   quím 1", CraftSay.Line("9mm (12)", 3, 0, 1, 0, "es"));
            Assert.AreEqual("Craft   scrap 4   cloth 2   tape 1", CraftSay.Line("", 4, 2, 0, 1, "en"));
            Assert.AreEqual("Fabricar   chatarra 4   tela 2   cinta 1", CraftSay.Line("", 4, 2, -3, 1, "es"));
            Assert.AreEqual("Inspired", Loc.Mood(ColonyDay.Mood(80f), "en"));
            Assert.AreEqual("Inspirado", Loc.Mood(ColonyDay.Mood(80f), "es"));
            Assert.AreEqual("Steady", Loc.Mood("Steady", "en"));
            Assert.AreEqual("Estable", Loc.Mood("Steady", "es"));
            Assert.AreEqual("Colapso", Loc.Mood("Breakdown", "es"));
            Assert.AreEqual("", Loc.Mood("", "es"));
        }

        [Test]
        public void TheDayLineSpeaksSpanish()
        {
            Assert.AreEqual("Day 3  18:30", ClockFace.Read(3, 18.5f, "en"));
            Assert.AreEqual("Día 3  18:30", ClockFace.Read(3, 18.5f, "es"));
            Assert.AreEqual("Day 1  06:30", ClockFace.Read(1, 6.5f, "en"));
            Assert.AreEqual("Day 1  00:00", ClockFace.Read(0, -2f, "en"));
            Assert.AreEqual("Day 2  00:00", ClockFace.Read(2, 24f, "en"));
            Assert.AreEqual("Day 4  morning watch", ClockFace.Morning(4, "en"));
            Assert.AreEqual("Día 4  guardia de la mañana", ClockFace.Morning(4, "es"));
            Assert.AreEqual("Day 1  morning watch", ClockFace.Morning(0, "en"));
        }

        [Test]
        public void OpeningARationCallsAnythingClose()
        {
            Assert.IsTrue(RationNoise.Calls(12f, 0f));
            Assert.IsTrue(RationNoise.Calls(0f, 20f));
            Assert.IsFalse(RationNoise.Calls(0f, 0f));
            Assert.IsFalse(RationNoise.Calls(-4f, 0f));
            Assert.AreEqual(2.6f, RationNoise.Radius, 0.001f);
            Assert.AreEqual(0.22f, RationNoise.Loud, 0.001f);
            Assert.Greater(RationNoise.Radius, 2f);
            Assert.Less(RationNoise.Radius, ShellRing.Radius);
            Assert.AreEqual("[Bite, east]", Presentation.Caption(NoiseType.RationBite, 1f, 0f, "en"));
            Assert.AreEqual("[Bocado, este]", Presentation.Caption(NoiseType.RationBite, 1f, 0f, "es"));
            Assert.AreEqual("", Presentation.Caption(NoiseType.WalkFootstep, 1f, 0f, "en"));
            Assert.IsTrue(ClipBook.Has("bite"));
            Assert.AreEqual(6f, AudioSpace.MaxDistance("bite"), 0.001f);
            Assert.IsFalse(StormCover.Masks(10f, 10.6f, NoiseType.RationBite));
        }

        [Test]
        public void AMedkitAndAFeverFollowTheLanguage()
        {
            Assert.AreEqual("Medkit used  +50 HP", FieldHand.Dose(0));
            Assert.AreEqual("Medkit used  +56 HP", FieldHand.Dose(4, "en"));
            Assert.AreEqual("Medkit used  +60 HP", FieldHand.Dose(8, "en"));
            Assert.AreEqual("Botiquín usado  +50 PS", FieldHand.Dose(0, "es"));
            Assert.AreEqual("Botiquín usado  +56 PS", FieldHand.Dose(4, "es"));
            Assert.AreEqual("Antibiotics won't help", FieldHand.Fail("en"));
            Assert.AreEqual("Los antibióticos no sirven", FieldHand.Fail("es"));
            Assert.AreEqual("The fever breaks", FieldHand.Breaks("en"));
            Assert.AreEqual("La fiebre cede", FieldHand.Breaks("es"));
            Assert.AreEqual("Painkillers", FieldHand.Relief("painkillers", "en"));
            Assert.AreEqual("Analgésicos", FieldHand.Relief("painkillers", "es"));
            Assert.AreEqual("Used Bandage", FieldHand.Spent("bandage", "en"));
            Assert.AreEqual("Usado Vendaje", FieldHand.Spent("bandage", "es"));
        }

        [Test]
        public void ABeltAndADropFollowTheLanguage()
        {
            Assert.AreEqual("Belt is full", PackSay.Full("en"));
            Assert.AreEqual("El cinturón está lleno", PackSay.Full("es"));
            Assert.AreEqual("Cleared belt", PackSay.Clear("en"));
            Assert.AreEqual("Cinturón vacío", PackSay.Clear("es"));
            Assert.AreEqual("Belt 5", PackSay.Slot(5, "en"));
            Assert.AreEqual("Belt 5", PackSay.Slot(0, "en"));
            Assert.AreEqual("Cinturón 8", PackSay.Slot(8, "es"));
            Assert.AreEqual("Cinturón 8", PackSay.Slot(12, "es"));
            Assert.AreEqual("Dropped Bandage", PackSay.Dropped("bandage", "", "en"));
            Assert.AreEqual("Soltado Vendaje", PackSay.Dropped("bandage", "Bandage", "es"));
            Assert.AreEqual("Dropped Spare", PackSay.Dropped("spare_widget", "Spare", "en"));
            Assert.AreEqual("Dropped spare_widget", PackSay.Dropped("spare_widget", "", "en"));
            Assert.AreEqual("Pack is full", PackSay.Pack("en"));
            Assert.AreEqual("La mochila está llena", PackSay.Pack("es"));
            Assert.AreEqual("Poisoned", PackSay.Poison("en"));
            Assert.AreEqual("Envenenado", PackSay.Poison("es"));
            Assert.AreEqual("Crafted Painkillers", PackSay.Made("painkillers", "Painkillers", "en"));
            Assert.AreEqual("Fabricado Analgésicos", PackSay.Made("painkillers", "Painkillers", "es"));
            Assert.AreEqual("Crafted Spare", PackSay.Made("spare_widget", "Spare", "en"));
        }

        [Test]
        public void AGateAndACrateFollowTheLanguage()
        {
            Assert.AreEqual("Pack is too heavy", GateLine.Heavy("en"));
            Assert.AreEqual("La mochila pesa demasiado", GateLine.Heavy("es"));
            Assert.AreEqual("Left some loot behind", GateLine.Left("en"));
            Assert.AreEqual("Quedó botín atrás", GateLine.Left("es"));
            Assert.AreEqual("Container open", GateLine.Open("en"));
            Assert.AreEqual("Contenedor abierto", GateLine.Open("es"));
            Assert.AreEqual("Empty", GateLine.Empty("en"));
            Assert.AreEqual("Vacío", GateLine.Empty("es"));
            Assert.AreEqual("Pick an open district", GateLine.District("en"));
            Assert.AreEqual("Elige un distrito abierto", GateLine.District("es"));
            Assert.AreEqual("Dragged back to the gate", GateLine.Drag("en"));
            Assert.AreEqual("Arrastrado a la puerta", GateLine.Drag("es"));
            Assert.AreEqual("Objectives unfinished", GateLine.Quota("en"));
            Assert.AreEqual("Objetivos sin cumplir", GateLine.Quota("es"));
            Assert.AreEqual("They're too close", GateLine.Close("en"));
            Assert.AreEqual("Están demasiado cerca", GateLine.Close("es"));
            Assert.AreEqual("That road is still closed", GateLine.Road("en"));
            Assert.AreEqual("Ese camino sigue cerrado", GateLine.Road("es"));
            Assert.AreEqual("Radio part recovered", GateLine.Radio("en"));
            Assert.AreEqual("Pieza de radio recuperada", GateLine.Radio("es"));
            Assert.AreEqual("The broadcast is already out", GateLine.Broadcast("en"));
            Assert.AreEqual("La emisión ya salió", GateLine.Broadcast("es"));
            Assert.AreEqual("Extracted", GateLine.Extracted("en"));
            Assert.AreEqual("Extraído", GateLine.Extracted("es"));
            Assert.AreEqual("Save failed", GateLine.SaveFail("en"));
            Assert.AreEqual("No se pudo guardar", GateLine.SaveFail("es"));
            Assert.AreEqual("Game saved", GateLine.Saved("en"));
            Assert.AreEqual("Partida guardada", GateLine.Saved("es"));
            Assert.AreEqual("No save file", GateLine.NoFile("en"));
            Assert.AreEqual("No hay partida", GateLine.NoFile("es"));
            Assert.AreEqual("Save could not be read", GateLine.Unread("en"));
            Assert.AreEqual("No se pudo leer la partida", GateLine.Unread("es"));
            Assert.AreEqual("Save loaded", GateLine.Loaded("en"));
            Assert.AreEqual("Partida cargada", GateLine.Loaded("es"));
            Assert.AreEqual("Autosave loaded", GateLine.AutoLoaded("en"));
            Assert.AreEqual("Autoguardado cargado", GateLine.AutoLoaded("es"));
        }

        [Test]
        public void AYardLineFollowsTheLanguage()
        {
            Assert.AreEqual("Build mode: click the yard", YardSay.Mode(true, "en"));
            Assert.AreEqual("Modo construir: pulsa el patio", YardSay.Mode(true, "es"));
            Assert.AreEqual("Build mode off", YardSay.Mode(false, "en"));
            Assert.AreEqual("Modo construir apagado", YardSay.Mode(false, "es"));
            Assert.AreEqual("Facing 90", YardSay.Facing(90, "en"));
            Assert.AreEqual("Orientación 0", YardSay.Facing(0, "es"));
            Assert.AreEqual("Recovered 4 scrap", YardSay.Recovered(4, "en"));
            Assert.AreEqual("Recovered 0 scrap", YardSay.Recovered(-2, "en"));
            Assert.AreEqual("Recuperados 4 chatarra", YardSay.Recovered(4, "es"));
            Assert.AreEqual("That square is taken", YardSay.Taken("en"));
            Assert.AreEqual("Esa casilla está ocupada", YardSay.Taken("es"));
            Assert.AreEqual("Need 8 camp scrap", YardSay.Need(8, "en"));
            Assert.AreEqual("Need 0 camp scrap", YardSay.Need(-1, "en"));
            Assert.AreEqual("Hacen falta 8 de chatarra", YardSay.Need(8, "es"));
            Assert.AreEqual("Site marked Generator", YardSay.Marked("Generator", "en"));
            Assert.AreEqual("Sitio marcado Generador", YardSay.Marked("Generator", "es"));
            Assert.AreEqual("Site marked TradingPost", YardSay.Marked("TradingPost", "en"));
            Assert.AreEqual("Sitio marcado Puesto", YardSay.Marked("TradingPost", "es"));
            Assert.AreEqual("Site marked Relay", YardSay.Marked("Relay", "en"));
            Assert.AreEqual("Generator is up", YardSay.Up("Generator", "en"));
            Assert.AreEqual("Generador en pie", YardSay.Up("Generator", "es"));
            Assert.AreEqual("Patched Barricade", YardSay.Mend("Barricade", true, "en"));
            Assert.AreEqual("Parcheado Barricada", YardSay.Mend("Barricade", true, "es"));
            Assert.AreEqual("Mended Cot", YardSay.Mend("Cot", false, "en"));
            Assert.AreEqual("Arreglado Camilla", YardSay.Mend("Cot", false, "es"));
            Assert.AreEqual("A barricade gave way", YardSay.Barricade("en"));
            Assert.AreEqual("Una barricada cedió", YardSay.Barricade("es"));
            Assert.AreEqual("The spikes broke", YardSay.Spikes("en"));
            Assert.AreEqual("Los pinchos se rompieron", YardSay.Spikes("es"));
            Assert.AreEqual("The oil catches", YardSay.Oil("en"));
            Assert.AreEqual("El aceite prende", YardSay.Oil("es"));
            Assert.AreEqual("Not enough camp supplies", YardSay.Short("en"));
            Assert.AreEqual("Faltan suministros", YardSay.Short("es"));
            Assert.AreEqual("Stores are full", YardSay.Stores("en"));
            Assert.AreEqual("El almacén está lleno", YardSay.Stores("es"));
            Assert.AreEqual("", YardSay.Kind("", "en"));
        }

        [Test]
        public void ATakedownAndAGunOnTheGroundFollowTheLanguage()
        {
            Assert.AreEqual("Takedown", FightSay.Start("en"));
            Assert.AreEqual("Derribo", FightSay.Start("es"));
            Assert.AreEqual("Takedown slipped", FightSay.Slip("en"));
            Assert.AreEqual("El derribo falló", FightSay.Slip("es"));
            Assert.AreEqual("Down", FightSay.Down("en"));
            Assert.AreEqual("Abajo", FightSay.Down("es"));
            Assert.AreEqual("Pipe bomb burst", FightSay.Burst("en"));
            Assert.AreEqual("La bomba de tubo estalló", FightSay.Burst("es"));
            Assert.AreEqual("Flare lit", FightSay.Flare("en"));
            Assert.AreEqual("Bengala encendida", FightSay.Flare("es"));
            Assert.AreEqual("Already carrying that", FightSay.Held("en"));
            Assert.AreEqual("Ya llevas eso", FightSay.Held("es"));
            Assert.AreEqual("Took Tactical 9mm Pistol", FightSay.Took("pistol_9mm", "", "en"));
            Assert.AreEqual("Tomaste Pistola táctica 9mm", FightSay.Took("pistol_9mm", "Tactical 9mm Pistol", "es"));
            Assert.AreEqual("Took Remington 870 Shotgun", FightSay.Took("shotgun_pump", "", "en"));
            Assert.AreEqual("Tomaste Escopeta Remington 870", FightSay.Took("shotgun_pump", "", "es"));
            Assert.AreEqual("Took Assault Rifle", FightSay.Took("rifle_assault", "", "en"));
            Assert.AreEqual("Tomaste Rifle de asalto", FightSay.Took("rifle_assault", "", "es"));
            Assert.AreEqual("Took Compact SMG", FightSay.Took("smg", "", "en"));
            Assert.AreEqual("Tomaste Subfusil compacto", FightSay.Took("smg", "", "es"));
            Assert.AreEqual("Took Steel Machete", FightSay.Took("machete", "", "en"));
            Assert.AreEqual("Tomaste Machete de acero", FightSay.Took("machete", "", "es"));
            Assert.AreEqual("Took Relay Gun", FightSay.Took("relay", "Relay Gun", "en"));
            Assert.AreEqual("Took relay", FightSay.Took("relay", "", "en"));
            Assert.AreEqual("Swapped weapons", FightSay.Swap("en"));
            Assert.AreEqual("Armas cambiadas", FightSay.Swap("es"));
            Assert.AreEqual("The boards gave way", FightSay.Boards("en"));
            Assert.AreEqual("Los tablones cedieron", FightSay.Boards("es"));
            Assert.AreEqual("The roster is full", FightSay.Roster("en"));
            Assert.AreEqual("La lista está llena", FightSay.Roster("es"));
            Assert.AreEqual("The tower is not ready", FightSay.Tower("en"));
            Assert.AreEqual("La torre no está lista", FightSay.Tower("es"));
            Assert.AreEqual("The board could not be written", FightSay.Ledger("en"));
            Assert.AreEqual("No se pudo escribir el tablero", FightSay.Ledger("es"));
        }

        [Test]
        public void AStallToastFollowsTheLanguage()
        {
            Assert.AreEqual("No caravan until the next visit", StallVoice.Wait("en"));
            Assert.AreEqual("No hay caravana hasta la próxima visita", StallVoice.Wait("es"));
            Assert.AreEqual("Iron Militia will not trade", StallVoice.Refuse(StallVoice.Name("militia", "en"), "en"));
            Assert.AreEqual("Milicia de Hierro no comercia", StallVoice.Refuse(StallVoice.Name("militia", "es"), "es"));
            Assert.AreEqual("The merchant shakes their head", StallVoice.Shake("en"));
            Assert.AreEqual("El mercader niega con la cabeza", StallVoice.Shake("es"));
            Assert.AreEqual("The pack is full", StallVoice.Full("en"));
            Assert.AreEqual("La mochila está llena", StallVoice.Full("es"));
            Assert.AreEqual("Iron Militia deal sealed", StallVoice.Deal("militia", 0, "en"));
            Assert.AreEqual("Iron Militia deal sealed  Rounds +4", StallVoice.Deal("militia", 4, "en"));
            Assert.AreEqual("Milicia de Hierro trato cerrado", StallVoice.Deal("militia", -2, "es"));
            Assert.AreEqual("La Caravana trato cerrado  Balas +6", StallVoice.Deal("caravan", 6, "es"));
            Assert.AreEqual("No bandage to barter", StallVoice.NoBandage("en"));
            Assert.AreEqual("No hay vendaje para trocar", StallVoice.NoBandage("es"));
            Assert.AreEqual("Bartered a bandage for 3 scrap", StallVoice.Bartered(3, "en"));
            Assert.AreEqual("Bartered a bandage for 0 scrap", StallVoice.Bartered(-1, "en"));
            Assert.AreEqual("Cambiaste un vendaje por 3 chatarra", StallVoice.Bartered(3, "es"));
            Assert.AreEqual("The Clinic wants 4 medkits", StallVoice.Quest("clinic", false, "en"));
            Assert.AreEqual("La Clínica pide 4 botiquines", StallVoice.Quest("clinic", false, "es"));
            Assert.AreEqual("Clinic blueprint: field dressings", StallVoice.Blueprint("en"));
            Assert.AreEqual("Plano de la Clínica: vendajes de campaña", StallVoice.Blueprint("es"));
            Assert.AreEqual("Free Farmers remember the cleared nest", StallVoice.Nest("en"));
            Assert.AreEqual("Los Granjeros Libres recuerdan el nido limpio", StallVoice.Nest("es"));
            Assert.AreEqual("The Caravan made it through", StallVoice.Through("en"));
            Assert.AreEqual("La Caravana logró pasar", StallVoice.Through("es"));
            Assert.AreEqual("The Caravan is at the gate", StallVoice.Arrival("caravan", "en"));
            Assert.AreEqual("La Caravana está en la puerta", StallVoice.Arrival("caravan", "es"));
            Assert.AreEqual(CaravanBook.Display("clinic"), StallVoice.Name("clinic", "en"));
        }

        [Test]
        public void AStreetPromptFollowsTheLanguage()
        {
            Assert.AreEqual("Search container", StreetAsk.Search("en"));
            Assert.AreEqual("Busca el contenedor", StreetAsk.Search("es"));
            Assert.AreEqual("Take from container", StreetAsk.Take("en"));
            Assert.AreEqual("Coge del contenedor", StreetAsk.Take("es"));
            Assert.AreEqual("Take the radio part", StreetAsk.Radio("en"));
            Assert.AreEqual("Coge la pieza de radio", StreetAsk.Radio("es"));
            Assert.AreEqual("Search the cache", StreetAsk.Cache("en"));
            Assert.AreEqual("Busca el alijo", StreetAsk.Cache("es"));
            Assert.AreEqual("Recover gear", StreetAsk.Gear("en"));
            Assert.AreEqual("Recupera el equipo", StreetAsk.Gear("es"));
            Assert.AreEqual("Step inside", DoorMap.Prompt(false));
            Assert.AreEqual("Step outside", DoorMap.Prompt(true));
            Assert.AreEqual("Entra", DoorMap.Prompt(false, "es"));
            Assert.AreEqual("Sal", DoorMap.Prompt(true, "es"));
            Assert.AreEqual("Find the cache", ObjectiveTracker.LineFor("cache", false));
            Assert.AreEqual("Radio part stowed", ObjectiveTracker.LineFor("radio", true));
            Assert.AreEqual("", ObjectiveTracker.LineFor("", false));
            Assert.AreEqual("Busca el alijo", ObjectiveTracker.LineFor("cache", false, "es"));
            Assert.AreEqual("Busca la pieza de radio", ObjectiveTracker.LineFor("radio", false, "es"));
            Assert.AreEqual("Alijo registrado", ObjectiveTracker.LineFor("cache", true, "es"));
            Assert.AreEqual("Pieza de radio guardada", ObjectiveTracker.LineFor("radio", true, "es"));
            Assert.AreEqual("Bring Mara along", StreetAsk.Along("Mara", "en"));
            Assert.AreEqual("Trae a Mara", StreetAsk.Along("Mara", "es"));
            Assert.AreEqual("Bring Survivor along", StreetAsk.Along("", "en"));
            Assert.AreEqual("Trae a Superviviente", StreetAsk.Along("Survivor", "es"));
            Assert.AreEqual("Bring Mara to the gate", StreetAsk.ToGate("Mara", "en"));
            Assert.AreEqual("Lleva a Mara a la puerta", StreetAsk.ToGate("Mara", "es"));
            Assert.AreEqual("Mara is with you", StreetAsk.With("Mara", "en"));
            Assert.AreEqual("Mara va contigo", StreetAsk.With("Mara", "es"));
            Assert.AreEqual("Mara takes the gate", StreetAsk.Takes("Mara", "en"));
            Assert.AreEqual("Mara toma la puerta", StreetAsk.Takes("Mara", "es"));
            Assert.AreEqual("Back inside the gate", StreetAsk.Back("en"));
            Assert.AreEqual("De vuelta en la puerta", StreetAsk.Back("es"));
            Assert.AreEqual("Ash Market — Loot the stalls, watch the alleys", StreetAsk.Place("ash_market", "Ash Market", "Loot the stalls, watch the alleys", "en"));
            Assert.AreEqual("Mercado de ceniza — Saquea los puestos, vigila los callejones", StreetAsk.Place("ash_market", "Ash Market", "Loot the stalls, watch the alleys", "es"));
            Assert.AreEqual("Downtown Core — The tower site is past the plaza", StreetAsk.Place("downtown_core", "Downtown Core", "The tower site is past the plaza", "en"));
            Assert.AreEqual("Centro — El sitio de la torre queda tras la plaza", StreetAsk.Place("downtown_core", "", "", "es"));
            Assert.AreEqual("Relay — Still closed", StreetAsk.Place("relay", "Relay", "Still closed", "en"));
        }

        [Test]
        public void ALoadingCardAndAKillTapeFollowTheLanguage()
        {
            Assert.AreEqual("WAKING THE GATE", SceneRoute.Title(FlowStep.Boot));
            Assert.AreEqual("THE SANCTUARY", SceneRoute.Title(FlowStep.Sanctuary, "en"));
            Assert.AreEqual("INTO THE DISTRICT", SceneRoute.Title(FlowStep.Expedition, "en"));
            Assert.AreEqual("BACK INSIDE", SceneRoute.Title(FlowStep.Results, "en"));
            Assert.AreEqual("OUTPOST ZERO", SceneRoute.Title(FlowStep.MainMenu, "en"));
            Assert.AreEqual("DESPERTANDO LA PUERTA", SceneRoute.Title(FlowStep.Boot, "es"));
            Assert.AreEqual("EL SANTUARIO", SceneRoute.Title(FlowStep.Sanctuary, "es"));
            Assert.AreEqual("AL DISTRITO", SceneRoute.Title(FlowStep.Expedition, "es"));
            Assert.AreEqual("DE VUELTA DENTRO", SceneRoute.Title(FlowStep.Results, "es"));
            Assert.AreEqual("OUTPOST ZERO", SceneRoute.Title(FlowStep.MainMenu, "es"));
            Assert.AreEqual(SceneRoute.Tips[0], SceneRoute.Tip(FlowStep.Boot, 0));
            Assert.AreEqual(SceneRoute.Tips[0], SceneRoute.Tip(FlowStep.Boot, 0, "en"));
            Assert.AreEqual("El ruido llega más lejos que el disparo.", SceneRoute.Tip(FlowStep.Boot, 0, "es"));
            Assert.AreEqual(SceneRoute.Tips[9], SceneRoute.Tip(FlowStep.Expedition, 0, "en"));
            Assert.AreEqual("Los barriles de aceite, tóxicos y de pólvora encadenan si los rompes.", SceneRoute.Tip(FlowStep.Expedition, 0, "es"));
            Assert.AreEqual("Walker", KillTape.Name("walker"));
            Assert.AreEqual("Runner\nBrute\nWalker", KillTape.Show("Runner\nBrute\nWalker", "en"));
            Assert.AreEqual("Corredor\nBruto\nCaminante", KillTape.Show("Runner\nBrute\nWalker", "es"));
            Assert.AreEqual("Relay", KillTape.Show("Relay", "es"));
            Assert.AreEqual("", KillTape.Show("", "es"));
            Assert.AreEqual("Take Assault Rifle", FightSay.Lift("rifle_assault", "Assault Rifle", "en"));
            Assert.AreEqual("Llevar Rifle de asalto", FightSay.Lift("rifle_assault", "Assault Rifle", "es"));
            Assert.AreEqual("Take Tactical 9mm Pistol", FightSay.Lift("pistol_9mm", "", "en"));
        }

        [Test]
        public void ACompassMarkFollowsTheLanguage()
        {
            Assert.AreEqual("N   Gate right", StreetHeading.Readout(0f, 1f, 0f, 0f, false, 0f, 0f, true, 10f, 0f));
            Assert.AreEqual("N   Gate behind", StreetHeading.Readout(0f, 1f, 0f, 0f, false, 0f, 0f, true, 0f, -10f));
            Assert.AreEqual("E   POI left", StreetHeading.Readout(1f, 0f, 0f, 0f, true, 0f, 12f, false, 0f, 0f));
            Assert.AreEqual("N   Puerta derecha", StreetHeading.Readout(0f, 1f, 0f, 0f, false, 0f, 0f, true, 10f, 0f, "es"));
            Assert.AreEqual("N   Puerta detrás", StreetHeading.Readout(0f, 1f, 0f, 0f, false, 0f, 0f, true, 0f, -10f, "es"));
            Assert.AreEqual("E   Punto izquierda", StreetHeading.Readout(1f, 0f, 0f, 0f, true, 0f, 12f, false, 0f, 0f, "es"));
            Assert.AreEqual("Gate front", StreetHeading.Mark("Gate", 0f, "en"));
            Assert.AreEqual("Puerta frente", StreetHeading.Mark("Puerta", 0f, "es"));
            Assert.AreEqual("front", StreetHeading.Sector(0f));
        }

        [Test]
        public void AWeaponNameOnTheStreetFollowsTheLanguage()
        {
            Assert.AreEqual("> 1  Pistol", WeaponWheel.Row(0, "Pistol", true));
            Assert.AreEqual("  3  empty", WeaponWheel.Row(2, "", false));
            Assert.AreEqual("  3  vacío", WeaponWheel.Row(2, "", false, "es"));
            Assert.AreEqual("> 1  Rifle de asalto", WeaponWheel.Row(0, FightSay.Gun("rifle_assault", "Assault Rifle", "es"), true, "es"));
            Assert.AreEqual("Assault Rifle", FightSay.Gun("rifle_assault", "Assault Rifle", "en"));
            Assert.AreEqual("Rifle de asalto", FightSay.Gun("rifle_assault", "Assault Rifle", "es"));
            Assert.AreEqual("Steel Machete", FightSay.Gun("machete", "Steel Machete", "en"));
            Assert.AreEqual("Machete de acero", FightSay.Gun(WeaponCard.IdFor(WeaponType.Melee), "Steel Machete", "es"));
        }

        [Test]
        public void AFinishedRunAndAFrameCapFollowTheLanguage()
        {
            var held = RunBoard.Make("Bo", 9, 2, 0, 3, true);
            var fell = RunBoard.Make("Ada", 3, 4, 1, 1, false);
            Assert.AreEqual("Held  day 9  kills 2  lost 0  streets 3", RunBoard.Line(held));
            Assert.AreEqual("Fell  day 3  kills 4  lost 1  streets 1", RunBoard.Line(fell, "en"));
            Assert.AreEqual("Aguantó  día 9  bajas 2  perdidos 0  calles 3", RunBoard.Line(held, "es"));
            Assert.AreEqual("Cayó  día 3  bajas 4  perdidos 1  calles 1", RunBoard.Line(fell, "es"));
            Assert.AreEqual("Auto", PlayOptions.FrameName(0));
            Assert.AreEqual("30 fps", PlayOptions.FrameName(1, "en"));
            Assert.AreEqual("60 fps", PlayOptions.FrameName(2, "es"));
            Assert.AreEqual("120 fps", PlayOptions.FrameName(3));
            Assert.AreEqual("Uncapped", PlayOptions.FrameName(4, "en"));
            Assert.AreEqual("Sin tope", PlayOptions.FrameName(4, "es"));
            Assert.AreEqual("Auto", PlayOptions.FrameName(-1, "es"));
        }

        [Test]
        public void AWatchPageFollowsTheLanguage()
        {
            Assert.AreEqual("Chase  Player  1.3", AiWatch.Line("Chase", "Player", 1.26f));
            Assert.AreEqual("Chase  -  0.0", AiWatch.Line("Chase", "", -1f, "en"));
            Assert.AreEqual("Persecución  Jugador  1.3", AiWatch.Line("Chase", "Player", 1.26f, "es"));
            Assert.AreEqual("Investiga  Mara  0.0", AiWatch.Line("InvestigateNoise", "Mara", 0f, "es"));
            Assert.AreEqual("Ataque  -  2.0", AiWatch.Line("Attack", "", 2f, "es"));
            bool wasOpen = AiWatch.Open;
            if (!AiWatch.Open) AiWatch.Toggle();
            Assert.AreEqual("watch", AiWatch.Page(null, 6));
            Assert.AreEqual("vigía", AiWatch.Page(null, 6, "es"));
            Assert.AreEqual("watch\nChase  Player  1.3", AiWatch.Page(new[] { "Chase  Player  1.3" }, 6, "en"));
            if (AiWatch.Open != wasOpen) AiWatch.Toggle();
            VfxLedger.Reset();
            VfxLedger.Borrow();
            Assert.AreEqual("vfx 1  peak 1", VfxLedger.Line());
            Assert.AreEqual("efectos 1  pico 1", VfxLedger.Line("es"));
            VfxLedger.Reset();
        }

        [Test]
        public void AnExtractLineNamesTheStreetInTheLanguage()
        {
            Assert.AreEqual("Street  kills 0/1  scrap 0/1", ExtractSlip.Line("", -2, 0, -4, 0, "", ""));
            Assert.AreEqual("Ash Market", ExtractSlip.Place("ash_market", "en"));
            Assert.AreEqual("Mercado de ceniza", ExtractSlip.Place("ash_market", "es"));
            Assert.AreEqual("Street", ExtractSlip.Place("", "en"));
            Assert.AreEqual("Calle", ExtractSlip.Place("", "es"));
            Assert.AreEqual("Relay", ExtractSlip.Place("Relay", "en"));
            Assert.AreEqual("Mercado de ceniza  bajas 3/8  chatarra 4/15", ExtractSlip.Line(ExtractSlip.Place("ash_market", "es"), 3, 8, 4, 15, "bajas", "chatarra"));
            Assert.AreEqual("Ash Market  kills 8/8  scrap 15/15", ExtractSlip.Line(ExtractSlip.Place("ash_market", "en"), 8, 8, 15, 15, "kills", "scrap"));
        }

        [Test]
        public void ADayNoteFollowsTheLanguage()
        {
            Assert.AreEqual("A death in the camp", NoteSay.One("grief", "en"));
            Assert.AreEqual("Una muerte en el campamento", NoteSay.One("grief", "es"));
            Assert.AreEqual("A fever spread, The pot cooked", NoteSay.Read("fever, stew", "en"));
            Assert.AreEqual("Una fiebre se extendió, La olla cocinó", NoteSay.Read("fever, stew", "es"));
            Assert.AreEqual("Someone is on their feet", NoteSay.Read("recovery", "en"));
            Assert.AreEqual("Alguien se levanta", NoteSay.Read("recovery", "es"));
            Assert.AreEqual("A friendship formed", NoteSay.One("friendship", "en"));
            Assert.AreEqual("Nació una amistad", NoteSay.One("friendship", "es"));
            Assert.AreEqual("An argument at the table", NoteSay.One("argument", "en"));
            Assert.AreEqual("Una discusión en la mesa", NoteSay.One("argument", "es"));
            Assert.AreEqual("Someone broke", NoteSay.One("breakdown", "en"));
            Assert.AreEqual("Alguien se quebró", NoteSay.One("breakdown", "es"));
            Assert.AreEqual("The camp celebrated", NoteSay.One("celebration", "en"));
            Assert.AreEqual("El campamento celebró", NoteSay.One("celebration", "es"));
            Assert.AreEqual("", NoteSay.Read("", "en"));
            Assert.AreEqual("relay", NoteSay.Read("relay", "es"));
        }

        [Test]
        public void StreetLampsFollowTheDarkAndNoonStaysOut()
        {
            Assert.AreEqual(0f, DayNightCycle.HourToNight(12f), 0.001f);
            Assert.AreEqual(1f, DayNightCycle.HourToNight(23f), 0.001f);
            Assert.AreEqual(0f, LampClock.Factor(0f), 0.001f);
            Assert.AreEqual(0f, LampClock.Factor(-1f), 0.001f);
            Assert.AreEqual(1f, LampClock.Factor(1f), 0.001f);
            Assert.AreEqual(1f, LampClock.Factor(2f), 0.001f);
            Assert.AreEqual(0.5f, LampClock.Factor(0.5f), 0.001f);
            Assert.AreEqual(0f, LampClock.Glow(0f, 0.7f), 0.001f);
            Assert.AreEqual(0.7f, LampClock.Glow(1f, 0.7f), 0.001f);
            Assert.AreEqual(1.4f, LampClock.Glow(1f, 1.4f), 0.001f);
            Assert.AreEqual(0.7f, LampClock.Glow(0.5f, 1.4f), 0.001f);
            Assert.AreEqual(0f, LampClock.Resolve(0f, 12f), 0.001f);
            Assert.AreEqual(1f, LampClock.Resolve(0f, 23f), 0.001f);
            Assert.AreEqual(1f, LampClock.Resolve(1f, 12f), 0.001f);
            Assert.Greater(LampClock.Resolve(0f, 18.5f), 0.4f);
        }

        [Test]
        public void ASodiumLampFlickersAndAWreckTradesItsLamps()
        {
            Assert.AreEqual(9f, SodiumLamp.Range, 0.001f);
            Assert.AreEqual(1f, SodiumLamp.Tint.r, 0.001f);
            Assert.AreEqual(184f / 255f, SodiumLamp.Tint.g, 0.001f);
            Assert.AreEqual(112f / 255f, SodiumLamp.Tint.b, 0.001f);
            Assert.IsTrue(SodiumLamp.Dead(0));
            Assert.IsTrue(SodiumLamp.Dead(1));
            Assert.IsTrue(SodiumLamp.Dead(2));
            Assert.IsFalse(SodiumLamp.Dead(3));
            Assert.IsFalse(SodiumLamp.Dead(9));
            Assert.IsTrue(SodiumLamp.Dead(10));
            Assert.IsTrue(SodiumLamp.Dead(-1));
            Assert.AreEqual(1f, SodiumLamp.Flicker(0f, false), 0.001f);
            Assert.AreEqual(0.78f, SodiumLamp.Flicker(0.28f, false), 0.001f);
            Assert.AreEqual(0f, SodiumLamp.Flicker(0.28f, true), 0.001f);
            Assert.AreEqual(1f, SodiumLamp.Flicker(0.1f, false), 0.001f);
            Assert.AreEqual(1f, FirePulse.Scale(0f, true), 0.001f);
            Assert.AreEqual(1.18f, FirePulse.Scale(0.1125f, true), 0.001f);
            Assert.AreEqual(0.82f, FirePulse.Scale(0.3375f, true), 0.001f);
            Assert.AreEqual(0f, FirePulse.Scale(1f, false), 0.001f);
            Assert.AreEqual(1.6f, FirePulse.Peak, 0.001f);
            Assert.IsTrue(HazardBlink.Lit(0f, true));
            Assert.IsTrue(HazardBlink.Lit(0.27f, true));
            Assert.IsFalse(HazardBlink.Lit(0.28f, true));
            Assert.IsFalse(HazardBlink.Lit(0.69f, true));
            Assert.IsTrue(HazardBlink.Lit(0.7f, true));
            Assert.IsFalse(HazardBlink.Lit(0f, false));
            Assert.IsTrue(HazardBlink.Left(0f, true));
            Assert.IsFalse(HazardBlink.Right(0f, true));
            Assert.IsFalse(HazardBlink.Left(0.4f, true));
            Assert.IsTrue(HazardBlink.Right(0.4f, true));
            Assert.IsFalse(HazardBlink.Right(0f, false));
        }

        [Test]
        public void TheFlashlightShowsAConeAndWallsKeepTheirShadow()
        {
            float rim = 8f * Mathf.Tan(31f * Mathf.Deg2Rad);
            Assert.AreEqual(rim, LampShaft.Radius(LampShaft.Length, LampCookie.Outer), 0.0001f);
            Assert.AreEqual(0f, LampShaft.Radius(0f, 62f), 0.001f);
            Assert.AreEqual(0f, LampShaft.Radius(8f, 0f), 0.001f);
            Assert.AreEqual(13, LampShaft.VertexCount(LampShaft.Sides));
            Assert.AreEqual(36, LampShaft.IndexCount(LampShaft.Sides));
            Assert.AreEqual(1f, LampShaft.Fade(0f), 0.001f);
            Assert.AreEqual(0.25f, LampShaft.Fade(0.5f), 0.001f);
            Assert.AreEqual(0f, LampShaft.Fade(1f), 0.001f);
            Assert.AreEqual(0.22f, LampShaft.Alpha, 0.001f);
            Assert.AreEqual(2, ShadowRig.Cascades);
            Assert.AreEqual(0.2f, ShadowRig.Near, 0.001f);
            Assert.IsTrue(WallSeal.Casts("Building_Storefront_NW"));
            Assert.IsTrue(WallSeal.Casts("Building_Warehouse_NE"));
            Assert.IsFalse(WallSeal.Casts("StreetLamp_NW"));
            Assert.IsFalse(WallSeal.Casts(""));
            Assert.AreEqual(18f, QualityProfile.For(0).ShadowDistance, 0.001f);
            Assert.AreEqual(40f, QualityProfile.For(1).ShadowDistance, 0.001f);
        }

        [Test]
        public void NightBringsAMoonAndTheQuotaPullsDuskForward()
        {
            Assert.AreEqual(0f, DayNightCycle.HourToNight(12f), 0.001f);
            Assert.AreEqual(1f, DayNightCycle.HourToNight(23f), 0.001f);
            Assert.AreEqual(1.15f, SkyGrade.Sun(0f), 0.001f);
            Assert.AreEqual(0.15f, SkyGrade.Sun(1f), 0.001f);
            Assert.AreEqual(0.65f, SkyGrade.Sun(0.5f), 0.001f);
            Assert.AreEqual(0.08f, SkyGrade.NightSky.b, 0.001f);
            Assert.Less(SkyGrade.NightSky.r, SkyGrade.NightSky.b);
            Assert.IsTrue(SkyGrade.NightSky == SkyGrade.Sky(1f));
            Assert.AreEqual(SkyGrade.DaySky, SkyGrade.Sky(0f));
            Assert.AreEqual(1.05f, SkyGrade.Exposure(0f), 0.001f);
            Assert.AreEqual(0.35f, SkyGrade.Exposure(1f), 0.001f);
            Assert.AreEqual(0f, SkyGrade.Job(0f, 8f, 0f, 15f), 0.001f);
            Assert.AreEqual(1f, SkyGrade.Job(8f, 8f, 15f, 15f), 0.001f);
            Assert.AreEqual(0.5f, SkyGrade.Job(4f, 8f, 7.5f, 15f), 0.001f);
            Assert.AreEqual(0f, SkyGrade.JobNight(0.69f), 0.001f);
            Assert.AreEqual(0f, SkyGrade.JobNight(0.70f), 0.001f);
            Assert.AreEqual(0.5f, SkyGrade.JobNight(0.85f), 0.001f);
            Assert.AreEqual(1f, SkyGrade.JobNight(1f), 0.001f);
            Assert.AreEqual(1f, SkyGrade.JobNight(1.4f), 0.001f);
        }

        [Test]
        public void ADistrictBlockGetsOneProbeAndTheYardKeepsASixMeterGrid()
        {
            Assert.AreEqual(6f, ProbeGrid.Step, 0.001f);
            Assert.AreEqual(128, ProbeGrid.Resolution);
            Assert.AreEqual(1.6f, ProbeGrid.Eye, 0.001f);
            Assert.AreEqual(1, ProbeGrid.Span(0f, 0f));
            Assert.AreEqual(3, ProbeGrid.Span(-6f, 6f));
            Assert.AreEqual(11, ProbeGrid.Span(ProbeGrid.YardMin, ProbeGrid.YardMax));
            var local = ProbeGrid.Lights(-6f, 6f, -6f, 6f);
            Assert.AreEqual(9, local.Length);
            Assert.AreEqual(-6f, local[0].x, 0.001f);
            Assert.AreEqual(1.6f, local[0].y, 0.001f);
            Assert.AreEqual(-6f, local[0].z, 0.001f);
            Assert.AreEqual(0f, local[4].x, 0.001f);
            Assert.AreEqual(0f, local[4].z, 0.001f);
            var yard = ProbeGrid.Lights(ProbeGrid.YardMin, ProbeGrid.YardMax, ProbeGrid.YardMin, ProbeGrid.YardMax);
            Assert.AreEqual(121, yard.Length);
            Assert.AreEqual(new Vector3(-4f, 1.6f, 2f), ProbeGrid.Center(-8f, 0f, -4f, 8f));
            var box = ProbeGrid.Box(-14f, 0f, -10f, 16f);
            Assert.AreEqual(16f, box.x, 0.001f);
            Assert.AreEqual(8f, box.y, 0.001f);
            Assert.AreEqual(28f, box.z, 0.001f);
            var tight = ProbeGrid.Box(1f, 2f, 3f, 4f);
            Assert.AreEqual(8f, tight.x, 0.001f);
            Assert.AreEqual(8f, tight.z, 0.001f);
        }

        [Test]
        public void ASpareGunBreaksIntoScrapAtTheBench()
        {
            Assert.IsFalse(StripYield.Can(1, false, true));
            Assert.IsFalse(StripYield.Can(2, true, true));
            Assert.IsFalse(StripYield.Can(2, false, false));
            Assert.IsTrue(StripYield.Can(2, false, true));
            Assert.AreEqual(8, StripYield.Scrap("pistol_9mm"));
            Assert.AreEqual(0, StripYield.Chemicals("pistol_9mm"));
            Assert.AreEqual(12, StripYield.Scrap("shotgun_pump"));
            Assert.AreEqual(1, StripYield.Chemicals("shotgun_pump"));
            Assert.AreEqual(16, StripYield.Scrap("rifle_assault"));
            Assert.AreEqual(1, StripYield.Chemicals("rifle_assault"));
            Assert.AreEqual(10, StripYield.Scrap("smg"));
            Assert.AreEqual(1, StripYield.Chemicals("smg"));
            Assert.AreEqual(0, StripYield.Scrap("machete"));
            Assert.AreEqual(0, StripYield.Scrap(""));
            Assert.IsTrue(StripYield.RoomFor(0, 80, 8, 0));
            Assert.IsTrue(StripYield.RoomFor(0, 80, 16, 1));
            Assert.IsFalse(StripYield.RoomFor(80, 80, 8, 0));
            Assert.IsFalse(StripYield.RoomFor(76, 80, 8, 1));
            Assert.AreEqual("Desguazar arma", Loc.T("camp.strip", "es"));
            Assert.AreEqual("Guarda un arma", Loc.T("camp.strip_none", "es"));
            Assert.AreEqual("El almacén está lleno", Loc.T("camp.strip_full", "es"));
            Assert.AreEqual("Piezas recuperadas", Loc.T("camp.strip_ok", "es"));
        }

        [Test]
        public void ADeepBuilderRaisesExtraAndAFourthShiftDoesNot()
        {
            Assert.AreEqual(0, BuildDepth.Raise(4));
            Assert.AreEqual(0, BuildDepth.Raise(0));
            Assert.AreEqual(0, BuildDepth.Raise(-1));
            Assert.AreEqual(1, BuildDepth.Raise(5));
            Assert.AreEqual(4, BuildDepth.Raise(8));
            Assert.AreEqual(4, BuildDepth.Raise(12));
            Assert.AreEqual(1, Practice.Bonus(4));
            Assert.AreEqual(1, Practice.Bonus(8));
            Assert.AreEqual(2, BuildSite.Shift("Field Engineer", 40f));
            Assert.AreEqual(1, BuildSite.Shift("Steady Hands", 40f));
            Assert.AreEqual(0, BuildSite.Shift("Steady Hands", 5f));
            int trained = BuildSite.Shift("Steady Hands", 40f) + Practice.Bonus(4) + BuildDepth.Raise(4);
            int deep = BuildSite.Shift("Steady Hands", 40f) + Practice.Bonus(8) + BuildDepth.Raise(8);
            Assert.AreEqual(2, trained);
            Assert.AreEqual(6, deep);
            BuildSite.Work(1, 0, 4, trained, out int site, out int hours, out bool done);
            Assert.AreEqual(1, site);
            Assert.AreEqual(2, hours);
            Assert.IsFalse(done);
            BuildSite.Work(1, 0, 4, deep, out site, out hours, out done);
            Assert.AreEqual(0, site);
            Assert.AreEqual(4, hours);
            Assert.IsTrue(done);
        }

        [Test]
        public void ACampMateSquatsAtRestAndSwingsAtTheWall()
        {
            Assert.AreEqual(22f, YardPose.Lean("Rest", 1f), 0.001f);
            Assert.AreEqual(0.72f, YardPose.Scale("Rest"), 0.001f);
            Assert.AreEqual(1f, YardPose.Scale("Guard"), 0.001f);
            Assert.AreEqual(-8f, YardPose.Lean("Guard", 0.4f), 0.001f);
            Assert.AreEqual(0f, YardPose.Stir(0f), 0.001f);
            Assert.AreEqual(0.5f, YardPose.Stir(0.2f), 0.001f);
            Assert.AreEqual(1f, YardPose.Stir(0.4f), 0.001f);
            Assert.AreEqual(28f, YardPose.Lean("Build", 0.4f), 0.001f);
            Assert.AreEqual(14f, YardPose.Lean("Cook", 0.4f), 0.001f);
            Assert.AreEqual(18f, YardPose.Lean("Clear", 0.4f), 0.001f);
            Assert.AreEqual(16f, YardPose.Lean("Medic", 0.2f), 0.001f);
            Assert.AreEqual(10f, YardPose.Lean("Scavenge", 0.2f), 0.001f);
            Assert.AreEqual(0f, YardPose.Lean("Lead", 0.4f), 0.001f);
            Assert.AreEqual(0f, YardPose.Lean(null, 0.4f), 0.001f);
        }

        [Test]
        public void ALampCellDiesAndAnOldSaveStartsFull()
        {
            Assert.AreEqual(96f, LampCell.Tick(100f, true, 1f), 0.001f);
            Assert.AreEqual(0f, LampCell.Tick(4f, true, 1f), 0.001f);
            Assert.AreEqual(0f, LampCell.Tick(0f, true, 1f), 0.001f);
            Assert.AreEqual(2f, LampCell.Tick(0f, false, 1f), 0.001f);
            Assert.AreEqual(100f, LampCell.Tick(99f, false, 1f), 0.001f);
            Assert.AreEqual(50f, LampCell.Tick(50f, true, 0f), 0.001f);
            Assert.AreEqual(50f, LampCell.Tick(50f, true, -1f), 0.001f);
            Assert.IsFalse(LampCell.Live(0f));
            Assert.IsFalse(LampCell.Live(0.5f));
            Assert.IsTrue(LampCell.Live(0.51f));
            Assert.AreEqual(1f, LampCell.Beam(100f), 0.001f);
            Assert.AreEqual(0f, LampCell.Beam(0f), 0.001f);
            Assert.AreEqual(0.675f, LampCell.Beam(50f), 0.001f);
            Assert.AreEqual(2.8f, LampCell.Intensity(100f), 0.001f);
            Assert.AreEqual(0f, LampCell.Intensity(0f), 0.001f);
            Assert.AreEqual(0f, LampCell.Spent(100f), 0.001f);
            Assert.AreEqual(60f, LampCell.Spent(40f), 0.001f);
            Assert.AreEqual(100f, LampCell.FromSpent(0f), 0.001f);
            Assert.AreEqual(0f, LampCell.FromSpent(100f), 0.001f);
            Assert.AreEqual(100f, LampCell.FromSpent(-4f), 0.001f);
            Assert.AreEqual(1f, SpotRange.Exposure(true, false, true, 1f, 0f), 0.001f);
            Assert.IsTrue(SaveCodec.TryDeserialize("{\"schemaVersion\":1}", out var legacy, out var error), error);
            Assert.AreEqual(0f, legacy.lampSpent, 0.001f);
            Assert.AreEqual(100f, LampCell.FromSpent(legacy.lampSpent), 0.001f);
            Assert.AreEqual("Linterna", Loc.T("hud.lamp", "es"));
        }

        [Test]
        public void ALampCellFromTheBenchFillsSixtyAndTheStreetStaysPut()
        {
            var cell = ItemCatalog.Find("cell");
            Assert.IsNotNull(cell);
            Assert.AreEqual(ItemUse.Cell, cell.Use);
            Assert.AreEqual(0.15f, cell.Weight, 0.001f);
            Assert.AreEqual("Fills the lamp by 60", ItemBrief.Effect(cell));
            Assert.AreEqual("Pila", Loc.Item("cell", "es"));
            Assert.AreEqual("Llena la linterna 60", Loc.T("unit.cell", "es"));
            Assert.AreEqual(60f, LampCell.Pack, 0.001f);
            Assert.AreEqual(60f, LampCell.Fill(0f, LampCell.Pack), 0.001f);
            Assert.AreEqual(100f, LampCell.Fill(50f, LampCell.Pack), 0.001f);
            Assert.AreEqual(100f, LampCell.Fill(100f, LampCell.Pack), 0.001f);
            Assert.AreEqual(40f, LampCell.Fill(40f, 0f), 0.001f);
            Assert.IsTrue(LampCell.Tops(100f));
            Assert.IsFalse(LampCell.Tops(99f));
            Assert.AreEqual(96f, LampCell.Tick(100f, true, 1f), 0.001f);
            Assert.IsTrue(CraftBill.TryOf("cell", out var bill));
            Assert.AreEqual(3, bill.Scrap);
            Assert.AreEqual(1, bill.Chemicals);
            Assert.AreEqual(0, bill.Cloth);
            Assert.AreEqual(CraftBill.Workbench, bill.Station);
            Assert.AreEqual(1, CraftGate.TierOf("cell"));
            Assert.IsTrue(CraftGate.Open("cell", 1, ""));
            Assert.AreEqual("Pila", Loc.T("recipe.cell", "es"));
            var street = LootTables.Roll("street", 2);
            Assert.AreEqual(7, street.Length);
            Assert.AreEqual("raw_food", street[6].ItemId);
            Assert.AreEqual("pipe_bomb", street[5].ItemId);
        }

        [Test]
        public void ABeltMolotovLeavesTheHandAndACellStaysInThePack()
        {
            Assert.AreEqual(TossKind.Fire, TossKind.Of("molotov"));
            Assert.AreEqual(TossKind.Lure, TossKind.Of("noise_lure"));
            Assert.AreEqual(TossKind.Flare, TossKind.Of("flare"));
            Assert.AreEqual(TossKind.Bomb, TossKind.Of("pipe_bomb"));
            Assert.AreEqual(TossKind.None, TossKind.Of("cell"));
            Assert.AreEqual(TossKind.None, TossKind.Of("medkit"));
            Assert.AreEqual(TossKind.None, TossKind.Of(null));
            Assert.AreEqual(TossKind.None, TossKind.Of(""));
            Assert.IsTrue(TossKind.Throws("molotov"));
            Assert.IsFalse(TossKind.Throws("cell"));
            Assert.AreEqual(18f, ThrowArc.NoiseRadius(false), 0.01f);
            Assert.AreEqual(42f, PipeBlast.Damage, 0.001f);
            Assert.AreEqual(1.2f, PipeBlast.Fuse, 0.001f);
            Assert.AreEqual(20f, FlareClock.Duration, 0.001f);
            Assert.AreEqual("Nada que lanzar", Loc.T("toss.none", "es"));
            Assert.AreEqual(28f, FirePatch.Burst, 0.001f);
            Assert.AreEqual(3.2f, FirePatch.BurstRadius, 0.001f);
            Assert.IsTrue(FirePatch.Hot(0f));
            Assert.IsTrue(FirePatch.Hot(3.9f));
            Assert.IsFalse(FirePatch.Hot(4f));
            Assert.IsFalse(FirePatch.Hot(-0.1f));
            Assert.IsTrue(FirePatch.Inside(2.4f));
            Assert.IsFalse(FirePatch.Inside(2.41f));
            Assert.IsFalse(FirePatch.Inside(-1f));
            Assert.IsTrue(FirePatch.TickDue(-1f, 0f));
            Assert.IsFalse(FirePatch.TickDue(0f, 0.4f));
            Assert.IsTrue(FirePatch.TickDue(0f, 0.5f));
            Assert.IsFalse(FirePatch.TickDue(3.6f, 4f));
            Assert.AreEqual(6f, FirePatch.Damage, 0.001f);
            Assert.AreEqual(14f, OilBurn.Damage, 0.001f);
            Assert.AreEqual(8f, OilBurn.Duration, 0.001f);
            Assert.AreEqual(42f, PipeBlast.Damage, 0.001f);
            Assert.AreEqual(4.2f, PipeBlast.Radius, 0.001f);
            Assert.AreEqual(1.6f, PipeBlast.Shove, 0.001f);
            Assert.AreEqual(0.55f, PipeBlast.Stun, 0.001f);
            Assert.AreEqual(0.55f, HitStun.Resist(PipeBlast.Stun, false), 0.001f);
            Assert.AreEqual(0.165f, HitStun.Resist(PipeBlast.Stun, true), 0.001f);
            Assert.IsTrue(BlastBall.Shows(HazardKind.Explosive));
            Assert.IsTrue(BlastWake.Ring(HazardKind.Explosive));
            Assert.IsFalse(BlastBall.Shows(HazardKind.Oil));
            var street = LootTables.Roll("street", 2);
            Assert.AreEqual(7, street.Length);
            Assert.AreEqual("raw_food", street[6].ItemId);
        }

        [Test]
        public void ADeepScavengerBringsExtraScrapAndSometimesACell()
        {
            Assert.AreEqual(0, ScrapDepth.Extra(4));
            Assert.AreEqual(0, ScrapDepth.Extra(0));
            Assert.AreEqual(0, ScrapDepth.Extra(-1));
            Assert.AreEqual(1, ScrapDepth.Extra(5));
            Assert.AreEqual(4, ScrapDepth.Extra(8));
            Assert.AreEqual(4, ScrapDepth.Extra(12));
            Assert.AreEqual(1, Practice.Bonus(4));
            Assert.AreEqual(1, Practice.Bonus(8));
            Assert.IsFalse(HaulCell.Due(5, 3));
            Assert.IsFalse(HaulCell.Due(1, 4));
            Assert.IsTrue(HaulCell.Due(5, 4));
            Assert.IsFalse(HaulCell.Due(1, 8));
            Assert.IsTrue(HaulCell.Due(3, 8));
            Assert.IsFalse(HaulCell.Due(0, 0));
            Assert.IsTrue(HaulCell.Due(-5, 4));
            CraftBill.Salvage(1, false, out int cloth, out int chemicals, out int tape);
            Assert.AreEqual(1, cloth);
            Assert.AreEqual(1, chemicals);
            Assert.AreEqual(0, tape);
            Assert.IsTrue(SaveCodec.TryDeserialize("{\"schemaVersion\":1}", out var legacy, out var error), error);
            Assert.AreEqual(0, legacy.cells);
            Assert.AreEqual("Pilas", Loc.T("camp.cell", "es"));
        }

        [Test]
        public void ADeepMedicMendsHarderAndADeepGuardHoldsMore()
        {
            Assert.AreEqual(1, Practice.Bonus(4));
            Assert.AreEqual(1, Practice.Bonus(8));
            Assert.AreEqual(0, MedDepth.Mend(4));
            Assert.AreEqual(0, MedDepth.Mend(0));
            Assert.AreEqual(0, MedDepth.Mend(-1));
            Assert.AreEqual(6, MedDepth.Mend(5));
            Assert.AreEqual(24, MedDepth.Mend(8));
            Assert.AreEqual(24, MedDepth.Mend(12));
            Assert.AreEqual(18f, 12f + Practice.Bonus(4) * 6f + MedDepth.Mend(4), 0.001f);
            Assert.AreEqual(42f, 12f + Practice.Bonus(8) * 6f + MedDepth.Mend(8), 0.001f);
            Assert.AreEqual(0, GuardDepth.Post(4));
            Assert.AreEqual(0, GuardDepth.Post(0));
            Assert.AreEqual(1, GuardDepth.Post(5));
            Assert.AreEqual(4, GuardDepth.Post(8));
            Assert.AreEqual(4, GuardDepth.Post(12));
            Assert.AreEqual(0, TraitHook.WatchCost("Brave"));
            Assert.AreEqual(2, TraitHook.WatchPay("Brave", 2));
            Assert.AreEqual(2, TraitHook.WatchPay("Brave", 1 + Practice.Bonus(4) + GuardDepth.Post(4)));
            Assert.AreEqual(6, TraitHook.WatchPay("Brave", 1 + Practice.Bonus(8) + GuardDepth.Post(8)));
            Assert.AreEqual(0, TraitHook.WatchPay("Cowardly", 1 + Practice.Bonus(8) + GuardDepth.Post(8)));
        }

        [Test]
        public void ADeepLeaderQuietsALouderFeud()
        {
            Assert.AreEqual(6, MealTable.FeudShift(6, false));
            Assert.AreEqual(3, MealTable.FeudShift(6, true));
            Assert.AreEqual(3, MealTable.FeudShift(6, true, 3));
            Assert.AreEqual(2, MealTable.FeudShift(6, true, 4));
            Assert.AreEqual(1, MealTable.FeudShift(6, true, 5));
            Assert.AreEqual(0, MealTable.FeudShift(6, true, 8));
            Assert.AreEqual(0, MealTable.FeudShift(6, true, 12));
            Assert.AreEqual(6, MealTable.FeudShift(6, false, 8));
            Assert.AreEqual(2, LeadDepth.Ease(2, 4));
            Assert.AreEqual(2, LeadDepth.Ease(2, 0));
            Assert.AreEqual(1, LeadDepth.Ease(2, 5));
            Assert.AreEqual(0, LeadDepth.Ease(2, 8));
            Assert.AreEqual(0, LeadDepth.Ease(0, 8));
            Assert.AreEqual(0, LeadDepth.Ease(-1, 8));
        }

        [Test]
        public void APartnerGrievesHarderAndARivalSoursTheShift()
        {
            Assert.AreEqual("Partner", BondMark.Kind(80));
            Assert.AreEqual("Partner", BondMark.Kind(100));
            Assert.AreEqual("Friend", BondMark.Kind(40));
            Assert.AreEqual("", BondMark.Kind(39));
            Assert.AreEqual("", BondMark.Kind(-39));
            Assert.AreEqual("Rival", BondMark.Kind(-40));
            Assert.AreEqual("", KinBoard.Bitter(""));
            Assert.AreEqual("", KinBoard.Bitter("ellis:-39"));
            Assert.AreEqual("ellis", KinBoard.Bitter("ellis:-40"));
            Assert.AreEqual("ellis", KinBoard.Bitter("jonas:-40|ellis:-80"));
            Assert.AreEqual("ellis", KinBoard.Closest("jonas:40|ellis:80"));
            Assert.AreEqual("Pareja", Loc.T("bond.partner", "es"));
            Assert.AreEqual("Amigo", Loc.T("bond.friend", "es"));
            Assert.AreEqual("Rival", Loc.T("bond.rival", "es"));

            var partnered = new List<ColonistDay>
            {
                new ColonistDay { id = "jonas", task = "Guard", kin = "mara:80", morale = 80f, hunger = 78f, thirst = 78f },
                new ColonistDay { id = "ellis", task = "Scavenge", morale = 80f, hunger = 78f, thirst = 78f },
                new ColonistDay { id = "mara", name = "Mara Quill", alive = false, task = "Fallen" }
            };
            int food = 0;
            int water = 0;
            var grief = ColonyDay.Simulate(partnered, ref food, ref water, false, false, "Mara Quill");
            Assert.AreEqual(28f, partnered[0].morale, 0.001f);
            Assert.AreEqual(55f, partnered[1].morale, 0.001f);
            Assert.Contains("grief", grief);

            var rivals = new List<ColonistDay>
            {
                new ColonistDay { id = "jonas", task = "Guard", morale = 80f, hunger = 78f, thirst = 78f, opinion = 20, kin = "ellis:-40" },
                new ColonistDay { id = "ellis", task = "Guard", morale = 80f, hunger = 78f, thirst = 78f, opinion = 20 }
            };
            ColonyDay.Simulate(rivals, ref food, ref water, false, false, "");
            Assert.AreEqual(18, rivals[0].opinion);
            Assert.AreEqual(22, rivals[1].opinion);
            Assert.AreEqual("ellis:-38", rivals[0].kin);
        }

        [Test]
        public void BloodyBootsPrintSixStepsThenDry()
        {
            Assert.AreEqual(0, BootPrint.Charge(4, true, 0));
            Assert.AreEqual(6, BootPrint.Charge(0, true, 1));
            Assert.AreEqual(4, BootPrint.Charge(4, false, 1));
            Assert.AreEqual(6, BootPrint.Charge(2, true, 2));
            Assert.IsFalse(BootPrint.Due(0, 1));
            Assert.IsFalse(BootPrint.Due(4, 0));
            Assert.IsTrue(BootPrint.Due(4, 1));
            Assert.AreEqual(5, BootPrint.Spend(6));
            Assert.AreEqual(0, BootPrint.Spend(0));
            Assert.AreEqual(-0.12f, BootPrint.Side(6), 0.001f);
            Assert.AreEqual(0.12f, BootPrint.Side(5), 0.001f);
            Assert.AreEqual(0.16f, BootPrint.Size(1), 0.001f);
            Assert.AreEqual(0.22f, BootPrint.Size(2), 0.001f);
            Assert.AreEqual(0.42f, GoreMark.Size("blood", 1), 0.001f);
            Assert.IsTrue(BootPrint.Near(0f, 0f, 1.4f, 0f));
            Assert.IsFalse(BootPrint.Near(0f, 0f, 1.41f, 0f));
            Assert.IsFalse(BootPrint.Through(0f, 0f, null, null, 1));
            Assert.IsTrue(BootPrint.Through(0f, 0f, new[] { 3f, 0.2f }, new[] { 0f, 0f }, 2));
            Assert.IsFalse(BootPrint.Through(0f, 0f, new[] { 3f }, new[] { 0f }, 1));
            Assert.AreEqual(0, GoreMark.Splats(0, false, true));
            Assert.AreEqual(5, GoreMark.Splats(2, true, true));
        }

        [Test]
        public void ACompactSmgFiresFasterThanTheRifle()
        {
            var rifle = WeaponCard.Find("rifle_assault");
            var smg = WeaponCard.Find("smg");
            Assert.AreEqual(26f, rifle.Damage, 0.001f);
            Assert.AreEqual(9f, rifle.Rate, 0.001f);
            Assert.AreEqual(30, rifle.Magazine);
            Assert.AreEqual(34f, rifle.Noise, 0.001f);
            Assert.AreEqual(16f, smg.Damage, 0.001f);
            Assert.AreEqual(14f, smg.Rate, 0.001f);
            Assert.AreEqual(22f, smg.Range, 0.001f);
            Assert.AreEqual(5.5f, smg.Spread, 0.001f);
            Assert.AreEqual(25, smg.Magazine);
            Assert.AreEqual(18f, smg.Noise, 0.001f);
            Assert.AreEqual(WeaponType.SMG, smg.Type);
            Assert.IsTrue(smg.Automatic);
            Assert.IsTrue(smg.Projectile);
            Assert.AreEqual("smg", WeaponCard.IdFor(WeaponType.SMG));
            var rounds = ItemCatalog.Find("ammo_smg");
            Assert.AreEqual(ItemUse.Ammo, rounds.Use);
            Assert.AreEqual(WeaponType.SMG, rounds.AmmoType);
            Assert.AreEqual(25, rounds.AmmoAmount);
            Assert.AreEqual(0.05f, rounds.Weight, 0.001f);
            Assert.IsTrue(CraftBill.TryOf("ammo_smg", out var bill));
            Assert.AreEqual(6, bill.Scrap);
            Assert.AreEqual(1, bill.Chemicals);
            Assert.AreEqual(5, AmmoPress.Rounds("ammo_smg", 1));
            Assert.AreEqual(24, AmmoPress.Rounds("ammo_smg", 5));
            Assert.AreEqual(8, AmmoPress.Rounds("ammo_rifle", 1));
            Assert.AreEqual(7, CaravanBook.BasePrice("ammo_smg"));
            Assert.AreEqual(3, CaravanBook.Stock("militia").Length);
            Assert.AreEqual("ammo_smg", CaravanBook.Stock("militia")[2]);
            var street = LootTables.Roll("street", 2);
            Assert.AreEqual(7, street.Length);
            Assert.AreEqual("raw_food", street[6].ItemId);
            Assert.AreEqual("Cargador de subfusil", Loc.T("item.ammo_smg", "es"));
            Assert.AreEqual("Sirve para el subfusil.", Loc.T("blurb.ammo_smg", "es"));
        }

        [Test]
        public void AShotgunPelletFadesPastFourMeters()
        {
            var shotgun = WeaponCard.Find("shotgun_pump");
            Assert.AreEqual(19f, shotgun.Damage, 0.001f);
            Assert.AreEqual(16f, shotgun.Range, 0.001f);
            Assert.AreEqual(7, shotgun.Pellets);
            Assert.AreEqual(1f, PelletDrop.Scale(0f, 16f, WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(1f, PelletDrop.Scale(4f, 16f, WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(0.675f, PelletDrop.Scale(10f, 16f, WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(0.35f, PelletDrop.Scale(16f, 16f, WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(0.35f, PelletDrop.Scale(20f, 16f, WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(1f, PelletDrop.Scale(-2f, 16f, WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(1f, PelletDrop.Scale(10f, 16f, WeaponType.Rifle), 0.001f);
            Assert.AreEqual(1f, PelletDrop.Scale(10f, 16f, WeaponType.SMG), 0.001f);
            Assert.AreEqual(1f, PelletDrop.Scale(10f, 16f, WeaponType.Pistol), 0.001f);
            Assert.AreEqual(19f, PelletDrop.Damage(19f, 4f, 16f, WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(6.65f, PelletDrop.Damage(19f, 16f, 16f, WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(26f, PelletDrop.Damage(26f, 16f, 32f, WeaponType.Rifle), 0.001f);
            Assert.AreEqual(2f, DamageResolver.HeadshotMultiplier, 0.001f);
            Assert.AreEqual(1.45f, DamageResolver.HeadshotHeight, 0.001f);
        }

        [Test]
        public void AShotInTheLegSlowsTheChase()
        {
            Assert.IsTrue(LimbCut.Leg(0.4f, 0f));
            Assert.IsTrue(LimbCut.Leg(0.05f, 0f));
            Assert.IsTrue(LimbCut.Leg(0.62f, 0f));
            Assert.IsFalse(LimbCut.Leg(0.04f, 0f));
            Assert.IsFalse(LimbCut.Leg(0.63f, 0f));
            Assert.IsFalse(LimbCut.Leg(1.0f, 0f));
            Assert.IsFalse(LimbCut.Leg(1.5f, 0f));
            Assert.AreEqual(4f, LimbCut.Seconds, 0.001f);
            Assert.AreEqual(4.6f, LimbCut.Speed(4.6f, false, false), 0.001f);
            Assert.AreEqual(2.53f, LimbCut.Speed(4.6f, true, false), 0.001f);
            Assert.AreEqual(3.588f, LimbCut.Speed(4.6f, true, true), 0.001f);
            Assert.AreEqual(0.99f, LimbCut.Speed(1.8f, true, false), 0.001f);
            Assert.AreEqual(0f, LimbCut.Speed(-2f, true, false), 0.001f);
            Assert.AreEqual(3f, LimbCut.Tick(4f, 1f), 0.001f);
            Assert.AreEqual(0f, LimbCut.Tick(0.2f, 1f), 0.001f);
            Assert.AreEqual(0f, LimbCut.Tick(-1f, 1f), 0.001f);
            Assert.AreEqual(4f, LimbCut.Tick(4f, 0f), 0.001f);
        }

        [Test]
        public void AToxicCloudSlowsThePackAndDoesNotBurnThem()
        {
            Assert.AreEqual(4f, GasCloud.Radius, 0.001f);
            Assert.AreEqual(25f, GasCloud.Life, 0.001f);
            Assert.AreEqual(0.62f, GasCloud.Pace, 0.001f);
            Assert.AreEqual(0f, GasCloud.Hurt, 0.001f);
            Assert.IsTrue(GasCloud.Live(0f));
            Assert.IsTrue(GasCloud.Live(24.9f));
            Assert.IsFalse(GasCloud.Live(25f));
            Assert.IsFalse(GasCloud.Live(-0.1f));
            Assert.IsTrue(GasCloud.Inside(4f, 0f));
            Assert.IsFalse(GasCloud.Inside(4.01f, 0f));
            Assert.IsFalse(GasCloud.Inside(3f, 3f));
            Assert.IsTrue(GasCloud.Covers(3f, 1f, 0f, 0f, 1f));
            Assert.IsFalse(GasCloud.Covers(3f, 1f, 0f, 0f, 25f));
            Assert.IsFalse(GasCloud.Covers(6f, 0f, 0f, 0f, 1f));
            Assert.AreEqual(4.6f, GasCloud.Speed(4.6f, false), 0.001f);
            Assert.AreEqual(2.852f, GasCloud.Speed(4.6f, true), 0.001f);
            Assert.AreEqual(0f, GasCloud.Speed(-2f, true), 0.001f);
            Assert.AreEqual(4.5f, BlastWake.Hold(HazardKind.Toxic), 0.001f);
        }

        [Test]
        public void AShotLightsTheOilOnTheStreet()
        {
            Assert.AreEqual(1.6f, StreetSlick.Radius, 0.001f);
            Assert.AreEqual(12f, StreetSlick.Life, 0.001f);
            Assert.AreEqual(2.8f, StreetSlick.Light, 0.001f);
            Assert.IsTrue(StreetSlick.Wet(0f));
            Assert.IsTrue(StreetSlick.Wet(11.9f));
            Assert.IsFalse(StreetSlick.Wet(12f));
            Assert.IsFalse(StreetSlick.Wet(-0.1f));
            Assert.IsTrue(StreetSlick.On(1.6f, 0f, 0f, 0f));
            Assert.IsFalse(StreetSlick.On(1.61f, 0f, 0f, 0f));
            Assert.IsTrue(StreetSlick.Crosses(-5f, 0f, 5f, 0f, 0f, 0f));
            Assert.IsFalse(StreetSlick.Crosses(-5f, 3f, 5f, 3f, 0f, 0f));
            Assert.IsTrue(StreetSlick.Crosses(0f, 0f, 0.4f, 0f, 0f, 0f));
            Assert.IsFalse(StreetSlick.Crosses(4f, 4f, 6f, 6f, 0f, 0f));
            Assert.IsTrue(StreetSlick.Near(2.8f, 0f, 0f, 0f));
            Assert.IsFalse(StreetSlick.Near(2.81f, 0f, 0f, 0f));
            Assert.AreEqual(3.5f, OilBurn.Ignite, 0.001f);
            Assert.AreEqual(14f, OilBurn.Damage, 0.001f);
            Assert.AreEqual(4f, FirePatch.Life, 0.001f);
        }

        [Test]
        public void ABodyKeepsBurningAfterItLeavesTheFire()
        {
            Assert.AreEqual(3.5f, Ember.Seconds, 0.001f);
            Assert.AreEqual(0.5f, Ember.Gap, 0.001f);
            Assert.AreEqual(4f, Ember.Damage, 0.001f);
            Assert.AreEqual(1.4f, Ember.Spread, 0.001f);
            Assert.IsFalse(Ember.Alight(0f));
            Assert.IsTrue(Ember.Alight(0.2f));
            Assert.AreEqual(3.5f, Ember.Catch(0f), 0.001f);
            Assert.AreEqual(3.5f, Ember.Catch(-1f), 0.001f);
            Assert.AreEqual(1.2f, Ember.Catch(1.2f), 0.001f);
            Assert.AreEqual(3f, Ember.Tick(3.5f, 0.5f), 0.001f);
            Assert.AreEqual(0f, Ember.Tick(0.2f, 0.5f), 0.001f);
            Assert.AreEqual(3.5f, Ember.Tick(3.5f, 0f), 0.001f);
            Assert.IsTrue(Ember.Due(3.5f, 3f));
            Assert.IsFalse(Ember.Due(2.6f, 2.5f));
            Assert.IsFalse(Ember.Due(0.4f, 0f));
            Assert.IsFalse(Ember.Due(0f, 0f));
            Assert.IsTrue(Ember.Reaches(1.4f, 0f));
            Assert.IsFalse(Ember.Reaches(1.41f, 0f));
            Assert.AreEqual(6f, FirePatch.Damage, 0.001f);
            Assert.AreEqual(4f, FirePatch.Life, 0.001f);
        }

        [Test]
        public void APowderBarrelLeavesAFireOnTheStreet()
        {
            Assert.AreEqual(10f, PowderBed.Life, 0.001f);
            Assert.AreEqual(1f, PowderBed.Gap, 0.001f);
            Assert.AreEqual(3.2f, PowderBed.Radius, 0.001f);
            Assert.AreEqual(8f, PowderBed.Damage, 0.001f);
            Assert.IsTrue(PowderBed.Hot(0f));
            Assert.IsTrue(PowderBed.Hot(9.9f));
            Assert.IsFalse(PowderBed.Hot(10f));
            Assert.IsFalse(PowderBed.Hot(-0.1f));
            Assert.IsTrue(PowderBed.Inside(3.2f));
            Assert.IsFalse(PowderBed.Inside(3.21f));
            Assert.IsFalse(PowderBed.Inside(-1f));
            Assert.IsTrue(PowderBed.TickDue(-1f, 0f));
            Assert.IsFalse(PowderBed.TickDue(0f, 0.9f));
            Assert.IsTrue(PowderBed.TickDue(0f, 1f));
            Assert.IsFalse(PowderBed.TickDue(9.1f, 10f));
            Assert.AreEqual(28f, FirePatch.Burst, 0.001f);
            Assert.AreEqual(4f, FirePatch.Life, 0.001f);
            Assert.AreEqual(6f, FirePatch.Damage, 0.001f);
        }

        [Test]
        public void AFireOnTheStreetBreaksTheView()
        {
            Assert.IsTrue(SmokeVeil.Between(-4f, 0f, 4f, 0f, 0f, 0f, 2.4f));
            Assert.IsFalse(SmokeVeil.Between(1f, 0f, 6f, 0f, 0f, 0f, 2.4f));
            Assert.IsFalse(SmokeVeil.Between(-4f, 0f, 1f, 0f, 0f, 0f, 2.4f));
            Assert.IsFalse(SmokeVeil.Between(-5f, 3f, 5f, 3f, 0f, 0f, 2.4f));
            Assert.IsFalse(SmokeVeil.Between(-4f, 0f, 4f, 0f, 0f, 0f, 0f));
            Assert.IsFalse(SmokeVeil.Between(-4f, 0f, 4f, 0f, 0f, 0f, -1f));
            Assert.IsTrue(SmokeVeil.Between(-6f, 0f, 6f, 0f, 0f, 0f, 3.2f));
            Assert.IsFalse(SmokeVeil.Inside(2.41f, 0f, 0f, 0f, 2.4f));
            Assert.IsTrue(SmokeVeil.Inside(2.4f, 0f, 0f, 0f, 2.4f));
            Assert.AreEqual(2.4f, FirePatch.Radius, 0.001f);
            Assert.AreEqual(3.2f, PowderBed.Radius, 0.001f);
            Assert.AreEqual(1.8f, CoverSight.Reach, 0.001f);
        }

        [Test]
        public void APlayerKeepsBurningAfterTheyLeaveTheFire()
        {
            Assert.AreEqual("On fire", Loc.T("hud.burn", "en"));
            Assert.AreEqual("En llamas", Loc.T("hud.burn", "es"));
            Assert.AreEqual("You're on fire", Loc.T("burn.you", "en"));
            Assert.AreEqual("Estás en llamas", Loc.T("burn.you", "es"));
            Assert.AreEqual(3.5f, Ember.Catch(0f), 0.001f);
            Assert.AreEqual(2f, Ember.Catch(2f), 0.001f);
            Assert.IsTrue(Ember.Due(3.5f, 3f));
            Assert.IsFalse(Ember.Due(0.4f, 0f));
            Assert.AreEqual(4f, Ember.Damage, 0.001f);
            Assert.AreEqual(1.4f, Ember.Spread, 0.001f);
            Assert.AreEqual(3.5f, Ember.Seconds, 0.001f);
        }

        [Test]
        public void AMissedRoundStillCracksPastANearbyBody()
        {
            Assert.AreEqual(1.1f, WhiffClock.Reach, 0.001f);
            Assert.AreEqual(0.45f, WhiffClock.Seconds, 0.001f);
            Assert.AreEqual(0.72f, WhiffClock.Pace, 0.001f);
            Assert.IsTrue(WhiffClock.Passes(0f, 0f, 10f, 0f, 5f, 1.1f, out float nearX, out float nearZ));
            Assert.AreEqual(5f, nearX, 0.001f);
            Assert.AreEqual(0f, nearZ, 0.001f);
            Assert.IsFalse(WhiffClock.Passes(0f, 0f, 10f, 0f, 5f, 1.11f, out _, out _));
            Assert.IsTrue(WhiffClock.Passes(0f, 0f, 10f, 0f, 5f, 0f, out _, out _));
            Assert.IsFalse(WhiffClock.Passes(0f, 0f, 4f, 0f, 8f, 0f, out _, out _));
            Assert.AreEqual(4.6f, WhiffClock.Speed(4.6f, false), 0.001f);
            Assert.AreEqual(3.312f, WhiffClock.Speed(4.6f, true), 0.001f);
            Assert.AreEqual(0f, WhiffClock.Speed(-1f, true), 0.001f);
            Assert.AreEqual(0.25f, WhiffClock.Tick(0.45f, 0.2f), 0.001f);
            Assert.AreEqual(0f, WhiffClock.Tick(0.1f, 0.2f), 0.001f);
            Assert.AreEqual(0.6f, HitStun.Seconds(WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(0.25f, HitStun.Seconds(WeaponType.Pistol), 0.001f);
        }

        [Test]
        public void RainEatsAFireFasterThanAClearStreet()
        {
            Assert.AreEqual(1f, RainQuench.Pace(WeatherKind.Clear), 0.001f);
            Assert.AreEqual(1f, RainQuench.Pace(WeatherKind.Fog), 0.001f);
            Assert.AreEqual(1f, RainQuench.Pace(WeatherKind.Overcast), 0.001f);
            Assert.AreEqual(2.5f, RainQuench.Pace(WeatherKind.Rain), 0.001f);
            Assert.AreEqual(3.5f, RainQuench.Pace(WeatherKind.Storm), 0.001f);
            Assert.AreEqual(1f, RainQuench.Step(0.4f, WeatherKind.Rain), 0.001f);
            Assert.AreEqual(1.4f, RainQuench.Step(0.4f, WeatherKind.Storm), 0.001f);
            Assert.AreEqual(0.4f, RainQuench.Step(0.4f, WeatherKind.Clear), 0.001f);
            Assert.AreEqual(0f, RainQuench.Step(-1f, WeatherKind.Storm), 0.001f);
            Assert.AreEqual(0.4f, RainQuench.Now(0.4f), 0.001f);
            Assert.AreEqual(4f, FirePatch.Life, 0.001f);
            Assert.AreEqual(10f, PowderBed.Life, 0.001f);
            Assert.AreEqual(3.5f, Ember.Seconds, 0.001f);
            Assert.AreEqual(0.65f, WeatherSurface.Wetness(WeatherKind.Rain), 0.001f);
            Assert.AreEqual(0.85f, WeatherSurface.Wetness(WeatherKind.Storm), 0.001f);
        }

        [Test]
        public void AnOilSlickSlowsAStepUntilItCatches()
        {
            Assert.AreEqual(0.7f, StreetSlick.Drag, 0.001f);
            Assert.AreEqual(4.5f, StreetSlick.Speed(4.5f, false), 0.001f);
            Assert.AreEqual(3.15f, StreetSlick.Speed(4.5f, true), 0.001f);
            Assert.AreEqual(1.54f, StreetSlick.Speed(2.2f, true), 0.001f);
            Assert.AreEqual(0f, StreetSlick.Speed(-1f, true), 0.001f);
            Assert.AreEqual(1.6f, StreetSlick.Radius, 0.001f);
            Assert.AreEqual(12f, StreetSlick.Life, 0.001f);
            Assert.AreEqual(2.8f, StreetSlick.Light, 0.001f);
            Assert.IsTrue(StreetSlick.On(1.6f, 0f, 0f, 0f));
            Assert.IsFalse(StreetSlick.On(1.61f, 0f, 0f, 0f));
        }

        [Test]
        public void AStormSoaksTheYardAndACookStaysDry()
        {
            Assert.AreEqual(4, YardSoak.Keep(4, "Scavenge", WeatherKind.Clear));
            Assert.AreEqual(4, YardSoak.Keep(4, "Scavenge", WeatherKind.Fog));
            Assert.AreEqual(4, YardSoak.Keep(4, "Build", WeatherKind.Overcast));
            Assert.AreEqual(3, YardSoak.Keep(4, "Scavenge", WeatherKind.Rain));
            Assert.AreEqual(2, YardSoak.Keep(4, "Scavenge", WeatherKind.Storm));
            Assert.AreEqual(1, YardSoak.Keep(1, "Guard", WeatherKind.Rain));
            Assert.AreEqual(0, YardSoak.Keep(1, "Guard", WeatherKind.Storm));
            Assert.AreEqual(2, YardSoak.Keep(2, "Cook", WeatherKind.Storm));
            Assert.AreEqual(0, YardSoak.Keep(0, "Clear", WeatherKind.Storm));
            Assert.AreEqual(0, YardSoak.Keep(-3, "Build", WeatherKind.Rain));
            Assert.AreEqual(22f, YardSoak.Wear(22f, "Scavenge", WeatherKind.Clear), 0.001f);
            Assert.AreEqual(28f, YardSoak.Wear(22f, "Guard", WeatherKind.Rain), 0.001f);
            Assert.AreEqual(36f, YardSoak.Wear(22f, "Build", WeatherKind.Storm), 0.001f);
            Assert.AreEqual(22f, YardSoak.Wear(22f, "Cook", WeatherKind.Storm), 0.001f);
            Assert.AreEqual(100f, YardSoak.Wear(90f, "Clear", WeatherKind.Storm), 0.001f);
            Assert.AreEqual(0f, YardSoak.Mood("Cook", WeatherKind.Storm), 0.001f);
            Assert.AreEqual(4f, YardSoak.Mood("Scavenge", WeatherKind.Storm), 0.001f);
            Assert.AreEqual("The yard is soaked", NoteSay.One("soak", "en"));
            Assert.AreEqual("El patio está empapado", NoteSay.One("soak", "es"));

            var yard = new List<ColonistDay>
            {
                new ColonistDay { id = "ada", task = "Scavenge", hunger = 78f, thirst = 78f, morale = 70f }
            };
            int food = 0;
            int water = 0;
            int raw = 0;
            var clear = ColonyDay.Simulate(yard, ref food, ref water, false, false, "", 0, ref raw);
            Assert.AreEqual(22f, yard[0].fatigue, 0.001f);
            Assert.AreEqual(70f, yard[0].morale, 0.001f);
            Assert.IsFalse(System.Array.IndexOf(clear, "soak") >= 0);

            yard[0].fatigue = 0f;
            yard[0].morale = 70f;
            yard[0].hunger = 78f;
            yard[0].thirst = 78f;
            var storm = ColonyDay.Simulate(yard, ref food, ref water, false, false, "", 0, ref raw, WeatherKind.Storm);
            Assert.AreEqual(36f, yard[0].fatigue, 0.001f);
            Assert.AreEqual(66f, yard[0].morale, 0.001f);
            Assert.AreEqual(60f, yard[0].hunger, 0.001f);
            Assert.Contains("soak", storm);

            var kitchen = new List<ColonistDay>
            {
                new ColonistDay { id = "ada", task = "Cook", hunger = 78f, thirst = 78f, morale = 70f }
            };
            var wet = ColonyDay.Simulate(kitchen, ref food, ref water, false, false, "", 0, ref raw, WeatherKind.Storm);
            Assert.AreEqual(22f, kitchen[0].fatigue, 0.001f);
            Assert.AreEqual(70f, kitchen[0].morale, 0.001f);
            Assert.Contains("soak", wet);
        }

        [Test]
        public void PoisonClosesTheViewAndAClearBodyKeepsTheGrade()
        {
            Assert.AreEqual(0.28f, RaidGrade.Vignette(false, 1), 0.001f);
            Assert.AreEqual(0.46f, RaidGrade.Vignette(true, 1), 0.001f);
            Assert.AreEqual(0.28f, PoisonVeil.Shade(0.28f, false), 0.001f);
            Assert.AreEqual(0.52f, PoisonVeil.Shade(0.28f, true), 0.001f);
            Assert.AreEqual(0.7f, PoisonVeil.Shade(0.46f, true), 0.001f);
            Assert.AreEqual(0.78f, PoisonVeil.Shade(0.62f, true), 0.001f);
            Assert.AreEqual(0.24f, PoisonVeil.Shade(-1f, true), 0.001f);
            Assert.IsFalse(PoisonVeil.Soft(false, false));
            Assert.IsTrue(PoisonVeil.Soft(true, false));
            Assert.IsTrue(PoisonVeil.Soft(false, true));
            Assert.AreEqual(0f, PoisonVeil.BlurOf(false, false), 0.001f);
            Assert.AreEqual(0.35f, PoisonVeil.BlurOf(false, true), 0.001f);
            Assert.AreEqual(0.62f, PoisonVeil.BlurOf(true, false), 0.001f);
            Assert.AreEqual(0.62f, PoisonVeil.BlurOf(true, true), 0.001f);
            PoisonVeil.Tint(false, 1f, 0.96f, 0.9f, out float r, out float g, out float b);
            Assert.AreEqual(1f, r, 0.001f);
            Assert.AreEqual(0.96f, g, 0.001f);
            Assert.AreEqual(0.9f, b, 0.001f);
            PoisonVeil.Tint(true, 1f, 0.96f, 0.9f, out r, out g, out b);
            Assert.AreEqual(0.72f, r, 0.001f);
            Assert.AreEqual(0.912f, g, 0.001f);
            Assert.AreEqual(0.558f, b, 0.001f);
        }

        [Test]
        public void WindSlidesPaperAlongTheStreetAndAClearDayLeavesItDown()
        {
            Assert.IsFalse(WindSheet.Skims(WeatherKind.Clear));
            Assert.IsTrue(WindSheet.Skims(WeatherKind.Rain));
            Assert.IsTrue(WindSheet.Skims(WeatherKind.Storm));
            Assert.IsTrue(WindSheet.Skims(WeatherKind.Fog));
            Assert.IsTrue(WindSheet.Skims(WeatherKind.Overcast));
            Assert.AreEqual(0.65f, GroundMist.Wind(WeatherKind.Rain), 0.001f);
            Assert.AreEqual(0.9f, GroundMist.Wind(WeatherKind.Storm), 0.001f);
            Assert.AreEqual(0.08f, GroundMist.Wind(WeatherKind.Clear), 0.001f);
            WindSheet.Step(0f, 6f, 0.65f, 1f, out float x, out float z);
            Assert.AreEqual(2.08f, x, 0.001f);
            Assert.AreEqual(6f, z, 0.001f);
            WindSheet.Step(0f, 6f, 0.9f, 1f, out x, out z);
            Assert.AreEqual(2.88f, x, 0.001f);
            WindSheet.Step(0f, 6f, 0f, 5f, out x, out z);
            Assert.AreEqual(0f, x, 0.001f);
            WindSheet.Step(0f, 6f, 1f, -2f, out x, out z);
            Assert.AreEqual(0f, x, 0.001f);
            WindSheet.Step(21f, 6f, 1f, 1f, out x, out z);
            Assert.AreEqual(-19.8f, x, 0.001f);
            Assert.AreEqual(6f, z, 0.001f);
            WindSheet.Home(0, out x, out z);
            Assert.AreEqual(-18f, x, 0.001f);
            Assert.AreEqual(6f, z, 0.001f);
            WindSheet.Home(4, out x, out z);
            Assert.AreEqual(18f, x, 0.001f);
            Assert.AreEqual(0f, WindSheet.Tilt(0f, 0, 1f), 0.001f);
            Assert.AreEqual(18.512f, WindSheet.Tilt(0.2f, 0, 1f), 0.01f);
            Assert.AreEqual(0f, WindSheet.Tilt(0.2f, 0, 0f), 0.001f);
        }

        [Test]
        public void AUsableThingWearsARimUntilYouStepAway()
        {
            Assert.AreEqual(0.035f, HoverMark.Width, 0.001f);
            Assert.AreEqual(0.35f, HoverMark.Red, 0.001f);
            Assert.AreEqual(0.85f, HoverMark.Green, 0.001f);
            Assert.AreEqual(0.95f, HoverMark.Blue, 0.001f);
            Assert.IsTrue(HoverMark.Live(true));
            Assert.IsFalse(HoverMark.Live(false));
        }

        [Test]
        public void ARoofTakesDirtAndRainDrawsARipple()
        {
            Assert.AreEqual(0f, StreetCoat.Cover(0.65f), 0.001f);
            Assert.AreEqual(0f, StreetCoat.Cover(0f), 0.001f);
            Assert.AreEqual(0f, StreetCoat.Cover(-1f), 0.001f);
            Assert.AreEqual(0.175f, StreetCoat.Cover(0.825f), 0.001f);
            Assert.AreEqual(0.35f, StreetCoat.Cover(1f), 0.001f);
            Assert.AreEqual(0.35f, StreetCoat.Cover(2f), 0.001f);
            Assert.AreEqual(0f, StreetCoat.Shimmer(0f, 1f), 0.001f);
            Assert.AreEqual(0f, StreetCoat.Shimmer(0.65f, 0f), 0.001f);
            Assert.AreEqual(0.052f, StreetCoat.Shimmer(0.65f, 1f), 0.001f);
            Assert.AreEqual(0.08f, StreetCoat.Shimmer(1f, 1f), 0.001f);
            Assert.AreEqual(0.08f, StreetCoat.Shimmer(2f, 2f), 0.001f);
            Assert.AreEqual(0.65f, WeatherSurface.Wetness(WeatherKind.Rain), 0.001f);
        }

        [Test]
        public void AStreetBottleThrowsLikeALureAndSpeaksBothLanguages()
        {
            var bottle = ItemCatalog.Find("street_bottle");
            Assert.IsNotNull(bottle);
            Assert.AreEqual(0.25f, bottle.Weight, 0.001f);
            Assert.AreEqual(ItemUse.Lure, bottle.Use);
            Assert.AreEqual(0.2f, ItemCatalog.Find("noise_lure").Weight, 0.001f);
            Assert.AreEqual(TossKind.Lure, TossKind.Of("street_bottle"));
            Assert.AreEqual(TossKind.Lure, TossKind.Of("noise_lure"));
            Assert.IsTrue(TossKind.Throws("street_bottle"));
            Assert.AreEqual(TossKind.None, TossKind.Of("water"));
            Assert.AreEqual(18f, ThrowArc.LureRadius, 0.001f);
            Assert.Greater(ThrowArc.Flight(ThrowArc.Height, ThrowArc.Forward, ThrowArc.Lift, ThrowArc.Gravity), 15f);
            Assert.AreEqual("Street Bottle", Loc.Item("street_bottle", "en"));
            Assert.AreEqual("Botella de la calle", Loc.Item("street_bottle", "es"));
            Assert.AreEqual("Take the bottle", Loc.T("toss.take_bottle", "en"));
            Assert.AreEqual("Coge la botella", Loc.T("toss.take_bottle", "es"));
            Assert.AreEqual("Breaks loud enough to pull a group.", Loc.T("blurb.street_bottle", "en"));
            Assert.AreEqual("Se rompe lo bastante fuerte para atraer a un grupo.", Loc.T("blurb.street_bottle", "es"));
            Assert.AreEqual("pipe_bomb", LootTables.Roll("street", 2)[5].ItemId);
            Assert.AreEqual("raw_food", LootTables.Roll("street", 2)[6].ItemId);
        }

        [Test]
        public void StreetShoutsFollowTheLanguage()
        {
            Assert.AreEqual("Barrel exploded", FightSay.Hazard(HazardKind.Explosive, "en"));
            Assert.AreEqual("El barril explotó", FightSay.Hazard(HazardKind.Explosive, "es"));
            Assert.AreEqual("Toxic cloud", FightSay.Hazard(HazardKind.Toxic, "en"));
            Assert.AreEqual("Nube tóxica", FightSay.Hazard(HazardKind.Toxic, "es"));
            Assert.AreEqual("Oil spill", FightSay.Hazard(HazardKind.Oil, "en"));
            Assert.AreEqual("Derrame de aceite", FightSay.Hazard(HazardKind.Oil, "es"));
            Assert.AreEqual("Molotov burst", FightSay.Impact(true, "en"));
            Assert.AreEqual("Estalló el molotov", FightSay.Impact(true, "es"));
            Assert.AreEqual("Lure clattered", FightSay.Impact(false, "en"));
            Assert.AreEqual("El cebo resonó", FightSay.Impact(false, "es"));
            Assert.AreEqual("Mara stays at the sanctuary", StreetAsk.Stays("Mara", "en"));
            Assert.AreEqual("Mara se queda en el santuario", StreetAsk.Stays("Mara", "es"));
            Assert.AreEqual("Radio part stowed", StreetAsk.Stowed(true, "en"));
            Assert.AreEqual("Pieza de radio guardada", StreetAsk.Stowed(true, "es"));
            Assert.AreEqual("Cache searched", StreetAsk.Stowed(false, "en"));
            Assert.AreEqual("Alijo registrado", StreetAsk.Stowed(false, "es"));
            Assert.AreEqual("Gear recovered", StreetAsk.Kept(true, "en"));
            Assert.AreEqual("Equipo recuperado", StreetAsk.Kept(true, "es"));
            Assert.AreEqual("Nothing left but the name", StreetAsk.Kept(false, "en"));
            Assert.AreEqual("No queda más que el nombre", StreetAsk.Kept(false, "es"));
            Assert.AreEqual("Inside", DoorMap.Cross(false, "en"));
            Assert.AreEqual("Dentro", DoorMap.Cross(false, "es"));
            Assert.AreEqual("Back on the street", DoorMap.Cross(true, "en"));
            Assert.AreEqual("De vuelta en la calle", DoorMap.Cross(true, "es"));
            Assert.AreEqual("Step inside", DoorMap.Prompt(false));
            Assert.AreEqual("Generator repair fitted", CraftSay.Fitted("repair_kit", "Repair Kit", "en"));
            Assert.AreEqual("Reparación del generador colocado", CraftSay.Fitted("repair_kit", "Repair Kit", "es"));
            Assert.AreEqual("Pipe bomb burst", FightSay.Burst("en"));
        }

        [Test]
        public void AStormPullsTheWireHarderThanAClearDay()
        {
            Assert.AreEqual(0.08f, GroundMist.Wind(WeatherKind.Clear), 0.001f);
            Assert.AreEqual(0.9f, GroundMist.Wind(WeatherKind.Storm), 0.001f);
            Assert.AreEqual(0f, WireGust.Side(0.08f, 0f), 0.001f);
            Assert.AreEqual(0.0726f, WireGust.Side(0.08f, 1f), 0.001f);
            Assert.AreEqual(0.817f, WireGust.Side(0.9f, 1f), 0.001f);
            Assert.AreEqual(0.227f, WireGust.Side(0.25f, 1f), 0.001f);
            Assert.AreEqual(0f, WireGust.Side(-1f, 1f), 0.001f);
            Assert.AreEqual(0.12f, WireGust.Base, 0.001f);
            Assert.AreEqual(0.65f, WireGust.Rate, 0.001f);
        }

        [Test]
        public void ABoltLightsACrouchAndAQuietSkyStaysDark()
        {
            Assert.AreEqual(0.55f, BoltGlare.Hold, 0.001f);
            Assert.AreEqual(0.42f, BoltGlare.Lift, 0.001f);
            Assert.AreEqual(0.85f, BoltGlare.Flash, 0.001f);
            Assert.AreEqual(0.45f, BoltGlare.Mix, 0.001f);
            Assert.IsFalse(BoltGlare.Live(0f, 10f));
            Assert.IsFalse(BoltGlare.Live(10f, 9f));
            Assert.IsTrue(BoltGlare.Live(10f, 10f));
            Assert.IsTrue(BoltGlare.Live(10f, 10.55f));
            Assert.IsFalse(BoltGlare.Live(10f, 10.56f));
            Assert.AreEqual(0.22f, BoltGlare.Glare(0.22f, false), 0.001f);
            Assert.AreEqual(0.64f, BoltGlare.Glare(0.22f, true), 0.001f);
            Assert.AreEqual(1f, BoltGlare.Glare(0.85f, true), 0.001f);
            Assert.AreEqual(1f, BoltGlare.Glare(1f, true), 0.001f);
            Assert.AreEqual(0.42f, BoltGlare.Glare(-1f, true), 0.001f);
            Assert.AreEqual(0.15f, BoltGlare.Bright(0.15f, false), 0.001f);
            Assert.AreEqual(1f, BoltGlare.Bright(0.15f, true), 0.001f);
            Assert.AreEqual(0.65f, BoltGlare.Bright(-0.2f, true), 0.001f);
            Assert.AreEqual(-0.2f, RaidGrade.Exposure(1f, true), 0.001f);
            BoltGlare.Wash(false, 1f, 0.96f, 0.9f, out float stillR, out float stillG, out float stillB);
            Assert.AreEqual(1f, stillR, 0.001f);
            Assert.AreEqual(0.96f, stillG, 0.001f);
            Assert.AreEqual(0.9f, stillB, 0.001f);
            BoltGlare.Wash(true, 0.72f, 0.58f, 0.78f, out float washR, out float washG, out float washB);
            Assert.AreEqual(0.846f, washR, 0.001f);
            Assert.AreEqual(0.769f, washG, 0.001f);
            Assert.AreEqual(0.879f, washB, 0.001f);
            Assert.AreEqual(4.5f, SkyBand.BoltGap, 0.001f);
            Assert.AreEqual(0.6f, StormCover.Delay, 0.001f);
        }

        [Test]
        public void AWindowPaneLetsALookThroughAndAShotBreaksIt()
        {
            Assert.IsTrue(PaneGlass.Opening("wall_window", 0.9f, 1.2f, 1f, 2f));
            Assert.IsFalse(PaneGlass.Opening("wall_window", 0f, 0.9f, 1f, 2f));
            Assert.IsFalse(PaneGlass.Opening("wall_window", 2.1f, 0.9f, 1f, 2f));
            Assert.IsFalse(PaneGlass.Opening("wall_window", 0f, 3f, 0.5f, 2f));
            Assert.IsFalse(PaneGlass.Opening("wall_window_broken", 0.9f, 1.2f, 1f, 2f));
            Assert.IsFalse(PaneGlass.Opening("wall_plain", 0f, 3f, 2f, 2f));
            Assert.IsTrue(PaneGlass.SeeThrough("KitGlass"));
            Assert.IsFalse(PaneGlass.SeeThrough("KitBlock"));
            Assert.IsFalse(PaneGlass.SeeThrough(""));
            Assert.IsFalse(PaneGlass.Occluded(null));
            Assert.IsFalse(PaneGlass.Occluded(new string[0]));
            Assert.IsFalse(PaneGlass.Occluded(new[] { "KitGlass" }));
            Assert.IsFalse(PaneGlass.Occluded(new[] { "KitGlass", "KitGlass" }));
            Assert.IsTrue(PaneGlass.Occluded(new[] { "KitGlass", "KitBlock" }));
            Assert.IsTrue(PaneGlass.Occluded(new[] { "KitBlock" }));
            Assert.AreEqual(12f, PaneGlass.Hp, 0.001f);
            Assert.AreEqual(9f, PaneGlass.Noise, 0.001f);
            Assert.AreEqual(8f, PaneGlass.After(12f, 4f), 0.001f);
            Assert.AreEqual(0f, PaneGlass.After(12f, 12f), 0.001f);
            Assert.AreEqual(0f, PaneGlass.After(12f, 34f), 0.001f);
            Assert.AreEqual(12f, PaneGlass.After(12f, 0f), 0.001f);
            Assert.IsFalse(PaneGlass.Gone(8f));
            Assert.IsTrue(PaneGlass.Gone(0f));
            Assert.AreEqual(0.38f, PaneGlass.Tint.a, 0.001f);
            Assert.AreEqual("The pane shatters", Loc.T("pane.break", "en"));
            Assert.AreEqual("El cristal se rompe", Loc.T("pane.break", "es"));
            Assert.AreEqual(34f, WeaponCard.Find("pistol_9mm").Damage, 0.001f);
        }

        [Test]
        public void AshHangsOverTheMarketAndAClearYardStaysOpen()
        {
            Assert.AreEqual(0.82f, AshVeil.Cut, 0.001f);
            Assert.AreEqual(0.28f, AshVeil.Mix, 0.001f);
            Assert.AreEqual(1f, WeatherSurface.Sight(WeatherKind.Clear), 0.001f);
            Assert.AreEqual(0.62f, WeatherSurface.Sight(WeatherKind.Fog), 0.001f);
            Assert.AreEqual(1f, AshVeil.Scale(1f, false), 0.001f);
            Assert.AreEqual(0.82f, AshVeil.Scale(1f, true), 0.001f);
            Assert.AreEqual(0.5084f, AshVeil.Scale(0.62f, true), 0.001f);
            Assert.AreEqual(0.8f, AshVeil.Scale(0.8f, false), 0.001f);
            Assert.AreEqual(0f, AshVeil.Scale(-1f, true), 0.001f);
            Assert.IsTrue(AshFall.Falls("ash_market"));
            Assert.IsFalse(AshFall.Falls("rail_yard"));
            AshVeil.Grit(false, 1f, 0.96f, 0.9f, out float openR, out float openG, out float openB);
            Assert.AreEqual(1f, openR, 0.001f);
            Assert.AreEqual(0.96f, openG, 0.001f);
            Assert.AreEqual(0.9f, openB, 0.001f);
            AshVeil.Grit(true, 1f, 0.96f, 0.9f, out float gritR, out float gritG, out float gritB);
            Assert.AreEqual(0.874f, gritR, 0.001f);
            Assert.AreEqual(0.8368f, gritG, 0.001f);
            Assert.AreEqual(0.7824f, gritB, 0.001f);
            Assert.AreEqual("Ash hangs in the air", Loc.T("ash.air", "en"));
            Assert.AreEqual("La ceniza flota en el aire", Loc.T("ash.air", "es"));
        }

        [Test]
        public void AFollowerCriesOutWhenTheDeadComeClose()
        {
            Assert.AreEqual(7f, StraggleCall.Near, 0.001f);
            Assert.AreEqual(6f, StraggleCall.Gap, 0.001f);
            Assert.AreEqual(16f, StraggleCall.Radius, 0.001f);
            Assert.IsFalse(StraggleCall.Due(0f, 10f, -1f));
            Assert.IsFalse(StraggleCall.Due(0f, 10f, 7.1f));
            Assert.IsTrue(StraggleCall.Due(0f, 10f, 7f));
            Assert.IsTrue(StraggleCall.Due(0f, 10f, 0f));
            Assert.IsFalse(StraggleCall.Due(10f, 15.9f, 3f));
            Assert.IsTrue(StraggleCall.Due(10f, 16f, 3f));
            Assert.IsFalse(StraggleCall.Due(10f, 9f, 3f));
            Assert.AreEqual("Imani Cole cries out", StreetAsk.Cry("Imani Cole", "en"));
            Assert.AreEqual("Imani Cole grita", StreetAsk.Cry("Imani Cole", "es"));
            Assert.AreEqual(1.6f, RescueBook.FollowGap, 0.001f);
            RescueBook.Step(0f, 0f, 0f, 6f, 4f, 1f, out float stepX, out float stepZ);
            Assert.AreEqual(0f, stepX, 0.001f);
            Assert.AreEqual(4f, stepZ, 0.001f);
        }

        [Test]
        public void ARaidSpeaksTheLanguageAndKeepsTheApproach()
        {
            Assert.AreEqual("Broadcast night — hold the tower", RaidSay.Open(true, "gate", "en"));
            Assert.AreEqual("Night raid from the gate", RaidSay.Open(false, "gate", "en"));
            Assert.AreEqual("Night raid from the alley", RaidSay.Open(false, "alley", "en"));
            Assert.AreEqual("Night raid from the yard", RaidSay.Open(false, "yard", "en"));
            Assert.AreEqual("Night raid from the fence", RaidSay.Open(false, "fence", "en"));
            Assert.AreEqual("The gate held", RaidSay.Held(false, "en"));
            Assert.AreEqual("The broadcast went out", RaidSay.Held(true, "en"));
            Assert.AreEqual("The raid broke the stores", RaidSay.Broke("en"));
            Assert.AreEqual("They come from the yard", RaidSay.Coming("yard", "en"));
            Assert.AreEqual("Noche de emisión — aguanta la torre", RaidSay.Open(true, "alley", "es"));
            Assert.AreEqual("Incursión nocturna desde la puerta", RaidSay.Open(false, "gate", "es"));
            Assert.AreEqual("Incursión nocturna desde el callejón", RaidSay.Open(false, "alley", "es"));
            Assert.AreEqual("La puerta aguantó", RaidSay.Held(false, "es"));
            Assert.AreEqual("La emisión salió", RaidSay.Held(true, "es"));
            Assert.AreEqual("La incursión rompió las reservas", RaidSay.Broke("es"));
            Assert.AreEqual("Vienen desde el callejón", RaidSay.Coming("alley", "es"));
            Assert.AreEqual("Vienen desde el patio", RaidSay.Coming("yard", "es"));
            Assert.AreEqual("Vienen desde la valla", RaidSay.Coming("fence", "es"));
            Assert.AreEqual("gate", RaidPlan.Side(1, 0, 0) == "gate" || RaidPlan.Side(1, 0, 0) == "alley" || RaidPlan.Side(1, 0, 0) == "yard" || RaidPlan.Side(1, 0, 0) == "fence" ? RaidPlan.Side(1, 0, 0) : "gate");
        }

        [Test]
        public void ABrokenPaneLeavesGlassThatCutsTheFirstStep()
        {
            Assert.AreEqual(0.55f, GlassCrunch.Radius, 0.001f);
            Assert.AreEqual(1.28f, GlassCrunch.Reach, 0.001f);
            Assert.AreEqual(1.28f, StepReach.GlassCrunchReach, 0.001f);
            Assert.AreEqual(3f, GlassCrunch.Nick, 0.001f);
            Assert.AreEqual(8, GlassCrunch.Cap);
            Assert.IsTrue(GlassCrunch.On(0f, 0f, 0f, 0f));
            Assert.IsTrue(GlassCrunch.On(0.5f, 0f, 0f, 0f));
            Assert.IsFalse(GlassCrunch.On(0.7f, 0f, 0f, 0f));
            Assert.AreEqual("step_glass", AudioMix.StepId("Shard_glass"));
            Assert.AreEqual("step", AudioMix.StepId("Ground"));
            Assert.AreEqual(7.68f, StepReach.Radius(6f, "step_glass"), 0.001f);
            Assert.AreEqual(8.1f, StepReach.Radius(6f, "step_metal"), 0.001f);
            Assert.IsTrue(ClipBook.Has("step_glass"));
            Assert.AreEqual(12f, PaneGlass.Hp, 0.001f);
            Assert.AreEqual("Glass cuts", Loc.T("pane.cut", "en"));
            Assert.AreEqual("El cristal corta", Loc.T("pane.cut", "es"));
        }

        [Test]
        public void ABruteChargeSmashesAPaneAndAWallStillStopsIt()
        {
            Assert.AreEqual(18f, PaneCharge.Hit, 0.001f);
            Assert.AreEqual(12f, PaneGlass.Hp, 0.001f);
            Assert.IsTrue(PaneCharge.Smashes("KitGlass"));
            Assert.IsFalse(PaneCharge.Smashes("KitBlock"));
            Assert.IsFalse(PaneCharge.Smashes(""));
            Assert.IsTrue(PaneCharge.Through(12f, 18f));
            Assert.IsFalse(PaneCharge.Through(12f, 4f));
            Assert.AreEqual(45f, BoardBreak.ChargeHit, 0.001f);
            Assert.AreEqual(1.5f, SpecialBeat.WallStun, 0.001f);
        }

        [Test]
        public void AWalkerClawsAPaneUntilItFails()
        {
            Assert.AreEqual(4f, PaneClaw.Hit, 0.001f);
            Assert.AreEqual(1.1f, PaneClaw.Reach, 0.001f);
            Assert.AreEqual(0.8f, PaneClaw.Gap, 0.001f);
            Assert.AreEqual(3, PaneClaw.Strikes(12f, 4f));
            Assert.AreEqual(1, PaneClaw.Strikes(12f, 18f));
            Assert.AreEqual(0, PaneClaw.Strikes(0f, 4f));
            Assert.AreEqual(0, PaneClaw.Strikes(12f, 0f));
            Assert.AreEqual(12f, PaneGlass.Hp, 0.001f);
            Assert.AreEqual(18f, PaneCharge.Hit, 0.001f);
        }

        [Test]
        public void AshMakesYouCoughAndAClearYardStaysQuiet()
        {
            Assert.AreEqual(7f, AshCough.Gap, 0.001f);
            Assert.AreEqual(11f, AshCough.CrouchGap, 0.001f);
            Assert.AreEqual(8f, AshCough.Radius, 0.001f);
            Assert.IsFalse(AshCough.Due(false, false, 0f, 20f));
            Assert.IsTrue(AshCough.Due(true, false, 0f, 1f));
            Assert.IsFalse(AshCough.Due(true, false, 10f, 16.9f));
            Assert.IsTrue(AshCough.Due(true, false, 10f, 17f));
            Assert.IsFalse(AshCough.Due(true, true, 10f, 20.9f));
            Assert.IsTrue(AshCough.Due(true, true, 10f, 21f));
            Assert.IsFalse(AshCough.Due(true, false, 10f, 9f));
            Assert.AreEqual(8f, AshCough.Carry(false), 0.001f);
            Assert.AreEqual(4f, AshCough.Carry(true), 0.001f);
            Assert.AreEqual(0.82f, AshVeil.Cut, 0.001f);
            Assert.IsTrue(AshFall.Falls("ash_market"));
            Assert.IsFalse(AshFall.Falls("rail_yard"));
            Assert.AreEqual("[Cough, east]", Presentation.Caption(NoiseType.Cough, 1f, 0f, "en"));
            Assert.AreEqual("[Tos, este]", Presentation.Caption(NoiseType.Cough, 1f, 0f, "es"));
            Assert.AreEqual("", Presentation.Caption(NoiseType.WalkFootstep, 1f, 0f, "en"));
            Assert.AreEqual(0f, HearGate.Perceived(8f, 40f, 1f, false, NoiseType.Thunder), 0.001f);
            Assert.Greater(HearGate.Perceived(2f, 8f, 0.7f, false, NoiseType.Cough), 0.05f);
            Assert.IsFalse(StormCover.Masks(10f, 11f, NoiseType.Cough));
            Assert.IsTrue(ClipBook.Has("cough"));
            Assert.AreEqual(10f, AudioSpace.MaxDistance("cough"), 0.001f);
        }

        [Test]
        public void AStreetBiteComesHomeOnTheLeadersInjury()
        {
            Assert.AreEqual(0, HomeSick.Carry(0, 0));
            Assert.AreEqual(1, HomeSick.Carry(0, 1));
            Assert.AreEqual(2, HomeSick.Carry(0, 2));
            Assert.AreEqual(2, HomeSick.Carry(1, 2));
            Assert.AreEqual(3, HomeSick.Carry(3, 1));
            Assert.AreEqual(2, HomeSick.Carry(2, 0));
            Assert.AreEqual(2, HomeSick.Carry(-1, 2));
            Assert.AreEqual(3, HomeSick.Carry(0, 3));
            Assert.AreEqual(3, HomeSick.Carry(0, 9));
            Assert.AreEqual(3, HomeSick.Carry(5, 1));
            Assert.IsTrue(HomeSick.Rises(0, 1));
            Assert.IsTrue(HomeSick.Rises(0, 2));
            Assert.IsTrue(HomeSick.Rises(2, 3));
            Assert.IsTrue(HomeSick.Rises(-1, 2));
            Assert.IsFalse(HomeSick.Rises(2, 1));
            Assert.IsFalse(HomeSick.Rises(2, 0));
            Assert.IsFalse(HomeSick.Rises(3, 1));
            Assert.IsFalse(HomeSick.Rises(5, 1));
            Assert.AreEqual("The bite came home", HomeSick.Line(1, "en"));
            Assert.AreEqual("La mordedura llegó a casa", HomeSick.Line(1, "es"));
            Assert.AreEqual("The fever came home", HomeSick.Line(2, "en"));
            Assert.AreEqual("La fiebre llegó a casa", HomeSick.Line(2, "es"));
            Assert.AreEqual("La fiebre llegó a casa", HomeSick.Line(3, "es"));
            Assert.AreEqual("", HomeSick.Line(0, "en"));
            Assert.AreEqual(2, FeverSpread.Sick);
            Assert.AreEqual(3, FeverSpread.Cap);
            Assert.AreEqual(2, SuccessionLedger.MercyInjury);
            Assert.AreEqual(1, Affliction.Stage(1f));
            Assert.AreEqual(1, Affliction.Stage(89f));
            Assert.AreEqual(2, Affliction.Stage(90f));
            Assert.AreEqual(2, Affliction.Stage(179f));
            Assert.AreEqual(3, Affliction.Stage(180f));
            Assert.IsTrue(Affliction.AntibioticsWork(1));
            Assert.IsTrue(Affliction.AntibioticsWork(2));
            Assert.IsFalse(Affliction.AntibioticsWork(3));
            Assert.IsFalse(Affliction.AntibioticsWork(0));
        }

        [Test]
        public void TheCampCardNamesABiteAFeverAndACriticalWound()
        {
            Assert.AreEqual("", WoundCard.Line(0, "en"));
            Assert.AreEqual("", WoundCard.Line(-1, "es"));
            Assert.AreEqual("Bitten", WoundCard.Line(1, "en"));
            Assert.AreEqual("Mordida", WoundCard.Line(1, "es"));
            Assert.AreEqual("Fever", WoundCard.Line(2, "en"));
            Assert.AreEqual("Fiebre", WoundCard.Line(2, "es"));
            Assert.AreEqual("Critical", WoundCard.Line(3, "en"));
            Assert.AreEqual("Crítica", WoundCard.Line(3, "es"));
            Assert.AreEqual("Critical", WoundCard.Line(4, "en"));
            Assert.AreEqual("Medic", CampRoutine.Choose("Guard", 80f, 80f, 70f, 2));
            Assert.AreEqual("Guard", CampRoutine.Choose("Guard", 80f, 80f, 70f, 1));
            Assert.AreEqual(1, HomeSick.Carry(0, 1));
            Assert.AreEqual(2, HomeSick.Carry(0, 2));
            Assert.AreEqual(3, FeverSpread.Cap);
        }

        [Test]
        public void AHurtColonistCanTakeTheCotAndAHealthyOneStaysUp()
        {
            Assert.IsTrue(CotPull.Holds(1));
            Assert.IsTrue(CotPull.Holds(2));
            Assert.IsTrue(CotPull.Holds(3));
            Assert.IsFalse(CotPull.Holds(0));
            Assert.IsFalse(CotPull.Holds(-1));
            Assert.AreEqual("On the cot", CotPull.Bed("en"));
            Assert.AreEqual("En la camilla", CotPull.Bed("es"));
            Assert.AreEqual("They are not hurt", CotPull.Refuse("en"));
            Assert.AreEqual("No está herido", CotPull.Refuse("es"));
            Assert.AreEqual(40f, ShiftWear.After(80f, "Quarantine", false), 0.001f);
            Assert.AreEqual(10f, ShiftWear.After(80f, "Quarantine", true), 0.001f);
            Assert.AreEqual(22f, ShiftWear.After(0f, "Guard", false), 0.001f);
            Assert.AreEqual(0f, ShiftWear.After(0f, "Fallen", false), 0.001f);
            Assert.IsFalse(YardSoak.Outdoor("Quarantine"));
            Assert.IsTrue(YardSoak.Outdoor("Guard"));
            Assert.IsFalse(FeverSpread.Source(2, "Quarantine", true));
            Assert.IsTrue(FeverSpread.Source(2, "Guard", true));
            Assert.IsFalse(FeverSpread.Source(1, "Guard", true));
            Assert.AreEqual("Medic", CampRoutine.Choose("Quarantine", 80f, 80f, 70f, 1));
            Assert.AreEqual("Cuarentena", Loc.Task("Quarantine", "es"));
        }

        [Test]
        public void ACampWoundSlowsTheStreetAndAFeverRefusesASprint()
        {
            Assert.AreEqual(0.92f, StreetLimp.Bite, 0.001f);
            Assert.AreEqual(0.78f, StreetLimp.Fever, 0.001f);
            Assert.AreEqual(0.62f, StreetLimp.Critical, 0.001f);
            Assert.AreEqual(4.5f, StreetLimp.Pace(4.5f, 0), 0.001f);
            Assert.AreEqual(4.14f, StreetLimp.Pace(4.5f, 1), 0.001f);
            Assert.AreEqual(3.51f, StreetLimp.Pace(4.5f, 2), 0.001f);
            Assert.AreEqual(2.79f, StreetLimp.Pace(4.5f, 3), 0.001f);
            Assert.AreEqual(2.79f, StreetLimp.Pace(4.5f, 9), 0.001f);
            Assert.AreEqual(0f, StreetLimp.Pace(-2f, 2), 0.001f);
            Assert.IsTrue(StreetLimp.AllowsSprint(0));
            Assert.IsTrue(StreetLimp.AllowsSprint(1));
            Assert.IsTrue(StreetLimp.AllowsSprint(-1));
            Assert.IsFalse(StreetLimp.AllowsSprint(2));
            Assert.IsFalse(StreetLimp.AllowsSprint(3));
            Assert.AreEqual("", StreetLimp.Line(0, "en"));
            Assert.AreEqual("The bite slows you", StreetLimp.Line(1, "en"));
            Assert.AreEqual("La mordedura te frena", StreetLimp.Line(1, "es"));
            Assert.AreEqual("The fever slows you", StreetLimp.Line(2, "en"));
            Assert.AreEqual("La fiebre te frena", StreetLimp.Line(2, "es"));
            Assert.AreEqual("You can barely walk", StreetLimp.Line(3, "en"));
            Assert.AreEqual("Apenas puedes caminar", StreetLimp.Line(4, "es"));
            Assert.AreEqual(2, FeverSpread.Sick);
        }

        [Test]
        public void ACampWoundOpensTheShotAndAClearLeaderKeepsTheSights()
        {
            Assert.AreEqual(1.12f, WoundSway.Bite, 0.001f);
            Assert.AreEqual(1.35f, WoundSway.Fever, 0.001f);
            Assert.AreEqual(1.6f, WoundSway.Critical, 0.001f);
            Assert.AreEqual(5.5f, WoundSway.Angle(5.5f, 0), 0.001f);
            Assert.AreEqual(6.16f, WoundSway.Angle(5.5f, 1), 0.001f);
            Assert.AreEqual(7.425f, WoundSway.Angle(5.5f, 2), 0.001f);
            Assert.AreEqual(8.8f, WoundSway.Angle(5.5f, 3), 0.001f);
            Assert.AreEqual(8.8f, WoundSway.Angle(5.5f, 9), 0.001f);
            Assert.AreEqual(0f, WoundSway.Angle(-1f, 2), 0.001f);
            Assert.AreEqual(0.62f, SightGroup.Tight, 0.001f);
            Assert.AreEqual(3.41f, SightGroup.Angle(5.5f, true), 0.001f);
            Assert.AreEqual(5.5f, SightGroup.Angle(5.5f, false), 0.001f);
            Assert.AreEqual(4.6035f, WoundSway.Angle(SightGroup.Angle(5.5f, true), 2), 0.001f);
            Assert.AreEqual(0.8f, TraitHook.Aim("Sharpshooter"), 0.001f);
            Assert.AreEqual(1f, TraitHook.Aim("Brave"), 0.001f);
            Assert.AreEqual(0.92f, StreetLimp.Bite, 0.001f);
        }

        [Test]
        public void AStreetDoseEasesTheCampWoundAndAClearLeaderStaysClear()
        {
            Assert.IsFalse(WoundEase.Helps(0));
            Assert.IsFalse(WoundEase.Helps(-1));
            Assert.IsTrue(WoundEase.Helps(1));
            Assert.IsTrue(WoundEase.Helps(3));
            Assert.AreEqual(0, WoundEase.After(0));
            Assert.AreEqual(0, WoundEase.After(-2));
            Assert.AreEqual(0, WoundEase.After(1));
            Assert.AreEqual(1, WoundEase.After(2));
            Assert.AreEqual(2, WoundEase.After(3));
            Assert.AreEqual("The wound eases", WoundEase.Line("en"));
            Assert.AreEqual("La herida cede", WoundEase.Line("es"));
            Assert.AreEqual("Kit", WoundEase.Note("Kit", false, "en"));
            Assert.AreEqual("Kit  The wound eases", WoundEase.Note("Kit", true, "en"));
            Assert.AreEqual("Kit  La herida cede", WoundEase.Note("Kit", true, "es"));
            Assert.AreEqual("The wound eases", WoundEase.Note("", true, "en"));
            Assert.AreEqual(50, FieldHand.Medkit(0));
            Assert.IsFalse(Affliction.AntibioticsWork(0));
            Assert.IsTrue(Affliction.AntibioticsWork(1));
            Assert.IsTrue(Affliction.AntibioticsWork(2));
            Assert.IsFalse(Affliction.AntibioticsWork(3));
            Assert.AreEqual(1.12f, WoundSway.Bite, 0.001f);
        }

        [Test]
        public void ACampWoundCarriesTheStepAndAClearLeaderStaysQuiet()
        {
            Assert.AreEqual(1.15f, LimpStep.Bite, 0.001f);
            Assert.AreEqual(1.4f, LimpStep.Fever, 0.001f);
            Assert.AreEqual(1.7f, LimpStep.Critical, 0.001f);
            Assert.AreEqual(6f, LimpStep.Radius(6f, 0), 0.001f);
            Assert.AreEqual(6.9f, LimpStep.Radius(6f, 1), 0.001f);
            Assert.AreEqual(8.4f, LimpStep.Radius(6f, 2), 0.001f);
            Assert.AreEqual(10.2f, LimpStep.Radius(6f, 3), 0.001f);
            Assert.AreEqual(10.2f, LimpStep.Radius(6f, 9), 0.001f);
            Assert.AreEqual(0f, LimpStep.Radius(-1f, 2), 0.001f);
            Assert.AreEqual(2.8f, LimpStep.Radius(2f, 2), 0.001f);
            Assert.AreEqual(6f, StepReach.Radius(6f, "step"), 0.001f);
            Assert.AreEqual(8.1f, StepReach.Radius(6f, "step_metal"), 0.001f);
            Assert.AreEqual(9.315f, LimpStep.Radius(StepReach.Radius(6f, "step_metal"), 1), 0.001f);
            Assert.AreEqual(0.92f, StreetLimp.Bite, 0.001f);
            Assert.AreEqual("", Presentation.Caption(NoiseType.WalkFootstep, 1f, 0f, "en"));
        }

        [Test]
        public void AStormFillsTheCollectorAndAClearDayLeavesIt()
        {
            Assert.AreEqual(1, RainCatch.RainExtra);
            Assert.AreEqual(2, RainCatch.StormExtra);
            Assert.AreEqual(1, RainCatch.Extra("Water", 100, 0, WeatherKind.Rain));
            Assert.AreEqual(2, RainCatch.Extra("Water", 100, 0, WeatherKind.Storm));
            Assert.AreEqual(0, RainCatch.Extra("Water", 100, 0, WeatherKind.Clear));
            Assert.AreEqual(0, RainCatch.Extra("Water", 100, 0, WeatherKind.Fog));
            Assert.AreEqual(0, RainCatch.Extra("Water", 100, 0, WeatherKind.Overcast));
            Assert.AreEqual(0, RainCatch.Extra("Water", 0, 0, WeatherKind.Storm));
            Assert.AreEqual(0, RainCatch.Extra("Water", 40, 1, WeatherKind.Rain));
            Assert.AreEqual(0, RainCatch.Extra("Purifier", 100, 0, WeatherKind.Storm));
            Assert.AreEqual(0, RainCatch.Extra("Farm", 100, 0, WeatherKind.Rain));
            Assert.AreEqual("The collector caught the rain", RainCatch.Line(WeatherKind.Rain, "en"));
            Assert.AreEqual("El colector atrapó la lluvia", RainCatch.Line(WeatherKind.Rain, "es"));
            Assert.AreEqual("The collector caught the storm", RainCatch.Line(WeatherKind.Storm, "en"));
            Assert.AreEqual("El colector atrapó la tormenta", RainCatch.Line(WeatherKind.Storm, "es"));
            Assert.AreEqual("", RainCatch.Line(WeatherKind.Clear, "en"));
            var plots = new[]
            {
                new CampYield.Plot { Kind = "Purifier", Integrity = 100 },
                new CampYield.Plot { Kind = "Water", Integrity = 100 }
            };
            CampYield.Produce(plots, false, out _, out int dry);
            CampYield.Produce(plots, true, out _, out int wet);
            Assert.AreEqual(3, dry);
            Assert.AreEqual(4, wet);
            Assert.AreEqual(1, CampYield.CollectorWater);
            Assert.AreEqual(2, CampYield.PurifierWater);
            Assert.AreEqual(1, CampYield.RainBonus);
        }

        [Test]
        public void AStormDrinksTheTankAndAClearNightDoesNot()
        {
            Assert.AreEqual(0.8f, StormBurn.Pull, 0.001f);
            Assert.AreEqual(10f, StormBurn.After(10f, 10f, WeatherKind.Storm), 0.001f);
            Assert.AreEqual(5f, StormBurn.After(10f, 5f, WeatherKind.Clear), 0.001f);
            Assert.AreEqual(5f, StormBurn.After(10f, 5f, WeatherKind.Rain), 0.001f);
            Assert.AreEqual(5f, StormBurn.After(10f, 5f, WeatherKind.Fog), 0.001f);
            Assert.AreEqual(1f, StormBurn.After(10f, 5f, WeatherKind.Storm), 0.001f);
            Assert.AreEqual(6.4f, StormBurn.After(10f, 8f, WeatherKind.Storm), 0.001f);
            Assert.AreEqual(0f, StormBurn.After(1f, 0f, WeatherKind.Storm), 0.001f);
            Assert.AreEqual(0f, StormBurn.After(-1f, 5f, WeatherKind.Storm), 0.001f);
            Assert.AreEqual(90f, FuelTank.NightRate, 0.001f);
            Assert.AreEqual(0.45f, FuelTank.Dusk, 0.001f);
            Assert.AreEqual(5f, FuelTank.Drink(10f, 200f, true, 0.46f), 0.001f);
            Assert.AreEqual(10f, FuelTank.Drink(10f, 200f, true, 0.45f), 0.001f);
            Assert.AreEqual(10f, FuelTank.Drink(10f, 200f, false, 0.8f), 0.001f);
            Assert.AreEqual("A storm drinks the tank", Loc.T("tank.storm", "en"));
            Assert.AreEqual("Una tormenta bebe el tanque", Loc.T("tank.storm", "es"));
            Assert.AreEqual(2, RainCatch.StormExtra);
        }

        [Test]
        public void AStormWearsTheYardAndAClearDayLeavesIt()
        {
            Assert.AreEqual(6, StormWear.StormHit);
            Assert.AreEqual(2, StormWear.RainHit);
            Assert.AreEqual(94, StormWear.After(100, 0, "Farm", WeatherKind.Storm));
            Assert.AreEqual(98, StormWear.After(100, 0, "Barricade", WeatherKind.Rain));
            Assert.AreEqual(100, StormWear.After(100, 0, "Farm", WeatherKind.Clear));
            Assert.AreEqual(100, StormWear.After(100, 0, "Farm", WeatherKind.Fog));
            Assert.AreEqual(100, StormWear.After(100, 0, "Cot", WeatherKind.Storm));
            Assert.AreEqual(100, StormWear.After(100, 0, "Workbench", WeatherKind.Storm));
            Assert.AreEqual(100, StormWear.After(100, 0, "Campfire", WeatherKind.Storm));
            Assert.AreEqual(100, StormWear.After(100, 1, "Farm", WeatherKind.Storm));
            Assert.AreEqual(0, StormWear.After(4, 0, "Generator", WeatherKind.Storm));
            Assert.AreEqual(0, StormWear.After(0, 0, "Farm", WeatherKind.Storm));
            Assert.AreEqual(94, StormWear.After(100, 0, "Purifier", WeatherKind.Storm));
            Assert.AreEqual(94, StormWear.After(100, 0, "Lamp", WeatherKind.Storm));
            Assert.IsTrue(StormWear.Outdoor("Watchtower"));
            Assert.IsFalse(StormWear.Outdoor("Crate"));
            Assert.AreEqual("The storm wore the yard", StormWear.Line(WeatherKind.Storm, "en"));
            Assert.AreEqual("La tormenta gastó el patio", StormWear.Line(WeatherKind.Storm, "es"));
            Assert.AreEqual("The rain wore the yard", StormWear.Line(WeatherKind.Rain, "en"));
            Assert.AreEqual("La lluvia gastó el patio", StormWear.Line(WeatherKind.Rain, "es"));
            Assert.AreEqual("", StormWear.Line(WeatherKind.Clear, "en"));
            Assert.IsTrue(BuildSite.Ready(0, 94));
            Assert.IsFalse(BuildSite.Ready(0, 0));
            Assert.AreEqual(0.8f, StormBurn.Pull, 0.001f);
        }

        [Test]
        public void AMacheteSwingSpendsBreathAndAGunDoesNot()
        {
            Assert.AreEqual(8f, SwingCost.Melee, 0.001f);
            Assert.AreEqual(8f, SwingCost.Of(WeaponType.Melee), 0.001f);
            Assert.AreEqual(0f, SwingCost.Of(WeaponType.Pistol), 0.001f);
            Assert.AreEqual(0f, SwingCost.Of(WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(0f, SwingCost.Of(WeaponType.Rifle), 0.001f);
            Assert.AreEqual(0f, SwingCost.Of(WeaponType.SMG), 0.001f);
            Assert.IsTrue(SwingCost.Pays(8f, WeaponType.Melee));
            Assert.IsTrue(SwingCost.Pays(40f, WeaponType.Melee));
            Assert.IsFalse(SwingCost.Pays(7.9f, WeaponType.Melee));
            Assert.IsFalse(SwingCost.Pays(0f, WeaponType.Melee));
            Assert.IsTrue(SwingCost.Pays(0f, WeaponType.Pistol));
            Assert.AreEqual(92f, SwingCost.After(100f, WeaponType.Melee), 0.001f);
            Assert.AreEqual(0f, SwingCost.After(8f, WeaponType.Melee), 0.001f);
            Assert.AreEqual(0f, SwingCost.After(3f, WeaponType.Melee), 0.001f);
            Assert.AreEqual(0f, SwingCost.After(-2f, WeaponType.Melee), 0.001f);
            Assert.AreEqual(40f, SwingCost.After(40f, WeaponType.Pistol), 0.001f);
            Assert.AreEqual("Too tired to swing", SwingCost.Line("en"));
            Assert.AreEqual("Demasiado cansado para cortar", SwingCost.Line("es"));
            Assert.AreEqual(48f, WeaponCard.Find("machete").Damage, 0.001f);
            Assert.AreEqual(1.8f, WeaponCard.Find("machete").Rate, 0.001f);
            Assert.AreEqual(1.9f, WeaponCard.Find("machete").Range, 0.001f);
            Assert.AreEqual(2f, WeaponCard.Find("machete").Noise, 0.001f);
            Assert.AreEqual(34f, WeaponCard.Find("pistol_9mm").Damage, 0.001f);
        }

        [Test]
        public void ACrouchedBladeStaysCloseAndAGunKeepsItsReach()
        {
            Assert.AreEqual(0.45f, QuietSwing.Crouch, 0.001f);
            Assert.AreEqual(1f, QuietSwing.Scale(false, WeaponType.Melee), 0.001f);
            Assert.AreEqual(0.45f, QuietSwing.Scale(true, WeaponType.Melee), 0.001f);
            Assert.AreEqual(1f, QuietSwing.Scale(true, WeaponType.Pistol), 0.001f);
            Assert.AreEqual(1f, QuietSwing.Scale(true, WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(2f, QuietSwing.Radius(2f, false, WeaponType.Melee), 0.001f);
            Assert.AreEqual(0.9f, QuietSwing.Radius(2f, true, WeaponType.Melee), 0.001f);
            Assert.AreEqual(0f, QuietSwing.Radius(-1f, true, WeaponType.Melee), 0.001f);
            Assert.AreEqual(20f, QuietSwing.Radius(20f, true, WeaponType.Pistol), 0.001f);
            Assert.AreEqual(34f, QuietSwing.Radius(34f, true, WeaponType.Rifle), 0.001f);
            Assert.AreEqual(2f, WeaponCard.Find("machete").Noise, 0.001f);
            Assert.AreEqual(8f, SwingCost.Melee, 0.001f);
            Assert.AreEqual(48f, WeaponCard.Find("machete").Damage, 0.001f);
        }

        [Test]
        public void ABladeOnAWallRingsFartherThanACleanSwing()
        {
            Assert.AreEqual(9f, BladeClang.Reach, 0.001f);
            Assert.AreEqual(0.55f, BladeClang.Crouch, 0.001f);
            Assert.AreEqual(9f, BladeClang.Radius(false), 0.001f);
            Assert.AreEqual(4.95f, BladeClang.Radius(true), 0.001f);
            Assert.AreEqual(2f, QuietSwing.Radius(2f, false, WeaponType.Melee), 0.001f);
            Assert.AreEqual(0.9f, QuietSwing.Radius(2f, true, WeaponType.Melee), 0.001f);
            Assert.AreEqual("clang", ContactCue.Impact(false, true));
            Assert.AreEqual("chop", ContactCue.Impact(true, true));
            Assert.AreEqual("", ContactCue.Impact(false, false));
            Assert.AreEqual(8f, SwingCost.Melee, 0.001f);
            Assert.AreEqual(2f, WeaponCard.Find("machete").Noise, 0.001f);
            Assert.AreEqual(0.45f, QuietSwing.Crouch, 0.001f);
        }

        [Test]
        public void ABladeBreaksAPaneAndALightRakeDoesNot()
        {
            Assert.AreEqual(48f, BladePane.Hit(48f), 0.001f);
            Assert.AreEqual(0f, BladePane.Hit(-3f), 0.001f);
            Assert.IsTrue(BladePane.Breaks(48f));
            Assert.IsTrue(BladePane.Breaks(12f));
            Assert.IsFalse(BladePane.Breaks(4f));
            Assert.IsFalse(BladePane.Breaks(0f));
            Assert.AreEqual(12f, PaneGlass.Hp, 0.001f);
            Assert.AreEqual(0f, PaneGlass.After(PaneGlass.Hp, 48f), 0.001f);
            Assert.AreEqual(8f, PaneGlass.After(PaneGlass.Hp, 4f), 0.001f);
            Assert.AreEqual(48f, WeaponCard.Find("machete").Damage, 0.001f);
            Assert.AreEqual(4f, PaneClaw.Hit, 0.001f);
            Assert.AreEqual(18f, PaneCharge.Hit, 0.001f);
            Assert.AreEqual(9f, PaneGlass.Noise, 0.001f);
            Assert.AreEqual(9f, BladeClang.Reach, 0.001f);
            Assert.AreEqual("The pane shatters", Loc.T("pane.break", "en"));
        }

        [Test]
        public void ABarredDoorHoldsUntilTheThirdSwingAndTheWayOutStaysOpen()
        {
            Assert.AreEqual(3, DoorBar.Hits);
            Assert.AreEqual(8f, DoorBar.Noise, 0.001f);
            Assert.AreEqual(12f, DoorBar.BreakNoise, 0.001f);
            Assert.IsTrue(DoorBar.Holds(3));
            Assert.IsTrue(DoorBar.Holds(1));
            Assert.IsFalse(DoorBar.Holds(0));
            Assert.IsFalse(DoorBar.Holds(-1));
            Assert.AreEqual(2, DoorBar.After(3));
            Assert.AreEqual(1, DoorBar.After(2));
            Assert.AreEqual(0, DoorBar.After(1));
            Assert.AreEqual(0, DoorBar.After(0));
            Assert.AreEqual("Barred", DoorBar.Face("en"));
            Assert.AreEqual("Atrancada", DoorBar.Face("es"));
            Assert.AreEqual("The bar holds", DoorBar.Hold("en"));
            Assert.AreEqual("La tranca aguanta", DoorBar.Hold("es"));
            Assert.AreEqual("The bar gives", DoorBar.Gives("en"));
            Assert.AreEqual("La tranca cede", DoorBar.Gives("es"));
            Assert.AreEqual("Step inside", DoorMap.Prompt(false));
            Assert.AreEqual("Step outside", DoorMap.Prompt(true));
            Assert.AreEqual("Inside", DoorMap.Cross(false, "en"));
            Assert.AreEqual(90f, DoorMap.Shift, 0.001f);
            Assert.AreEqual(48f, WeaponCard.Find("machete").Damage, 0.001f);
        }

        [Test]
        public void AClawWorksTheBarAndAChargeRipsItOpen()
        {
            Assert.AreEqual(1.1f, BarClaw.Reach, 0.001f);
            Assert.AreEqual(0.8f, BarClaw.Gap, 0.001f);
            Assert.AreEqual(3, BarClaw.Charge);
            Assert.AreEqual(2, BarClaw.Rake(3));
            Assert.AreEqual(1, BarClaw.Rake(2));
            Assert.AreEqual(0, BarClaw.Rake(1));
            Assert.AreEqual(0, BarClaw.Rake(0));
            Assert.AreEqual(0, BarClaw.Rake(-1));
            Assert.AreEqual(0, BarClaw.Rush(3));
            Assert.AreEqual(0, BarClaw.Rush(1));
            Assert.AreEqual(0, BarClaw.Rush(0));
            Assert.AreEqual(0, BarClaw.Rush(-2));
            Assert.IsFalse(BarClaw.Opens(3, false));
            Assert.IsTrue(BarClaw.Opens(1, false));
            Assert.IsTrue(BarClaw.Opens(3, true));
            Assert.IsFalse(BarClaw.Opens(0, true));
            Assert.IsFalse(BarClaw.Opens(0, false));
            Assert.AreEqual("The bar rattles", BarClaw.Rattle("en"));
            Assert.AreEqual("La tranca vibra", BarClaw.Rattle("es"));
            Assert.AreEqual(3, DoorBar.Hits);
            Assert.AreEqual(8f, DoorBar.Noise, 0.001f);
            Assert.AreEqual(12f, DoorBar.BreakNoise, 0.001f);
            Assert.AreEqual(4f, PaneClaw.Hit, 0.001f);
            Assert.AreEqual(18f, PaneCharge.Hit, 0.001f);
            Assert.AreEqual(1.1f, PaneClaw.Reach, 0.001f);
            Assert.AreEqual("Step inside", DoorMap.Prompt(false));
        }

        [Test]
        public void AShotHitsTheBarAndABlastSpendsTwo()
        {
            Assert.AreEqual(1, BarShot.Bullet);
            Assert.AreEqual(2, BarShot.Blast);
            Assert.AreEqual(1, BarShot.Hits(WeaponType.Pistol));
            Assert.AreEqual(1, BarShot.Hits(WeaponType.Rifle));
            Assert.AreEqual(1, BarShot.Hits(WeaponType.SMG));
            Assert.AreEqual(2, BarShot.Hits(WeaponType.Shotgun));
            Assert.AreEqual(0, BarShot.Hits(WeaponType.Melee));
            Assert.AreEqual(2, BarShot.Volley(WeaponType.Shotgun, 7));
            Assert.AreEqual(2, BarShot.Volley(WeaponType.Shotgun, 1));
            Assert.AreEqual(0, BarShot.Volley(WeaponType.Shotgun, 0));
            Assert.AreEqual(1, BarShot.Volley(WeaponType.Pistol, 7));
            Assert.AreEqual(0, BarShot.Volley(WeaponType.Melee, 1));
            Assert.AreEqual(2, BarShot.After(3, WeaponType.Pistol));
            Assert.AreEqual(2, BarShot.After(3, WeaponType.Rifle));
            Assert.AreEqual(2, BarShot.After(3, WeaponType.SMG));
            Assert.AreEqual(1, BarShot.After(3, WeaponType.Shotgun));
            Assert.AreEqual(0, BarShot.After(2, WeaponType.Shotgun));
            Assert.AreEqual(0, BarShot.After(1, WeaponType.Shotgun));
            Assert.AreEqual(0, BarShot.After(1, WeaponType.Pistol));
            Assert.AreEqual(0, BarShot.After(0, WeaponType.Shotgun));
            Assert.AreEqual(0, BarShot.After(-1, WeaponType.Pistol));
            Assert.AreEqual(3, BarShot.After(3, WeaponType.Melee));
            Assert.IsFalse(BarShot.Opens(3, WeaponType.Pistol));
            Assert.IsTrue(BarShot.Opens(1, WeaponType.Pistol));
            Assert.IsFalse(BarShot.Opens(3, WeaponType.Shotgun));
            Assert.IsTrue(BarShot.Opens(2, WeaponType.Shotgun));
            Assert.IsFalse(BarShot.Opens(0, WeaponType.Shotgun));
            Assert.AreEqual("The shot hits the bar", BarShot.Line("en"));
            Assert.AreEqual("El disparo pega en la tranca", BarShot.Line("es"));
            Assert.AreEqual(3, DoorBar.Hits);
            Assert.AreEqual(19f, WeaponCard.Find("shotgun_pump").Damage, 0.001f);
            Assert.AreEqual(7, WeaponCard.Find("shotgun_pump").Pellets);
            Assert.AreEqual(34f, WeaponCard.Find("pistol_9mm").Damage, 0.001f);
            Assert.AreEqual("The bar holds", DoorBar.Hold("en"));
            Assert.AreEqual("The bar rattles", BarClaw.Rattle("en"));
        }

        [Test]
        public void ACampWoundSlowsTheReloadAndTheNextSwing()
        {
            Assert.AreEqual(1.2f, WoundRack.Bite, 0.001f);
            Assert.AreEqual(1.45f, WoundRack.Fever, 0.001f);
            Assert.AreEqual(1.8f, WoundRack.Critical, 0.001f);
            Assert.AreEqual(1f, WoundRack.Scale(0), 0.001f);
            Assert.AreEqual(1f, WoundRack.Scale(-2), 0.001f);
            Assert.AreEqual(1.2f, WoundRack.Scale(1), 0.001f);
            Assert.AreEqual(1.45f, WoundRack.Scale(2), 0.001f);
            Assert.AreEqual(1.8f, WoundRack.Scale(3), 0.001f);
            Assert.AreEqual(1.8f, WoundRack.Scale(9), 0.001f);
            Assert.AreEqual(1.8f, WoundRack.Reload(1.8f, 0, 0), 0.001f);
            Assert.AreEqual(2.16f, WoundRack.Reload(1.8f, 0, 1), 0.001f);
            Assert.AreEqual(2.61f, WoundRack.Reload(1.8f, 0, 2), 0.001f);
            Assert.AreEqual(3.24f, WoundRack.Reload(1.8f, 0, 3), 0.001f);
            Assert.AreEqual(3.24f, WoundRack.Reload(1.8f, 0, 9), 0.001f);
            Assert.AreEqual(0f, WoundRack.Reload(-2f, 0, 2), 0.001f);
            Assert.AreEqual(1.296f, WoundRack.Reload(1.8f, 8, 0), 0.001f);
            Assert.AreEqual(1.8792f, WoundRack.Reload(1.8f, 8, 2), 0.001f);
            Assert.AreEqual(1f, FieldHand.Reload(0), 0.001f);
            Assert.AreEqual(0.8f, FieldHand.Reload(8), 0.001f);
            Assert.AreEqual(1f, HandDepth.Reload(4), 0.001f);
            Assert.AreEqual(0.9f, HandDepth.Reload(8), 0.001f);
            float machete = 1f / 1.8f;
            Assert.AreEqual(machete, WoundRack.Swing(machete, 0, WeaponType.Melee), 0.001f);
            Assert.AreEqual(machete * 1.45f, WoundRack.Swing(machete, 2, WeaponType.Melee), 0.001f);
            Assert.AreEqual(machete * 1.8f, WoundRack.Swing(machete, 3, WeaponType.Melee), 0.001f);
            Assert.AreEqual(0f, WoundRack.Swing(-1f, 2, WeaponType.Melee), 0.001f);
            Assert.AreEqual(1f / 9f, WoundRack.Swing(1f / 9f, 3, WeaponType.Rifle), 0.001f);
            Assert.AreEqual(1f / 14f, WoundRack.Swing(1f / 14f, 3, WeaponType.SMG), 0.001f);
            Assert.AreEqual("", WoundRack.Line(0, "en"));
            Assert.AreEqual("The wound slows your hands", WoundRack.Line(1, "en"));
            Assert.AreEqual("The wound slows your hands", WoundRack.Line(3, "en"));
            Assert.AreEqual("La herida retrasa las manos", WoundRack.Line(2, "es"));
            Assert.AreEqual(1.8f, WeaponCard.Find("machete").Rate, 0.001f);
            Assert.AreEqual(9f, WeaponCard.Find("rifle_assault").Rate, 0.001f);
            Assert.AreEqual(14f, WeaponCard.Find("smg").Rate, 0.001f);
            Assert.AreEqual(34f, WeaponCard.Find("pistol_9mm").Damage, 0.001f);
        }

        [Test]
        public void ABittenGuardShootsShortAndAFeverLeavesTheLine()
        {
            Assert.AreEqual(0.72f, PostBite.Reach, 0.001f);
            Assert.AreEqual(0.7f, PostBite.Hit, 0.001f);
            Assert.AreEqual(16f, PostBite.Range(0), 0.001f);
            Assert.AreEqual(16f, PostBite.Range(-1), 0.001f);
            Assert.AreEqual(11.52f, PostBite.Range(1), 0.001f);
            Assert.AreEqual(0f, PostBite.Range(2), 0.001f);
            Assert.AreEqual(0f, PostBite.Range(3), 0.001f);
            Assert.AreEqual(8f, PostBite.Damage(0), 0.001f);
            Assert.AreEqual(8f, PostBite.Damage(-2), 0.001f);
            Assert.AreEqual(5.6f, PostBite.Damage(1), 0.001f);
            Assert.AreEqual(0f, PostBite.Damage(2), 0.001f);
            Assert.AreEqual(0f, PostBite.Damage(9), 0.001f);
            Assert.AreEqual("The bite pulls the shot", PostBite.Line("en"));
            Assert.AreEqual("La mordedura tira el tiro", PostBite.Line("es"));
            Assert.AreEqual("Guard", GuardStand.Face("Guard", 60f, 1));
            Assert.AreEqual("Medic", GuardStand.Face("Guard", 60f, 2));
            Assert.AreEqual("Medic", GuardStand.Face("Guard", 60f, 3));
            Assert.AreEqual(8f, GuardVolley.Damage, 0.001f);
            Assert.AreEqual(16f, GuardVolley.Range, 0.001f);
            Assert.AreEqual(16f, GuardVolley.Hit(2), 0.001f);
            Assert.AreEqual(24f, GuardVolley.Hit(4), 0.001f);
            Assert.AreEqual(1.4f, GuardVolley.Interval, 0.001f);
        }

        [Test]
        public void AFollowerSwingsUpCloseAndACryStaysFarther()
        {
            Assert.AreEqual(1.6f, StreetAid.Reach, 0.001f);
            Assert.AreEqual(1.35f, StreetAid.Gap, 0.001f);
            Assert.AreEqual(14f, StreetAid.Damage, 0.001f);
            Assert.AreEqual(6f, StreetAid.Noise, 0.001f);
            Assert.IsTrue(StreetAid.Due(0f, 10f, 1.6f));
            Assert.IsTrue(StreetAid.Due(0f, 10f, 0f));
            Assert.IsFalse(StreetAid.Due(0f, 10f, 1.61f));
            Assert.IsFalse(StreetAid.Due(0f, 10f, -1f));
            Assert.IsFalse(StreetAid.Due(5f, 6f, 1f));
            Assert.IsTrue(StreetAid.Due(5f, 6.35f, 1f));
            Assert.IsFalse(StreetAid.Due(5f, 4f, 1f));
            Assert.IsFalse(StreetAid.Due(5f, 6.35f, 4f));
            Assert.IsTrue(StraggleCall.Due(0f, 10f, 4f));
            Assert.AreEqual(7f, StraggleCall.Near, 0.001f);
            Assert.AreEqual(6f, StraggleCall.Gap, 0.001f);
            Assert.AreEqual(16f, StraggleCall.Radius, 0.001f);
            Assert.AreEqual("Maya swings", StreetAid.Line("Maya", "en"));
            Assert.AreEqual("Maya golpea", StreetAid.Line("Maya", "es"));
            Assert.AreEqual("Survivor swings", StreetAid.Line("", "en"));
            Assert.AreEqual("Superviviente golpea", StreetAid.Line("Survivor", "es"));
            Assert.AreEqual("Maya cries out", StreetAsk.Cry("Maya", "en"));
        }

        [Test]
        public void ACloseZombieBitesTheFollowerAndTheBiteComesHome()
        {
            Assert.AreEqual(1.2f, FollowBite.Reach, 0.001f);
            Assert.AreEqual(2.4f, FollowBite.Gap, 0.001f);
            Assert.AreEqual(1, FollowBite.Wound);
            Assert.AreEqual(3, FollowBite.Cap);
            Assert.IsTrue(FollowBite.Due(0f, 10f, 1.2f));
            Assert.IsTrue(FollowBite.Due(0f, 10f, 0f));
            Assert.IsFalse(FollowBite.Due(0f, 10f, 1.21f));
            Assert.IsFalse(FollowBite.Due(0f, 10f, 1.5f));
            Assert.IsTrue(StreetAid.Due(0f, 10f, 1.5f));
            Assert.IsFalse(FollowBite.Due(4f, 6f, 1f));
            Assert.IsTrue(FollowBite.Due(4f, 6.4f, 1f));
            Assert.IsFalse(FollowBite.Due(6f, 5f, 1f));
            Assert.AreEqual(1, FollowBite.After(0));
            Assert.AreEqual(2, FollowBite.After(1));
            Assert.AreEqual(3, FollowBite.After(2));
            Assert.AreEqual(3, FollowBite.After(3));
            Assert.AreEqual(3, FollowBite.After(9));
            Assert.AreEqual(1, FollowBite.After(-1));
            Assert.AreEqual(0, FollowBite.Bring(0));
            Assert.AreEqual(1, FollowBite.Bring(1));
            Assert.AreEqual(3, FollowBite.Bring(3));
            Assert.AreEqual(3, FollowBite.Bring(9));
            Assert.AreEqual(0, FollowBite.Bring(-2));
            Assert.AreEqual(1, HomeSick.Carry(0, 1));
            Assert.AreEqual(3, HomeSick.Carry(0, 9));
            Assert.AreEqual("Maya is bitten", FollowBite.Line("Maya", "en"));
            Assert.AreEqual("Maya recibe una mordedura", FollowBite.Line("Maya", "es"));
            Assert.AreEqual("Survivor is bitten", FollowBite.Line("", "en"));
            Assert.AreEqual(1.6f, StreetAid.Reach, 0.001f);
            Assert.AreEqual(14f, StreetAid.Damage, 0.001f);
            Assert.AreEqual(7f, StraggleCall.Near, 0.001f);
        }

        [Test]
        public void ABittenFollowerSlowsAndACriticalOneStopsSwinging()
        {
            Assert.AreEqual(0.85f, FollowLimp.Bite, 0.001f);
            Assert.AreEqual(0.62f, FollowLimp.Fever, 0.001f);
            Assert.AreEqual(0.4f, FollowLimp.Critical, 0.001f);
            Assert.AreEqual(3, FollowLimp.Still);
            Assert.AreEqual(4.2f, FollowLimp.Pace(4.2f, 0), 0.001f);
            Assert.AreEqual(4.2f, FollowLimp.Pace(4.2f, -1), 0.001f);
            Assert.AreEqual(3.57f, FollowLimp.Pace(4.2f, 1), 0.001f);
            Assert.AreEqual(2.604f, FollowLimp.Pace(4.2f, 2), 0.001f);
            Assert.AreEqual(1.68f, FollowLimp.Pace(4.2f, 3), 0.001f);
            Assert.AreEqual(1.68f, FollowLimp.Pace(4.2f, 9), 0.001f);
            Assert.AreEqual(0f, FollowLimp.Pace(-2f, 2), 0.001f);
            Assert.IsTrue(FollowLimp.Swings(0));
            Assert.IsTrue(FollowLimp.Swings(1));
            Assert.IsTrue(FollowLimp.Swings(2));
            Assert.IsFalse(FollowLimp.Swings(3));
            Assert.IsFalse(FollowLimp.Swings(9));
            Assert.AreEqual("Maya can barely keep up", FollowLimp.Line("Maya", "en"));
            Assert.AreEqual("Maya apenas puede seguir", FollowLimp.Line("Maya", "es"));
            Assert.AreEqual("Survivor can barely keep up", FollowLimp.Line("", "en"));
            Assert.AreEqual(1.6f, RescueBook.FollowGap, 0.001f);
            Assert.AreEqual(14f, RescueBook.CatchUp, 0.001f);
            Assert.AreEqual(1.6f, StreetAid.Reach, 0.001f);
            Assert.AreEqual(1.2f, FollowBite.Reach, 0.001f);
            Assert.AreEqual(3, FollowBite.Cap);
            Assert.AreEqual("Maya is bitten", FollowBite.Line("Maya", "en"));
        }

        [Test]
        public void AMedkitEasesTheFollowerAndLeavesTheLeader()
        {
            Assert.IsTrue(FollowEase.Helps(1, 1));
            Assert.IsTrue(FollowEase.Helps(3, 2));
            Assert.IsFalse(FollowEase.Helps(1, 0));
            Assert.IsFalse(FollowEase.Helps(0, 2));
            Assert.IsFalse(FollowEase.Helps(-1, 1));
            Assert.AreEqual(0, FollowEase.After(1));
            Assert.AreEqual(1, FollowEase.After(2));
            Assert.AreEqual(2, FollowEase.After(3));
            Assert.AreEqual(0, FollowEase.After(0));
            Assert.AreEqual(0, FollowEase.After(-2));
            Assert.AreEqual(0, WoundEase.After(1));
            Assert.AreEqual(2, WoundEase.After(3));
            Assert.AreEqual("Ease the bite", FollowEase.Prompt("en"));
            Assert.AreEqual("Alivia la mordedura", FollowEase.Prompt("es"));
            Assert.AreEqual("The wound eases", FollowEase.Line("en"));
            Assert.AreEqual("La herida cede", FollowEase.Line("es"));
            Assert.AreEqual("The wound eases", WoundEase.Line("en"));
            Assert.AreEqual("Bitten", WoundCard.Line(1, "en"));
            Assert.AreEqual("Fever", WoundCard.Line(2, "en"));
            Assert.AreEqual("Critical", WoundCard.Line(3, "en"));
            Assert.AreEqual("", WoundCard.Line(0, "en"));
            Assert.AreEqual("Mara is with you", StreetAsk.With("Mara", "en"));
            Assert.AreEqual(50, FieldHand.Medkit(0));
        }

        [Test]
        public void AFourthBiteDropsTheFollowerAndThreeWoundsStay()
        {
            Assert.IsFalse(FollowFall.Drops(0));
            Assert.IsFalse(FollowFall.Drops(2));
            Assert.IsFalse(FollowFall.Drops(-1));
            Assert.IsTrue(FollowFall.Drops(3));
            Assert.IsTrue(FollowFall.Drops(9));
            Assert.AreEqual(3, FollowBite.After(3));
            Assert.AreEqual(3, FollowBite.Cap);
            Assert.IsFalse(FollowLimp.Swings(3));
            Assert.AreEqual("Maya falls", FollowFall.Line("Maya", "en"));
            Assert.AreEqual("Maya cae", FollowFall.Line("Maya", "es"));
            Assert.AreEqual("Survivor falls", FollowFall.Line("", "en"));
            Assert.AreEqual("Superviviente cae", FollowFall.Line("Survivor", "es"));
            Assert.AreEqual("Maya can barely keep up", FollowLimp.Line("Maya", "en"));
            Assert.AreEqual(8, RescueBook.RosterCap);
        }

        [Test]
        public void AStreetLossHurtsLessThanALeaderAndLeavesAMemorial()
        {
            Assert.AreEqual(8f, StreetMourn.Loss, 0.001f);
            Assert.AreEqual(64f, StreetMourn.After(72f), 0.001f);
            Assert.AreEqual(0f, StreetMourn.After(8f), 0.001f);
            Assert.AreEqual(0f, StreetMourn.After(3f), 0.001f);
            Assert.AreEqual(0f, StreetMourn.After(0f), 0.001f);
            Assert.AreEqual(0f, StreetMourn.After(-4f), 0.001f);
            Assert.AreEqual(92f, StreetMourn.After(100f), 0.001f);
            Assert.AreEqual("street", StreetMourn.Cause());
            Assert.AreEqual(25f, SuccessionLedger.CampLoss, 0.001f);
            Assert.AreEqual(40f, SuccessionLedger.FriendLoss, 0.001f);
            var row = new SuccessionLedger.Memorial { name = "Maya", day = 2, kills = 0, cause = StreetMourn.Cause(), district = "mall" };
            Assert.AreEqual("Maya  day 2  kills 0  street", SuccessionLedger.Card(row));
            Assert.AreEqual(3, FollowBite.Cap);
            Assert.IsTrue(FollowFall.Drops(3));
            Assert.IsFalse(FollowFall.Drops(2));
        }

        [Test]
        public void BringingTheStreetBodyHomeGivesALittleMoraleBack()
        {
            Assert.AreEqual(4f, StreetMourn.Back, 0.001f);
            Assert.AreEqual(68f, StreetMourn.Lift(64f), 0.001f);
            Assert.AreEqual(100f, StreetMourn.Lift(98f), 0.001f);
            Assert.AreEqual(100f, StreetMourn.Lift(100f), 0.001f);
            Assert.AreEqual(4f, StreetMourn.Lift(0f), 0.001f);
            Assert.AreEqual(4f, StreetMourn.Lift(-2f), 0.001f);
            Assert.IsTrue(StreetMourn.Named("street", "Maya", "Maya"));
            Assert.IsFalse(StreetMourn.Named("raid", "Maya", "Maya"));
            Assert.IsFalse(StreetMourn.Named("street", "Maya", "Imani"));
            Assert.IsFalse(StreetMourn.Named("street", "", "Maya"));
            Assert.AreEqual("The body is home", StreetMourn.Line("en"));
            Assert.AreEqual("El cuerpo está en casa", StreetMourn.Line("es"));
            Assert.AreEqual(8f, StreetMourn.Loss, 0.001f);
            Assert.AreEqual(64f, StreetMourn.After(72f), 0.001f);
            Assert.AreEqual(25f, SuccessionLedger.CampLoss, 0.001f);
            Assert.AreEqual(40f, SuccessionLedger.FriendLoss, 0.001f);
            Assert.AreEqual("Nothing left but the name", Loc.T("ask.nameonly", "en"));
            Assert.AreEqual("Gear recovered", Loc.T("ask.kept", "en"));
        }

        [Test]
        public void AFollowerSwingOrCryPullsAnIdleZombieAndAChaseStays()
        {
            Assert.IsTrue(FollowPull.Chases(true, true, false, NoiseType.MeleeSwing));
            Assert.IsTrue(FollowPull.Chases(true, true, false, NoiseType.ZombieScream));
            Assert.IsFalse(FollowPull.Chases(true, true, true, NoiseType.MeleeSwing));
            Assert.IsFalse(FollowPull.Chases(true, true, true, NoiseType.ZombieScream));
            Assert.IsFalse(FollowPull.Chases(true, false, false, NoiseType.MeleeSwing));
            Assert.IsFalse(FollowPull.Chases(false, true, false, NoiseType.ZombieScream));
            Assert.IsFalse(FollowPull.Chases(true, true, false, NoiseType.GunshotLoud));
            Assert.IsFalse(FollowPull.Chases(true, true, false, NoiseType.GunshotQuiet));
            Assert.IsFalse(FollowPull.Chases(true, true, false, NoiseType.WalkFootstep));
            Assert.IsFalse(FollowPull.Chases(true, true, false, NoiseType.SprintFootstep));
            Assert.IsFalse(FollowPull.Chases(true, true, false, NoiseType.Cough));
            Assert.IsFalse(FollowPull.Chases(true, true, false, NoiseType.DoorSwing));
            Assert.IsFalse(FollowPull.Chases(true, true, false, NoiseType.BleedDrip));
            Assert.IsFalse(FollowPull.Chases(true, true, false, NoiseType.ObjectBroken));
            Assert.AreEqual(6f, StreetAid.Noise, 0.001f);
            Assert.AreEqual(16f, StraggleCall.Radius, 0.001f);
            Assert.AreEqual(1.6f, StreetAid.Reach, 0.001f);
            Assert.AreEqual(7f, StraggleCall.Near, 0.001f);
        }

        [Test]
        public void AFallenFollowerDropsTheChaseAndALandedBiteStillCounts()
        {
            Assert.IsTrue(TargetDrop.Gone(true, false));
            Assert.IsFalse(TargetDrop.Gone(true, true));
            Assert.IsFalse(TargetDrop.Gone(false, false));
            Assert.IsFalse(TargetDrop.Gone(false, true));
            Assert.IsTrue(FollowBite.Due(0f, 10f, 0f));
            Assert.IsFalse(FollowBite.Due(10f, 10f, 0f));
            Assert.IsTrue(FollowBite.Due(7f, 9.4f, 0f));
            Assert.AreEqual(1.2f, FollowBite.Reach, 0.001f);
            Assert.AreEqual(2.4f, FollowBite.Gap, 0.001f);
            Assert.IsFalse(FollowPull.Chases(true, true, true, NoiseType.ZombieScream));
            Assert.IsTrue(FollowFall.Drops(3));
        }

        [Test]
        public void AZombieWithoutARigStillAttacksAndFalls()
        {
            Assert.AreEqual(14f, PoseSheet.Lean(ZombieAI.ZombieState.Chase, 0f), 0.001f);
            Assert.AreEqual(0f, PoseSheet.Lean(ZombieAI.ZombieState.Idle, 1f), 0.001f);
            Assert.AreEqual(0f, PoseSheet.Lean(ZombieAI.ZombieState.Wander, 1f), 0.001f);
            Assert.AreEqual(-22f, PoseSheet.Lean(ZombieAI.ZombieState.Stunned, 0.4f), 0.001f);
            Assert.AreEqual(0f, PoseSheet.Swing(0f), 0.001f);
            Assert.AreEqual(1f, PoseSheet.Swing(0.2f), 0.001f);
            Assert.AreEqual(0f, PoseSheet.Swing(0.45f), 0.001f);
            Assert.AreEqual(38f, PoseSheet.Lean(ZombieAI.ZombieState.Attack, 0.2f), 0.001f);
            Assert.AreEqual(0f, PoseSheet.Fall(0f), 0.001f);
            Assert.AreEqual(1f, PoseSheet.Fall(0.6f), 0.001f);
            Assert.AreEqual(1f, PoseSheet.Fall(2f), 0.001f);
            Assert.AreEqual(88f, PoseSheet.Lean(ZombieAI.ZombieState.Dead, 0.6f), 0.001f);
            Assert.AreEqual(0f, PoseSheet.Sink(ZombieAI.ZombieState.Chase, 1f), 0.001f);
            Assert.AreEqual(-0.55f, PoseSheet.Sink(ZombieAI.ZombieState.Dead, 0.6f), 0.001f);
            Assert.AreEqual(4.2f, PoseSheet.Speed(ZombieAI.ZombieState.Chase), 0.001f);
            Assert.AreEqual(1.1f, PoseSheet.Speed(ZombieAI.ZombieState.Wander), 0.001f);
            Assert.AreEqual(0f, PoseSheet.Speed(ZombieAI.ZombieState.Dead), 0.001f);
            Assert.IsTrue(PoseSheet.Sprint(ZombieAI.ZombieState.Chase));
            Assert.IsFalse(PoseSheet.Sprint(ZombieAI.ZombieState.Wander));
            Assert.AreEqual(0.05f, PoseSheet.Hop(ZombieAI.ZombieState.Chase, 1.5707963f), 0.001f);
            Assert.AreEqual(0f, PoseSheet.Hop(ZombieAI.ZombieState.Idle, 1.5707963f), 0.001f);
        }

        [Test]
        public void AHeadshotMistsAndABleedDrips()
        {
            Assert.IsTrue(WoundShow.MistDue(true, 1));
            Assert.IsTrue(WoundShow.MistDue(true, 2));
            Assert.IsFalse(WoundShow.MistDue(true, 0));
            Assert.IsFalse(WoundShow.MistDue(false, 2));
            Assert.IsFalse(WoundShow.Bleeds(0));
            Assert.IsTrue(WoundShow.Bleeds(1));
            Assert.IsTrue(WoundShow.DripDue(1f, 0f));
            Assert.IsFalse(WoundShow.DripDue(1.5f, 1f));
            Assert.IsTrue(WoundShow.DripDue(1.85f, 1f));
            Assert.IsTrue(WoundShow.DripDue(0.4f, 1f));
            Assert.IsTrue(WoundShow.Soaked(0.65f));
            Assert.IsFalse(WoundShow.Soaked(0.2f));
            Assert.IsFalse(WoundShow.Soaked(0f));
            Assert.AreEqual(0, WoundShow.Puffs(true, true, true));
            Assert.AreEqual(2, WoundShow.Puffs(false, false, false));
            Assert.AreEqual(6, WoundShow.Puffs(true, false, false));
            Assert.AreEqual(5, WoundShow.Puffs(false, false, true));
            Assert.AreEqual(7, WoundShow.Puffs(true, false, true));
            Assert.AreEqual(0.46f, WoundShow.Mist, 0.001f);
            Assert.AreEqual(10f, AudioSpace.MaxDistance("mist"), 0.001f);
            Assert.AreEqual(8f, AudioSpace.MaxDistance("splash"), 0.001f);
        }

        [Test]
        public void ABarrelLeavesAWakeAfterTheFlash()
        {
            Assert.AreEqual("fire", BlastWake.Wake(HazardKind.Explosive));
            Assert.AreEqual("cloud", BlastWake.Wake(HazardKind.Toxic));
            Assert.AreEqual("slick", BlastWake.Wake(HazardKind.Oil));
            Assert.AreEqual(3.2f, BlastWake.Hold(HazardKind.Explosive), 0.001f);
            Assert.AreEqual(4.5f, BlastWake.Hold(HazardKind.Toxic), 0.001f);
            Assert.AreEqual(6f, BlastWake.Hold(HazardKind.Oil), 0.001f);
            Assert.IsTrue(BlastWake.Ring(HazardKind.Explosive));
            Assert.IsFalse(BlastWake.Ring(HazardKind.Toxic));
            Assert.IsFalse(BlastWake.Ring(HazardKind.Oil));
            Assert.IsTrue(BlastWake.Smokes(HazardKind.Explosive));
            Assert.IsFalse(BlastWake.Smokes(HazardKind.Oil));
            Assert.AreEqual("burn", BlastWake.Sound(HazardKind.Explosive));
            Assert.AreEqual("burn", BlastWake.Sound(HazardKind.Oil));
            Assert.AreEqual("cloud", BlastWake.Sound(HazardKind.Toxic));
            Assert.AreEqual(0.32f, BlastWake.Volume(HazardKind.Oil), 0.001f);
            Assert.AreEqual(0.26f, BlastWake.Volume(HazardKind.Toxic), 0.001f);
            Assert.AreEqual(2.4f, BlastWake.Smoke, 0.001f);
            Assert.AreEqual(14f, AudioSpace.MaxDistance("burn"), 0.001f);
            Assert.AreEqual(12f, AudioSpace.MaxDistance("cloud"), 0.001f);
            Assert.AreEqual(1.4f, BarrelFuse.Length(HazardKind.Explosive), 0.001f);
            Assert.AreEqual(14f, OilBurn.Damage, 0.001f);
            Assert.IsTrue(BlastChunk.Throws(HazardKind.Explosive));
            Assert.IsFalse(BlastChunk.Throws(HazardKind.Toxic));
            Assert.IsFalse(BlastChunk.Throws(HazardKind.Oil));
            Assert.AreEqual(6, BlastChunk.Count);
            Assert.AreEqual(1.1f, BlastChunk.Life, 0.001f);
            Assert.AreEqual(4.5f, BlastChunk.Speed, 0.001f);
        }

        [Test]
        public void APowderBlastOpensAFireball()
        {
            Assert.IsTrue(BlastBall.Shows(HazardKind.Explosive));
            Assert.IsFalse(BlastBall.Shows(HazardKind.Toxic));
            Assert.IsFalse(BlastBall.Shows(HazardKind.Oil));
            Assert.AreEqual(4, BlastBall.Frames);
            Assert.AreEqual(0.48f, BlastBall.Life, 0.001f);
            Assert.AreEqual(0, BlastBall.Frame(0f));
            Assert.AreEqual(0, BlastBall.Frame(-0.2f));
            Assert.AreEqual(0, BlastBall.Frame(0.24f));
            Assert.AreEqual(1, BlastBall.Frame(0.25f));
            Assert.AreEqual(2, BlastBall.Frame(0.5f));
            Assert.AreEqual(3, BlastBall.Frame(0.75f));
            Assert.AreEqual(3, BlastBall.Frame(0.99f));
            Assert.AreEqual(3, BlastBall.Frame(2f));
            Assert.AreEqual(0.6f, BlastBall.Scale(0f), 0.001f);
            Assert.AreEqual(0.6f, BlastBall.Scale(-1f), 0.001f);
            Assert.AreEqual(2f, BlastBall.Scale(0.5f), 0.001f);
            Assert.AreEqual(3.4f, BlastBall.Scale(1f), 0.001f);
            Assert.AreEqual(3.4f, BlastBall.Scale(2f), 0.001f);
            Assert.AreEqual(0.4f, BlastBall.WarpScale(0f), 0.001f);
            Assert.AreEqual(6.6f, BlastBall.WarpScale(1f), 0.001f);
            Assert.AreEqual(6.2f, BlastBall.Warp, 0.001f);
            Assert.AreEqual(0.36f, BlastBall.WarpTime, 0.001f);
        }

        [Test]
        public void AFireDriftsEmbersAndABrokenLampSpitsSparks()
        {
            Assert.IsFalse(YardGlow.EmbersDue(false, 10f, 0f));
            Assert.IsTrue(YardGlow.EmbersDue(true, 1f, 0f));
            Assert.IsFalse(YardGlow.EmbersDue(true, 1.3f, 1f));
            Assert.IsTrue(YardGlow.EmbersDue(true, 1.6f, 1f));
            Assert.IsTrue(YardGlow.EmbersDue(true, 0.4f, 1f));
            Assert.IsFalse(YardGlow.SparksDue(1, 40, 10f, 0f));
            Assert.IsFalse(YardGlow.SparksDue(0, 0, 10f, 0f));
            Assert.IsFalse(YardGlow.SparksDue(0, 100, 10f, 0f));
            Assert.IsTrue(YardGlow.SparksDue(0, 40, 1f, 0f));
            Assert.IsFalse(YardGlow.SparksDue(0, 40, 1.3f, 1f));
            Assert.IsTrue(YardGlow.SparksDue(0, 40, 1.6f, 1f));
            Assert.AreEqual(8, YardGlow.Embers);
            Assert.AreEqual(5, YardGlow.Sparks);
            Assert.AreEqual(0.18f, YardGlow.Spit, 0.001f);
            Assert.AreEqual(8f, AudioSpace.MaxDistance("spit"), 0.001f);
            Assert.IsTrue(MendBoard.Needs(0, 40));
            Assert.IsFalse(MendBoard.Needs(0, 100));
            Assert.IsTrue(BuildSite.Ready(0, 40));
            Assert.IsFalse(BuildSite.Ready(1, 40));
        }

        [Test]
        public void TheGeneratorHumsAndTheFireCrackles()
        {
            YardBed.Mix(false, false, false, out float hum, out float crackle, out float buzz);
            Assert.AreEqual(0f, hum, 0.001f);
            Assert.AreEqual(0f, crackle, 0.001f);
            Assert.AreEqual(0f, buzz, 0.001f);
            YardBed.Mix(true, false, true, out hum, out crackle, out buzz);
            Assert.AreEqual(0.28f, hum, 0.001f);
            Assert.AreEqual(0f, crackle, 0.001f);
            Assert.AreEqual(0.14f, buzz, 0.001f);
            YardBed.Mix(false, true, true, out hum, out crackle, out buzz);
            Assert.AreEqual(0f, hum, 0.001f);
            Assert.AreEqual(0.22f, crackle, 0.001f);
            Assert.AreEqual(0f, buzz, 0.001f);
            YardBed.Mix(true, true, false, out hum, out crackle, out buzz);
            Assert.AreEqual(0.28f, hum, 0.001f);
            Assert.AreEqual(0.22f, crackle, 0.001f);
            Assert.AreEqual(0f, buzz, 0.001f);
            var dark = new List<PlacedModule> { new PlacedModule { kind = "Generator", x = 4f, z = -2f, site = 1, integrity = 100 } };
            Assert.IsFalse(YardBed.Spot(dark, "Generator", out _, out _));
            var lit = new List<PlacedModule>
            {
                new PlacedModule { kind = "Campfire", x = 1f, z = 2f, site = 0, integrity = 80 },
                new PlacedModule { kind = "Lamp", x = 6f, z = 3f, site = 0, integrity = 40 }
            };
            Assert.IsTrue(YardBed.Spot(lit, "Campfire", out float x, out float z));
            Assert.AreEqual(1f, x, 0.001f);
            Assert.AreEqual(2f, z, 0.001f);
            Assert.IsTrue(YardBed.Spot(lit, "Lamp", out x, out z));
            Assert.AreEqual(6f, x, 0.001f);
            Assert.AreEqual(18f, YardBed.Reach, 0.001f);
            Assert.AreEqual(MixBus.Ambience, AudioMix.BusOf("hum"));
            Assert.AreEqual(MixBus.Ambience, AudioMix.BusOf("crackle"));
            Assert.AreEqual(MixBus.Ambience, AudioMix.BusOf("buzz"));
            Assert.AreEqual(1f, AudioSpace.SpatialBlend("hum"), 0.001f);
        }

        [Test]
        public void AHitBarrelHissesThenCooksOff()
        {
            Assert.IsTrue(BarrelFuse.Arms(HazardKind.Explosive));
            Assert.IsTrue(BarrelFuse.Arms(HazardKind.Toxic));
            Assert.IsFalse(BarrelFuse.Arms(HazardKind.Oil));
            Assert.AreEqual(1.4f, BarrelFuse.Length(HazardKind.Explosive), 0.001f);
            Assert.AreEqual(0.8f, BarrelFuse.Length(HazardKind.Toxic), 0.001f);
            Assert.AreEqual(0f, BarrelFuse.Length(HazardKind.Oil), 0.001f);
            Assert.IsFalse(BarrelFuse.Due(0f, 5f, 1.4f));
            Assert.IsFalse(BarrelFuse.Due(1f, 2.3f, 1.4f));
            Assert.IsTrue(BarrelFuse.Due(1f, 2.4f, 1.4f));
            Assert.IsTrue(BarrelFuse.HissDue(0f, 1f));
            Assert.IsFalse(BarrelFuse.HissDue(1f, 1.2f));
            Assert.IsTrue(BarrelFuse.HissDue(1f, 1.35f));
            Assert.AreEqual(20f, AudioSpace.MaxDistance("hiss"), 0.001f);
            Assert.AreEqual("El barril silba", Loc.T("barrel.hiss", "es"));
        }

        [Test]
        public void ASwingChopsFleshAndClangsAWall()
        {
            Assert.AreEqual("chop", ContactCue.Impact(true, true));
            Assert.AreEqual("chop", ContactCue.Impact(true, false));
            Assert.AreEqual("clang", ContactCue.Impact(false, true));
            Assert.AreEqual("", ContactCue.Impact(false, false));
            Assert.IsTrue(ContactCue.Wall(GameLayers.Environment));
            Assert.IsFalse(ContactCue.Wall(GameLayers.Enemy));
            Assert.IsTrue(ContactCue.Pain(true, 40f));
            Assert.IsFalse(ContactCue.Pain(true, 0f));
            Assert.IsFalse(ContactCue.Pain(false, 40f));
            Assert.AreEqual(0f, AudioSpace.SpatialBlend("pained"), 0.001f);
            Assert.AreEqual(1f, AudioSpace.SpatialBlend("clang"), 0.001f);
        }

        [Test]
        public void PickingAPileIsASmallNoise()
        {
            Assert.AreEqual(3.5f, LootTake.Noise, 0.001f);
            Assert.AreEqual("take_soft", LootTake.Sound(LootKind.Medkit));
            Assert.AreEqual("take_box", LootTake.Sound(LootKind.Ammo9mm));
            Assert.AreEqual("take_box", LootTake.Sound(LootKind.AmmoShotgun));
            Assert.AreEqual("take_metal", LootTake.Sound(LootKind.Scrap));
            Assert.AreEqual("Picked up Medkit +2", LootTake.Line(LootKind.Medkit, 2, "en"));
            Assert.AreEqual("Recogido Botiquín +2", LootTake.Line(LootKind.Medkit, 2, "es"));
            Assert.AreEqual("Recogido Chatarra +1", LootTake.Line(LootKind.Scrap, 0, "es"));
            Assert.AreEqual("Recogido Cartuchos +6", LootTake.Line(LootKind.AmmoShotgun, 6, "es"));
        }

        [Test]
        public void AYoungNightRestsMoreAndAnOldSaveStaysAtEight()
        {
            Assert.AreEqual(8, LifeLine.Rest(0));
            Assert.AreEqual(10, LifeLine.Rest(22));
            Assert.AreEqual(10, LifeLine.Rest(28));
            Assert.AreEqual(8, LifeLine.Rest(29));
            Assert.AreEqual(8, LifeLine.Rest(51));
            Assert.AreEqual(5, LifeLine.Rest(52));
            Assert.AreEqual(5, LifeLine.Rest(61));
            int years = LifeLine.YearsOf("mara_quil");
            Assert.GreaterOrEqual(years, 22);
            Assert.LessOrEqual(years, 61);
            Assert.AreEqual(years, LifeLine.YearsOf("mara_quil"));
            Assert.AreNotEqual(LifeLine.Past("mara_quil"), LifeLine.Past("jonas_reed"));
            Assert.AreEqual("", LifeLine.Line(0, "past.nurse", "es"));
            Assert.AreEqual("34  Enfermera", LifeLine.Line(34, "past.nurse", "es"));
            Assert.AreEqual("Soldado", Loc.T("past.soldier", "es"));
        }

        [Test]
        public void AClipStepSilencesTheTimerBeat()
        {
            Assert.IsTrue(StepGate.AllowTimer(1f, 0f));
            Assert.IsFalse(StepGate.AllowTimer(1.1f, 1f));
            Assert.IsTrue(StepGate.AllowTimer(1.22f, 1f));
            Assert.IsTrue(StepGate.AllowTimer(0.5f, 1f));
            Assert.AreEqual(0.22f, StepGate.Hold, 0.001f);
        }

        [Test]
        public void AHostileMilitiaOpensWithExtraBodies()
        {
            Assert.IsFalse(CaravanBook.Ambush(-40));
            Assert.IsTrue(CaravanBook.Ambush(-41));
            Assert.AreEqual(0, AmbushBeat.Bodies(false, 32));
            Assert.AreEqual(0, AmbushBeat.Bodies(false, 8));
            Assert.AreEqual(4, AmbushBeat.Bodies(true, 16));
            Assert.AreEqual(4, AmbushBeat.Bodies(true, 32));
            Assert.AreEqual(4, AmbushBeat.Bodies(true, 40));
            Assert.AreEqual(2, AmbushBeat.Bodies(true, 15));
            Assert.AreEqual(2, AmbushBeat.Bodies(true, 4));
            Assert.AreEqual("The militia has the dead waiting", Loc.T("ambush.warn", "en"));
            Assert.AreEqual("La milicia tiene a los muertos esperando", Loc.T("ambush.warn", "es"));
        }

        [Test]
        public void FliesSitOnTheNearestDumpster()
        {
            Assert.IsTrue(FlyBed.Counts("Dumpster_Alley"));
            Assert.IsFalse(FlyBed.Counts("Crate_01"));
            Assert.IsFalse(FlyBed.Counts(null));
            Assert.IsFalse(FlyBed.Counts(""));
            Assert.AreEqual(0.16f, FlyBed.Gain(0f), 0.001f);
            Assert.AreEqual(0.08f, FlyBed.Gain(4f), 0.001f);
            Assert.AreEqual(0f, FlyBed.Gain(8f), 0.001f);
            Assert.AreEqual(0f, FlyBed.Gain(9f), 0.001f);
            Assert.AreEqual(0f, FlyBed.Gain(-1f), 0.001f);
            Assert.AreEqual(2, FlyBed.Nearest(new[] { 9f, 3f, 1f }));
            Assert.AreEqual(1, FlyBed.Nearest(new[] { 6f, 2f, 8f }));
            Assert.AreEqual(-1, FlyBed.Nearest(new[] { 8f, 12f }));
            Assert.AreEqual(-1, FlyBed.Nearest(null));
            Assert.AreEqual(8f, AudioSpace.MaxDistance("flies"), 0.001f);
            Assert.AreEqual(MixBus.Ambience, AudioMix.BusOf("flies"));
        }

        [Test]
        public void ACasingClinksAndARifleTracerWaitsForTheThirdRound()
        {
            Assert.IsFalse(BrassCue.Ejects(WeaponType.Melee));
            Assert.IsTrue(BrassCue.Ejects(WeaponType.Pistol));
            Assert.IsTrue(BrassCue.Ejects(WeaponType.Rifle));
            Assert.AreEqual("", BrassCue.Sound(WeaponType.Melee));
            Assert.AreEqual("clink", BrassCue.Sound(WeaponType.Pistol));
            Assert.AreEqual("clink", BrassCue.Sound(WeaponType.Rifle));
            Assert.AreEqual("clink", BrassCue.Sound(WeaponType.SMG));
            Assert.AreEqual("clack", BrassCue.Sound(WeaponType.Shotgun));
            Assert.AreEqual(0f, BrassCue.Volume(WeaponType.Melee), 0.001f);
            Assert.AreEqual(0.22f, BrassCue.Volume(WeaponType.Rifle), 0.001f);
            Assert.AreEqual(0.34f, BrassCue.Volume(WeaponType.Shotgun), 0.001f);
            Assert.IsFalse(BrassCue.Due(1f, 0f));
            Assert.IsFalse(BrassCue.Due(0.5f, 1f));
            Assert.IsFalse(BrassCue.Due(1.34f, 1f));
            Assert.IsTrue(BrassCue.Due(1.35f, 1f));
            Assert.IsTrue(BrassCue.Tracer(WeaponType.Pistol, 1));
            Assert.IsTrue(BrassCue.Tracer(WeaponType.Shotgun, 2));
            Assert.IsFalse(BrassCue.Tracer(WeaponType.Rifle, 1));
            Assert.IsFalse(BrassCue.Tracer(WeaponType.Rifle, 2));
            Assert.IsTrue(BrassCue.Tracer(WeaponType.Rifle, 3));
            Assert.IsTrue(BrassCue.Tracer(WeaponType.Rifle, 6));
            Assert.IsFalse(BrassCue.Tracer(WeaponType.SMG, 0));
            Assert.IsFalse(BrassCue.Tracer(WeaponType.SMG, 4));
            Assert.IsTrue(BrassCue.Tracer(WeaponType.SMG, 3));
            Assert.AreEqual(6f, AudioSpace.MaxDistance("clink"), 0.001f);
            Assert.AreEqual(6f, AudioSpace.MaxDistance("clack"), 0.001f);
        }

        [Test]
        public void AHitReadsTheSurfaceAndACorpseBurnsAway()
        {
            Assert.AreEqual("flesh", StrikeFace.OfName("Zombie_Walker"));
            Assert.AreEqual("metal", StrikeFace.OfName("Dumpster_Alley"));
            Assert.AreEqual("metal", StrikeFace.OfName("Prop_Barrel_Red"));
            Assert.AreEqual("wood", StrikeFace.OfName("RoomCrate"));
            Assert.AreEqual("wood", StrikeFace.OfName("StreetDoor"));
            Assert.AreEqual("concrete", StrikeFace.OfName("DistrictLot"));
            Assert.AreEqual("concrete", StrikeFace.OfName(null));
            Assert.AreEqual("concrete", StrikeFace.OfName(""));
            Assert.AreEqual("spray", StrikeFace.Sound("flesh"));
            Assert.AreEqual("spark", StrikeFace.Sound("metal"));
            Assert.AreEqual("splinter", StrikeFace.Sound("wood"));
            Assert.AreEqual("dust", StrikeFace.Sound("concrete"));
            Assert.AreEqual(0.4f, StrikeFace.Volume("flesh"), 0.001f);
            Assert.AreEqual(0.38f, StrikeFace.Volume("metal"), 0.001f);
            Assert.AreEqual(0.36f, StrikeFace.Volume("wood"), 0.001f);
            Assert.AreEqual(0.28f, StrikeFace.Volume("concrete"), 0.001f);
            Assert.AreEqual(16f, AudioSpace.MaxDistance("spark"), 0.001f);
            Assert.AreEqual(12f, AudioSpace.MaxDistance("splinter"), 0.001f);
            Assert.AreEqual(8f, AudioSpace.MaxDistance("dust"), 0.001f);
            Assert.AreEqual(0f, CorpseMelt.Amount(0f), 0.001f);
            Assert.AreEqual(0.5f, CorpseMelt.Amount(1.5f), 0.001f);
            Assert.AreEqual(1f, CorpseMelt.Amount(3f), 0.001f);
            Assert.AreEqual(1f, CorpseMelt.Amount(4f), 0.001f);
            Assert.AreEqual(3f, CorpseMelt.Length, 0.001f);
        }

        [Test]
        public void ABulletHoleStaysOnTheStreetUntilTheRunEnds()
        {
            Assert.IsTrue(MarkStay.Holds("hole"));
            Assert.IsTrue(MarkStay.Holds("scorch"));
            Assert.IsFalse(MarkStay.Holds("blood"));
            Assert.IsFalse(MarkStay.Holds("oil"));
            Assert.IsFalse(MarkStay.Holds(null));
            Assert.AreEqual(8f, MarkStay.Life("blood"), 0.001f);
            Assert.AreEqual(8f, MarkStay.Life("oil"), 0.001f);
            Assert.AreEqual(0f, MarkStay.Life("hole"), 0.001f);
            Assert.AreEqual(0f, MarkStay.Life("scorch"), 0.001f);
            Assert.IsTrue(MarkStay.OnStreet(GameState.ExpeditionActive));
            Assert.IsTrue(MarkStay.OnStreet(GameState.RaidActive));
            Assert.IsFalse(MarkStay.OnStreet(GameState.CampManagement));
            Assert.IsTrue(MarkStay.Visible("hole", true, true));
            Assert.IsFalse(MarkStay.Visible("hole", true, false));
            Assert.IsFalse(MarkStay.Visible("scorch", false, true));
            Assert.IsTrue(MarkStay.Visible("blood", false, false));
            Assert.IsTrue(GoreMark.Near(28f, 0f, 28f));
            Assert.IsFalse(GoreMark.Near(30f, 0f, 30f));
        }

        [Test]
        public void GoreOffDropsBloodAndAShotgunSpraysTheWall()
        {
            Assert.AreEqual(0, GoreMark.Splats(0, false, true));
            Assert.AreEqual(1, GoreMark.Splats(0, false, false));
            Assert.AreEqual(1, GoreMark.Splats(1, false, true));
            Assert.AreEqual(3, GoreMark.Splats(1, true, true));
            Assert.AreEqual(5, GoreMark.Splats(2, true, true));
            Assert.AreEqual(0.42f, GoreMark.Size("blood", 1), 0.001f);
            Assert.AreEqual(0.567f, GoreMark.Size("blood", 2), 0.001f);
            Assert.AreEqual(0.7f, GoreMark.Size("scorch", 1), 0.001f);
            Assert.IsTrue(GoreMark.Near(10f, 0f, 10f));
            Assert.IsFalse(GoreMark.Near(30f, 0f, 30f));
            Assert.AreEqual(0.42f, GoreMark.FadeScale(2f, 0.42f), 0.001f);
            Assert.AreEqual(0.21f, GoreMark.FadeScale(1f, 0.42f), 0.001f);
            Assert.AreEqual(0f, GoreMark.FadeScale(0f, 0.42f), 0.001f);
            Assert.AreEqual(0.098f, GoreMark.Spread(0f, 0.28f), 0.001f);
            Assert.AreEqual(0.28f, GoreMark.Spread(3f, 0.28f), 0.001f);
            GoreMark.Offset(1, out float x, out float y);
            Assert.AreEqual(0.18f, x, 0.001f);
            Assert.AreEqual(0.08f, y, 0.001f);
        }

        [Test]
        public void BloodStreaksWithTheShot()
        {
            Assert.IsFalse(BloodDrift.Shows(0, true));
            Assert.IsFalse(BloodDrift.Shows(1, false));
            Assert.IsTrue(BloodDrift.Shows(1, true));
            Assert.IsTrue(BloodDrift.Shows(2, true));
            BloodDrift.Along(1f, 0f, 0f, out float ox, out float oy, out float oz);
            Assert.AreEqual(0.55f, ox, 0.001f);
            Assert.AreEqual(0f, oy, 0.001f);
            Assert.AreEqual(0f, oz, 0.001f);
            BloodDrift.Along(0f, -1f, 0f, out ox, out oy, out oz);
            Assert.AreEqual(0f, ox, 0.001f);
            Assert.AreEqual(0f, oy, 0.001f);
            Assert.AreEqual(0f, oz, 0.001f);
            BloodDrift.Along(0f, 0f, 0f, out ox, out oy, out oz);
            Assert.AreEqual(0f, ox, 0.001f);
            Assert.AreEqual(0f, oz, 0.001f);
            BloodDrift.Along(3f, -1f, 0f, out ox, out oy, out oz);
            Assert.AreEqual(0.55f, ox, 0.001f);
            Assert.AreEqual(0f, oy, 0.001f);
            Assert.AreEqual(0f, oz, 0.001f);
            BloodDrift.Along(-1f, 2f, -1f, out ox, out oy, out oz);
            Assert.AreEqual(-0.3889f, ox, 0.001f);
            Assert.AreEqual(0f, oy, 0.001f);
            Assert.AreEqual(-0.3889f, oz, 0.001f);
            Assert.AreEqual(3, BloodDrift.Drops);
            Assert.AreEqual(0, GoreMark.Splats(0, false, true));
        }

        [Test]
        public void AWallStopsAScreamAndMufflesTheGun()
        {
            Assert.AreEqual(0.8f, EarWall.Gain(0.8f, false, true), 0.001f);
            Assert.AreEqual(0f, EarWall.Gain(0.8f, true, true), 0.001f);
            Assert.AreEqual(0.36f, EarWall.Gain(0.8f, true, false), 0.001f);
            Assert.AreEqual(EarWall.MuffledHz, EarWall.Muffle(AudioMix.OpenHz), 0.001f);
            Assert.AreEqual(AudioMix.PausedHz, EarWall.Muffle(AudioMix.PausedHz), 0.001f);
            Assert.IsTrue(EarWall.InWorld(AudioSpace.SpatialBlend("gun")));
            Assert.IsTrue(EarWall.InWorld(AudioSpace.SpatialBlend("scream")));
            Assert.IsFalse(EarWall.InWorld(AudioSpace.SpatialBlend("step")));
            Assert.IsFalse(EarWall.InWorld(AudioSpace.SpatialBlend("rain")));
            Assert.IsFalse(EarWall.InWorld(AudioSpace.SpatialBlend("ambient")));
        }

        [Test]
        public void BrightFlashesStayUnderThreeASecond()
        {
            Assert.IsTrue(FlashCap.Allow(0f, 0.34f, false));
            Assert.IsFalse(FlashCap.Allow(1f, 1.2f, false));
            Assert.IsFalse(FlashCap.Allow(0f, 5f, true));
            Assert.IsFalse(FlashCap.Due(false, 0f, 20f));
            Assert.IsFalse(FlashCap.Due(true, 0f, 7.9f));
            Assert.IsTrue(FlashCap.Due(true, 0f, 8f));
            Assert.IsTrue(FlashCap.Due(true, 8f, 16f));
            Assert.IsFalse(FlashCap.Due(true, 8f, 15.9f));
            Assert.AreEqual("Sin destellos", Loc.T("set.flash_off", "es"));
            Assert.AreEqual("Destellos sí", Loc.T("set.flash_on", "es"));
        }

        [Test]
        public void AMuzzleMatchesTheGunAndTheWatchCountsIt()
        {
            Assert.IsFalse(MuzzleShape.Shows(WeaponType.Melee));
            Assert.IsTrue(MuzzleShape.Shows(WeaponType.Pistol));
            Assert.IsTrue(MuzzleShape.Smoke(WeaponType.Shotgun));
            Assert.IsFalse(MuzzleShape.Smoke(WeaponType.Pistol));
            Assert.IsFalse(MuzzleShape.Smoke(WeaponType.Rifle));
            Assert.IsTrue(MuzzleShape.Strobe(WeaponType.Rifle));
            Assert.IsTrue(MuzzleShape.Strobe(WeaponType.SMG));
            Assert.IsFalse(MuzzleShape.Strobe(WeaponType.Shotgun));
            Assert.IsFalse(MuzzleShape.Strobe(WeaponType.Pistol));
            Assert.AreEqual(0.12f, MuzzleShape.Scale(WeaponType.Pistol), 0.001f);
            Assert.AreEqual(0.28f, MuzzleShape.Scale(WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(0.08f, MuzzleShape.Scale(WeaponType.Rifle), 0.001f);
            Assert.AreEqual(0.08f, MuzzleShape.Scale(WeaponType.SMG), 0.001f);
            Assert.AreEqual(3.2f, MuzzleShape.Range(WeaponType.Pistol), 0.001f);
            Assert.AreEqual(6.5f, MuzzleShape.Range(WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(5.5f, MuzzleShape.Range(WeaponType.Rifle), 0.001f);
            Assert.AreEqual(2.4f, MuzzleShape.Intensity(WeaponType.Pistol), 0.001f);
            Assert.AreEqual(4.2f, MuzzleShape.Intensity(WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(5f, MuzzleShape.Intensity(WeaponType.SMG), 0.001f);
            Assert.AreEqual(0.05f, MuzzleShape.Hold(WeaponType.Pistol), 0.001f);
            Assert.AreEqual(0.08f, MuzzleShape.Hold(WeaponType.Shotgun), 0.001f);
            Assert.AreEqual(0.03f, MuzzleShape.Hold(WeaponType.Rifle), 0.001f);
            VfxLedger.Reset();
            VfxLedger.Borrow();
            VfxLedger.Borrow();
            Assert.AreEqual(2, VfxLedger.Live);
            Assert.AreEqual(2, VfxLedger.Peak);
            VfxLedger.Return();
            Assert.AreEqual(1, VfxLedger.Live);
            Assert.AreEqual(2, VfxLedger.Peak);
            Assert.AreEqual("vfx 1  peak 2", VfxLedger.Line());
            VfxLedger.Return();
            VfxLedger.Return();
            Assert.AreEqual(0, VfxLedger.Live);
            VfxLedger.Reset();
        }

        [Test]
        public void ThunderCoversAGunshotAndStillNamesItself()
        {
            Assert.IsFalse(StormCover.ThunderDue(0f, 20f, false));
            Assert.IsFalse(StormCover.ThunderDue(10f, 10.59f, false));
            Assert.IsTrue(StormCover.ThunderDue(10f, 10.6f, false));
            Assert.IsFalse(StormCover.ThunderDue(10f, 10.6f, true));
            Assert.IsFalse(StormCover.Masks(0f, 10f, NoiseType.GunshotLoud));
            Assert.IsFalse(StormCover.Masks(10f, 10.5f, NoiseType.GunshotLoud));
            Assert.IsTrue(StormCover.Masks(10f, 10.6f, NoiseType.GunshotLoud));
            Assert.IsTrue(StormCover.Masks(10f, 11.9f, NoiseType.GunshotQuiet));
            Assert.IsFalse(StormCover.Masks(10f, 12f, NoiseType.GunshotLoud));
            Assert.IsFalse(StormCover.Masks(10f, 10.6f, NoiseType.Explosion));
            Assert.IsFalse(StormCover.Masks(10f, 10.6f, NoiseType.WalkFootstep));
            Assert.AreEqual(0f, HearGate.Perceived(0f, 40f, 1f, false, NoiseType.Thunder), 0.001f);
            Assert.AreEqual(0.09f, HearGate.Perceived(8f, 10f, 1f, true, NoiseType.GunshotLoud), 0.001f);
            Assert.AreEqual(0.8f, WeatherSurface.Sight(WeatherKind.Rain), 0.001f);
            Assert.AreEqual(1f, RainMask.Heard(1f, false), 0.001f);
            Assert.AreEqual(0f, RainMask.Heard(0f, true), 0.001f);
            Assert.AreEqual(0.85f, RainMask.Heard(1f, true), 0.001f);
            Assert.AreEqual(0.051f, RainMask.Heard(0.06f, true), 0.001f);
            Assert.AreEqual(0f, RainMask.Heard(0.05f, true), 0.001f);
            Assert.AreEqual(0.15f, RainMask.Cover, 0.001f);
            Assert.AreEqual("whoosh", SwingCue.Sound(WeaponType.Melee));
            Assert.AreEqual("", SwingCue.Sound(WeaponType.Pistol));
            Assert.AreEqual(0.36f, SwingCue.Volume, 0.001f);
            Assert.AreEqual(14f, AudioSpace.MaxDistance("whoosh"), 0.001f);
            Assert.AreEqual(0.95f, PitchGate.Next(0f, 0f), 0.001f);
            Assert.AreEqual(1.05f, PitchGate.Next(0f, 1f), 0.001f);
            Assert.AreEqual(1f, PitchGate.Next(0f, 0.5f), 0.001f);
            Assert.AreEqual(1.02f, PitchGate.Next(1f, 0.5f), 0.001f);
            Assert.AreEqual(0.97f, PitchGate.Next(0.95f, 0f), 0.001f);
            Assert.AreEqual(1.03f, PitchGate.Next(1.05f, 1f), 0.001f);
            Assert.AreEqual(0, KinBoard.Read("", "ellis"));
            Assert.AreEqual(0, KinBoard.Read("ellis:2", "jonas"));
            Assert.AreEqual("ellis:2", KinBoard.Shift("", "ellis", 2));
            Assert.AreEqual("ellis:100", KinBoard.Shift("ellis:99", "ellis", 5));
            Assert.AreEqual("ellis:-100", KinBoard.Shift("ellis:0", "ellis", -140));
            Assert.IsFalse(KinBoard.Close("ellis:39", "ellis"));
            Assert.IsTrue(KinBoard.Close("ellis:40", "ellis"));
            Assert.AreEqual("ellis", KinBoard.Closest("jonas:10|ellis:40"));
            Assert.AreEqual("", KinBoard.Closest("ellis:39"));
            Assert.IsTrue(KinBoard.Grieves("Close to Mara", "", "Mara Quill", ""));
            Assert.IsFalse(KinBoard.Grieves("", "", "Mara Quill", "mara"));
            Assert.IsTrue(KinBoard.Grieves("", "mara:40", "Mara Quill", "mara"));
            var mourned = new List<ColonistDay>
            {
                new ColonistDay { id = "jonas", task = "Guard", kin = "mara:40", morale = 80f, hunger = 78f, thirst = 78f },
                new ColonistDay { id = "ellis", task = "Scavenge", morale = 80f, hunger = 78f, thirst = 78f },
                new ColonistDay { id = "mara", name = "Mara Quill", alive = false, task = "Fallen" }
            };
            int kinFood = 0;
            int kinWater = 0;
            var kinGrief = ColonyDay.Simulate(mourned, ref kinFood, ref kinWater, false, false, "Mara Quill");
            Assert.AreEqual(40f, mourned[0].morale);
            Assert.AreEqual(55f, mourned[1].morale);
            Assert.Contains("grief", kinGrief);
            Assert.AreEqual("mara", KinBoard.FallenId(mourned, "Mara Quill"));
            var blank = JsonUtility.FromJson<SurvivorSave>("{\"id\":\"ada\"}");
            Assert.IsNull(blank.kin);
            Assert.AreEqual("close", Loc.T("camp.close", "en"));
            Assert.AreEqual("cercano", Loc.T("camp.close", "es"));
            Assert.AreEqual("[Thunder, east]", Presentation.Caption(NoiseType.Thunder, 4f, 0f, "en"));
            Assert.AreEqual("[Trueno, este]", Presentation.Caption(NoiseType.Thunder, 4f, 0f, "es"));
            Assert.AreEqual(48f, AudioSpace.MaxDistance("thunder"), 0.001f);
            Assert.AreEqual(0.7f, StormCover.Volume, 0.001f);
            Assert.AreEqual(40f, StormCover.Radius, 0.001f);
        }

        [Test]
        public void AFarShotCarriesALowerTail()
        {
            Assert.AreEqual(0f, SoundTail.Gun(11.9f), 0.001f);
            Assert.AreEqual(0f, SoundTail.Gun(12f), 0.001f);
            Assert.AreEqual(0.175f, SoundTail.Gun(22f), 0.001f);
            Assert.AreEqual(0.35f, SoundTail.Gun(32f), 0.001f);
            Assert.AreEqual(0.35f, SoundTail.Gun(80f), 0.001f);
            Assert.AreEqual(0f, SoundTail.Echo(18f), 0.001f);
            Assert.AreEqual(0.2f, SoundTail.Echo(33f), 0.001f);
            Assert.AreEqual(0.4f, SoundTail.Echo(48f), 0.001f);
            Assert.AreEqual(1f, SoundTail.Pitch(12f), 0.001f);
            Assert.AreEqual(0.62f, SoundTail.Pitch(32f), 0.001f);
            Assert.AreEqual(48f, AudioSpace.MaxDistance("boom_far"), 0.001f);
            Assert.AreEqual(32f, AudioSpace.MaxDistance("gun_far"), 0.001f);
        }

        [Test]
        public void ALowBodyThumpsAndPants()
        {
            Assert.IsFalse(BodyCue.Heart(0.25f));
            Assert.IsTrue(BodyCue.Heart(0.24f));
            Assert.IsFalse(BodyCue.Heart(0f));
            Assert.AreEqual(0f, BodyCue.HeartGap(0.25f), 0.001f);
            Assert.AreEqual(0.76f, BodyCue.HeartGap(0.125f), 0.001f);
            Assert.AreEqual(0.81f, BodyCue.HeartPitch(0.125f), 0.001f);
            Assert.IsFalse(BodyCue.Breath(0.2f));
            Assert.IsTrue(BodyCue.Breath(0.19f));
            Assert.IsFalse(BodyCue.Breath(0f));
            Assert.AreEqual(2.4f, BodyCue.BreathGap, 0.001f);
            Assert.IsFalse(BodyCue.Due(1f, 1f, 1.1f));
            Assert.IsTrue(BodyCue.Due(2.1f, 1f, 1.1f));
            Assert.AreEqual(0f, AudioSpace.SpatialBlend("heart"), 0.001f);
            Assert.AreEqual(0f, AudioSpace.SpatialBlend("breath"), 0.001f);
        }

        [Test]
        public void AnEmptyGunClicksAndAReloadSpeaksThreeTimes()
        {
            Assert.AreEqual("dry", GunCue.Click(0, false));
            Assert.AreEqual("", GunCue.Click(1, false));
            Assert.AreEqual("", GunCue.Click(0, true));
            Assert.AreEqual("mag_out", GunCue.Stage(0f, 0));
            Assert.AreEqual("", GunCue.Stage(0.2f, 1));
            Assert.AreEqual("mag_in", GunCue.Stage(0.33f, 1));
            Assert.AreEqual("rack", GunCue.Stage(0.72f, 2));
            Assert.AreEqual("", GunCue.Stage(1f, 3));
            Assert.AreEqual(3, GunCue.Mark("rack"));
        }

        [Test]
        public void ARunnerShrieksAndABruteStomps()
        {
            Assert.AreEqual("walker", ZombieVoice.Breed("walker", 0));
            Assert.AreEqual("runner", ZombieVoice.Breed("walker", 1));
            Assert.AreEqual("brute", ZombieVoice.Breed("", 2));
            Assert.AreEqual("runner", ZombieVoice.Breed("Zombie_Runner", 0));
            Assert.AreEqual("groan", ZombieVoice.Idle("walker"));
            Assert.AreEqual("shriek", ZombieVoice.Idle("runner"));
            Assert.AreEqual("roar", ZombieVoice.Idle("brute"));
            Assert.AreEqual("snarl", ZombieVoice.Bite("runner"));
            Assert.AreEqual("stomp", ZombieVoice.Bite("brute"));
            Assert.AreEqual("shriek", ZombieVoice.Hurt("runner"));
            Assert.AreEqual("roar", ZombieVoice.Death("brute"));
            Assert.IsFalse(ZombieVoice.IdleDue(22.1f, 0, 10f, 0f));
            Assert.IsFalse(ZombieVoice.IdleDue(10f, 6, 10f, 0f));
            Assert.IsFalse(ZombieVoice.IdleDue(10f, 2, 10f, 6f));
            Assert.IsTrue(ZombieVoice.IdleDue(10f, 5, 10.8f, 6f));
            var seats = new float[6];
            ZombieVoice.Seat(seats, 1f);
            ZombieVoice.Seat(seats, 1f);
            Assert.AreEqual(2, ZombieVoice.Live(1.1f, seats));
            Assert.AreEqual(0, ZombieVoice.Live(1f + ZombieVoice.Hold, seats));
            Assert.AreEqual(40f, AudioSpace.MaxDistance("stomp"), 0.001f);
            Assert.AreEqual(36f, AudioSpace.MaxDistance("scream"), 0.001f);
            Assert.AreEqual(22f, AudioSpace.MaxDistance("groan"), 0.001f);
        }

        [Test]
        public void DemolishingAModuleReturnsHalfTheScrap()
        {
            Assert.AreEqual(3, ScrapRefund.Half(6));
            Assert.AreEqual(4, ScrapRefund.Half(9));
            Assert.AreEqual(11, ScrapRefund.Half(22));
            Assert.AreEqual(0, ScrapRefund.Half(1));
            Assert.AreEqual(0, ScrapRefund.Half(0));
            Assert.AreEqual(0, ScrapRefund.Half(-4));
            Assert.AreEqual(90, ScrapRefund.Turn(0));
            Assert.AreEqual(180, ScrapRefund.Turn(90));
            Assert.AreEqual(270, ScrapRefund.Turn(180));
            Assert.AreEqual(0, ScrapRefund.Turn(270));
            Assert.AreEqual(90, ScrapRefund.Turn(-30));
        }

        [Test]
        public void AnEastGridKeepsAWalkFromTheSpawnToTheFarGate()
        {
            var ash = RoadGraph.Build(1701, "ash_market");
            Assert.AreEqual(25, ash.Cells.Length);
            Assert.AreEqual("storefront", ash.Footprint);
            Assert.AreEqual(32f, ash.PoiX, 0.001f);
            Assert.AreEqual(12f, ash.PoiZ, 0.001f);
            Assert.AreEqual(40f, ash.ExtractX, 0.001f);
            Assert.AreEqual(0f, ash.ExtractZ, 0.001f);
            Assert.AreEqual("spine", RoadGraph.KindAt(ash, 4f, 0f));
            Assert.AreEqual("alley", RoadGraph.KindAt(ash, 24f, 4f));
            Assert.AreEqual("lot", RoadGraph.KindAt(ash, 24f, 8f));
            Assert.AreEqual("hole", RoadGraph.KindAt(ash, 24f, 12f));
            Assert.IsTrue(ash.HasLoot);
            Assert.IsTrue(RoadGraph.Navigable(ash));

            var hospital = RoadGraph.Build(1701, "old_hospital");
            Assert.AreEqual("clinic", hospital.Footprint);
            Assert.AreEqual("alley", RoadGraph.KindAt(hospital, 24f, 4f));
            Assert.AreEqual("hole", RoadGraph.KindAt(hospital, 40f, 4f));
            Assert.AreEqual(40f, hospital.NestX, 0.001f);
            Assert.AreEqual(4f, hospital.NestZ, 0.001f);

            var downtown = RoadGraph.Build(4, "downtown_core");
            Assert.AreEqual("station", downtown.Footprint);
            Assert.AreEqual("road", RoadGraph.KindAt(downtown, 24f, 8f));
            Assert.AreEqual("hole", RoadGraph.KindAt(downtown, 40f, 4f));
            Assert.AreEqual("poi", RoadGraph.KindAt(downtown, 32f, 12f));

            var mall = RoadGraph.Build(1701, "mall");
            var mallAgain = RoadGraph.Build(1701, "mall");
            Assert.AreEqual(RoadGraph.Signature(mall), RoadGraph.Signature(mallAgain));
            Assert.AreEqual("alley", RoadGraph.KindAt(mall, 24f, 4f));
            Assert.AreEqual("lot", RoadGraph.KindAt(mall, 28f, 4f));
            Assert.AreEqual("hole", RoadGraph.KindAt(mall, 40f, 8f));
            Assert.AreEqual(28f, mall.LootX, 0.001f);
            Assert.AreEqual(4f, mall.LootZ, 0.001f);
            Assert.AreEqual(40f, mall.NestX, 0.001f);
            Assert.AreEqual(8f, mall.NestZ, 0.001f);
            Assert.AreNotEqual(RoadGraph.Signature(mall), RoadGraph.Signature(RoadGraph.Build(99991, "mall")));

            var ids = CampaignBoard.All();
            for (int seed = 1; seed <= 100; seed++)
            {
                for (int d = 0; d < ids.Length; d++)
                {
                    var map = RoadGraph.Build(seed, ids[d].Id);
                    Assert.IsTrue(RoadGraph.Navigable(map), ids[d].Id + " " + seed);
                }
            }
        }

        [Test]
        public void ADodgeSpendsStaminaAndIgnoresTheOpeningOfTheRoll()
        {
            Assert.IsTrue(DodgeClock.Ready(22f, 0.85f));
            Assert.IsFalse(DodgeClock.Ready(21.9f, 1f));
            Assert.IsFalse(DodgeClock.Ready(22f, 0.84f));
            DodgeClock.Direction(0f, 0f, 0f, 1f, out float faceX, out float faceZ);
            Assert.AreEqual(0f, faceX, 0.001f);
            Assert.AreEqual(1f, faceZ, 0.001f);
            DodgeClock.Direction(1f, 0f, 0f, 1f, out float moveX, out float moveZ);
            Assert.AreEqual(1f, moveX, 0.001f);
            Assert.AreEqual(0f, moveZ, 0.001f);
            DodgeClock.Direction(0f, 0f, 0f, 0f, out float fallX, out float fallZ);
            Assert.AreEqual(0f, fallX, 0.001f);
            Assert.AreEqual(1f, fallZ, 0.001f);
            Assert.IsTrue(DodgeClock.Untouchable(0f));
            Assert.IsTrue(DodgeClock.Untouchable(0.21f));
            Assert.IsFalse(DodgeClock.Untouchable(0.22f));
            Assert.IsFalse(DodgeClock.Untouchable(-1f));

            PadBindings.ResetDefaults();
            try
            {
                Assert.AreEqual("RightShoulder", PadBindings.Label(PadBindings.Action.Dodge));
                string[] parts = PadBindings.Pack().Split(',');
                Assert.AreEqual(17, parts.Length);
                PadBindings.Unpack(string.Join(",", parts, 0, 16));
                Assert.AreEqual("RightShoulder", PadBindings.Label(PadBindings.Action.Dodge));
                Assert.AreEqual("South", PadBindings.Label(PadBindings.Action.Interact));
            }
            finally
            {
                PadBindings.ResetDefaults();
            }
        }

        private static bool HasEdge(RoadGraph.Edge[] edges, float x, float z, string kind)
        {
            if (edges == null) return false;
            for (int i = 0; i < edges.Length; i++)
            {
                float dx = edges[i].X - x;
                float dz = edges[i].Z - z;
                if (dx < 0f) dx = -dx;
                if (dz < 0f) dz = -dz;
                if (dx < 0.02f && dz < 0.02f && edges[i].Kind == kind) return true;
            }
            return false;
        }
    }
}
