using UnityEngine;

namespace OutpostZero.Player
{
    public class TopDownCameraFollow : MonoBehaviour
    {
        [Header("Target & Positioning")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 16f, -11f);
        [SerializeField] private float smoothTime = 0.18f;

        [Header("Tactical Lookahead")]
        [SerializeField] private float mouseInfluence = 3.5f;
        [SerializeField] private float maxMouseLeadDistance = 4.5f;

        [Header("Field of View & Zoom")]
        [SerializeField] private float defaultFov = 50f;
        [SerializeField] private float aimFov = 42f;
        [SerializeField] private float zoomSpeed = 8f;

        [Header("Trauma / Camera Shake")]
        [SerializeField] private float traumaDecayRate = 1.4f;
        [SerializeField] private float maxShakeAngle = 2.5f;
        [SerializeField] private float maxShakeOffset = 0.4f;

        private Vector3 currentVelocity;
        private Camera cam;
        private float trauma = 0f;
        private PlayerController playerController;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            if (target == null)
            {
                var player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    target = player.transform;
                    playerController = player;
                }
            }
            else
            {
                playerController = target.GetComponent<PlayerController>();
            }

            transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // 1. Calculate Target Base Position
            Vector3 targetPos = target.position + offset;

            // 2. Mouse Lead / Lookahead
            Vector3 mouseLead = Vector3.zero;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, target.position.y, 0f));

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 mouseWorldPos = ray.GetPoint(enter);
                Vector3 toMouse = mouseWorldPos - target.position;
                toMouse.y = 0f;
                mouseLead = Vector3.ClampMagnitude(toMouse * (mouseInfluence * 0.1f), maxMouseLeadDistance);
            }

            Vector3 finalDest = targetPos + mouseLead;

            // 3. Camera Shake Offset
            Vector3 shakeOffset = Vector3.zero;
            if (trauma > 0.01f)
            {
                float shakeMagnitude = trauma * trauma; // Non-linear shake curve
                shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * maxShakeOffset * shakeMagnitude,
                    (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * maxShakeOffset * shakeMagnitude,
                    0f
                );
                trauma = Mathf.Max(0f, trauma - traumaDecayRate * Time.deltaTime);
            }

            // 4. Smooth Damp Movement
            transform.position = Vector3.SmoothDamp(transform.position, finalDest + shakeOffset, ref currentVelocity, smoothTime);

            // 5. ADS Zoom Handling
            if (playerController != null && cam != null)
            {
                float targetFov = playerController.IsAimingDownSights ? aimFov : defaultFov;
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, zoomSpeed * Time.deltaTime);
            }
        }

        public void AddTrauma(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (newTarget != null)
            {
                playerController = newTarget.GetComponent<PlayerController>();
            }
        }
    }
}
