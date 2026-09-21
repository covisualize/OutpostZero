using System;
using UnityEngine;

namespace OutpostZero.Combat
{
    public static class CombatEvents
    {
        public static event Action<Vector3, Vector3, GameObject> OnHit;
        public static event Action<GameObject, GameObject> OnKill;
        public static event Action<Vector3, WeaponBase> OnShotFired;

        public static void RaiseHit(Vector3 point, Vector3 normal, GameObject target)
        {
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
