using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Sensory;
using OutpostZero.Shell;

namespace OutpostZero.Player
{
    public enum LootKind
    {
        Medkit,
        Ammo9mm,
        AmmoShotgun,
        Scrap
    }

    /// <summary>
    /// Minimal world pickup used by the prototype arena. The ItemDefinition interaction system replaces this in M1.
    /// </summary>
    public class LootPickup : MonoBehaviour
    {
        [SerializeField] private LootKind kind = LootKind.Scrap;
        [SerializeField] private int amount = 1;
        [SerializeField] private float spinDegreesPerSecond = 40f;
        [SerializeField] private float bobHeight = 0.12f;
        [SerializeField] private float bobSpeed = 2.2f;

        private Vector3 baseLocalPosition;
        private bool collected;

        public LootKind Kind => kind;
        public int Amount => amount;

        public void Configure(LootKind lootKind, int lootAmount)
        {
            kind = lootKind;
            amount = Mathf.Max(1, lootAmount);
        }

        private void Awake()
        {
            baseLocalPosition = transform.localPosition;
            EnsureTrigger();
            GameLayers.ApplyRecursively(gameObject, GameLayers.Loot);
        }

        public static LootPickup Spawn(LootKind lootKind, int lootAmount, Vector3 worldPosition)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Loot_" + lootKind;
            go.transform.position = worldPosition + Vector3.up * 0.45f;
            go.transform.localScale = Vector3.one * 0.35f;

            var pickup = go.AddComponent<LootPickup>();
            pickup.Configure(lootKind, lootAmount);
            return pickup;
        }

        private void EnsureTrigger()
        {
            var colliders = GetComponentsInChildren<Collider>();
            if (colliders.Length == 0)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.size = Vector3.one * 1.2f;
                colliders = new Collider[] { box };
            }

            foreach (var collider in colliders)
            {
                if (collider is MeshCollider mesh)
                {
                    mesh.convex = true;
                }
                collider.isTrigger = true;
            }

            var body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = baseLocalPosition + new Vector3(0f, bob, 0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryCollect(other.GetComponentInParent<PlayerController>());
        }

        private void TryCollect(PlayerController player)
        {
            if (collected || player == null) return;

            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null || !inventory.TryCollect(kind, amount)) return;

            collected = true;
            if (NoiseManager.Instance != null)
                NoiseManager.Instance.EmitNoise(transform.position, LootTake.Noise, 0.4f, NoiseType.ObjectBroken, player.gameObject);
            AudioManager.Instance?.PlayAt(LootTake.Sound(kind), transform.position, LootTake.Volume);
            string language = SettingsService.Instance != null ? SettingsService.Instance.Language : "en";
            GameplayFeedback.Toast(LootTake.Line(kind, amount, language));
            Destroy(gameObject);
        }
    }
}
