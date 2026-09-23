using UnityEngine;
using UnityEngine.InputSystem;

namespace OutpostZero.Player
{
    public enum InputPhase
    {
        Held,
        Pressed,
        Released
    }

    /// <summary>
    /// Raw device state behind <see cref="ExpeditionInput"/>. The game reads the attached devices;
    /// tests swap in <see cref="FakeInputProvider"/> through <see cref="ExpeditionInput.Provider"/>.
    /// </summary>
    public interface IInputProvider
    {
        bool Key(Key key, InputPhase phase);
        bool Pad(string button, InputPhase phase);
        bool Mouse(int button, InputPhase phase);
        bool HasPad { get; }
        bool HasPointer { get; }
        Vector2 LeftStick { get; }
        Vector2 RightStick { get; }
        Vector2 Pointer { get; }
        float Scroll { get; }
    }
}
