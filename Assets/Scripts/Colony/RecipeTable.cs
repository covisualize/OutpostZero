using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>
    /// The recipes the camp crafts from. Empty until <see cref="RecipeBook.Ensure"/> fills it from
    /// Resources/RecipeBook; until then, and for any id the book lacks, the code tables in
    /// <see cref="CraftBill"/>, <see cref="CraftGate"/> and <see cref="CraftingBench.BuiltIn"/> answer.
    /// </summary>
    public static class RecipeTable
    {
        public sealed class Row
        {
            public string Id;
            public string Label;
            public string OutputId;
            public int OutputCount = 1;
            public CraftBill.Cost Cost;
            public int Tier = 1;
            public string Print = "";
        }

        private static readonly Dictionary<string, Row> rows = new Dictionary<string, Row>();
        private static Recipe[] recipes;

        public static bool FromAsset => recipes != null;

        /// <summary>The book's recipes in order, or null when no book is loaded.</summary>
        public static Recipe[] Active => recipes;

        public static void Use(IList<Row> list)
        {
            Clear();
            if (list == null || list.Count == 0) return;
            var ordered = new List<Recipe>();
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null || string.IsNullOrEmpty(row.Id) || rows.ContainsKey(row.Id)) continue;
                rows[row.Id] = row;
                ordered.Add(new Recipe
                {
                    Id = row.Id,
                    Label = string.IsNullOrEmpty(row.Label) ? row.Id : row.Label,
                    ScrapCost = row.Cost.Scrap,
                    OutputId = string.IsNullOrEmpty(row.OutputId) ? row.Id : row.OutputId,
                    OutputCount = row.OutputCount < 1 ? 1 : row.OutputCount
                });
            }
            if (ordered.Count > 0) recipes = ordered.ToArray();
        }

        public static void Clear()
        {
            rows.Clear();
            recipes = null;
        }

        public static bool TryRow(string id, out Row row)
        {
            row = null;
            return !string.IsNullOrEmpty(id) && rows.TryGetValue(id, out row);
        }

        /// <summary>The code tables as rows: what Sync Recipe Book writes and what the committed assets must match.</summary>
        public static List<Row> BuiltInRows()
        {
            var list = new List<Row>();
            foreach (var recipe in CraftingBench.BuiltIn)
            {
                if (!CraftBill.CodeOf(recipe.Id, out var cost)) continue;
                list.Add(new Row
                {
                    Id = recipe.Id,
                    Label = recipe.Label,
                    OutputId = recipe.OutputId,
                    OutputCount = recipe.OutputCount,
                    Cost = cost,
                    Tier = CraftGate.CodeTier(recipe.Id),
                    Print = CraftGate.CodePrint(recipe.Id)
                });
            }
            return list;
        }
    }
}
