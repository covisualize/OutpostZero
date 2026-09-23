using UnityEngine;

namespace OutpostZero.Colony
{
    /// <summary>One trading faction: its name, its table, what trust adds, and when it turns the camp away.</summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Faction", fileName = "Faction")]
    public class FactionDefinition : ScriptableObject
    {
        [Tooltip("Faction id used by saves, quests and strings; one of caravan, militia, clinic, farmers.")]
        public string id = "";
        [Tooltip("Name on the stall and the camp report.")]
        public string label = "";

        [Header("Table")]
        [Tooltip("Item ids the faction sells to any camp.")]
        public string[] stock = new string[0];
        [Tooltip("Item id added to the table once standing reaches Trusted (30), or empty.")]
        public string premium = "";

        [Header("Temper")]
        [Tooltip("Standing below which the faction won't trade; -101 means it always trades.")]
        [Range(-101, 100)] public int refuseBelow = CaravanBook.Never;

        [Header("Prices")]
        [Tooltip("Percent of the base price this faction asks before standing and haggling (90 to 150).")]
        [Range(CaravanBook.MarkupFloor, CaravanBook.MarkupCeiling)] public int markup = 100;

        public FactionTable.Row ToRow()
        {
            return new FactionTable.Row
            {
                Id = id,
                Label = label,
                Stock = stock != null ? (string[])stock.Clone() : new string[0],
                Premium = premium ?? "",
                RefuseBelow = refuseBelow,
                Markup = markup
            };
        }

        public void CopyFrom(FactionTable.Row row)
        {
            id = row.Id;
            label = row.Label;
            stock = row.Stock != null ? (string[])row.Stock.Clone() : new string[0];
            premium = row.Premium ?? "";
            refuseBelow = row.RefuseBelow;
            markup = row.Markup;
        }
    }
}
