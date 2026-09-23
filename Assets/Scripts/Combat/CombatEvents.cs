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
        /// <summary>An explosion at a point with its damage radius.</summary>
        public static event Action<Vector3, float> OnBlast;
        public static bool FromWeapon { get; private set; }
        public static WeaponType LastWeapon { get; private set; } = WeaponType.Pistol;
        public static float DirX { get; private set; }
        public static float DirY { get; private set; }
        public static float DirZ { get; private set; }

        public static void NoteDir(Vector3 dir)
        {
            DirX = dir.x;
            DirY = dir.y;
            DirZ = dir.z;
        }

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

        public static void RaiseBlast(Vector3 at, float radius)
        {
            OnBlast?.Invoke(at, radius);
        }
    }
}
