using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Sensory;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// A storefront pane lets a look pass and still stops a body.
    /// One solid hit breaks it. The sill, the header, and a broken frame stay shut.
    /// </summary>
    public static class PaneGlass
    {
        public const float Hp = 12f;
        public const float Noise = 9f;
        public const string Name = "KitGlass";

        public static readonly Color Tint = new Color(0.62f, 0.8f, 0.86f, 0.38f);

        public static bool Opening(string pieceId, float y, float height, float width, float pieceWidth)
        {
            if (pieceId != "wall_window") return false;
            if (pieceWidth <= 0f) return false;
            if (y < 0.85f || y >= 2f) return false;
            if (height <= 0f || height > 1.25f) return false;
            if (width <= 0f || width >= pieceWidth - 0.4f) return false;
            return true;
        }

        public static bool SeeThrough(string colliderName)
        {
            return colliderName == Name;
        }

        public static bool Occluded(string[] names)
        {
            if (names == null || names.Length == 0) return false;
            for (int i = 0; i < names.Length; i++)
            {
                if (!SeeThrough(names[i])) return true;
            }
            return false;
        }

        public static float After(float hp, float hit)
        {
            if (hp <= 0f) return 0f;
            if (hit <= 0f) return hp;
            float left = hp - hit;
            return left < 0f ? 0f : left;
        }

        public static bool Gone(float hp)
        {
            return hp <= 0f;
        }

        public static bool Coat(Renderer renderer)
        {
            if (renderer == null) return false;
            var shader = Shader.Find("OutpostZero/Glass");
            if (shader == null) return false;
            var mat = new Material(shader);
            mat.SetColor("_Color", Tint);
            renderer.sharedMaterial = mat;
            return true;
        }
    }

    public class GlassPane : MonoBehaviour
    {
        private float hp = PaneGlass.Hp;
        private bool gone;

        public float Current => hp;

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, GameObject attacker)
        {
            if (gone) return;
            hp = PaneGlass.After(hp, amount);
            if (!PaneGlass.Gone(hp)) return;
            gone = true;
            GameplayFeedback.Toast(OutpostZero.Shell.Loc.T("pane.break"));
            OutpostZero.Shell.AudioManager.Instance?.PlayAt("splinter", transform.position, 0.55f);
            if (NoiseManager.Instance != null)
                NoiseManager.Instance.EmitNoise(transform.position, PaneGlass.Noise, 0.8f, NoiseType.ObjectBroken, null);
            GlassShard.Leave(transform.position);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// A brute charge breaks a pane and keeps going. A wall still stops the rush.
    /// </summary>
    public static class PaneCharge
    {
        public const float Hit = 18f;

        public static bool Smashes(string colliderName)
        {
            return colliderName == PaneGlass.Name;
        }

        public static bool Through(float hp, float hit)
        {
            return PaneGlass.Gone(PaneGlass.After(hp, hit));
        }
    }
}
