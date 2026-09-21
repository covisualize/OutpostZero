using UnityEngine;
using Unity.Cinemachine;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// Cinemachine follow camera for the expedition. The legacy follow script steps aside
    /// so the brain owns the transform. Aim-down-sights, mouse lead, and shake stay here.
    /// </summary>
    public class ExpeditionCameraRig : MonoBehaviour
    {
        private CinemachineCamera virtualCamera;
        private CinemachineFollow follow;
        private float trauma;

        private void Start()
        {
            var brain = GetComponent<CinemachineBrain>() ?? gameObject.AddComponent<CinemachineBrain>();
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;

            var legacy = GetComponent<TopDownCameraFollow>();
            if (legacy != null) legacy.enabled = false;

            var rig = new GameObject("CM_Expedition");
            virtualCamera = rig.AddComponent<CinemachineCamera>();
            virtualCamera.Follow = PlayerRegistry.Current != null ? PlayerRegistry.Current.transform : null;
            virtualCamera.LookAt = virtualCamera.Follow;
            follow = rig.AddComponent<CinemachineFollow>();
            follow.FollowOffset = new Vector3(0f, 16f, -11f);
            rig.AddComponent<CinemachineHardLookAt>();
        }

        public void AddTrauma(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }

        private void Update()
        {
            if (virtualCamera == null || follow == null) return;
            if (virtualCamera.Follow == null && PlayerRegistry.Current != null)
            {
                virtualCamera.Follow = PlayerRegistry.Current.transform;
                virtualCamera.LookAt = virtualCamera.Follow;
            }

            Vector3 offset = new Vector3(0f, 16f, -11f);
            var player = PlayerRegistry.Current;
            if (player != null && Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(ExpeditionInput.Pointer);
                var plane = new Plane(Vector3.up, player.transform.position);
                if (plane.Raycast(ray, out float enter))
                {
                    Vector3 lead = ray.GetPoint(enter) - player.transform.position;
                    lead.y = 0f;
                    lead = Vector3.ClampMagnitude(lead * 0.15f, 4.5f);
                    offset += lead;
                }
                float fov = player.IsAimingDownSights ? 42f : 55f;
                var lens = virtualCamera.Lens;
                lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, fov, 4f * Time.deltaTime);
                virtualCamera.Lens = lens;
            }

            if (trauma > 0.01f)
            {
                float magnitude = trauma * trauma * 0.6f * SettingsService.ShakeScale;
                offset += new Vector3(
                    (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * magnitude,
                    (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * magnitude,
                    0f);
                trauma = Mathf.Max(0f, trauma - 1.6f * Time.deltaTime);
            }

            follow.FollowOffset = offset;
        }
    }
}
