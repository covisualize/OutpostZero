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
            new Recipe { Id = "bandage", Label = "Bandage", ScrapCost = 3, OutputId = "bandage", OutputCount = 1 },
            new Recipe { Id = "medkit", Label = "Medkit", ScrapCost = 8, OutputId = "medkit", OutputCount = 1 },
            new Recipe { Id = "ammo_9mm", Label = "9mm (12)", ScrapCost = 4, OutputId = "ammo_9mm", OutputCount = 1 },
            new Recipe { Id = "ammo_shells", Label = "Shells (6)", ScrapCost = 5, OutputId = "ammo_shells", OutputCount = 1 },
            new Recipe { Id = "ammo_rifle", Label = "Rifle mag", ScrapCost = 7, OutputId = "ammo_rifle", OutputCount = 1 },
            new Recipe { Id = "noise_lure", Label = "Noise lure", ScrapCost = 2, OutputId = "noise_lure", OutputCount = 1 },
            new Recipe { Id = "molotov", Label = "Molotov", ScrapCost = 6, OutputId = "molotov", OutputCount = 1 },
            new Recipe { Id = "suppressor", Label = "Suppressor", ScrapCost = 12, OutputId = "suppressor", OutputCount = 1 }
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
            if (recipe == null) return false;
            if (ColonyStorage.Instance == null || !ColonyStorage.Instance.TrySpendScrap(recipe.ScrapCost))
            {
                GameplayFeedback.Toast("Not enough camp scrap");
                return false;
            }

            if (recipe.OutputId == "suppressor")
            {
                var player = PlayerRegistry.Current;
                var weapon = player != null ? player.ActiveWeapon : null;
                if (weapon == null)
                {
                    ColonyStorage.Instance.AddScrap(recipe.ScrapCost);
                    return false;
                }
                var mod = weapon.GetComponent<WeaponMod>() ?? weapon.gameObject.AddComponent<WeaponMod>();
                mod.ApplySuppressor();
                GameplayFeedback.Toast("Suppressor fitted");
                return true;
            }

            var record = ItemCatalog.Find(recipe.OutputId);
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            if (record == null || inventory == null)
            {
                ColonyStorage.Instance.AddScrap(recipe.ScrapCost);
                return false;
            }

            if (record.Use == ItemUse.Ammo)
            {
                inventory.GrantAmmoPublic(record.AmmoType, record.AmmoAmount * recipe.OutputCount);
            }
            else if (!inventory.TryAddItem(record.Id, record.DisplayName, record.Category, recipe.OutputCount, record.Weight))
            {
                ColonyStorage.Instance.AddScrap(recipe.ScrapCost);
                GameplayFeedback.Toast("Pack is full");
                return false;
            }

            GameplayFeedback.Toast("Crafted " + recipe.Label);
            return true;
        }
    }
}
