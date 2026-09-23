using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Colony
{
    /// <summary>The recipe list the camp loads from Resources. Tools > Outpost Zero > Sync Recipe Book rebuilds it.</summary>
    public class RecipeBook : ScriptableObject
    {
        public const string ResourcePath = "RecipeBook";

        public CraftingRecipe[] recipes = new CraftingRecipe[0];

        private static bool loaded;

        public static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            Use(Resources.Load<RecipeBook>(ResourcePath));
        }

        public static void Use(RecipeBook book)
        {
            var rows = new List<RecipeTable.Row>();
            if (book != null && book.recipes != null)
            {
                foreach (var recipe in book.recipes)
                    if (recipe != null && !string.IsNullOrEmpty(recipe.id)) rows.Add(recipe.ToRow());
            }
            RecipeTable.Use(rows);
        }
    }
}
