using System;
using UnityEngine;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;

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
            new Recipe { Id = "noise_lure", Label = "Noise lure", ScrapCost = 2, OutputId = "noise_lure", OutputCount = 1 },
            new Recipe { Id = "molotov", Label = "Molotov", ScrapCost = 6, OutputId = "molotov", OutputCount = 1 },
            new Recipe { Id = "pipe_bomb", Label = "Pipe Bomb", ScrapCost = 8, OutputId = "pipe_bomb", OutputCount = 1 },
            new Recipe { Id = "suppressor", Label = "Suppressor", ScrapCost = 12, OutputId = "suppressor", OutputCount = 1 },
            new Recipe { Id = "optic", Label = "Optic", ScrapCost = 9, OutputId = "optic", OutputCount = 1 },
            new Recipe { Id = "extended_mag", Label = "Extended mag", ScrapCost = 8, OutputId = "extended_mag", OutputCount = 1 }
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
            int due = Priced(bill.Scrap, workbench);
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

            if (record.Use == ItemUse.Ammo)
            {
                inventory.GrantAmmoPublic(record.AmmoType, record.AmmoAmount * recipe.OutputCount);
            }
            else if (!inventory.TryAddItem(record.Id, record.DisplayName, record.Category, recipe.OutputCount, record.Weight))
            {
                Refund(due, bill);
                GameplayFeedback.Toast("Pack is full");
                return false;
            }

            GameplayFeedback.Toast("Crafted " + recipe.Label);
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
            if (scrap > 0) storage.AddScrap(scrap);
            if (bill.Cloth > 0) storage.AddCloth(bill.Cloth);
            if (bill.Chemicals > 0) storage.AddChemicals(bill.Chemicals);
            if (bill.Tape > 0) storage.AddTape(bill.Tape);
        }

        public static int Priced(int scrap, bool workbench) => CraftBill.ScrapDue(scrap, workbench);
    }
}
