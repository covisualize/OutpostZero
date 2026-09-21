using System;
using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Sensory;

namespace OutpostZero.Combat
{
    public abstract class WeaponBase : MonoBehaviour
    {
        [Header("General Info")]
        [SerializeField] protected string weaponName = "Default Weapon";
        [SerializeField] protected WeaponType weaponType = WeaponType.Pistol;
        [SerializeField] protected Sprite icon;

        [Header("Combat Stats")]
        [SerializeField] protected float baseDamage = 25f;
        [SerializeField] protected float attackRate = 2f; // Attacks per second
        [SerializeField] protected float range = 25f;
        [SerializeField] protected float staminaCost = 0f;

        [Header("Acoustic Signature")]
        [SerializeField] protected float noiseRadius = 15f;
        [SerializeField] protected float noiseIntensity = 1f;
        [SerializeField] protected NoiseType noiseType = NoiseType.GunshotQuiet;

        protected float nextAttackTime = 0f;
        protected Transform ownerTransform;
        protected GameObject ownerGameObject;

        public string WeaponName => weaponName;
        public WeaponType Type => weaponType;
        public Sprite Icon => icon;
        public float NoiseRadius => noiseRadius;

        public event Action OnAttackFired;

        public virtual void Initialize(Transform owner)
        {
            ownerTransform = owner;
            ownerGameObject = owner != null ? owner.gameObject : null;
        }

        public virtual bool CanAttack()
        {
            return Time.time >= nextAttackTime;
        }

        public abstract bool TryAttack(Vector3 targetDirection);

        protected void EmitWeaponNoise()
        {
            if (NoiseManager.Instance != null && noiseRadius > 0f)
            {
                Vector3 origin = ownerTransform != null ? ownerTransform.position : transform.position;
                NoiseManager.Instance.EmitNoise(origin, noiseRadius, noiseIntensity, noiseType, ownerGameObject);
            }
        }

        protected void TriggerAttackEvent()
        {
            OnAttackFired?.Invoke();
        }
    }
}
