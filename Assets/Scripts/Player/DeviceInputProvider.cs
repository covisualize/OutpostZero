using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace OutpostZero.Player
{
    /// <summary>The current keyboard, mouse and gamepad, read through the Input System.</summary>
    public sealed class DeviceInputProvider : IInputProvider
    {
        public bool HasPad => Gamepad.current != null;
        public bool HasPointer => UnityEngine.InputSystem.Mouse.current != null;
        public Vector2 LeftStick => Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
        public Vector2 RightStick => Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
        public Vector2 Pointer => UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero;
        public float Scroll => UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y : 0f;

        public bool Key(Key key, InputPhase phase)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || key == UnityEngine.InputSystem.Key.None) return false;
            return Read(keyboard[key], phase);
        }

        public bool Pad(string button, InputPhase phase)
        {
            var pad = Gamepad.current;
            return pad != null && Read(Button(pad, button), phase);
        }

        public bool Mouse(int button, InputPhase phase)
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return false;
            return Read(button == 0 ? mouse.leftButton : button == 1 ? mouse.rightButton : button == 2 ? mouse.middleButton : null, phase);
        }

        private static bool Read(ButtonControl control, InputPhase phase)
        {
            if (control == null) return false;
            switch (phase)
            {
                case InputPhase.Pressed: return control.wasPressedThisFrame;
                case InputPhase.Released: return control.wasReleasedThisFrame;
                default: return control.isPressed;
            }
        }

        private static ButtonControl Button(Gamepad pad, string name)
        {
            switch (name)
            {
                case "South": return pad.buttonSouth;
                case "East": return pad.buttonEast;
                case "West": return pad.buttonWest;
                case "North": return pad.buttonNorth;
                case "Start": return pad.startButton;
                case "Select": return pad.selectButton;
                case "LeftShoulder": return pad.leftShoulder;
                case "RightShoulder": return pad.rightShoulder;
                case "LeftTrigger": return pad.leftTrigger;
                case "RightTrigger": return pad.rightTrigger;
                case "LeftStick": return pad.leftStickButton;
                case "RightStick": return pad.rightStickButton;
                case "DpadUp": return pad.dpad.up;
                case "DpadDown": return pad.dpad.down;
                case "DpadLeft": return pad.dpad.left;
                case "DpadRight": return pad.dpad.right;
                default: return null;
            }
        }
    }
}
