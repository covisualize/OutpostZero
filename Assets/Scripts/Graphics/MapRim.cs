using UnityEngine;
using UnityEngine.AI;
using OutpostZero.Core;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// A low wall around the 70 m yard so the street does not fall off the plane.
    /// The sanctuary and the road stay inside the opening.
    /// </summary>
    public static class MapRim
    {
        public const float Half = 34f;
        public const float Thick = 1.2f;
        public const float Height = 3.2f;

        public static float Open => Half - Thick * 0.5f;

        public static bool Inside(float x, float z)
        {
            float open = Open;
            return x >= -open && x <= open && z >= -open && z <= open;
        }

        public static void Raise()
        {
            if (GameObject.Find("MapRim") != null) return;
            var root = new GameObject("MapRim");
            float span = Half * 2f + Thick;
            Wall(root.transform, "MapRim_N", 0f, Half, span, Thick);
            Wall(root.transform, "MapRim_S", 0f, -Half, span, Thick);
            Wall(root.transform, "MapRim_E", Half, 0f, Thick, span);
            Wall(root.transform, "MapRim_W", -Half, 0f, Thick, span);
        }

        private static void Wall(Transform parent, string name, float x, float z, float width, float depth)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.position = new Vector3(x, Height * 0.5f, z);
            wall.transform.localScale = new Vector3(width, Height, depth);
            wall.layer = GameLayers.Environment;
            var obstacle = wall.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = NavMeshObstacleShape.Box;
            var renderer = wall.GetComponent<Renderer>();
            if (renderer != null && !MaterialLibrary.Dress(renderer, SurfaceFamily.BrickGrey)) renderer.material.color = new Color(0.24f, 0.22f, 0.2f);
        }
    }
}
