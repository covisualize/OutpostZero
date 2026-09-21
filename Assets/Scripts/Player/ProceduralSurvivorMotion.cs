using UnityEngine;

namespace OutpostZero.Player
{
    /// <summary>
    /// Crouch scale and step bob for the leader mesh. The exported characters are still static meshes,
    /// so locomotion reads as motion until humanoid clips exist.
    /// </summary>
    public class ProceduralSurvivorMotion : MonoBehaviour
    {
        private PlayerController controller;
        private CharacterController body;
        private Transform visual;
        private float bob;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            body = GetComponent<CharacterController>();
        }

        private void LateUpdate()
        {
            if (visual == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.name.Contains("Survivor") || child.name.Contains("Mesh") || child.name.Contains("Visual"))
                    {
                        visual = child;
                        break;
                    }
                }
                if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            }
            if (visual == null || controller == null) return;

            float speed = body != null ? body.velocity.magnitude : 0f;
            bob += speed * Time.deltaTime * 2.2f;
            float crouch = controller.IsCrouching ? 0.72f : 1f;
            float hop = speed > 0.2f ? Mathf.Sin(bob) * 0.04f : 0f;
            visual.localScale = new Vector3(1f, crouch, 1f);
            visual.localPosition = new Vector3(0f, hop, 0f);
        }
    }
}
