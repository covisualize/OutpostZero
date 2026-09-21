using NUnit.Framework;
using OutpostZero.AI;
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
    }
}
