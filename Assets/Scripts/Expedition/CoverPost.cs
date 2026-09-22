using System.Collections.Generic;
using UnityEngine;
using OutpostZero.AI;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// A street slab the player can stand behind. Zombies on the far side see a shorter distance.
    /// </summary>
    public class CoverPost : MonoBehaviour
    {
        private static readonly List<CoverPost> All = new List<CoverPost>();

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        public static float ScaleFor(Vector3 player, Vector3 threat, bool crouching)
        {
            if (All.Count == 0) return 1f;
            var xs = new float[All.Count];
            var zs = new float[All.Count];
            var yaw = new float[All.Count];
            int count = 0;
            for (int i = 0; i < All.Count; i++)
            {
                var post = All[i];
                if (post == null) continue;
                xs[count] = post.transform.position.x;
                zs[count] = post.transform.position.z;
                yaw[count] = post.transform.eulerAngles.y;
                count++;
            }
            if (count == 0) return 1f;
            if (count < xs.Length)
            {
                var trimX = new float[count];
                var trimZ = new float[count];
                var trimYaw = new float[count];
                for (int i = 0; i < count; i++)
                {
                    trimX[i] = xs[i];
                    trimZ[i] = zs[i];
                    trimYaw[i] = yaw[i];
                }
                xs = trimX;
                zs = trimZ;
                yaw = trimYaw;
            }
            return CoverSight.Best(player.x, player.z, threat.x, threat.z, xs, zs, yaw, crouching);
        }
    }
}
