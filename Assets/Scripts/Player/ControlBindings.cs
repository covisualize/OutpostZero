using System;
using System.Text;
using UnityEngine.InputSystem;

namespace OutpostZero.Player
{
    /// <summary>
    /// Keyboard bindings for the expedition actions. Gamepad buttons stay fixed.
    /// </summary>
    public static class ControlBindings
    {
        public enum Action
        {
            Pause,
            Reload,
            Interact,
            Medkit,
            Flashlight,
            Crouch,
            Sprint,
            Inventory,
            Throw,
            Takedown,
            Build
        }

        private static readonly Key[] defaults =
        {
            Key.Escape,
            Key.R,
            Key.E,
            Key.Q,
            Key.F,
            Key.C,
            Key.LeftShift,
            Key.Tab,
            Key.G,
            Key.V,
            Key.B
        };

        private static readonly Key[] current = (Key[])defaults.Clone();

        public static int Count => current.Length;

        public static Key KeyFor(Action action) => current[(int)action];

        public static string Label(Action action) => Name(KeyFor(action));

        /// <summary>
        /// Keys the game reads directly: movement, dodge, the weapon and quick-slot rows, the wheel, and debug keys.
        /// </summary>
        public static readonly Key[] Reserved =
        {
            Key.W, Key.A, Key.S, Key.D, Key.Space, Key.I, Key.Z, Key.F3, Key.F4, Key.F9,
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8
        };

        public enum RebindResult
        {
            Bound,
            Taken,
            Reserved,
            Invalid
        }

        public static bool IsReserved(Key key) => Array.IndexOf(Reserved, key) >= 0;

        /// <summary>
        /// The other action already on this key, or -1.
        /// </summary>
        public static int Holder(Action action, Key key)
        {
            for (int i = 0; i < current.Length; i++)
            {
                if (i != (int)action && current[i] == key) return i;
            }
            return -1;
        }

        public static RebindResult Check(Action action, Key key)
        {
            if (key == Key.None) return RebindResult.Invalid;
            if (IsReserved(key)) return RebindResult.Reserved;
            if (Holder(action, key) >= 0) return RebindResult.Taken;
            return RebindResult.Bound;
        }

        public static string Name(Key key)
        {
            if (key >= Key.Digit1 && key <= Key.Digit0) return key == Key.Digit0 ? "0" : ((int)(key - Key.Digit1) + 1).ToString();
            switch (key)
            {
                case Key.LeftShift: return "Shift";
                case Key.LeftCtrl: return "Ctrl";
                case Key.LeftAlt: return "Alt";
                case Key.Escape: return "Esc";
                case Key.Backquote: return "`";
                default: return key.ToString();
            }
        }

        public static string Signature()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < current.Length; i++)
            {
                builder.Append((int)current[i]);
                builder.Append('.');
            }
            return builder.ToString();
        }

        public static string Pack()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < current.Length; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append((int)current[i]);
            }
            return builder.ToString();
        }

        public static void Unpack(string packed)
        {
            ResetDefaults();
            if (string.IsNullOrEmpty(packed)) return;
            var parts = packed.Split(',');
            for (int i = 0; i < parts.Length && i < current.Length; i++)
            {
                if (!int.TryParse(parts[i], out int value)) continue;
                if (!Enum.IsDefined(typeof(Key), value)) continue;
                var key = (Key)value;
                if (key == Key.None || IsReserved(key)) continue;
                current[i] = key;
            }
        }

        public static void ResetDefaults()
        {
            Array.Copy(defaults, current, defaults.Length);
        }

        public static bool TryRebind(Action action, Key key)
        {
            if (Check(action, key) != RebindResult.Bound) return false;
            current[(int)action] = key;
            return true;
        }

        public static bool TryRebindNamed(Action action, string token)
        {
            if (string.IsNullOrEmpty(token) || !Enum.TryParse(token, true, out Key key)) return false;
            return TryRebind(action, key);
        }
    }
}
