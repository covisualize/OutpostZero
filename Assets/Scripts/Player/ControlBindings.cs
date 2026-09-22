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

        public static string Label(Action action) => KeyFor(action).ToString();

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
                if (key == Key.None) continue;
                current[i] = key;
            }
        }

        public static void ResetDefaults()
        {
            Array.Copy(defaults, current, defaults.Length);
        }

        public static bool TryRebind(Action action, Key key)
        {
            if (key == Key.None) return false;
            int index = (int)action;
            for (int i = 0; i < current.Length; i++)
            {
                if (i != index && current[i] == key) return false;
            }
            current[index] = key;
            return true;
        }

        public static bool TryRebindNamed(Action action, string token)
        {
            if (string.IsNullOrEmpty(token) || !Enum.TryParse(token, true, out Key key)) return false;
            return TryRebind(action, key);
        }
    }
}
