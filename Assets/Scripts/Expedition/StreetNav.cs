using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// Rebuilds the walk mesh after a district is dressed, so lots and curbs
    /// exist before the dead path across them. An existing surface updates in
    /// the background. A missing one is baked once on the spot.
    /// </summary>
    public static class StreetNav
    {
        public static void Schedule(MonoBehaviour host)
        {
            if (host == null || !host.isActiveAndEnabled) return;
            host.StartCoroutine(Rebuild());
        }

        public static IEnumerator Rebuild()
        {
            var surface = Object.FindFirstObjectByType<NavMeshSurface>();
            if (surface == null)
            {
                var host = new GameObject("StreetNav");
                surface = host.AddComponent<NavMeshSurface>();
            }

            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.ignoreNavMeshAgent = true;
            surface.ignoreNavMeshObstacle = true;

            if (surface.navMeshData == null)
            {
                surface.BuildNavMesh();
                Settle();
                yield break;
            }

            var operation = surface.UpdateNavMesh(surface.navMeshData);
            if (operation != null)
            {
                while (!operation.isDone) yield return null;
            }
            Settle();
        }

        private static void Settle()
        {
            var agents = Object.FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < agents.Length; i++)
            {
                var agent = agents[i];
                if (agent == null || !agent.enabled || !agent.isOnNavMesh) continue;
                agent.Warp(agent.transform.position);
            }
        }
    }
}
