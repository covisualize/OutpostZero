using UnityEngine;
using OutpostZero.Player;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// Hides distant kit blocks and street clutter. Skyline cards stay so the edge still reads.
    /// </summary>
    public class LodGovernor : MonoBehaviour
    {
        private Renderer[] watched = System.Array.Empty<Renderer>();
        private int wait;

        private void LateUpdate()
        {
            wait--;
            if (wait <= 0)
            {
                wait = 20;
                Collect();
            }
            var player = PlayerRegistry.Current;
            if (player == null || watched == null) return;
            Vector3 origin = player.transform.position;
            for (int i = 0; i < watched.Length; i++)
            {
                var renderer = watched[i];
                if (renderer == null) continue;
                float distance = Vector3.Distance(origin, renderer.transform.position);
                bool hide = QualityProfile.Culls(renderer.gameObject.name, distance);
                if (renderer.enabled == hide) renderer.enabled = !hide;
            }
        }

        private void Collect()
        {
            var all = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                string name = all[i].gameObject.name;
                if (name == "KitBlock" || name.StartsWith("Dress_")) count++;
            }
            if (watched.Length != count) watched = new Renderer[count];
            int write = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                string name = all[i].gameObject.name;
                if (name == "KitBlock" || name.StartsWith("Dress_")) watched[write++] = all[i];
            }
        }
    }
}
