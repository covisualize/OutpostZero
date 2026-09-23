using UnityEngine;
using UnityEngine.Rendering.Universal;
using OutpostZero.Combat;
using OutpostZero.Core;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// Shifts clothing, lights the eyes, and spreads the shader's blood mask (heavier once health
    /// drops under 40%). A soft blob shadow decal and, on the player, a faint cyan aim stripe keep
    /// the silhouette readable from the top-down camera.
    /// </summary>
    public class CharacterVariety : MonoBehaviour
    {
        public const float BlobSize = 1.1f;
        public const float BlobOpacity = 0.85f;
        public const float AimLength = 1.6f;
        public const float AimOpacity = 0.55f;
        public const float MarkDepth = 0.5f;

        private string role = "walker";
        private int seed = 1;
        private bool wounded;
        private bool painted;
        private Transform eyeL;
        private Transform eyeR;
        private Transform blob;
        private Transform aim;
        private SettingsService listening;

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

        private void OnEnable()
        {
            listening = SettingsService.Instance;
            if (listening != null) listening.OnChanged += Repaint;
        }

        private void OnDisable()
        {
            if (listening != null) listening.OnChanged -= Repaint;
            listening = null;
        }

        private void Repaint()
        {
            painted = false;
        }

        private void LateUpdate()
        {
            var health = GetComponent<HealthSystem>();
            bool hurt = health != null && CharacterLook.Wounded(health.CurrentHealth, health.MaxHealth, GoreLevel());
            if (!painted || hurt != wounded) Paint();
            if (blob != null) blob.gameObject.SetActive(health == null || health.CurrentHealth > 0f);
        }

        private void EnsureMarks()
        {
            if (CharacterLook.Glows(role))
            {
                float height = CharacterLook.EyeHeight(role);
                eyeL = EnsureEye(eyeL, "ReadEyeL", new Vector3(-0.045f, height, 0.16f));
                eyeR = EnsureEye(eyeR, "ReadEyeR", new Vector3(0.045f, height, 0.16f));
            }
            float girth = role == "brute" ? 1.6f : 1f;
            blob = EnsureDecal(blob, "ReadBlob", DecalAtlas.Blob, Vector3.zero, new Vector2(BlobSize * girth, BlobSize * girth), BlobOpacity);
            if (role == "survivor")
                aim = EnsureDecal(aim, "ReadAim", DecalAtlas.Aim, new Vector3(0f, 0f, 0.45f + AimLength * 0.5f), new Vector2(0.5f, AimLength), AimOpacity);
        }

        private Transform EnsureEye(Transform existing, string markName, Vector3 local)
        {
            if (existing != null) return existing;
            var found = transform.Find(markName);
            if (found != null) return found;
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = markName;
            body.transform.SetParent(transform, false);
            body.transform.localPosition = local;
            body.transform.localScale = Vector3.one * 0.04f;
            var collider = body.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var material = Resources.Load<Material>("OutpostTriplanar");
            var renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (material != null) renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return body.transform;
        }

        /// <summary>
        /// A ground decal from the atlas, projected straight down from just above the feet. The angle
        /// fade keeps it on the floor and off the character's own legs.
        /// </summary>
        private Transform EnsureDecal(Transform existing, string markName, string kind, Vector3 local, Vector2 size, float opacity)
        {
            if (existing != null) return existing;
            var found = transform.Find(markName);
            if (found != null) return found;
            var material = Resources.Load<Material>(DecalAtlas.MaterialPath);
            if (material == null) return null;
            var go = new GameObject(markName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = local + new Vector3(0f, MarkDepth * 0.5f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var projector = go.AddComponent<DecalProjector>();
            projector.material = material;
            projector.scaleMode = DecalScaleMode.InheritFromHierarchy;
            projector.pivot = Vector3.zero;
            projector.size = new Vector3(size.x, size.y, MarkDepth + 0.2f);
            int cell = DecalAtlas.Cell(kind, 0);
            projector.uvScale = new Vector2(DecalAtlas.ScaleU, DecalAtlas.ScaleV);
            projector.uvBias = new Vector2(DecalAtlas.BiasU(cell), DecalAtlas.BiasV(cell));
            projector.fadeFactor = opacity;
            projector.startAngleFade = 25f;
            projector.endAngleFade = 50f;
            projector.drawDistance = GoreMark.Cull;
            return go.transform;
        }

        private static int GoreLevel()
        {
            return SettingsService.Instance != null ? SettingsService.Instance.Gore : 1;
        }

        private void Paint()
        {
            painted = true;
            var health = GetComponent<HealthSystem>();
            int goreLevel = GoreLevel();
            wounded = health != null && CharacterLook.Wounded(health.CurrentHealth, health.MaxHealth, goreLevel);
            var tint = CharacterLook.Clothing(seed);
            float gore = CharacterLook.GoreAmount(role, wounded, goreLevel);
            float goreSeed = CharacterLook.GoreSeed(seed);
            var settings = SettingsService.Instance;
            int vision = settings != null ? settings.ColorblindMode : 0;
            var eye = CharacterLook.Eye(role, vision);
            CharacterLook.Rim(role, vision, settings != null && settings.EnemyOutline, out var rim, out float rimAlpha, out float rimPower);
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
                else
                {
                    block.SetColor("_Tint", new Color(tint.R, tint.G, tint.B, 1f));
                    block.SetFloat("_Gore", gore);
                    block.SetFloat("_GoreSeed", goreSeed);
                    block.SetColor("_RimColor", new Color(rim.R, rim.G, rim.B, rimAlpha));
                    block.SetFloat("_RimPower", rimPower);
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
