using UnityEngine;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.Items
{
    /// <summary>
    /// A gun left on the street. Taking it swaps the magazine with the one you already carry, or adds the gun if you have none of that type.
    /// </summary>
    public class GroundWeapon : MonoBehaviour, IInteractable
    {
        [SerializeField] private string weaponId = "rifle_assault";
        [SerializeField] private int magazine = 12;
        [SerializeField] private int reserve = 30;

        public string Prompt
        {
            get
            {
                var spec = WeaponCard.Find(weaponId);
                string name = string.IsNullOrEmpty(spec.Name) ? weaponId : spec.Name;
                return FightSay.Lift(weaponId, name, null);
            }
        }

        public void Configure(string id, int mag, int spare)
        {
            weaponId = string.IsNullOrEmpty(id) ? "rifle_assault" : id;
            magazine = mag < 0 ? 0 : mag;
            reserve = spare < 0 ? 0 : spare;
        }

        public bool CanInteract(PlayerInventory inventory) => inventory != null && !string.IsNullOrEmpty(weaponId);

        public void Interact(PlayerInventory inventory)
        {
            if (!CanInteract(inventory)) return;
            var player = inventory.GetComponent<PlayerController>();
            if (player == null) return;
            if (!player.TakeFromGround(weaponId, magazine, reserve, out string leftId, out int leftMag, out int leftReserve))
            {
                GameplayFeedback.Toast(FightSay.Held(null));
                return;
            }
            if (string.IsNullOrEmpty(leftId))
            {
                var spec = WeaponCard.Find(weaponId);
                GameplayFeedback.Toast(FightSay.Took(weaponId, spec.Name, null));
                Destroy(gameObject);
                return;
            }
            Configure(leftId, leftMag, leftReserve);
            GameplayFeedback.Toast(FightSay.Swap(null));
        }
    }
}
