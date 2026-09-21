using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Gives every living colonist who is not holding the controller a body in the yard.
    /// They walk toward the station that matches the task on the board.
    /// </summary>
    public class CampPopulation : MonoBehaviour
    {
        private readonly Dictionary<string, Transform> bodies = new Dictionary<string, Transform>();

        private void Update()
        {
            bool camp = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.CampManagement;
            if (!camp)
            {
                if (bodies.Count > 0) Clear();
                return;
            }

            var roster = SurvivorRoster.Instance;
            if (roster == null) return;
            foreach (var survivor in roster.Survivors)
            {
                if (survivor == null || !survivor.alive || survivor.leader)
                {
                    if (survivor != null) Remove(survivor.id);
                    continue;
                }
                var body = Ensure(survivor.id, survivor.displayName);
                Vector3 goal = Station(survivor.task) + new Vector3(Mathf.Sin(survivor.id.GetHashCode()) * 0.6f, 0f, 0.4f);
                body.position = Vector3.MoveTowards(body.position, goal, 1.4f * Time.deltaTime);
                Vector3 face = goal - body.position;
                face.y = 0f;
                if (face.sqrMagnitude > 0.01f) body.rotation = Quaternion.LookRotation(face);
            }
        }

        private Transform Ensure(string id, string displayName)
        {
            if (bodies.TryGetValue(id, out var existing) && existing != null) return existing;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "CampMate_" + displayName;
            Destroy(body.GetComponent<Collider>());
            body.transform.position = new Vector3(-12f, 1f, -14f);
            body.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);
            var renderer = body.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.55f, 0.48f, 0.36f);
            bodies[id] = body.transform;
            return body.transform;
        }

        private static Vector3 Station(string task)
        {
            switch (task)
            {
                case "Cook": return new Vector3(-12f, 1f, -12f);
                case "Guard": return new Vector3(-8f, 1f, -12f);
                case "Medic": return new Vector3(-16f, 1f, -10f);
                case "Scavenge": return new Vector3(-18f, 1f, -8f);
                default: return new Vector3(-14f, 1f, -15f);
            }
        }

        private void Remove(string id)
        {
            if (!bodies.TryGetValue(id, out var body)) return;
            bodies.Remove(id);
            if (body != null) Destroy(body.gameObject);
        }

        private void Clear()
        {
            foreach (var pair in bodies)
            {
                if (pair.Value != null) Destroy(pair.Value.gameObject);
            }
            bodies.Clear();
        }
    }
}
