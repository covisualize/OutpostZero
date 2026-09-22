using System;
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

        public static readonly Recipe[] Recipes =
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
            new Recipe { Id = "suppressor", Label = "Suppressor", ScrapCost = 12, OutputId = "suppressor", OutputCount = 1 },
            new Recipe { Id = "optic", Label = "Optic", ScrapCost = 9, OutputId = "optic", OutputCount = 1 },
            new Recipe { Id = "extended_mag", Label = "Extended mag", ScrapCost = 8, OutputId = "extended_mag", OutputCount = 1 },
            new Recipe { Id = "dressing", Label = "Field dressings", ScrapCost = 2, OutputId = "bandage", OutputCount = 3 },
            new Recipe { Id = "flare", Label = "Flare", ScrapCost = 4, OutputId = "flare", OutputCount = 1 },
            new Recipe { Id = "repair_kit", Label = "Generator repair", ScrapCost = 6, OutputId = "repair_kit", OutputCount = 1 },
            new Recipe { Id = "barricade_kit", Label = "Reinforced wall", ScrapCost = 8, OutputId = "barricade_kit", OutputCount = 1 },
            new Recipe { Id = "radio_spare", Label = "Radio spare", ScrapCost = 12, OutputId = "radio_spare", OutputCount = 1 },
            new Recipe { Id = "cell", Label = "Lamp cell", ScrapCost = 3, OutputId = "cell", OutputCount = 1 }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public bool Craft(string recipeId)
        {
            Recipe recipe = null;
            foreach (var candidate in Recipes)
            {
                if (candidate.Id == recipeId) recipe = candidate;
            }
            if (recipe == null || !CraftBill.TryOf(recipeId, out var bill)) return false;
            var storage = ColonyStorage.Instance;
            if (storage == null) return false;

            bool workbench = GridBuilder.Instance != null && GridBuilder.Instance.HasKind("Workbench");
            bool cot = GridBuilder.Instance != null && GridBuilder.Instance.HasKind("Cot");
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
            int due = Priced(bill.Scrap, workbench, tier);
            string block = CraftBill.Block(bill.Station, bill.Skill, CraftBill.StationReady(bill.Station, workbench, cot), SkillReady(bill.Skill));
            if (!string.IsNullOrEmpty(block))
            {
                GameplayFeedback.Toast(block);
                return false;
            }
            if (!storage.TrySpendBill(due, bill.Cloth, bill.Chemicals, bill.Tape))
            {
                GameplayFeedback.Toast("Not enough camp supplies");
                return false;
            }

            if (recipe.Id == "repair_kit")
            {
                if (GridBuilder.Instance == null || !GridBuilder.Instance.RepairGenerator())
                {
                    Refund(due, bill);
                    GameplayFeedback.Toast(Loc.T("gate.gen"));
                    return false;
                }
                GameplayFeedback.Toast(recipe.Label + " fitted");
                return true;
            }
            if (recipe.Id == "barricade_kit")
            {
                if (GridBuilder.Instance == null || !GridBuilder.Instance.BraceWall())
                {
                    Refund(due, bill);
                    GameplayFeedback.Toast(Loc.T("gate.wall"));
                    return false;
                }
                GameplayFeedback.Toast(recipe.Label + " fitted");
                return true;
            }
            if (recipe.Id == "radio_spare")
            {
                var map = WorldMapService.Instance;
                if (map == null || CampaignBoard.PartsComplete(map.Parts))
                {
                    Refund(due, bill);
                    GameplayFeedback.Toast(Loc.T("camp.radio_full"));
                    return false;
                }
                map.GrantSpare();
                GameplayFeedback.Toast(recipe.Label + " fitted");
                return true;
            }

            if (recipe.OutputId == "suppressor" || recipe.OutputId == "optic" || recipe.OutputId == "extended_mag")
            {
                var player = PlayerRegistry.Current;
                var weapon = player != null ? player.ActiveWeapon : null;
                if (weapon == null)
                {
                    Refund(due, bill);
                    return false;
                }
                var mod = weapon.GetComponent<WeaponMod>() ?? weapon.gameObject.AddComponent<WeaponMod>();
                mod.Apply(recipe.OutputId);
                GameplayFeedback.Toast(recipe.Label + " fitted");
                return true;
            }

            var record = ItemCatalog.Find(recipe.OutputId);
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            if (record == null || inventory == null)
            {
                Refund(due, bill);
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
                Refund(due, bill);
                GameplayFeedback.Toast("Pack is full");
                return false;
            }

            GameplayFeedback.Toast(stock > 0 ? Loc.T("camp.press") + " " + stock : "Crafted " + recipe.Label);
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

        private static void Refund(int scrap, CraftBill.Cost bill)
        {
            var storage = ColonyStorage.Instance;
            if (storage == null) return;
            if (scrap > 0) storage.RestoreScrap(scrap);
            if (bill.Cloth > 0) storage.RestoreCloth(bill.Cloth);
            if (bill.Chemicals > 0) storage.RestoreChemicals(bill.Chemicals);
            if (bill.Tape > 0) storage.RestoreTape(bill.Tape);
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
