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
            int index = 0;
            foreach (var survivor in roster.Survivors)
            {
                if (survivor == null || !survivor.alive || survivor.leader)
                {
                    if (survivor != null) Remove(survivor.id);
                    continue;
                }
                var body = Ensure(survivor.id, survivor.displayName);
                Tint(body, survivor.morale);
                string action = CampRoutine.Choose(survivor.task, survivor.hunger, survivor.thirst, survivor.morale, survivor.injury);
                CampRoutine.Nudge(index, out float nudgeX, out float nudgeZ);
                index++;
                Vector3 goal = Station(action) + new Vector3(nudgeX, 0f, nudgeZ);
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

        private static void Tint(Transform body, float morale)
        {
            var renderer = body.GetComponent<Renderer>();
            if (renderer == null) return;
            Color tint;
            switch (ColonyDay.Mood(morale))
            {
                case "Inspired": tint = new Color(0.72f, 0.62f, 0.42f); break;
                case "Depressed": tint = new Color(0.35f, 0.35f, 0.38f); break;
                case "Breakdown": tint = new Color(0.45f, 0.2f, 0.18f); break;
                default: tint = new Color(0.55f, 0.48f, 0.36f); break;
            }
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", tint);
            block.SetColor("_Color", tint);
            renderer.SetPropertyBlock(block);
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
