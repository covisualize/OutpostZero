#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using OutpostZero.Graphics;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Bakes occlusion for the open scene. Static walls and buildings (<see cref="OcclusionPlan.Occludes"/>)
    /// occlude; smaller static clutter is only culled, so a crate never hides a zombie behind it.
    /// </summary>
    public static class OcclusionBake
    {
        public static int Run()
        {
            int occluders = 0;
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var go = renderer.gameObject;
                var flags = GameObjectUtility.GetStaticEditorFlags(go);
                if ((flags & StaticEditorFlags.BatchingStatic) == 0) continue;
                flags |= StaticEditorFlags.OccludeeStatic;
                if (OcclusionPlan.Occludes(renderer.bounds.size))
                {
                    flags |= StaticEditorFlags.OccluderStatic;
                    occluders++;
                }
                else
                {
                    flags &= ~StaticEditorFlags.OccluderStatic;
                }
                GameObjectUtility.SetStaticEditorFlags(go, flags);
            }

            StaticOcclusionCulling.smallestOccluder = OcclusionPlan.SmallestOccluder;
            StaticOcclusionCulling.smallestHole = OcclusionPlan.SmallestHole;
            StaticOcclusionCulling.backfaceThreshold = OcclusionPlan.BackfaceThreshold;
            bool baked = StaticOcclusionCulling.Compute();
            Debug.Log($"[Outpost Zero] Occlusion bake {(baked ? "done" : "failed")} with {occluders} occluders.");
            return baked ? occluders : -1;
        }
    }
}
#endif
