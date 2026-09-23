using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using OutpostZero.Core;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// The 70 m yard ends at a chain-link fence on a concrete plinth, so the edge reads as a fenced
    /// lot rather than the end of the plane. The tall wall behind it is invisible: it keeps bodies in
    /// and carves the NavMesh short of the fence.
    /// </summary>
    public static class MapRim
    {
        public const float Half = 34f;
        public const float Thick = 1.2f;
        public const float Height = 3.2f;
        public const float Plinth = 0.3f;
        public const float FenceTop = 2.6f;
        public const float PostEvery = 3f;
        public const float PostRadius = 0.05f;

        public static float Open => Half - Thick * 0.5f;

        /// <summary>The fence line, a hand's width inside the invisible wall.</summary>
        public static float Line => Open - 0.05f;

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
            Fence(root.transform);
        }

        /// <summary>Post positions (x, z) round the fence, every <see cref="PostEvery"/> m and at each corner.</summary>
        public static List<Vector2> Posts()
        {
            var posts = new List<Vector2>();
            float line = Line;
            int count = Mathf.CeilToInt(line * 2f / PostEvery);
            for (int side = 0; side < 4; side++)
            {
                for (int i = 0; i < count; i++)
                {
                    float along = -line + line * 2f * i / count;
                    switch (side)
                    {
                        case 0: posts.Add(new Vector2(along, line)); break;
                        case 1: posts.Add(new Vector2(line, -along)); break;
                        case 2: posts.Add(new Vector2(-along, -line)); break;
                        default: posts.Add(new Vector2(-line, along)); break;
                    }
                }
            }
            return posts;
        }

        /// <summary>A double-sided strip whose UVs run in metres, so the mesh keeps its link size on any span.</summary>
        public static void Strip(float length, float bottom, float top, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            int start = vertices.Count;
            float half = length * 0.5f;
            vertices.Add(new Vector3(-half, bottom, 0f));
            vertices.Add(new Vector3(half, bottom, 0f));
            vertices.Add(new Vector3(half, top, 0f));
            vertices.Add(new Vector3(-half, top, 0f));
            uvs.Add(new Vector2(0f, bottom));
            uvs.Add(new Vector2(length, bottom));
            uvs.Add(new Vector2(length, top));
            uvs.Add(new Vector2(0f, top));
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
            triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
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
            if (renderer != null) renderer.enabled = false;
        }

        private static void Fence(Transform parent)
        {
            float line = Line;
            float length = line * 2f;
            var mesh = FenceMesh(length);
            var link = MaterialLibrary.Lit(SurfaceFamily.ChainLink);
            for (int side = 0; side < 4; side++)
            {
                float yaw = side * 90f;
                var face = Quaternion.Euler(0f, yaw, 0f);
                var at = face * new Vector3(0f, 0f, line);

                var panel = new GameObject("MapRim_Fence_" + side);
                panel.transform.SetParent(parent, false);
                panel.transform.SetPositionAndRotation(at, face);
                panel.AddComponent<MeshFilter>().sharedMesh = mesh;
                var draw = panel.AddComponent<MeshRenderer>();
                if (link != null) draw.sharedMaterial = link;
                else draw.enabled = false;

                var plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plinth.name = "MapRim_Plinth_" + side;
                Object.Destroy(plinth.GetComponent<Collider>());
                plinth.transform.SetParent(parent, false);
                plinth.transform.SetPositionAndRotation(at + Vector3.up * Plinth * 0.5f, face);
                plinth.transform.localScale = new Vector3(length + 0.4f, Plinth, 0.4f);
                if (!MaterialLibrary.Dress(plinth.GetComponent<Renderer>(), SurfaceFamily.ConcreteCracked)) plinth.GetComponent<Renderer>().material.color = new Color(0.4f, 0.39f, 0.36f);

                var rail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rail.name = "MapRim_Rail_" + side;
                Object.Destroy(rail.GetComponent<Collider>());
                rail.transform.SetParent(parent, false);
                rail.transform.SetPositionAndRotation(at + Vector3.up * FenceTop, face * Quaternion.Euler(0f, 0f, 90f));
                rail.transform.localScale = new Vector3(PostRadius * 1.4f, length * 0.5f, PostRadius * 1.4f);
                MaterialLibrary.Dress(rail.GetComponent<Renderer>(), SurfaceFamily.MetalRusted);
            }
            foreach (var spot in Posts())
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "MapRim_Post";
                Object.Destroy(post.GetComponent<Collider>());
                post.transform.SetParent(parent, false);
                post.transform.position = new Vector3(spot.x, (FenceTop + 0.15f) * 0.5f, spot.y);
                post.transform.localScale = new Vector3(PostRadius * 2f, (FenceTop + 0.15f) * 0.5f, PostRadius * 2f);
                MaterialLibrary.Dress(post.GetComponent<Renderer>(), SurfaceFamily.MetalRusted);
            }
        }

        private static Mesh FenceMesh(float length)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            Strip(length, Plinth, FenceTop, vertices, uvs, triangles);
            var mesh = new Mesh { name = "MapRimFence" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
