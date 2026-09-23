using System.Collections.Generic;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Items;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class CraftingRecipeTests
    {
        private static readonly HashSet<string> Fitted = new HashSet<string>
        {
            "suppressor", "optic", "extended_mag", "rail", "repair_kit", "barricade_kit", "radio_spare",
            CraftingBench.CampFood, CraftingBench.CampWater
        };

        [Test]
        public void EveryRecipeHasABillAndARealOutput()
        {
            Assert.GreaterOrEqual(CraftingBench.Recipes.Length, 20);
            var ids = new HashSet<string>();
            foreach (var recipe in CraftingBench.Recipes)
            {
                Assert.IsTrue(ids.Add(recipe.Id), recipe.Id + " is listed twice");
                Assert.IsTrue(CraftBill.TryOf(recipe.Id, out var bill), recipe.Id + " has no bill");
                Assert.AreEqual(recipe.ScrapCost, bill.Scrap, recipe.Id);
                Assert.Greater(recipe.OutputCount, 0, recipe.Id);
                Assert.Greater(bill.Scrap + bill.Cloth + bill.Chemicals + bill.Tape + bill.Raw, 0, recipe.Id + " is free");
                if (!Fitted.Contains(recipe.OutputId)) Assert.IsNotNull(ItemCatalog.Find(recipe.OutputId), recipe.Id + " makes an unknown item");
                Assert.AreNotEqual("recipe." + recipe.Id, Loc.T("recipe." + recipe.Id, "en"), recipe.Id + " has no English name");
                Assert.AreNotEqual("recipe." + recipe.Id, Loc.T("recipe." + recipe.Id, "es"), recipe.Id + " has no Spanish name");
            }
        }

        [Test]
        public void DismantlingNeverPaysBackMoreThanTheCheapestCraft()
        {
            foreach (var recipe in CraftingBench.Recipes)
            {
                if (!CraftBill.Dismantle(recipe.OutputId, out int scrap, out int cloth, out int chemicals, out int tape)) continue;
                Assert.IsTrue(CraftBill.TryOf(recipe.OutputId, out var bill));
                int cheapest = CraftingBench.Priced(bill.Scrap, true, 2);
                Assert.LessOrEqual(scrap, cheapest, recipe.OutputId);
                Assert.LessOrEqual(cloth, bill.Cloth, recipe.OutputId);
                Assert.LessOrEqual(chemicals, bill.Chemicals, recipe.OutputId);
                Assert.LessOrEqual(tape, bill.Tape, recipe.OutputId);
            }
        }

        [Test]
        public void DismantleYieldsHalfTheBillForCraftedGear()
        {
            Assert.IsTrue(CraftBill.Dismantle("pipe_bomb", out int scrap, out int cloth, out int chemicals, out int tape));
            Assert.AreEqual(4, scrap);
            Assert.AreEqual(0, chemicals);
            Assert.IsTrue(CraftBill.Dismantle("bandage", out scrap, out cloth, out chemicals, out tape));
            Assert.AreEqual(0, scrap);
            Assert.AreEqual(1, cloth);
            Assert.IsFalse(CraftBill.Dismantle("ammo_9mm", out _, out _, out _, out _));
            Assert.IsFalse(CraftBill.Dismantle("canned_food", out _, out _, out _, out _));
            Assert.IsFalse(CraftBill.Dismantle("scrap", out _, out _, out _, out _));
            Assert.IsFalse(CraftBill.Dismantle("bottle", out _, out _, out _, out _));
            Assert.IsFalse(CraftBill.Dismantle(null, out _, out _, out _, out _));
            Assert.Greater(CraftBill.Value("radio_spare"), CraftBill.Value("bandage"));
            Assert.AreEqual(0, CraftBill.Value("canned_food"));
        }

        [Test]
        public void TheCampfireCooksRawFoodAndBoilsWater()
        {
            Assert.IsTrue(CraftBill.TryOf("cooked_meal", out var meal));
            Assert.AreEqual(CraftBill.Campfire, meal.Station);
            Assert.AreEqual(2, meal.Raw);
            Assert.IsTrue(CraftBill.TryOf("purified_water", out var water));
            Assert.AreEqual(1, water.Chemicals);
            Assert.IsFalse(CraftBill.StationReady(CraftBill.Campfire, true, true, false));
            Assert.IsTrue(CraftBill.StationReady(CraftBill.Campfire, false, false, true));
            Assert.IsTrue(CraftBill.StationReady(CraftBill.Workbench, true, false));
            Assert.AreEqual("Need a campfire", CraftBill.Block(CraftBill.Campfire, "", false, true));
            Assert.AreEqual("Hace falta una hoguera", StallVoice.Block("Need a campfire", "es"));
            Assert.AreEqual(1, CraftGate.TierOf("cooked_meal"));
        }

        [Test]
        public void AmmoIsCraftableForARealMaterialCostAndTierTwoNeedsTheBase()
        {
            foreach (var id in new[] { "ammo_9mm", "ammo_shells", "ammo_rifle", "ammo_smg" })
            {
                Assert.IsTrue(CraftBill.TryOf(id, out var bill), id);
                Assert.AreEqual(CraftBill.Workbench, bill.Station, id);
                Assert.GreaterOrEqual(bill.Chemicals, 1, id);
                Assert.Greater(CraftingBench.Priced(bill.Scrap, true, 2), 0, id);
            }
            foreach (var id in new[] { "flare", "repair_kit", "barricade_kit", "radio_spare" })
            {
                Assert.AreEqual(2, CraftGate.TierOf(id), id);
                Assert.IsFalse(CraftGate.Open(id, 1, CraftGate.PrintOf(id)), id);
            }
        }

        [Test]
        public void DismantledPartsNeedRoomInTheStores()
        {
            Assert.IsTrue(CraftBill.Fits(70, 80, 4, 0, 0, 0));
            Assert.IsFalse(CraftBill.Fits(78, 80, 0, 1, 0, 0));
            Assert.IsTrue(CraftBill.Fits(0, 80, 0, 0, 0, 0));
        }
    }
}
