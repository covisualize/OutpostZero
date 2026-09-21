using UnityEngine;
using UnityEngine.InputSystem;

namespace OutpostZero.Player
{
    /// <summary>
    /// Gameplay actions read from the Input System, with the legacy axes as a fallback
    /// while Active Input Handling is set to Both.
    /// </summary>
    public static class ExpeditionInput
    {
        public static Vector2 Move
        {
            get
            {
                var pad = Gamepad.current;
                if (pad != null)
                {
                    Vector2 stick = pad.leftStick.ReadValue();
                    if (stick.sqrMagnitude > 0.04f) return Vector2.ClampMagnitude(stick, 1f);
                }

                var keyboard = Keyboard.current;
                if (keyboard == null)
                {
                    return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                }

                float x = (Down(keyboard.dKey) || Down(keyboard.rightArrowKey) ? 1f : 0f) - (Down(keyboard.aKey) || Down(keyboard.leftArrowKey) ? 1f : 0f);
                float y = (Down(keyboard.wKey) || Down(keyboard.upArrowKey) ? 1f : 0f) - (Down(keyboard.sKey) || Down(keyboard.downArrowKey) ? 1f : 0f);

                var value = new Vector2(x, y);
                return value.sqrMagnitude > 1f ? value.normalized : value;
            }
        }

        public static Vector2 AimStick
        {
            get
            {
                var pad = Gamepad.current;
                if (pad == null) return Vector2.zero;
                return pad.rightStick.ReadValue();
            }
        }

        public static Vector2 Pointer
        {
            get
            {
                if (Mouse.current != null) return Mouse.current.position.ReadValue();
                return Input.mousePosition;
            }
        }

        public static float Scroll
        {
            get
            {
                if (Mouse.current != null) return Mouse.current.scroll.ReadValue().y;
                return Input.GetAxis("Mouse ScrollWheel");
            }
        }

        public static bool FireHeld
        {
            get
            {
                var pad = Gamepad.current;
                if (pad != null && (pad.rightTrigger.isPressed || pad.rightShoulder.isPressed)) return true;
                return Mouse.current != null ? Mouse.current.leftButton.isPressed : Input.GetMouseButton(0);
            }
        }

        public static bool AimHeld
        {
            get
            {
                var pad = Gamepad.current;
                if (pad != null && (pad.leftTrigger.isPressed || pad.leftShoulder.isPressed)) return true;
                return Mouse.current != null ? Mouse.current.rightButton.isPressed : Input.GetMouseButton(1);
            }
        }
        public static bool PausePressed => Pressed(Key.Escape) || PadDown(pad => pad.startButton.wasPressedThisFrame);
        public static bool ReloadPressed => Pressed(Key.R) || PadDown(pad => pad.buttonWest.wasPressedThisFrame);
        public static bool InteractPressed => Pressed(Key.E) || PadDown(pad => pad.buttonSouth.wasPressedThisFrame);
        public static bool MedkitPressed => Pressed(Key.Q) || PadDown(pad => pad.buttonNorth.wasPressedThisFrame);
        public static bool FlashlightPressed => Pressed(Key.F);
        public static bool CrouchHeld => Held(Key.C) || Held(Key.LeftCtrl);
        public static bool SprintHeld => Held(Key.LeftShift);
        public static bool InventoryPressed => Pressed(Key.Tab) || Pressed(Key.I) || PadDown(pad => pad.selectButton.wasPressedThisFrame);
        public static bool ThrowPressed => Pressed(Key.G) || PadDown(pad => pad.buttonEast.wasPressedThisFrame);
        public static bool TakedownPressed => Pressed(Key.V);
        public static bool BuildPressed => Pressed(Key.B);

        public static bool WeaponSlotPressed(int index)
        {
            switch (index)
            {
                case 0: return Pressed(Key.Digit1);
                case 1: return Pressed(Key.Digit2);
                case 2: return Pressed(Key.Digit3);
                case 3: return Pressed(Key.Digit4);
                default: return false;
            }
        }

        private static bool Pressed(Key key)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return Input.GetKeyDown(ToLegacy(key));
            return keyboard[key].wasPressedThisFrame;
        }

        private static bool Held(Key key)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return Input.GetKey(ToLegacy(key));
            return keyboard[key].isPressed;
        }

        private static bool Down(UnityEngine.InputSystem.Controls.KeyControl key) => key != null && key.isPressed;

        private static bool PadDown(System.Func<Gamepad, bool> read)
        {
            var pad = Gamepad.current;
            return pad != null && read(pad);
        }

        private static KeyCode ToLegacy(Key key)
        {
            switch (key)
            {
                case Key.Escape: return KeyCode.Escape;
                case Key.R: return KeyCode.R;
                case Key.E: return KeyCode.E;
                case Key.Q: return KeyCode.Q;
                case Key.F: return KeyCode.F;
                case Key.C: return KeyCode.C;
                case Key.LeftCtrl: return KeyCode.LeftControl;
                case Key.LeftShift: return KeyCode.LeftShift;
                case Key.Tab: return KeyCode.Tab;
                case Key.I: return KeyCode.I;
                case Key.G: return KeyCode.G;
                case Key.V: return KeyCode.V;
                case Key.B: return KeyCode.B;
                case Key.Digit1: return KeyCode.Alpha1;
                case Key.Digit2: return KeyCode.Alpha2;
                case Key.Digit3: return KeyCode.Alpha3;
                case Key.Digit4: return KeyCode.Alpha4;
                default: return KeyCode.None;
            }
        }
    }
}
