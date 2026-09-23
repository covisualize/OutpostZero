using System;
using UnityEngine.InputSystem;

namespace OutpostZero.Player
{
    /// <summary>
    /// Which device the player last touched, so prompts name the pad button or the key.
    /// </summary>
    public static class InputGlyphs
    {
        public static bool UsingPad { get; private set; }

        public static void Note(bool padActed, bool keysActed)
        {
            if (padActed) UsingPad = true;
            else if (keysActed) UsingPad = false;
        }

        public static void Poll()
        {
            bool pad = false;
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.leftStick.ReadValue().sqrMagnitude > 0.09f || gamepad.rightStick.ReadValue().sqrMagnitude > 0.09f) pad = true;
                for (int i = 0; !pad && i < PadBindings.Buttons.Length; i++)
                    pad = ExpeditionInput.PadButtonPressed(PadBindings.Buttons[i]);
            }
            bool keys = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.delta.ReadValue().sqrMagnitude > 4f)) keys = true;
            Note(pad, keys);
        }

        public static string PadName(string button)
        {
            switch (button)
            {
                case "South": return "A";
                case "East": return "B";
                case "West": return "X";
                case "North": return "Y";
                case "LeftShoulder": return "LB";
                case "RightShoulder": return "RB";
                case "LeftTrigger": return "LT";
                case "RightTrigger": return "RT";
                case "LeftStick": return "LS";
                case "RightStick": return "RS";
                case "DpadUp": return "D-pad Up";
                case "DpadDown": return "D-pad Down";
                case "DpadLeft": return "D-pad Left";
                case "DpadRight": return "D-pad Right";
                case "Start": return "Menu";
                case "Select": return "View";
                default: return string.IsNullOrEmpty(button) ? "None" : button;
            }
        }

        /// <summary>
        /// The prompt label for a keyboard action: the pad button on the same action while a pad is in use.
        /// </summary>
        public static string Label(ControlBindings.Action action)
        {
            if (UsingPad && Enum.TryParse(action.ToString(), false, out PadBindings.Action pad))
                return PadName(PadBindings.Label(pad));
            return ControlBindings.Label(action);
        }
    }
}
