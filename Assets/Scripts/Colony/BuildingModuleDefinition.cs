using UnityEngine;
using OutpostZero.Graphics;

namespace OutpostZero.Colony
{
    /// <summary>One yard module: what it costs, how long a builder takes over it, and how its stand-in looks.</summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Building Module", fileName = "Module")]
    public class BuildingModuleDefinition : ScriptableObject
    {
        [Tooltip("Module kind as saved in the yard (ModuleKind name, e.g. \"Watchtower\").")]
        public string id = "";

        [Header("Cost")]
        [Tooltip("Scrap spent when the site is laid; half comes back on a teardown.")]
        [Min(0)] public int scrap;
        [Tooltip("Builder hours before the site becomes the finished module.")]
        [Min(1)] public int buildHours = 1;

        [Header("Stand-in")]
        [Tooltip("Size of the box, collider and nav obstacle on its 2 m cell, in metres.")]
        public Vector3 size = Vector3.one;
        [Tooltip("The box shrinks and sinks as integrity drops (walls).")]
        public bool wears;
        [Tooltip("Library surface for the stand-in; None keeps a flat tint.")]
        public SurfaceFamily surface = SurfaceFamily.None;
        public Color tint = Color.grey;

        public ModuleTable.Row ToRow()
        {
            return new ModuleTable.Row
            {
                Id = id,
                Scrap = scrap,
                Hours = buildHours,
                Size = size,
                Wears = wears,
                Family = surface,
                Tint = tint
            };
        }

        public void CopyFrom(ModuleTable.Row row)
        {
            id = row.Id;
            scrap = row.Scrap;
            buildHours = row.Hours;
            size = row.Size;
            wears = row.Wears;
            surface = row.Family;
            tint = row.Tint;
        }
    }
}
