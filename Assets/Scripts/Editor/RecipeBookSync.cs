#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Colony;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Keeps Assets/Data/Recipes and Resources/RecipeBook in step with the built-in recipe list. Missing recipes
    /// are created from the code tables; existing assets keep their tuned values. Recipes added by hand in the
    /// folder join the book after the built-in ones.
    /// </summary>
    public static class RecipeBookSync
    {
        public const string RecipesDir = "Assets/Data/Recipes";
        public const string BookPath = "Assets/Resources/RecipeBook.asset";

        [MenuItem("Tools/Outpost Zero/Sync Recipe Book", false, 4)]
        public static void SyncFromMenu()
        {
            int count = Sync();
            Debug.Log("[RecipeBookSync] The recipe book lists " + count + " recipes.");
        }

        public static int Sync()
        {
            Directory.CreateDirectory(RecipesDir);
            Directory.CreateDirectory(Path.GetDirectoryName(BookPath));
            var listed = new List<CraftingRecipe>();
            var seen = new HashSet<string>();
            foreach (var row in RecipeTable.BuiltInRows())
            {
                string path = RecipesDir + "/" + row.Id + ".asset";
                var recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(path);
                if (recipe == null)
                {
                    recipe = ScriptableObject.CreateInstance<CraftingRecipe>();
                    recipe.CopyFrom(row);
                    AssetDatabase.CreateAsset(recipe, path);
                }
                listed.Add(recipe);
                seen.Add(recipe.id);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:CraftingRecipe", new[] { RecipesDir }))
            {
                var extra = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(AssetDatabase.GUIDToAssetPath(guid));
                if (extra == null || string.IsNullOrEmpty(extra.id) || !seen.Add(extra.id)) continue;
                listed.Add(extra);
            }

            var book = AssetDatabase.LoadAssetAtPath<RecipeBook>(BookPath);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<RecipeBook>();
                AssetDatabase.CreateAsset(book, BookPath);
            }
            book.recipes = listed.ToArray();
            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
            RecipeBook.Use(book);
            return listed.Count;
        }
    }
}
#endif
