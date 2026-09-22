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

        public static bool FirePressed
        {
            get
            {
                var pad = Gamepad.current;
                if (pad != null && (pad.rightTrigger.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame)) return true;
                return Mouse.current != null ? Mouse.current.leftButton.wasPressedThisFrame : Input.GetMouseButtonDown(0);
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
        public static bool PausePressed => Pressed(ControlBindings.Action.Pause) || PadDown(pad => pad.startButton.wasPressedThisFrame);
        public static bool ReloadPressed => Pressed(ControlBindings.Action.Reload) || PadDown(pad => pad.buttonWest.wasPressedThisFrame);
        public static bool InteractPressed => Pressed(ControlBindings.Action.Interact) || PadDown(pad => pad.buttonSouth.wasPressedThisFrame);
        public static bool MedkitPressed => Pressed(ControlBindings.Action.Medkit) || PadDown(pad => pad.buttonNorth.wasPressedThisFrame);
        public static bool FlashlightPressed => Pressed(ControlBindings.Action.Flashlight) || PadDown(pad => pad.dpadUp.wasPressedThisFrame);
        public static bool CrouchHeld => Held(ControlBindings.Action.Crouch) || Held(Key.LeftCtrl) || PadHeld(pad => pad.rightStickButton.isPressed);
        public static bool CrouchPressed => Pressed(ControlBindings.Action.Crouch) || Pressed(Key.LeftCtrl) || PadDown(pad => pad.rightStickButton.wasPressedThisFrame);
        public static bool SprintHeld => Held(ControlBindings.Action.Sprint) || PadHeld(pad => pad.leftStickButton.isPressed);
        public static bool SprintPressed => Pressed(ControlBindings.Action.Sprint) || PadDown(pad => pad.leftStickButton.wasPressedThisFrame);
        public static bool InventoryPressed => Pressed(ControlBindings.Action.Inventory) || Pressed(Key.I) || PadDown(pad => pad.selectButton.wasPressedThisFrame);
        public static bool ThrowPressed => Pressed(ControlBindings.Action.Throw) || PadDown(pad => pad.buttonEast.wasPressedThisFrame);
        public static bool TakedownPressed => Pressed(ControlBindings.Action.Takedown);
        public static bool BuildPressed => Pressed(ControlBindings.Action.Build) || PadDown(pad => pad.dpadDown.wasPressedThisFrame);

        public static int WeaponCycle
        {
            get
            {
                var pad = Gamepad.current;
                if (pad == null) return 0;
                if (pad.dpadRight.wasPressedThisFrame) return 1;
                if (pad.dpadLeft.wasPressedThisFrame) return -1;
                return 0;
            }
        }

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

        public static bool BeltPressed(int index)
        {
            switch (index)
            {
                case 0: return Pressed(Key.Digit5);
                case 1: return Pressed(Key.Digit6);
                case 2: return Pressed(Key.Digit7);
                case 3: return Pressed(Key.Digit8);
                default: return false;
            }
        }

        private static bool Pressed(ControlBindings.Action action) => Pressed(ControlBindings.KeyFor(action));

        private static bool Held(ControlBindings.Action action) => Held(ControlBindings.KeyFor(action));

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

        private static bool PadHeld(System.Func<Gamepad, bool> read)
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
                case Key.Digit5: return KeyCode.Alpha5;
                case Key.Digit6: return KeyCode.Alpha6;
                case Key.Digit7: return KeyCode.Alpha7;
                case Key.Digit8: return KeyCode.Alpha8;
                default: return KeyCode.None;
            }
        }
    }
}
