using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// The Cinemachine rig. A follow camera and an aim-down-sights camera share the
    /// <see cref="CameraTargetDriver"/> proxy and a confiner that keeps the view inside the fence and
    /// its skyline; priority hands off to the ADS shot, the camp overview, the fallen-leader slow zoom
    /// and the extraction pull-back sequence. Shake is a Cinemachine impulse read from the camera
    /// profile, faded with distance and scaled by the Screen Shake setting.
    /// </summary>
    public class ExpeditionCameraRig : MonoBehaviour
    {
        public const int Idle = 0;
        public const int FollowPriority = 10;
        public const int AimPriority = 20;
        public const int CampPriority = 30;
        public const int CinematicPriority = 50;

        public static ExpeditionCameraRig Instance { get; private set; }

        /// <summary>0 in the follow shot, 1 fully in the ADS shot; drives the depth-of-field volume.</summary>
        public static float AimWeight { get; private set; }
        public static float DeathWeight { get; private set; }

        private CinemachineBrain brain;
        private CameraTargetDriver driver;
        private CinemachineCamera followCam;
        private CinemachineCamera aimCam;
        private CinemachineCamera campCam;
        private CinemachineCamera deathCam;
        private CinemachineSequencerCamera results;
        private CinemachinePositionComposer campBody;
        private CinemachinePositionComposer deathBody;
        private CinemachineImpulseSource impulse;
        private BoxCollider bounds;
        private Transform mark;
        private GameManager watching;
        private float deathStarted = -1f;
        private bool resultsLive;

        public CinemachineCamera Live
        {
            get
            {
                if (deathStarted >= 0f) return deathCam;
                if (campCam != null && campCam.Priority.Value == CampPriority) return campCam;
                if (aimCam != null && aimCam.Priority.Value == AimPriority) return aimCam;
                return followCam;
            }
        }

        private void OnEnable()
        {
            Instance = this;
            CombatEvents.OnBlast += OnBlast;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            CombatEvents.OnBlast -= OnBlast;
            if (watching != null) watching.OnGameStateChanged -= OnState;
            watching = null;
        }

        private void Start()
        {
            var tuning = CameraProfile.Current;
            brain = Attach.Ensure<CinemachineBrain>(gameObject);
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.IgnoreTimeScale = true;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, tuning.aimBlend);

            driver = Attach.Ensure<CameraTargetDriver>(gameObject);
            if (driver.Target == null && PlayerRegistry.Current != null) driver.SetTarget(PlayerRegistry.Current.transform);

            bounds = BuildBounds();
            var root = new GameObject("CM_Rig").transform;
            followCam = Shot("CM_Follow", root, tuning.follow, driver.Proxy, FollowPriority, true, out _);
            aimCam = Shot("CM_Aim", root, tuning.aim, driver.Proxy, Idle, true, out _);
            campCam = Shot("CM_Camp", root, tuning.camp, driver.Overview, Idle, false, out campBody);
            mark = new GameObject("CM_Mark").transform;
            deathCam = Shot("CM_Death", root, tuning.death, mark, Idle, false, out deathBody);
            results = BuildResults(root, tuning);

            impulse = gameObject.AddComponent<CinemachineImpulseSource>();
            impulse.ImpulseDefinition.ImpulseChannel = 1;
            impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;

            Watch();
        }

        private void Watch()
        {
            if (watching != null || GameManager.Instance == null) return;
            watching = GameManager.Instance;
            watching.OnGameStateChanged += OnState;
        }

        private static float SettingFov => SettingsService.Instance != null ? SettingsService.Instance.FieldOfView : 55f;

        private CinemachineCamera Shot(string shotName, Transform parent, CameraShot shot, Transform follow, int priority, bool confine, out CinemachinePositionComposer body)
        {
            var go = new GameObject(shotName);
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(shot.pitch, 0f, 0f);
            var cam = go.AddComponent<CinemachineCamera>();
            cam.Follow = follow;
            cam.Priority = priority;
            var lens = cam.Lens;
            lens.FieldOfView = shot.Lens(SettingFov);
            cam.Lens = lens;
            body = go.AddComponent<CinemachinePositionComposer>();
            body.CameraDistance = shot.distance;
            body.Damping = Vector3.one * shot.damping;
            var listener = go.AddComponent<CinemachineImpulseListener>();
            listener.ApplyAfter = CinemachineCore.Stage.Noise;
            listener.ChannelMask = 1;
            listener.Gain = 1f;
            listener.UseCameraSpace = true;
            listener.ReactionSettings = new CinemachineImpulseListener.ImpulseReaction { AmplitudeGain = 1f, FrequencyGain = 1f, Duration = 1f };
            if (confine && bounds != null)
            {
                var confiner = go.AddComponent<CinemachineConfiner3D>();
                confiner.BoundingVolume = bounds;
            }
            return cam;
        }

        private CinemachineSequencerCamera BuildResults(Transform root, CameraTuning tuning)
        {
            var go = new GameObject("CM_Results");
            go.transform.SetParent(root, false);
            var sequence = go.AddComponent<CinemachineSequencerCamera>();
            sequence.Priority = Idle;
            sequence.Loop = false;
            var pull = Shot("CM_PullBack", go.transform, tuning.pullBack, mark, Idle, false, out _);
            var wide = Shot("CM_Wide", go.transform, tuning.wide, mark, Idle, false, out _);
            var blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, tuning.resultsBlend);
            sequence.Instructions = new List<CinemachineSequencerCamera.Instruction>
            {
                new CinemachineSequencerCamera.Instruction { Camera = pull, Blend = blend, Hold = tuning.pullBackHold },
                new CinemachineSequencerCamera.Instruction { Camera = wide, Blend = blend, Hold = 0f },
            };
            return sequence;
        }

        /// <summary>
        /// The confiner volume. It never touches physics: it sits on Ignore Raycast, is a trigger,
        /// and excludes every layer so nothing enters it.
        /// </summary>
        private static BoxCollider BuildBounds()
        {
            var go = new GameObject("CM_Bounds");
            go.layer = 2;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.excludeLayers = ~0;
            return box;
        }

        private void FitBounds(CameraTuning tuning, float fov)
        {
            if (bounds == null) return;
            bool yard = GameObject.Find("MapRim") != null;
            bounds.enabled = yard;
            if (!yard) return;
            float aspect = Screen.height > 0 ? Screen.width / (float)Screen.height : 16f / 9f;
            CameraMath.Confine(MapRim.Open, tuning.follow.pitch, tuning.follow.distance, fov, aspect, tuning.edgeSlack,
                out float halfX, out float minZ, out float maxZ);
            bounds.center = new Vector3(0f, 0f, (minZ + maxZ) * 0.5f);
            bounds.size = new Vector3(Mathf.Max(halfX * 2f, 0.01f), 200f, Mathf.Max(maxZ - minZ, 0.01f));
        }

        private void OnState(GameState state)
        {
            var player = PlayerRegistry.Current;
            Vector3 at = player != null ? player.transform.position : (driver != null && driver.Target != null ? driver.Target.position : transform.position);
            deathStarted = -1f;
            resultsLive = false;
            if (state == GameState.SuccessionScreen || state == GameState.GameOver)
            {
                mark.position = at;
                deathStarted = Time.unscaledTime;
            }
            else if (state == GameState.ExpeditionResults || state == GameState.Victory)
            {
                mark.position = at;
                resultsLive = true;
            }
        }

        private void OnBlast(Vector3 at, float radius)
        {
            Shake(CameraTuning.Explosion, at);
        }

        /// <summary>Fires the profile's shake <paramref name="id"/> from a world point.</summary>
        public static void At(string id, Vector3 at)
        {
            if (Instance != null) Instance.Shake(id, at);
        }

        public void Shake(string id, Vector3 at)
        {
            if (impulse == null) return;
            var tuning = CameraProfile.Current;
            var row = tuning.Shake(id);
            if (row == null) return;
            Vector3 listener = driver != null && driver.Target != null ? driver.Target.position : transform.position;
            float force = tuning.Force(id, Vector3.Distance(at, listener), SettingsService.ShakeScale);
            if (force <= 0.001f) return;
            impulse.ImpulseDefinition.ImpulseShape = ShapeOf(row.shape);
            impulse.ImpulseDefinition.ImpulseDuration = Mathf.Max(0.02f, row.duration);
            Vector3 push = (Vector3.down + Random.insideUnitSphere * 0.6f).normalized * force;
            impulse.GenerateImpulseAt(listener, push);
        }

        public static CinemachineImpulseDefinition.ImpulseShapes ShapeOf(string shape)
        {
            switch (shape)
            {
                case "recoil": return CinemachineImpulseDefinition.ImpulseShapes.Recoil;
                case "explosion": return CinemachineImpulseDefinition.ImpulseShapes.Explosion;
                case "rumble": return CinemachineImpulseDefinition.ImpulseShapes.Rumble;
                default: return CinemachineImpulseDefinition.ImpulseShapes.Bump;
            }
        }

        private void Update()
        {
            if (followCam == null) return;
            Watch();
            var tuning = CameraProfile.Current;
            var player = PlayerRegistry.Current;
            if (driver.Target == null && player != null) driver.SetTarget(player.transform);

            bool camp = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.CampManagement;
            bool dying = deathStarted >= 0f;
            bool aiming = !camp && !dying && !resultsLive && player != null && player.IsAimingDownSights;
            AimWeight = CameraMath.Toward(AimWeight, aiming ? 1f : 0f, tuning.aimBlend, Time.unscaledDeltaTime);

            followCam.Priority = FollowPriority;
            aimCam.Priority = aiming ? AimPriority : Idle;
            campCam.Priority = camp && !dying ? CampPriority : Idle;
            deathCam.Priority = dying ? CinematicPriority : Idle;
            results.Priority = resultsLive ? CinematicPriority : Idle;

            float fov = tuning.follow.Lens(SettingFov);
            SetFov(followCam, fov);
            SetFov(aimCam, tuning.aim.Lens(SettingFov));
            SetFov(campCam, tuning.camp.Lens(SettingFov));
            campBody.CameraDistance = driver.CampDistance;

            DeathWeight = dying ? CameraMath.Ease(Time.unscaledTime - deathStarted, tuning.deathZoom) : 0f;
            if (dying)
            {
                float t = DeathWeight;
                SetFov(deathCam, Mathf.Lerp(fov, tuning.death.Lens(fov), t));
                deathBody.CameraDistance = Mathf.Lerp(tuning.follow.distance, tuning.death.distance, t);
                deathCam.transform.rotation = Quaternion.Euler(Mathf.Lerp(tuning.follow.pitch, tuning.death.pitch, t), 0f, 0f);
            }
            float blend = dying || resultsLive ? tuning.resultsBlend : tuning.aimBlend;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blend);

            FitBounds(tuning, fov);
        }

        private static void SetFov(CinemachineCamera cam, float fov)
        {
            if (cam == null) return;
            var lens = cam.Lens;
            lens.FieldOfView = fov;
            cam.Lens = lens;
        }
    }
}
