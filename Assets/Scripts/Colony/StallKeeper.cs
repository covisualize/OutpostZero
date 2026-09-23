using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Plays the merchant's chore clips: Talk while the trade stall is open, Work the rest of the time.
    /// Does nothing on a model whose controller has no Activity parameter.
    /// </summary>
    public class StallKeeper : MonoBehaviour
    {
        public const string ModelId = "NPC_Merchant";

        private Animator animator;
        private bool acts;
        private int shown = -1;

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null) return;
            if (animator.runtimeAnimatorController == null)
                animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(CharacterRig.ResourcePath(ModelId));
            if (animator.runtimeAnimatorController == null) return;
            foreach (var parameter in animator.parameters)
                if (parameter.name == CharacterRig.Activity) acts = true;
        }

        private void Update()
        {
            if (!acts) return;
            int code = CharacterRig.MerchantActivity(FactionTrade.Instance != null && FactionTrade.Instance.Open);
            if (code == shown) return;
            shown = code;
            animator.SetInteger(CharacterRig.Activity, code);
        }
    }
}
