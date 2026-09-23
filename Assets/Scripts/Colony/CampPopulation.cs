using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Gives every living colonist who is not holding the controller a body in the yard: the colonist model
    /// from <see cref="CampCast"/>, or a capsule without it. They walk toward the station that matches
    /// the task on the board, round the modules when the yard has a NavMesh (<see cref="CampMateBody"/>).
    /// </summary>
    public class CampPopulation : MonoBehaviour
    {
        public static CampPopulation Instance { get; private set; }

        private static readonly Vector3 Gate = new Vector3(-12f, 0f, -14f);

        private readonly Dictionary<string, Transform> bodies = new Dictionary<string, Transform>();
        private readonly HashSet<string> capsules = new HashSet<string>();
        private readonly Dictionary<string, YardBark> barks = new Dictionary<string, YardBark>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            if (GetComponent<CampSelect>() == null) gameObject.AddComponent<CampSelect>();
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
            int mates = 0;
            foreach (var survivor in roster.Survivors)
            {
                if (survivor != null && survivor.alive && !survivor.leader) mates++;
            }
            var ids = new string[mates];
            var actions = new string[mates];
            var present = new bool[mates];
            var friends = new string[mates];
            int filled = 0;
            foreach (var survivor in roster.Survivors)
            {
                if (survivor == null || !survivor.alive || survivor.leader) continue;
                ids[filled] = survivor.id;
                present[filled] = true;
                filled++;
            }
            float hour = WorldClock.Instance != null ? WorldClock.Instance.Hour : 12f;
            filled = 0;
            foreach (var survivor in roster.Survivors)
            {
                if (survivor == null || !survivor.alive || survivor.leader) continue;
                friends[filled] = CloseFriend(survivor, ids);
                actions[filled] = raid
                    ? GuardStand.Face(survivor.task, survivor.morale, survivor.injury)
                    : CampUtility.Choose(survivor.id, survivor.task, survivor.hunger, survivor.thirst, survivor.morale, survivor.injury, survivor.fatigue, hour, friends[filled].Length > 0);
                filled++;
            }
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
                bool model = !capsules.Contains(survivor.id);
                if (!model) Tint(body, survivor.morale, survivor.trait, survivor.aside, survivor.mark);
                string action = actions[index];
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
                else if (action == CampUtility.Wander)
                {
                    CampUtility.WanderSpot(survivor.id, hour, out float roamX, out float roamZ);
                    goal = new Vector3(roamX, 1f, roamZ);
                }
                else if (action == CampUtility.Chat)
                {
                    if (CampUtility.Hosts(survivor.id, friends[index - 1])) goal = Station(CampUtility.Sleep, index - 1);
                    else if (TryStand(friends[index - 1], out Vector3 friendAt))
                    {
                        YardVisit.Stand(friendAt.x, friendAt.z, out float chatX, out float chatZ);
                        goal = new Vector3(chatX, 1f, chatZ);
                    }
                    else
                    {
                        action = CampUtility.Sleep;
                        goal = Station(action, index - 1);
                    }
                }
                else goal = Station(action, index - 1);
                if (!raid && action != CampUtility.Chat)
                {
                    string host = YardVisit.Host(survivor.id, action, survivor.kin, survivor.fatigue, ids, actions, present);
                    if (host.Length > 0 && TryStand(host, out Vector3 hostAt))
                    {
                        YardVisit.Stand(hostAt.x, hostAt.z, out float visitX, out float visitZ);
                        goal = new Vector3(visitX, 1f, visitZ);
                        action = "Visit";
                    }
                }
                float pace = ShiftWear.Stride(survivor.fatigue, raid);
                var walker = body.GetComponent<CampMateBody>();
                Vector3 face = walker.Drive(goal, pace);
                walker.Chore(YardPose.Activity(action));
                var beat = body.GetComponent<YardBeat>();
                if (beat == null) beat = body.gameObject.AddComponent<YardBeat>();
                float lean = YardPose.Lean(action, beat.Age, walker.Acts);
                float yaw = face.sqrMagnitude > 0.01f ? Quaternion.LookRotation(face).eulerAngles.y : body.eulerAngles.y;
                body.rotation = Quaternion.Euler(lean, yaw, 0f);
                float squat = YardPose.Scale(action);
                body.localScale = model ? new Vector3(1f, squat, 1f) : new Vector3(0.45f, 0.9f * squat, 0.45f);
                if (!barks.TryGetValue(survivor.id, out var bark) || bark == null)
                {
                    bark = YardBark.Raise(body);
                    barks[survivor.id] = bark;
                }
                bool speaks = YardBubble.Up(survivor.id, Time.time, survivor.morale);
                bark.Say(speaks, speaks ? OutpostZero.Shell.Loc.Bark(action, survivor.morale, survivor.fatigue) : "", model ? YardBubble.Height : YardBubble.Height - 1f);
            }
        }

        /// <summary>The first close friend in the yard, by id order, or empty.</summary>
        private static string CloseFriend(Survivor survivor, string[] ids)
        {
            string pick = "";
            for (int i = 0; i < ids.Length; i++)
            {
                string other = ids[i];
                if (string.IsNullOrEmpty(other) || other == survivor.id) continue;
                if (!KinBoard.Close(survivor.kin, other)) continue;
                if (pick.Length == 0 || string.CompareOrdinal(other, pick) < 0) pick = other;
            }
            return pick;
        }

        private Transform Ensure(string id, string displayName)
        {
            if (bodies.TryGetValue(id, out var existing) && existing != null) return existing;
            var prefab = CampCast.Mate;
            GameObject body;
            if (prefab != null)
            {
                body = Instantiate(prefab, Gate, Quaternion.identity);
                foreach (var collider in body.GetComponentsInChildren<Collider>()) collider.enabled = false;
                OutpostZero.Graphics.CharacterVariety.Ensure(body).Bind("colonist", false);
                capsules.Remove(id);
            }
            else
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(body.GetComponent<Collider>());
                body.transform.position = Gate + Vector3.up;
                body.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);
                var renderer = body.GetComponent<Renderer>();
                if (renderer != null && !OutpostZero.Graphics.MaterialLibrary.Dress(renderer, OutpostZero.Graphics.SurfaceFamily.Cloth, OutpostZero.Graphics.MaterialLibrary.TintFor(new Color(0.55f, 0.48f, 0.36f))))
                    renderer.material.color = new Color(0.55f, 0.48f, 0.36f);
                capsules.Add(id);
            }
            body.name = "CampMate_" + displayName;
            body.AddComponent<CampMateBody>().Dress(prefab != null);
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
            capsules.Remove(id);
            if (body != null) Destroy(body.gameObject);
            if (barks.TryGetValue(id, out var bark) && bark != null) Destroy(bark.gameObject);
            barks.Remove(id);
        }

        private void Clear()
        {
            foreach (var pair in bodies)
            {
                if (pair.Value != null) Destroy(pair.Value.gameObject);
            }
            bodies.Clear();
            capsules.Clear();
            foreach (var pair in barks)
            {
                if (pair.Value != null) Destroy(pair.Value.gameObject);
            }
            barks.Clear();
        }
    }
}
