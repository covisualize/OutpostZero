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
        public static CampPopulation Instance { get; private set; }

        private readonly Dictionary<string, Transform> bodies = new Dictionary<string, Transform>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool TryStand(string id, out Vector3 position)
        {
            position = Vector3.zero;
            if (string.IsNullOrEmpty(id) || !bodies.TryGetValue(id, out var body) || body == null) return false;
            position = body.position;
            return true;
        }

        private void Update()
        {
            var state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.MainMenu;
            bool camp = state == GameState.CampManagement;
            bool raid = state == GameState.RaidActive;
            if (!camp && !raid)
            {
                if (bodies.Count > 0) Clear();
                return;
            }

            var roster = SurvivorRoster.Instance;
            if (roster == null) return;
            int index = 0;
            int guardSlot = 0;
            foreach (var survivor in roster.Survivors)
            {
                if (survivor == null || !survivor.alive || survivor.leader)
                {
                    if (survivor != null) Remove(survivor.id);
                    continue;
                }
                var body = Ensure(survivor.id, survivor.displayName);
                Tint(body, survivor.morale, survivor.trait, survivor.aside, survivor.mark);
                string action = raid
                    ? GuardStand.Face(survivor.task, survivor.morale, survivor.injury)
                    : CampRoutine.Choose(survivor.task, survivor.hunger, survivor.thirst, survivor.morale, survivor.injury, survivor.fatigue);
                index++;
                Vector3 goal;
                if (raid && action == "Guard")
                {
                    var night = NightRaidController.Instance;
                    string side = night != null ? night.SideAt(guardSlot) : "gate";
                    int post = night != null ? night.PostOnSide(guardSlot) : guardSlot;
                    goal = Line(side, post);
                    guardSlot++;
                }
                else goal = Station(action, index - 1);
                float pace = ShiftWear.Stride(survivor.fatigue, raid);
                body.position = Vector3.MoveTowards(body.position, goal, pace * Time.deltaTime);
                Vector3 face = goal - body.position;
                face.y = 0f;
                var beat = body.GetComponent<YardBeat>();
                if (beat == null) beat = body.gameObject.AddComponent<YardBeat>();
                float lean = YardPose.Lean(action, beat.Age);
                float yaw = face.sqrMagnitude > 0.01f ? Quaternion.LookRotation(face).eulerAngles.y : body.eulerAngles.y;
                body.rotation = Quaternion.Euler(lean, yaw, 0f);
                float squat = YardPose.Scale(action);
                body.localScale = new Vector3(0.45f, 0.9f * squat, 0.45f);
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

        private static void Tint(Transform body, float morale, string trait, string aside, string mark)
        {
            var renderer = body.GetComponent<Renderer>();
            if (renderer == null) return;
            Color tint;
            switch (ColonyDay.Mood(morale, trait, aside, mark))
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

        private static Vector3 Line(string approach, int index)
        {
            var grid = GridBuilder.Instance;
            int count = grid != null ? grid.Placed.Count : 0;
            var kinds = new string[count];
            var sites = new int[count];
            var integrity = new int[count];
            var xs = new float[count];
            var zs = new float[count];
            for (int i = 0; i < count; i++)
            {
                var module = grid.Placed[i];
                kinds[i] = module.kind;
                sites[i] = module.site;
                integrity[i] = module.integrity;
                xs[i] = module.x;
                zs[i] = module.z;
            }
            GuardStand.Mark(approach, index, kinds, xs, zs, sites, integrity, out float x, out float z);
            return new Vector3(x, 1f, z);
        }

        private static Vector3 Station(string task, int index)
        {
            var grid = GridBuilder.Instance;
            int count = grid != null ? grid.Placed.Count : 0;
            var kinds = new string[count];
            var sites = new int[count];
            var integrity = new int[count];
            var jobs = new int[count];
            var xs = new float[count];
            var zs = new float[count];
            for (int i = 0; i < count; i++)
            {
                var module = grid.Placed[i];
                kinds[i] = module.kind;
                sites[i] = module.site;
                integrity[i] = module.integrity;
                jobs[i] = module.job;
                xs[i] = module.x;
                zs[i] = module.z;
            }
            int pick = CampPost.Pick(task, kinds, sites, integrity, jobs);
            bool found = pick >= 0;
            float moduleX = found ? xs[pick] : 0f;
            float moduleZ = found ? zs[pick] : 0f;
            CampPost.Place(task, index, moduleX, moduleZ, found, out float x, out float z);
            return new Vector3(x, 1f, z);
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
