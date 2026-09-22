using UnityEngine;
using OutpostZero.Combat;
using OutpostZero.Core;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// Shifts clothing, lights the eyes, and tears the body once health drops under 40%.
    /// A ground blob and, on the player, a cyan aim mark keep the silhouette readable.
    /// </summary>
    public class CharacterVariety : MonoBehaviour
    {
        private string role = "walker";
        private int seed = 1;
        private bool wounded;
        private bool painted;
        private Transform eyeL;
        private Transform eyeR;
        private Transform blob;
        private Transform aim;

        public static CharacterVariety Ensure(GameObject host)
        {
            if (host == null) return null;
            var variety = host.GetComponent<CharacterVariety>();
            if (variety == null) variety = host.AddComponent<CharacterVariety>();
            return variety;
        }

        public void Bind(string id, bool reroll)
        {
            role = CharacterLook.RoleOf(id);
            if (reroll || seed == 0) seed = NextSeed();
            EnsureMarks();
            painted = false;
            Paint();
        }

        private void Start()
        {
            painted = false;
        }

        private void LateUpdate()
        {
            var health = GetComponent<HealthSystem>();
            bool hurt = health != null && CharacterLook.Wounded(health.CurrentHealth, health.MaxHealth, GoreLevel());
            if (!painted || hurt != wounded) Paint();
        }

        private void EnsureMarks()
        {
            if (CharacterLook.Glows(role))
            {
                float height = CharacterLook.EyeHeight(role);
                eyeL = EnsureMark(eyeL, "ReadEyeL", PrimitiveType.Sphere, new Vector3(-0.045f, height, 0.16f), 0.04f);
                eyeR = EnsureMark(eyeR, "ReadEyeR", PrimitiveType.Sphere, new Vector3(0.045f, height, 0.16f), 0.04f);
            }
            blob = EnsureMark(blob, "ReadBlob", PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), 0.7f);
            if (blob != null) blob.localScale = new Vector3(0.7f, 0.02f, 0.7f);
            if (role == "survivor")
            {
                aim = EnsureMark(aim, "ReadAim", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0.9f), 0.2f);
                if (aim != null) aim.localScale = new Vector3(0.1f, 0.02f, 0.6f);
            }
        }

        private Transform EnsureMark(Transform existing, string markName, PrimitiveType shape, Vector3 local, float size)
        {
            if (existing != null) return existing;
            var found = transform.Find(markName);
            if (found != null) return found;
            var body = GameObject.CreatePrimitive(shape);
            body.name = markName;
            body.transform.SetParent(transform, false);
            body.transform.localPosition = local;
            body.transform.localScale = Vector3.one * size;
            var collider = body.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var material = Resources.Load<Material>("OutpostTriplanar");
            var renderer = body.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            return body.transform;
        }

        private static int GoreLevel()
        {
            return SettingsService.Instance != null ? SettingsService.Instance.Gore : 1;
        }

        private void Paint()
        {
            painted = true;
            var health = GetComponent<HealthSystem>();
            wounded = health != null && CharacterLook.Wounded(health.CurrentHealth, health.MaxHealth, GoreLevel());
            var tint = CharacterLook.Clothing(seed);
            if (wounded) tint = CharacterLook.Gore(tint);
            var eye = CharacterLook.Eye(role);
            float strength = CharacterLook.Strength(role);
            var renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                string mark = renderer.gameObject.name;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                if (mark == "ReadEyeL" || mark == "ReadEyeR")
                {
                    var glow = new Color(eye.R * strength, eye.G * strength, eye.B * strength, 1f);
                    block.SetColor("_BaseColor", glow);
                    block.SetColor("_Emission", glow);
                    block.SetFloat("_HasMaps", 0f);
                }
                else if (mark == "ReadBlob")
                {
                    block.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.02f, 1f));
                    block.SetColor("_Tint", Color.white);
                    block.SetFloat("_Dissolve", 0f);
                }
                else if (mark == "ReadAim")
                {
                    var cyan = new Color(0.25f, 0.9f, 1f, 1f);
                    block.SetColor("_BaseColor", cyan);
                    block.SetColor("_Emission", cyan);
                    block.SetFloat("_HasMaps", 0f);
                }
                else
                {
                    block.SetColor("_Tint", new Color(tint.R, tint.G, tint.B, 1f));
                    block.SetColor("_Emission", wounded ? new Color(0.35f, 0.02f, 0.02f, 1f) : Color.black);
                    block.SetFloat("_Dissolve", wounded ? 0.22f : 0f);
                    block.SetColor("_RimColor", new Color(0.85f, 0.55f, 0.28f, CharacterLook.Glows(role) ? 0.85f : 0.35f));
                    block.SetFloat("_RimPower", CharacterLook.Glows(role) ? 1.6f : 3.2f);
                }
                renderer.SetPropertyBlock(block);
            }
        }

        private static int serial = 1;

        private static int NextSeed()
        {
            serial++;
            if (serial > 100000) serial = 1;
            return serial;
        }
    }
}
