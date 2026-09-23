using UnityEngine;
using OutpostZero.Combat;

namespace OutpostZero.Player
{
    /// <summary>
    /// The arc drawn while the throw key is held: the same release point, speed and lift the throw uses,
    /// ending where the item lands. Walls are not traced; a throw into one stops early.
    /// </summary>
    public class ThrowPreview : MonoBehaviour
    {
        public const int Points = 24;

        private LineRenderer line;
        private readonly float[] along = new float[Points];
        private readonly float[] up = new float[Points];

        public bool Showing => line != null && line.enabled;

        public void Show(Vector3 feet, Vector3 facing)
        {
            if (line == null) Build();
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;
            facing.Normalize();
            Vector3 hand = feet + facing;
            int count = ThrowArc.Sample(ThrowArc.Height, ThrowArc.Forward, ThrowArc.Lift, ThrowArc.Gravity, along, up);
            line.positionCount = count;
            for (int i = 0; i < count; i++)
                line.SetPosition(i, new Vector3(hand.x + facing.x * along[i], feet.y + up[i], hand.z + facing.z * along[i]));
            line.enabled = true;
        }

        public void Hide()
        {
            if (line != null) line.enabled = false;
        }

        private void Build()
        {
            var go = new GameObject("ThrowArc");
            go.transform.SetParent(transform, false);
            line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.startWidth = 0.06f;
            line.endWidth = 0.14f;
            line.numCapVertices = 2;
            line.sharedMaterial = CombatVfx.SpriteMaterial();
            line.startColor = new Color(0.55f, 0.95f, 1f, 0.15f);
            line.endColor = new Color(0.55f, 0.95f, 1f, 0.8f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
        }
    }
}
