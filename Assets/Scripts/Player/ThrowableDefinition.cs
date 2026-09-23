using UnityEngine;

namespace OutpostZero.Player
{
    /// <summary>One throwable's numbers: what it does on landing, how loud, how long its fuse, and its reach.</summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Throwable", fileName = "Throwable")]
    public class ThrowableDefinition : ScriptableObject
    {
        [Tooltip("Item id this throws. Also the asset name.")]
        public string id = "";
        [Tooltip("1 lure (breaks loud), 2 fire, 3 flare (burns and calls), 4 bomb.")]
        [Range(1, 4)] public int kind = TossKind.Lure;
        [Tooltip("Metres its landing, or each flare pulse, is heard.")]
        [Min(0f)] public float noise = 18f;
        [Tooltip("Seconds in the air before it goes off by itself when nothing stops it.")]
        [Min(0.1f)] public float fuse = 0.7f;
        [Tooltip("Metres of blast, fire burst or flare light. 0 for a plain lure.")]
        [Min(0f)] public float radius;
        [Tooltip("Damage to everything inside the radius when it goes off.")]
        [Min(0f)] public float damage;
        [Tooltip("Seconds a flare burns. 0 for everything else.")]
        [Min(0f)] public float seconds;
        [Tooltip("Metres across the thrown body.")]
        [Min(0.05f)] public float size = 0.25f;

        public ThrowableTable.Row ToRow()
        {
            return new ThrowableTable.Row
            {
                Id = id,
                Kind = kind,
                Noise = noise,
                Fuse = fuse,
                Radius = radius,
                Damage = damage,
                Seconds = seconds,
                Size = size
            };
        }

        public void CopyFrom(ThrowableTable.Row row)
        {
            id = row.Id;
            kind = row.Kind;
            noise = row.Noise;
            fuse = row.Fuse;
            radius = row.Radius;
            damage = row.Damage;
            seconds = row.Seconds;
            size = row.Size;
        }
    }
}
