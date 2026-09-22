using UnityEngine;
using OutpostZero.Colony;
using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// Wooden boards on the street, or a placed camp barricade. A charge that connects breaks the street boards.
    /// </summary>
    public class StreetBoard : MonoBehaviour
    {
        private float integrity = BoardBreak.Wood;
        private bool linked;

        public bool Broken => integrity <= 0f;

        public void LinkToCamp() => linked = true;

        private void Awake()
        {
            if (GetComponent<Collider>() == null && GetComponentInChildren<Collider>() == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }
        }

        public void Strike(float amount)
        {
            if (Broken) return;
            if (linked && GridBuilder.Instance != null)
            {
                if (GridBuilder.Instance.StrikeAt(transform.position.x, transform.position.z, amount)) integrity = 0f;
                return;
            }

            integrity = BoardBreak.Apply(integrity, amount);
            if (!Broken) return;
            var colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
            GameplayFeedback.Toast(FightSay.Boards(null));
        }
    }
}
