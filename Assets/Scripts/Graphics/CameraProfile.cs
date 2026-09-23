using System;
using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// Committed camera tuning (Resources/CameraProfile.asset): every shot's pitch, distance, lens and
    /// damping, the aim lead, the overview's scroll and zoom, the cinematic timings and the shake
    /// table. With no asset, <see cref="CameraTuning"/>'s defaults reproduce the shipped feel.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraProfile", menuName = "Outpost Zero/Camera Profile")]
    public class CameraProfile : ScriptableObject
    {
        public const string ResourcePath = "CameraProfile";

        public CameraTuning tuning = new CameraTuning();

        private static CameraTuning cached;

        public static CameraTuning Current
        {
            get
            {
                if (cached == null)
                {
                    var asset = Resources.Load<CameraProfile>(ResourcePath);
                    cached = asset != null && asset.tuning != null ? asset.tuning : new CameraTuning();
                }
                return cached;
            }
        }
    }

    [Serializable]
    public class CameraShot
    {
        [Tooltip("Degrees below the horizon.")] public float pitch;
        [Tooltip("Metres from the target along the view.")] public float distance;
        [Tooltip("Vertical field of view; 0 follows the Field of View setting.")] public float fov;
        [Tooltip("Seconds the body takes to catch up.")] public float damping;

        public CameraShot() { }

        public CameraShot(float pitch, float distance, float fov, float damping)
        {
            this.pitch = pitch;
            this.distance = distance;
            this.fov = fov;
            this.damping = damping;
        }

        public float Lens(float setting) => fov > 0f ? fov : setting;
    }

    [Serializable]
    public class CameraShake
    {
        public string id;
        [Tooltip("Impulse strength at the source.")] public float force;
        [Tooltip("Metres to silence; 0 is the listener's own recoil and never fades.")] public float radius;
        [Tooltip("Seconds the impulse lasts.")] public float duration = 0.2f;
        [Tooltip("recoil, bump, explosion or rumble.")] public string shape = "bump";

        public CameraShake() { }

        public CameraShake(string id, float force, float radius, float duration, string shape)
        {
            this.id = id;
            this.force = force;
            this.radius = radius;
            this.duration = duration;
            this.shape = shape;
        }
    }

    [Serializable]
    public class CameraTuning
    {
        public const string Pistol = "pistol";
        public const string Rifle = "rifle";
        public const string Smg = "smg";
        public const string Shotgun = "shotgun";
        public const string Melee = "melee";
        public const string Explosion = "explosion";
        public const string BruteStomp = "brute_stomp";

        [Header("Expedition")]
        public CameraShot follow = new CameraShot(55.5f, 19.4f, 0f, 0.18f);
        public CameraShot aim = new CameraShot(59f, 17.8f, 42f, 0.12f);
        [Tooltip("Seconds to blend into and out of aim-down-sights.")] public float aimBlend = 0.25f;
        [Tooltip("Share of the way to the cursor the view leads.")] public float leadInfluence = 0.15f;
        [Tooltip("Metres the pointer lead can reach.")] public float leadMax = 4.5f;
        [Tooltip("Metres a full right-stick tilt leads.")] public float stickLead = 4f;
        [Tooltip("Metres of skyline the view may show past the fence.")] public float edgeSlack = 8f;

        [Header("Camp overview")]
        public CameraShot camp = new CameraShot(65f, 30f, 50f, 0.3f);
        public float campMinDistance = 18f;
        public float campMaxDistance = 44f;
        public float campZoomStep = 3f;
        [Tooltip("Share of the screen edge that scrolls the overview.")] public float edgeMargin = 0.03f;
        [Tooltip("Metres per second at the very edge.")] public float edgeSpeed = 14f;
        [Tooltip("Metres the overview may wander from the leader.")] public float campRange = 24f;

        [Header("Cinematics")]
        public CameraShot death = new CameraShot(68f, 11f, 30f, 0.6f);
        [Tooltip("Seconds of the slow zoom onto a fallen leader.")] public float deathZoom = 2.5f;
        public CameraShot pullBack = new CameraShot(50f, 34f, 50f, 0.8f);
        [Tooltip("Seconds the extraction pull-back holds before the results wide shot.")] public float pullBackHold = 2f;
        public CameraShot wide = new CameraShot(72f, 46f, 45f, 1f);
        [Tooltip("Seconds of each blend in the results sequence.")] public float resultsBlend = 1.5f;

        [Header("Shake")]
        public List<CameraShake> shakes = new List<CameraShake>
        {
            new CameraShake(Pistol, 0.12f, 0f, 0.12f, "recoil"),
            new CameraShake(Smg, 0.1f, 0f, 0.08f, "recoil"),
            new CameraShake(Rifle, 0.18f, 0f, 0.15f, "recoil"),
            new CameraShake(Shotgun, 0.4f, 0f, 0.22f, "recoil"),
            new CameraShake(Melee, 0.08f, 0f, 0.1f, "bump"),
            new CameraShake(Explosion, 0.9f, 24f, 0.6f, "explosion"),
            new CameraShake(BruteStomp, 0.45f, 12f, 0.3f, "bump"),
        };

        public CameraShake Shake(string id)
        {
            if (shakes == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < shakes.Count; i++)
                if (shakes[i] != null && shakes[i].id == id) return shakes[i];
            return null;
        }

        public static string ShotShake(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Melee: return Melee;
                case WeaponType.Shotgun: return Shotgun;
                case WeaponType.Rifle: return Rifle;
                case WeaponType.SMG: return Smg;
                default: return Pistol;
            }
        }

        /// <summary>Impulse force reaching a listener, after distance falloff and the Screen Shake setting.</summary>
        public float Force(string id, float distance, float setting)
        {
            var row = Shake(id);
            if (row == null || setting <= 0f) return 0f;
            return CameraMath.Attenuate(row.force, distance, row.radius) * setting;
        }
    }
}
