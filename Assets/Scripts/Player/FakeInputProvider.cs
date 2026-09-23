using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OutpostZero.Player
{
    /// <summary>
    /// Scripted input for tests. Set what is held, then <see cref="Press"/> or <see cref="Release"/>
    /// for one-frame edges; <see cref="EndFrame"/> clears the edges.
    /// </summary>
    public sealed class FakeInputProvider : IInputProvider
    {
        private readonly HashSet<string> held = new HashSet<string>();
        private readonly HashSet<string> pressed = new HashSet<string>();
        private readonly HashSet<string> released = new HashSet<string>();

        public bool HasPad { get; set; }
        public bool HasPointer { get; set; } = true;
        public Vector2 LeftStick { get; set; }
        public Vector2 RightStick { get; set; }
        public Vector2 Pointer { get; set; }
        public float Scroll { get; set; }

        public static string KeyId(Key key) => "key:" + key;
        public static string PadId(string button) => "pad:" + button;
        public static string MouseId(int button) => "mouse:" + button;

        public void Press(string id)
        {
            held.Add(id);
            pressed.Add(id);
        }

        public void Hold(string id) => held.Add(id);

        public void Release(string id)
        {
            held.Remove(id);
            released.Add(id);
        }

        public void EndFrame()
        {
            pressed.Clear();
            released.Clear();
        }

        public bool Key(Key key, InputPhase phase) => Read(KeyId(key), phase);
        public bool Pad(string button, InputPhase phase) => HasPad && Read(PadId(button), phase);
        public bool Mouse(int button, InputPhase phase) => HasPointer && Read(MouseId(button), phase);

        private bool Read(string id, InputPhase phase)
        {
            switch (phase)
            {
                case InputPhase.Pressed: return pressed.Contains(id);
                case InputPhase.Released: return released.Contains(id);
                default: return held.Contains(id);
            }
        }
    }
}
