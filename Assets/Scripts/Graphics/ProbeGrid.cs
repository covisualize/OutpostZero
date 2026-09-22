using UnityEngine;
using OutpostZero.Shell;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// One reflection probe covers a district block at 128.
    /// Light probes sit on a six-meter grid over the yard.
    /// </summary>
    public static class ProbeGrid
    {
        public const float Step = 6f;
        public const int Resolution = 128;
        public const float Eye = 1.6f;
        public const float YardMin = -30f;
        public const float YardMax = 30f;
        public const float BoxHeight = 8f;

        public static float Snap(float value)
        {
            return Mathf.Round(value / Step) * Step;
        }

        public static int Span(float min, float max)
        {
            if (max < min)
            {
                float swap = min;
                min = max;
                max = swap;
            }
            float start = Snap(min);
            float end = Snap(max);
            if (end < start) end = start;
            int count = 0;
            for (float x = start; x <= end + 0.001f; x += Step)
            {
                count++;
                if (count >= 32) break;
            }
            return count < 1 ? 1 : count;
        }

        public static Vector3[] Lights(float minX, float maxX, float minZ, float maxZ)
        {
            if (maxX < minX)
            {
                float swap = minX;
                minX = maxX;
                maxX = swap;
            }
            if (maxZ < minZ)
            {
                float swap = minZ;
                minZ = maxZ;
                maxZ = swap;
            }
            int nx = Span(minX, maxX);
            int nz = Span(minZ, maxZ);
            var points = new Vector3[nx * nz];
            float x0 = Snap(minX);
            float z0 = Snap(minZ);
            int i = 0;
            for (int z = 0; z < nz; z++)
            {
                for (int x = 0; x < nx; x++)
                {
                    points[i++] = new Vector3(x0 + x * Step, Eye, z0 + z * Step);
                }
            }
            return points;
        }

        public static Vector3 Center(float minX, float maxX, float minZ, float maxZ)
        {
            return new Vector3((minX + maxX) * 0.5f, Eye, (minZ + maxZ) * 0.5f);
        }

        public static Vector3 Box(float minX, float maxX, float minZ, float maxZ)
        {
            float width = maxX - minX;
            float depth = maxZ - minZ;
            if (width < 0f) width = -width;
            if (depth < 0f) depth = -depth;
            if (width < Step) width = Step;
            if (depth < Step) depth = Step;
            return new Vector3(width + 2f, BoxHeight, depth + 2f);
        }
    }

    public static class ProbeField
    {
        public static void Place(Transform parent, DistrictBlocks.Cell[] cells)
        {
            if (parent == null || cells == null || cells.Length == 0) return;
            float minX = cells[0].X;
            float maxX = cells[0].X;
            float minZ = cells[0].Z;
            float maxZ = cells[0].Z;
            for (int i = 1; i < cells.Length; i++)
            {
                if (cells[i].X < minX) minX = cells[i].X;
                if (cells[i].X > maxX) maxX = cells[i].X;
                if (cells[i].Z < minZ) minZ = cells[i].Z;
                if (cells[i].Z > maxZ) maxZ = cells[i].Z;
            }
            var host = new GameObject("BlockProbe");
            host.transform.SetParent(parent, false);
            host.transform.position = ProbeGrid.Center(minX, maxX, minZ, maxZ);
            var probe = host.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.resolution = ProbeGrid.Resolution;
            probe.boxProjection = true;
            probe.size = ProbeGrid.Box(minX, maxX, minZ, maxZ);
            probe.hdr = true;
            probe.RenderProbe();
        }
    }
}
