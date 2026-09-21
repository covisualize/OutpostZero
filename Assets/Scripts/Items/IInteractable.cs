using OutpostZero.Player;

namespace OutpostZero.Items
{
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract(PlayerInventory inventory);
        void Interact(PlayerInventory inventory);
    }
}
