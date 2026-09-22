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
                if (PadHeld(PadBindings.Action.Fire)) return true;
                return Mouse.current != null ? Mouse.current.leftButton.isPressed : Input.GetMouseButton(0);
            }
        }

        public static bool FirePressed
        {
            get
            {
                if (PadDown(PadBindings.Action.Fire)) return true;
                return Mouse.current != null ? Mouse.current.leftButton.wasPressedThisFrame : Input.GetMouseButtonDown(0);
            }
        }

        public static bool AimHeld
        {
            get
            {
                if (PadHeld(PadBindings.Action.Aim)) return true;
                return Mouse.current != null ? Mouse.current.rightButton.isPressed : Input.GetMouseButton(1);
            }
        }
        public static bool PausePressed => Pressed(ControlBindings.Action.Pause) || PadDown(PadBindings.Action.Pause);
        public static bool ReloadPressed => Pressed(ControlBindings.Action.Reload) || PadDown(PadBindings.Action.Reload);
        public static bool InteractPressed => Pressed(ControlBindings.Action.Interact) || PadDown(PadBindings.Action.Interact);
        public static bool MedkitPressed => Pressed(ControlBindings.Action.Medkit) || PadDown(PadBindings.Action.Medkit);
        public static bool FlashlightPressed => Pressed(ControlBindings.Action.Flashlight) || PadDown(PadBindings.Action.Flashlight);
        public static bool CrouchHeld => Held(ControlBindings.Action.Crouch) || Held(Key.LeftCtrl) || PadHeld(PadBindings.Action.Crouch);
        public static bool CrouchPressed => Pressed(ControlBindings.Action.Crouch) || Pressed(Key.LeftCtrl) || PadDown(PadBindings.Action.Crouch);
        public static bool SprintHeld => Held(ControlBindings.Action.Sprint) || PadHeld(PadBindings.Action.Sprint);
        public static bool SprintPressed => Pressed(ControlBindings.Action.Sprint) || PadDown(PadBindings.Action.Sprint);
        public static bool InventoryPressed => Pressed(ControlBindings.Action.Inventory) || Pressed(Key.I) || PadDown(PadBindings.Action.Inventory);
        public static bool ThrowPressed => Pressed(ControlBindings.Action.Throw) || PadDown(PadBindings.Action.Throw);
        public static bool TakedownPressed => Pressed(ControlBindings.Action.Takedown) || PadDown(PadBindings.Action.Takedown);
        public static bool BuildPressed => Pressed(ControlBindings.Action.Build) || PadDown(PadBindings.Action.Build);
        public static bool DodgePressed => Pressed(Key.Space) || PadDown(PadBindings.Action.Dodge);

        public static bool WheelHeld
        {
            get
            {
                if (PadHeld(PadBindings.Action.Wheel)) return true;
                if (Mouse.current != null && Mouse.current.middleButton.isPressed) return true;
                if (Mouse.current == null && Input.GetMouseButton(2)) return true;
                return Held(Key.Z);
            }
        }

        public static int WeaponCycle
        {
            get
            {
                if (PadDown(PadBindings.Action.NextWeapon)) return 1;
                if (PadDown(PadBindings.Action.PrevWeapon)) return -1;
                return 0;
            }
        }

        public static bool PadButtonPressed(string name)
        {
            var pad = Gamepad.current;
            if (pad == null) return false;
            var button = Button(pad, name);
            return button != null && button.wasPressedThisFrame;
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

        private static bool PadDown(PadBindings.Action action) => PadMatch(action, true);

        private static bool PadHeld(PadBindings.Action action) => PadMatch(action, false);

        private static bool PadMatch(PadBindings.Action action, bool down)
        {
            var pad = Gamepad.current;
            if (pad == null) return false;
            var button = Button(pad, PadBindings.Label(action));
            if (button == null) return false;
            return down ? button.wasPressedThisFrame : button.isPressed;
        }

        private static UnityEngine.InputSystem.Controls.ButtonControl Button(Gamepad pad, string name)
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
                case "DpadUp": return pad.dpadUp;
                case "DpadDown": return pad.dpadDown;
                case "DpadLeft": return pad.dpadLeft;
                case "DpadRight": return pad.dpadRight;
                default: return null;
            }
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
                case Key.Z: return KeyCode.Z;
                case Key.Space: return KeyCode.Space;
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
