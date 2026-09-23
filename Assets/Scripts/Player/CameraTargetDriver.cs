using UnityEngine;
using Unity.Cinemachine;
using OutpostZero.Colony;
using OutpostZero.Core;
using OutpostZero.Graphics;

namespace OutpostZero.Player
{
    /// <summary>
    /// Moves the proxies the Cinemachine cameras follow. <see cref="Proxy"/> is the leader plus the
    /// pointer or right-stick lead; <see cref="Overview"/> is the camp camera's point, which trails the
    /// leader until build mode, then scrolls at the screen edge and zooms on the wheel. With no
    /// Cinemachine brain on the camera it drives the camera itself so a bare scene still plays.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class CameraTargetDriver : MonoBehaviour
    {
        [SerializeField] private Transform target;

        private Transform proxy;
        private Transform overview;
        private Camera cam;
        private PlayerController playerController;
        private Vector3 velocity;
        private float campDistance = -1f;
        private bool wasCamp;

        public Transform Proxy => EnsureProxies();
        public Transform Overview
        {
            get
            {
                EnsureProxies();
                return overview;
            }
        }

        public Transform Target => target;
        public float CampDistance => campDistance > 0f ? campDistance : CameraProfile.Current.camp.distance;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        public void SetFollowTarget(Transform followTarget)
        {
            SetTarget(followTarget);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            playerController = newTarget != null ? newTarget.GetComponent<PlayerController>() : null;
        }

        private Transform EnsureProxies()
        {
            if (proxy == null)
            {
                proxy = new GameObject("CameraTarget").transform;
                if (target != null) proxy.position = target.position;
            }
            if (overview == null)
            {
                overview = new GameObject("CampCameraTarget").transform;
                overview.position = proxy.position;
            }
            return proxy;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = PlayerRegistry.Current;
                if (player == null) return;
                SetTarget(player.transform);
            }
            EnsureProxies();
            var tuning = CameraProfile.Current;
            bool camp = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.CampManagement;
            Vector3 home = target.position;

            float leadX = 0f, leadZ = 0f;
            if (!camp) Lead(tuning, home, out leadX, out leadZ);
            proxy.position = new Vector3(home.x + leadX, home.y, home.z + leadZ);

            DriveOverview(tuning, camp, home);
            wasCamp = camp;

            if (GetComponent<CinemachineBrain>() == null) DriveSelf(tuning, camp);
        }

        private void Lead(CameraTuning tuning, Vector3 home, out float x, out float z)
        {
            x = 0f;
            z = 0f;
            Vector2 stick = ExpeditionInput.AimStick;
            if (stick.sqrMagnitude > 0.04f)
            {
                CameraMath.StickLead(stick.x, stick.y, tuning.stickLead, out x, out z);
                return;
            }
            var view = cam != null ? cam : Camera.main;
            if (view == null) return;
            Ray ray = view.ScreenPointToRay(ExpeditionInput.Pointer);
            var ground = new Plane(Vector3.up, home);
            if (!ground.Raycast(ray, out float enter)) return;
            Vector3 toPointer = ray.GetPoint(enter) - home;
            CameraMath.Lead(toPointer.x, toPointer.z, tuning.leadInfluence, tuning.leadMax, out x, out z);
        }

        private void DriveOverview(CameraTuning tuning, bool camp, Vector3 home)
        {
            if (camp && !wasCamp) overview.position = home;
            bool building = camp && GridBuilder.Instance != null && GridBuilder.Instance.BuildMode;
            if (!building)
            {
                overview.position = Vector3.Lerp(overview.position, home, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
                return;
            }

            Vector2 pointer = ExpeditionInput.Pointer;
            CameraMath.EdgeScroll(pointer.x, pointer.y, Screen.width, Screen.height, tuning.edgeMargin, out float sx, out float sz);
            Vector2 stick = ExpeditionInput.AimStick;
            if (stick.sqrMagnitude > 0.04f)
            {
                sx = stick.x;
                sz = stick.y;
            }
            Vector3 next = overview.position + new Vector3(sx, 0f, sz) * tuning.edgeSpeed * Time.unscaledDeltaTime;
            Vector3 away = next - home;
            away.y = 0f;
            away = Vector3.ClampMagnitude(away, tuning.campRange);
            overview.position = new Vector3(home.x + away.x, home.y, home.z + away.z);

            campDistance = CameraMath.Zoom(CampDistance, ExpeditionInput.Scroll, tuning.campZoomStep, tuning.campMinDistance, tuning.campMaxDistance);
        }

        private void DriveSelf(CameraTuning tuning, bool camp)
        {
            var shot = camp ? tuning.camp : tuning.follow;
            float distance = camp ? CampDistance : shot.distance;
            CameraMath.Offset(shot.pitch, distance, out float up, out float back);
            Vector3 focus = camp ? overview.position : proxy.position;
            Vector3 goal = focus + new Vector3(0f, up, -back);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, shot.damping);
            transform.rotation = Quaternion.Euler(shot.pitch, 0f, 0f);
            if (cam == null) return;
            float setting = SettingsService.Instance != null ? SettingsService.Instance.FieldOfView : 55f;
            bool aiming = !camp && playerController != null && playerController.IsAimingDownSights;
            float fov = aiming ? tuning.aim.Lens(setting) : shot.Lens(setting);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, 8f * Time.deltaTime);
        }
    }
}
