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
                language = "es",
                districtIndex = 2,
                survivors = new[]
                {
                    new SurvivorSave { id = "mara", displayName = "Mara Quill", alive = false, leader = false, morale = 10f, task = "Fallen" }
                },
                modules = new[]
                {
                    new ModuleSave { kind = "Barricade", x = 2f, z = -4f, rotation = 0, age = 3 }
                }
            };

            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(data), out var loaded, out var error), error);
            Assert.AreEqual(1, loaded.schemaVersion);
            Assert.AreEqual(4, loaded.day);
            Assert.AreEqual(22, loaded.colonyScrap);
            Assert.AreEqual("es", loaded.language);
            Assert.AreEqual(2, loaded.districtIndex);
            Assert.AreEqual("mara", loaded.survivors[0].id);
            Assert.IsFalse(loaded.survivors[0].alive);
            Assert.AreEqual("Barricade", loaded.modules[0].kind);
            Assert.AreEqual(100, loaded.modules[0].integrity);
            Assert.AreEqual(3, loaded.modules[0].age);
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
            Assert.AreEqual(2, grants.Length);
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
        public void RainWetsTheGroundAndShotsSitInTheWorld()
        {
            Assert.AreEqual(0.65f, WeatherSurface.Wetness(WeatherKind.Rain));
            Assert.AreEqual(0.2f, WeatherSurface.Wetness(WeatherKind.Fog));
            Assert.AreEqual(0f, WeatherSurface.Wetness(WeatherKind.Clear));
            Assert.AreEqual(0.62f, WeatherSurface.Sight(WeatherKind.Fog));
            Assert.AreEqual(0f, AudioSpace.SpatialBlend("pulse"));
            Assert.AreEqual(1f, AudioSpace.SpatialBlend("gun"));
            Assert.AreEqual(0.35f, AudioSpace.SpatialBlend("step"));
            Assert.Greater(AudioSpace.MaxDistance("boom"), AudioSpace.MaxDistance("hit"));
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
            Assert.AreEqual("[Zombie scream, norte]", Presentation.Caption(NoiseType.ZombieScream, 0f, 4f, "es"));
            Assert.AreEqual("[Gunshot, west]", Presentation.Caption(NoiseType.GunshotLoud, -5f, 0f, "en"));
            Assert.AreEqual("[Explosion, here]", Presentation.Caption(NoiseType.Explosion, 0f, 0f, "en"));
            Assert.AreEqual("", Presentation.Caption(NoiseType.WalkFootstep, 1f, 0f, "en"));
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
        public void HungerSlowsRecoveryAndThirstShrinksStamina()
        {
            Assert.AreEqual(55f, NeedsPressure.HungerPerSecond * 600f, 0.01f);
            Assert.AreEqual(50f, NeedsPressure.ThirstPerSecond * 600f, 0.01f);
            Assert.AreEqual(55f, NeedsPressure.FatiguePerSecond * 600f, 0.01f);
            Assert.AreEqual(0.85f, NeedsPressure.Regen(24f), 0.001f);
            Assert.AreEqual(1f, NeedsPressure.Regen(25f), 0.001f);
            Assert.AreEqual(80f, NeedsPressure.StaminaCap(24f, 100f), 0.01f);
            Assert.AreEqual(100f, NeedsPressure.StaminaCap(25f, 100f), 0.01f);
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
