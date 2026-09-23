using UnityEngine;

namespace OutpostZero.Colony
{
    public enum CraftStation
    {
        Any = CraftBill.Any,
        Workbench = CraftBill.Workbench,
        MedicalCot = CraftBill.Cot,
        Campfire = CraftBill.Campfire
    }

    /// <summary>One camp recipe: what it makes, what it spends, where, and what unlocks it.</summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Crafting Recipe", fileName = "Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        [Tooltip("Recipe id used by the queue, saves and strings (recipe.<id>).")]
        public string id = "";
        [Tooltip("English fallback label when the string table has no recipe.<id> entry.")]
        public string label = "";
        [Tooltip("Item id delivered to the pack, or camp_food / camp_water for the stores.")]
        public string outputId = "";
        [Tooltip("How many of the output one craft delivers.")]
        [Min(1)] public int outputCount = 1;

        [Header("Bill")]
        [Tooltip("Scrap before the workbench discount.")]
        [Min(0)] public int scrap;
        [Min(0)] public int cloth;
        [Min(0)] public int chemicals;
        [Tooltip("Duct tape.")]
        [Min(0)] public int tape;
        [Tooltip("Raw food, taken from the camp stores.")]
        [Min(0)] public int raw;

        [Header("Gate")]
        public CraftStation station = CraftStation.Any;
        [Tooltip("Task someone must hold for the craft to go ahead (\"Medic\"), or empty.")]
        public string skill = "";
        [Tooltip("Skill someone in camp must hold, by task name (Build, Medic, Cook), or empty.")]
        public string know = "";
        [Tooltip("Level of that skill the recipe needs, 0 for none.")]
        [Range(0, Practice.Cap)] public int level;
        [Tooltip("Bench tier the recipe needs: 1, or 2 once the workbench is raised.")]
        [Range(1, 2)] public int tier = 1;
        [Tooltip("Blueprint id found on the street that reveals the recipe, or empty.")]
        public string blueprint = "";

        public RecipeTable.Row ToRow()
        {
            return new RecipeTable.Row
            {
                Id = id,
                Label = label,
                OutputId = outputId,
                OutputCount = outputCount,
                Cost = new CraftBill.Cost
                {
                    Scrap = scrap,
                    Cloth = cloth,
                    Chemicals = chemicals,
                    Tape = tape,
                    Raw = raw,
                    Station = (int)station,
                    Skill = skill ?? "",
                    Know = know ?? "",
                    Level = level
                },
                Tier = tier,
                Print = blueprint ?? ""
            };
        }

        public void CopyFrom(RecipeTable.Row row)
        {
            id = row.Id;
            label = row.Label;
            outputId = row.OutputId;
            outputCount = row.OutputCount;
            scrap = row.Cost.Scrap;
            cloth = row.Cost.Cloth;
            chemicals = row.Cost.Chemicals;
            tape = row.Cost.Tape;
            raw = row.Cost.Raw;
            station = (CraftStation)row.Cost.Station;
            skill = row.Cost.Skill ?? "";
            know = row.Cost.Know ?? "";
            level = row.Cost.Level;
            tier = row.Tier;
            blueprint = row.Print ?? "";
        }
    }
}
