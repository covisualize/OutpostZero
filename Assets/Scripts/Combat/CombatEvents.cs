using System;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Combat
{
    public static class CombatEvents
    {
        public static event Action<Vector3, Vector3, GameObject> OnHit;
        public static event Action<GameObject, GameObject> OnKill;
        public static event Action<Vector3, WeaponBase> OnShotFired;
        public static bool FromWeapon { get; private set; }
        public static WeaponType LastWeapon { get; private set; } = WeaponType.Pistol;

        public static void RaiseHit(Vector3 point, Vector3 normal, GameObject target)
        {
            FromWeapon = false;
            OnHit?.Invoke(point, normal, target);
        }

        public static void RaiseHit(Vector3 point, Vector3 normal, GameObject target, WeaponType weapon)
        {
            FromWeapon = true;
            LastWeapon = weapon;
            OnHit?.Invoke(point, normal, target);
        }

        public static void RaiseKill(GameObject victim, GameObject killer)
        {
            OnKill?.Invoke(victim, killer);
        }

        public static void RaiseShot(Vector3 muzzle, WeaponBase weapon)
        {
            OnShotFired?.Invoke(muzzle, weapon);
        }
    }
}
