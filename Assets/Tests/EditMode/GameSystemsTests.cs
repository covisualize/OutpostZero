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
                    new ModuleSave { kind = "Barricade", x = 2f, z = -4f, rotation = 0 }
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
    }
}
