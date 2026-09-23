using System;
using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Colony
{
    public class Recipe
    {
        public string Id;
        public string Label;
        public int ScrapCost;
        public string OutputId;
        public int OutputCount;
    }

    public class CraftingBench : MonoBehaviour
    {
        public static CraftingBench Instance { get; private set; }

        public const string CampFood = "camp_food";
        public const string CampWater = "camp_water";

        private readonly List<string> orders = new List<string>();
        public IReadOnlyList<string> Orders => orders;
        public string PackedOrders => CraftQueue.Pack(orders);

        /// <summary>The recipe book's list once loaded, otherwise the built-in one.</summary>
        public static Recipe[] Recipes => RecipeTable.Active ?? BuiltIn;

        public static readonly Recipe[] BuiltIn =
        {
            new Recipe { Id = "bandage", Label = "Bandage", ScrapCost = 1, OutputId = "bandage", OutputCount = 1 },
            new Recipe { Id = "medkit", Label = "Medkit", ScrapCost = 8, OutputId = "medkit", OutputCount = 1 },
            new Recipe { Id = "antibiotics", Label = "Antibiotics", ScrapCost = 10, OutputId = "antibiotics", OutputCount = 1 },
            new Recipe { Id = "painkillers", Label = "Painkillers", ScrapCost = 5, OutputId = "painkillers", OutputCount = 1 },
            new Recipe { Id = "ammo_9mm", Label = "9mm (12)", ScrapCost = 4, OutputId = "ammo_9mm", OutputCount = 1 },
            new Recipe { Id = "ammo_shells", Label = "Shells (6)", ScrapCost = 5, OutputId = "ammo_shells", OutputCount = 1 },
            new Recipe { Id = "ammo_rifle", Label = "Rifle mag", ScrapCost = 7, OutputId = "ammo_rifle", OutputCount = 1 },
            new Recipe { Id = "ammo_smg", Label = "SMG mag", ScrapCost = 6, OutputId = "ammo_smg", OutputCount = 1 },
            new Recipe { Id = "noise_lure", Label = "Noise lure", ScrapCost = 2, OutputId = "noise_lure", OutputCount = 1 },
            new Recipe { Id = "molotov", Label = "Molotov", ScrapCost = 6, OutputId = "molotov", OutputCount = 1 },
            new Recipe { Id = "pipe_bomb", Label = "Pipe Bomb", ScrapCost = 8, OutputId = "pipe_bomb", OutputCount = 1 },
            new Recipe { Id = "suppressor", Label = "Suppressor", ScrapCost = 8, OutputId = "suppressor", OutputCount = 1 },
            new Recipe { Id = "rail", Label = "Flashlight rail", ScrapCost = 5, OutputId = "rail", OutputCount = 1 },
            new Recipe { Id = "optic", Label = "Optic", ScrapCost = 9, OutputId = "optic", OutputCount = 1 },
            new Recipe { Id = "extended_mag", Label = "Extended mag", ScrapCost = 8, OutputId = "extended_mag", OutputCount = 1 },
            new Recipe { Id = "dressing", Label = "Field dressings", ScrapCost = 2, OutputId = "bandage", OutputCount = 3 },
            new Recipe { Id = "flare", Label = "Flare", ScrapCost = 4, OutputId = "flare", OutputCount = 1 },
            new Recipe { Id = "repair_kit", Label = "Generator repair", ScrapCost = 6, OutputId = "repair_kit", OutputCount = 1 },
            new Recipe { Id = "barricade_kit", Label = "Reinforced wall", ScrapCost = 8, OutputId = "barricade_kit", OutputCount = 1 },
            new Recipe { Id = "radio_spare", Label = "Radio spare", ScrapCost = 12, OutputId = "radio_spare", OutputCount = 1 },
            new Recipe { Id = "cell", Label = "Lamp cell", ScrapCost = 3, OutputId = "cell", OutputCount = 1 },
            new Recipe { Id = "cooked_meal", Label = "Cooked meal", ScrapCost = 1, OutputId = CampFood, OutputCount = 3 },
            new Recipe { Id = "purified_water", Label = "Purified water", ScrapCost = 1, OutputId = CampWater, OutputCount = 3 },
            new Recipe { Id = "bottle", Label = "Bottle", ScrapCost = 1, OutputId = "street_bottle", OutputCount = 1 }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            RecipeBook.Ensure();
        }

        public bool Craft(string recipeId)
        {
            if (!TrySpend(recipeId, out var recipe, out int due, out var bill)) return false;
            return Deliver(recipe, due, bill, true);
        }

        public bool Order(string recipeId)
        {
            if (!CraftQueue.CanAdd(orders, recipeId))
            {
                GameplayFeedback.Toast(Loc.T(CraftQueue.Orderable(recipeId) ? "craft.queue_full" : "craft.no_queue"));
                return false;
            }
            if (!TrySpend(recipeId, out var recipe, out _, out _)) return false;
            orders.Add(recipe.Id);
            GameplayFeedback.Toast(Loc.T("craft.queued") + " " + Loc.Recipe(recipe.Id, recipe.Label));
            return true;
        }

        /// <summary>A Craft shift finishes up to <paramref name="hands"/> orders. An order that can't be delivered waits.</summary>
        public int WorkOrders(int hands)
        {
            int made = 0;
            while (hands > 0 && orders.Count > 0)
            {
                var recipe = Find(orders[0]);
                if (recipe == null || !CraftBill.TryOf(recipe.Id, out var bill))
                {
                    orders.RemoveAt(0);
                    continue;
                }
                if (!Deliver(recipe, 0, bill, false)) break;
                orders.RemoveAt(0);
                made++;
                hands--;
            }
            return made;
        }

        public void SetOrders(string packed)
        {
            orders.Clear();
            orders.AddRange(CraftQueue.Unpack(packed));
        }

        private static Recipe Find(string recipeId)
        {
            foreach (var candidate in Recipes)
            {
                if (candidate.Id == recipeId) return candidate;
            }
            return null;
        }

        private bool TrySpend(string recipeId, out Recipe recipe, out int due, out CraftBill.Cost bill)
        {
            due = 0;
            bill = default;
            recipe = null;
            foreach (var candidate in Recipes)
            {
                if (candidate.Id == recipeId) recipe = candidate;
            }
            if (recipe == null || !CraftBill.TryOf(recipeId, out bill)) return false;
            var storage = ColonyStorage.Instance;
            if (storage == null) return false;

            bool workbench = GridBuilder.Instance != null && GridBuilder.Instance.HasKind("Workbench");
            bool cot = GridBuilder.Instance != null && GridBuilder.Instance.HasKind("Cot");
            bool fire = GridBuilder.Instance != null && GridBuilder.Instance.HasKind("Campfire");
            int tier = GridBuilder.Instance != null ? GridBuilder.Instance.BenchTier() : 1;
            string deny = CraftGate.Deny(recipeId, tier, storage.Prints);
            if (deny == "tier")
            {
                GameplayFeedback.Toast(Loc.T("gate.tier"));
                return false;
            }
            if (deny == "print")
            {
                GameplayFeedback.Toast(Loc.T("gate.print"));
                return false;
            }
            due = Priced(bill.Scrap, workbench, tier);
            string block = CraftBill.Block(bill.Station, bill.Skill, CraftBill.StationReady(bill.Station, workbench, cot, fire), SkillReady(bill.Skill));
            if (!string.IsNullOrEmpty(block))
            {
                GameplayFeedback.Toast(StallVoice.Block(block, null));
                return false;
            }
            if (!CraftBill.Knows(bill.Know, bill.Level, BestSkill(bill.Know)))
            {
                GameplayFeedback.Toast(CraftSay.Know(bill.Know, bill.Level, null));
                return false;
            }
            if (storage.Raw < bill.Raw || !storage.TrySpendBill(due, bill.Cloth, bill.Chemicals, bill.Tape))
            {
                GameplayFeedback.Toast(Loc.T("stall.short"));
                return false;
            }
            if (recipe.Id == "bandage") CodexDirector.Hear("craft_bandage");
            if (bill.Raw > 0) storage.TakeRaw(bill.Raw);
            return true;
        }

        private bool Deliver(Recipe recipe, int due, CraftBill.Cost bill, bool refund)
        {
            var storage = ColonyStorage.Instance;
            if (storage == null) return false;

            if (recipe.OutputId == CampFood || recipe.OutputId == CampWater)
            {
                int kept = recipe.OutputId == CampFood ? storage.AddFood(recipe.OutputCount) : storage.AddWater(recipe.OutputCount);
                if (kept <= 0)
                {
                    if (refund) Refund(due, bill);
                    GameplayFeedback.Toast(Loc.T("camp.strip_full"));
                    return false;
                }
                GameplayFeedback.Toast(PackSay.Made(recipe.Id, recipe.Label, null));
                return true;
            }

            if (recipe.Id == "repair_kit")
            {
                if (GridBuilder.Instance == null || !GridBuilder.Instance.RepairGenerator())
                {
                    if (refund) Refund(due, bill);
                    GameplayFeedback.Toast(Loc.T("gate.gen"));
                    return false;
                }
                GameplayFeedback.Toast(CraftSay.Fitted(recipe.Id, recipe.Label, null));
                return true;
            }
            if (recipe.Id == "barricade_kit")
            {
                if (GridBuilder.Instance == null || !GridBuilder.Instance.BraceWall())
                {
                    if (refund) Refund(due, bill);
                    GameplayFeedback.Toast(Loc.T("gate.wall"));
                    return false;
                }
                GameplayFeedback.Toast(CraftSay.Fitted(recipe.Id, recipe.Label, null));
                return true;
            }
            if (recipe.Id == "radio_spare")
            {
                var map = WorldMapService.Instance;
                if (map == null || CampaignBoard.PartsComplete(map.Parts))
                {
                    if (refund) Refund(due, bill);
                    GameplayFeedback.Toast(Loc.T("camp.radio_full"));
                    return false;
                }
                map.GrantSpare();
                GameplayFeedback.Toast(CraftSay.Fitted(recipe.Id, recipe.Label, null));
                return true;
            }

            if (recipe.OutputId == "suppressor" || recipe.OutputId == "optic" || recipe.OutputId == "extended_mag" || recipe.OutputId == "rail")
            {
                var player = PlayerRegistry.Current;
                var weapon = player != null ? player.ActiveWeapon : null;
                if (weapon == null)
                {
                    if (refund) Refund(due, bill);
                    return false;
                }
                var mod = Attach.Ensure<WeaponMod>(weapon.gameObject);
                mod.Apply(recipe.OutputId);
                GameplayFeedback.Toast(CraftSay.Fitted(recipe.Id, recipe.Label, null));
                return true;
            }

            var record = ItemCatalog.Find(recipe.OutputId);
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            if (record == null || inventory == null)
            {
                if (refund) Refund(due, bill);
                return false;
            }

            int stock = 0;
            if (record.Use == ItemUse.Ammo)
            {
                inventory.GrantAmmoPublic(record.AmmoType, record.AmmoAmount * recipe.OutputCount);
                stock = AmmoPress.Rounds(recipe.Id, recipe.OutputCount);
                if (stock > 0) storage.AddRounds(stock);
            }
            else if (!inventory.TryAddItem(record.Id, record.DisplayName, record.Category, recipe.OutputCount, record.Weight))
            {
                if (refund) Refund(due, bill);
                GameplayFeedback.Toast(PackSay.Pack(null));
                return false;
            }

            GameplayFeedback.Toast(stock > 0 ? Loc.T("camp.press") + " " + stock : PackSay.Made(recipe.Id, recipe.Label, null));
            return true;
        }

        private static bool SkillReady(string skill)
        {
            if (string.IsNullOrEmpty(skill)) return true;
            var roster = SurvivorRoster.Instance;
            if (roster == null) return false;
            foreach (var person in roster.Survivors)
            {
                if (CraftBill.OnDuty(person.trait, person.task, person.alive, skill)) return true;
            }
            return false;
        }

        /// <summary>The highest level of a skill among survivors still in camp.</summary>
        public static int BestSkill(string know)
        {
            var roster = SurvivorRoster.Instance;
            if (roster == null || string.IsNullOrEmpty(know)) return 0;
            int best = 0;
            foreach (var person in roster.Survivors)
            {
                if (person == null || !person.alive || person.task == "Left") continue;
                int level = CraftBill.SkillFor(know, person.medicine, person.engineering, person.cooking);
                if (level > best) best = level;
            }
            return best;
        }

        private static void Refund(int scrap, CraftBill.Cost bill)
        {
            var storage = ColonyStorage.Instance;
            if (storage == null) return;
            if (scrap > 0) storage.RestoreScrap(scrap);
            if (bill.Cloth > 0) storage.RestoreCloth(bill.Cloth);
            if (bill.Chemicals > 0) storage.RestoreChemicals(bill.Chemicals);
            if (bill.Tape > 0) storage.RestoreTape(bill.Tape);
            if (bill.Raw > 0) storage.AddRaw(bill.Raw);
        }

        public bool Dismantle(string itemId)
        {
            var storage = ColonyStorage.Instance;
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            if (storage == null || inventory == null) return false;
            if (GridBuilder.Instance == null || !GridBuilder.Instance.HasKind("Workbench"))
            {
                GameplayFeedback.Toast(Loc.T("gate.bench"));
                return false;
            }
            if (!CraftBill.Dismantle(itemId, out int scrap, out int cloth, out int chemicals, out int tape))
            {
                GameplayFeedback.Toast(Loc.T("camp.strip_none"));
                return false;
            }
            if (!CraftBill.Fits(storage.Used, storage.Room, scrap, cloth, chemicals, tape))
            {
                GameplayFeedback.Toast(Loc.T("camp.strip_full"));
                return false;
            }
            if (!inventory.TryConsume(itemId))
            {
                GameplayFeedback.Toast(Loc.T("camp.strip_none"));
                return false;
            }
            if (scrap > 0) storage.AddScrap(scrap);
            if (cloth > 0) storage.AddCloth(cloth);
            if (chemicals > 0) storage.AddChemicals(chemicals);
            if (tape > 0) storage.AddTape(tape);
            GameplayFeedback.Toast(Loc.T("camp.strip_ok"));
            return true;
        }

        public static int Priced(int scrap, bool workbench) => CraftBill.ScrapDue(scrap, workbench);

        public static int Priced(int scrap, bool workbench, int tier)
        {
            int due = CraftBill.ScrapDue(scrap, workbench);
            if (tier >= 2 && due > 1) due -= 1;
            return due;
        }
    }
}
