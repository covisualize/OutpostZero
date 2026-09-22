using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// The green disc left where a toxic barrel broke.
    /// Zombies in it slow down. The player keeps the poison while they stand in it.
    /// </summary>
    public class GasField : MonoBehaviour
    {
        private static readonly List<GasField> OpenFields = new List<GasField>();
        private float age = -1f;

        public static void Open(Vector3 at)
        {
            var go = new GameObject("GasField");
            go.transform.position = at;
            go.AddComponent<GasField>();
        }

        public static bool Covers(float x, float z)
        {
            for (int i = 0; i < OpenFields.Count; i++)
            {
                var field = OpenFields[i];
                if (field == null) continue;
                if (GasCloud.Covers(x, z, field.transform.position.x, field.transform.position.z, field.age))
                    return true;
            }
            return false;
        }

        private void OnEnable()
        {
            if (!OpenFields.Contains(this)) OpenFields.Add(this);
        }

        private void OnDisable()
        {
            OpenFields.Remove(this);
        }

        private void Start()
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "GasDisc";
            var solid = disc.GetComponent<Collider>();
            if (solid != null) Destroy(solid);
            disc.transform.SetParent(transform, false);
            disc.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            float width = GasCloud.Radius * 2f;
            disc.transform.localScale = new Vector3(width, 0.04f, width);
            var renderer = disc.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.35f, 0.82f, 0.28f, 0.35f);
        }

        private void Update()
        {
            float next = age < 0f ? 0f : age + Time.deltaTime;
            age = next;
            if (!GasCloud.Live(age))
            {
                Destroy(gameObject);
                return;
            }

            var player = PlayerRegistry.Current;
            if (player == null) return;
            if (!GasCloud.Covers(player.transform.position.x, player.transform.position.z, transform.position.x, transform.position.z, age))
                return;
            player.GetComponent<OutpostZero.Player.StatusEffectController>()?.HoldPoison(2f);
        }
    }
}
