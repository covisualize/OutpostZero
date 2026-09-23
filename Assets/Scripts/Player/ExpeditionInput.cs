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
        private static readonly IInputProvider Device = new DeviceInputProvider();
        private static IInputProvider provider;

        /// <summary>Where input comes from. Null restores the attached devices.</summary>
        public static IInputProvider Provider
        {
            get => provider ?? Device;
            set => provider = value;
        }

        public static Vector2 Move
        {
            get
            {
                var source = Provider;
                if (source.HasPad)
                {
                    Vector2 stick = source.LeftStick;
                    if (stick.sqrMagnitude > 0.04f) return Vector2.ClampMagnitude(stick, 1f);
                }

                float x = (Held(Key.D) || Held(Key.RightArrow) ? 1f : 0f) - (Held(Key.A) || Held(Key.LeftArrow) ? 1f : 0f);
                float y = (Held(Key.W) || Held(Key.UpArrow) ? 1f : 0f) - (Held(Key.S) || Held(Key.DownArrow) ? 1f : 0f);

                var value = new Vector2(x, y);
                return value.sqrMagnitude > 1f ? value.normalized : value;
            }
        }

        public static Vector2 AimStick => Provider.HasPad ? Provider.RightStick : Vector2.zero;

        public static Vector2 Pointer => Provider.HasPointer ? Provider.Pointer : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        public static float Scroll => Provider.HasPointer ? Provider.Scroll : 0f;

        public static bool FireHeld => PadHeld(PadBindings.Action.Fire) || Provider.Mouse(0, InputPhase.Held);

        public static bool FirePressed => PadDown(PadBindings.Action.Fire) || Provider.Mouse(0, InputPhase.Pressed);

        public static bool AimHeld => PadHeld(PadBindings.Action.Aim) || Provider.Mouse(1, InputPhase.Held);
        public static bool AimPressed => PadDown(PadBindings.Action.Aim) || Provider.Mouse(1, InputPhase.Pressed);
        public static bool BuildPlacePressed => FirePressed;
        public static bool BuildRemovePressed => AimPressed;
        public static bool BuildTurnPressed => Pressed(Key.R) || PadDown(PadBindings.Action.Reload);
        public static bool PausePressed => Pressed(ControlBindings.Action.Pause) || PadDown(PadBindings.Action.Pause);
        public static bool ReloadPressed => Pressed(ControlBindings.Action.Reload) || PadDown(PadBindings.Action.Reload);
        public static bool InteractPressed => Pressed(ControlBindings.Action.Interact) || PadDown(PadBindings.Action.Interact);
        public static bool InteractHeld => Held(ControlBindings.Action.Interact) || PadHeld(PadBindings.Action.Interact);
        public static bool MedkitPressed => Pressed(ControlBindings.Action.Medkit) || PadDown(PadBindings.Action.Medkit);
        public static bool FlashlightPressed => Pressed(ControlBindings.Action.Flashlight) || PadDown(PadBindings.Action.Flashlight);
        public static bool CrouchHeld => Held(ControlBindings.Action.Crouch) || Held(Key.LeftCtrl) || PadHeld(PadBindings.Action.Crouch);
        public static bool CrouchPressed => Pressed(ControlBindings.Action.Crouch) || Pressed(Key.LeftCtrl) || PadDown(PadBindings.Action.Crouch);
        public static bool SprintHeld => Held(ControlBindings.Action.Sprint) || PadHeld(PadBindings.Action.Sprint);
        public static bool SprintPressed => Pressed(ControlBindings.Action.Sprint) || PadDown(PadBindings.Action.Sprint);
        public static bool InventoryPressed => Pressed(ControlBindings.Action.Inventory) || Pressed(Key.I) || PadDown(PadBindings.Action.Inventory);
        public static bool ThrowPressed => Pressed(ControlBindings.Action.Throw) || PadDown(PadBindings.Action.Throw);
        public static bool ThrowHeld => Held(ControlBindings.Action.Throw) || PadHeld(PadBindings.Action.Throw);
        public static bool ThrowReleased => Released(ControlBindings.KeyFor(ControlBindings.Action.Throw)) || PadUp(PadBindings.Action.Throw);
        public static bool TakedownPressed => Pressed(ControlBindings.Action.Takedown) || PadDown(PadBindings.Action.Takedown);
        public static bool WatchPressed => Pressed(Key.F3);
        public static bool DevPressed => Pressed(Key.F9);
        public static bool RosterPressed => Pressed(Key.F4);
        public static bool BuildPressed => Pressed(ControlBindings.Action.Build) || PadDown(PadBindings.Action.Build);
        public static bool DodgePressed => Pressed(Key.Space) || PadDown(PadBindings.Action.Dodge);

        public static bool WheelHeld => PadHeld(PadBindings.Action.Wheel) || Provider.Mouse(2, InputPhase.Held) || Held(Key.Z);

        public static int WeaponCycle
        {
            get
            {
                if (PadDown(PadBindings.Action.NextWeapon)) return 1;
                if (PadDown(PadBindings.Action.PrevWeapon)) return -1;
                return 0;
            }
        }

        public static bool PadButtonPressed(string name) => Provider.Pad(name, InputPhase.Pressed);

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

        private static bool Pressed(Key key) => Provider.Key(key, InputPhase.Pressed);

        private static bool Held(Key key) => Provider.Key(key, InputPhase.Held);

        private static bool Released(Key key) => Provider.Key(key, InputPhase.Released);

        private static bool PadUp(PadBindings.Action action) => Provider.Pad(PadBindings.Label(action), InputPhase.Released);

        private static bool PadDown(PadBindings.Action action) => Provider.Pad(PadBindings.Label(action), InputPhase.Pressed);

        private static bool PadHeld(PadBindings.Action action) => Provider.Pad(PadBindings.Label(action), InputPhase.Held);
    }
}
