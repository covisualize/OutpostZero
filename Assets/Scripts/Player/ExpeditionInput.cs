using UnityEngine;
using UnityEngine.InputSystem;

namespace OutpostZero.Player
{
    /// <summary>
    /// Gameplay actions read from the Input System only. With no keyboard, mouse or pad attached,
    /// every action reads as idle and the pointer sits at the screen centre.
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
                if (keyboard == null) return Vector2.zero;

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
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            }
        }

        public static float Scroll
        {
            get
            {
                if (Mouse.current != null) return Mouse.current.scroll.ReadValue().y;
                return 0f;
            }
        }

        public static bool FireHeld
        {
            get
            {
                if (PadHeld(PadBindings.Action.Fire)) return true;
                return Mouse.current != null ? Mouse.current.leftButton.isPressed : false;
            }
        }

        public static bool FirePressed
        {
            get
            {
                if (PadDown(PadBindings.Action.Fire)) return true;
                return Mouse.current != null ? Mouse.current.leftButton.wasPressedThisFrame : false;
            }
        }

        public static bool AimHeld
        {
            get
            {
                if (PadHeld(PadBindings.Action.Aim)) return true;
                return Mouse.current != null ? Mouse.current.rightButton.isPressed : false;
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
        public static bool WatchPressed => Pressed(Key.F3);
        public static bool DevPressed => Pressed(Key.F9);
        public static bool RosterPressed => Pressed(Key.F4);
        public static bool BuildPressed => Pressed(ControlBindings.Action.Build) || PadDown(PadBindings.Action.Build);
        public static bool DodgePressed => Pressed(Key.Space) || PadDown(PadBindings.Action.Dodge);

        public static bool WheelHeld
        {
            get
            {
                if (PadHeld(PadBindings.Action.Wheel)) return true;
                if (Mouse.current != null && Mouse.current.middleButton.isPressed) return true;
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
            if (keyboard == null || key == Key.None) return false;
            return keyboard[key].wasPressedThisFrame;
        }

        private static bool Held(Key key)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || key == Key.None) return false;
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
                case "DpadUp": return pad.dpad.up;
                case "DpadDown": return pad.dpad.down;
                case "DpadLeft": return pad.dpad.left;
                case "DpadRight": return pad.dpad.right;
                default: return null;
            }
        }
    }
}
