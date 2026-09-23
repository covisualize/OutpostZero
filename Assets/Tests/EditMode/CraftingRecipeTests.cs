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
        public void NoRecipeMakesAMaterialSoNoCraftCanFeedItself()
        {
            var materials = new[] { "scrap", "cloth", "chemicals", "chemical", "tape", "duct_tape", "raw", "raw_food" };
            foreach (var recipe in CraftingBench.Recipes)
            {
                CollectionAssert.DoesNotContain(materials, recipe.Id, recipe.Id + " is a bill material");
                CollectionAssert.DoesNotContain(materials, recipe.OutputId, recipe.Id + " makes a bill material and could loop");
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
        public void AdvancedRecipesWaitOnASkilledSurvivorInCamp()
        {
            Assert.IsTrue(CraftBill.TryOf("suppressor", out var suppressor));
            Assert.AreEqual("Build", suppressor.Know);
            Assert.AreEqual(4, suppressor.Level);
            Assert.IsTrue(CraftBill.TryOf("antibiotics", out var antibiotics));
            Assert.AreEqual("Medic", antibiotics.Know);
            Assert.AreEqual(5, antibiotics.Level);
            foreach (var id in new[] { "bandage", "ammo_9mm", "ammo_shells", "ammo_rifle", "ammo_smg", "cooked_meal", "purified_water", "bottle", "molotov" })
            {
                Assert.IsTrue(CraftBill.TryOf(id, out var open), id);
                Assert.AreEqual(0, open.Level, id + " must stay open to a fresh camp");
            }
            foreach (var recipe in CraftingBench.Recipes)
            {
                Assert.IsTrue(CraftBill.TryOf(recipe.Id, out var bill), recipe.Id);
                if (bill.Level <= 0) continue;
                CollectionAssert.Contains(new[] { "Build", "Medic", "Cook" }, bill.Know, recipe.Id);
                Assert.LessOrEqual(bill.Level, Practice.Cap, recipe.Id + " can never be learned");
            }

            Assert.AreEqual(3, CraftBill.SkillFor("Build", 1, 3, 2));
            Assert.AreEqual(1, CraftBill.SkillFor("Medic", 1, 3, 2));
            Assert.AreEqual(2, CraftBill.SkillFor("Cook", 1, 3, 2));
            Assert.IsFalse(CraftBill.Knows("Build", 4, 3));
            Assert.IsTrue(CraftBill.Knows("Build", 4, 4));
            Assert.IsTrue(CraftBill.Knows("", 0, 0));

            Assert.AreEqual("Need Build 4 in camp", CraftSay.Know("Build", 4, "en"));
            StringAssert.Contains("5", CraftSay.Know("Medic", 5, "es"));
            StringAssert.Contains("campamento", CraftSay.Know("Medic", 5, "es"));
            string root = System.IO.Directory.GetCurrentDirectory();
            StringAssert.Contains("CraftBill.Knows(bill.Know, bill.Level, BestSkill(bill.Know))", System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets/Scripts/Colony/CraftingBench.cs")));
            StringAssert.Contains("Loc.Task(bill.Know) + \" \" + bill.Level", System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets/Scripts/UI/OutpostInterface.cs")));
        }

        [Test]
        public void DismantledPartsNeedRoomInTheStores()
        {
            Assert.IsTrue(CraftBill.Fits(70, 80, 4, 0, 0, 0));
            Assert.IsFalse(CraftBill.Fits(78, 80, 0, 1, 0, 0));
            Assert.IsTrue(CraftBill.Fits(0, 80, 0, 0, 0, 0));
        }

        [Test]
        public void TheIssueBillsHoldForTheSuppressorAntibioticsAndFieldPack()
        {
            Assert.IsTrue(CraftBill.CodeOf("suppressor", out var suppressor));
            Assert.AreEqual(8, suppressor.Scrap);
            Assert.AreEqual(2, suppressor.Tape);
            Assert.AreEqual("Build", suppressor.Know);
            Assert.AreEqual(4, suppressor.Level);

            Assert.IsTrue(CraftBill.CodeOf("antibiotics", out var antibiotics));
            Assert.AreEqual(3, antibiotics.Chemicals);
            Assert.AreEqual(5, antibiotics.Level);
            Assert.AreEqual(2, CraftGate.CodeTier("antibiotics"), "antibiotics wait on the raised bench");
            Assert.AreEqual("tier", CraftGate.Deny("antibiotics", 1, ""));

            Assert.AreEqual(6, PackOps.RaiseCloth);
            Assert.AreEqual(3, PackOps.RaiseTape);
            Assert.AreEqual(0, PackOps.RaiseScrap);
        }
    }
}
