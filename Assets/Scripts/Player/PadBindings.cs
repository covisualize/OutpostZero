using System;
using System.Text;

namespace OutpostZero.Player
{
    /// <summary>
    /// Gamepad buttons for the expedition actions. Keyboard bindings stay separate.
    /// </summary>
    public static class PadBindings
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
            Build,
            Wheel,
            Fire,
            Aim,
            NextWeapon,
            PrevWeapon
        }

        public static readonly string[] Buttons =
        {
            "South",
            "East",
            "West",
            "North",
            "Start",
            "Select",
            "LeftShoulder",
            "RightShoulder",
            "LeftTrigger",
            "RightTrigger",
            "LeftStick",
            "RightStick",
            "DpadUp",
            "DpadDown",
            "DpadLeft",
            "DpadRight"
        };

        private static readonly string[] defaults =
        {
            "Start",
            "West",
            "South",
            "North",
            "DpadUp",
            "RightStick",
            "LeftStick",
            "Select",
            "East",
            "",
            "DpadDown",
            "LeftShoulder",
            "RightTrigger",
            "LeftTrigger",
            "DpadRight",
            "DpadLeft"
        };

        private static readonly string[] current = (string[])defaults.Clone();

        public static int Count => current.Length;

        public static string Label(Action action)
        {
            string name = current[(int)action];
            return string.IsNullOrEmpty(name) ? "None" : name;
        }

        public static string Signature()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < current.Length; i++)
            {
                builder.Append(current[i]);
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
                builder.Append(current[i]);
            }
            return builder.ToString();
        }

        public static void Unpack(string packed)
        {
            ResetDefaults();
            if (string.IsNullOrEmpty(packed)) return;
            string[] parts = packed.Split(',');
            for (int i = 0; i < parts.Length && i < current.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                {
                    current[i] = "";
                    continue;
                }
                if (!Known(parts[i], out string canonical)) continue;
                current[i] = canonical;
            }
        }

        public static void ResetDefaults()
        {
            Array.Copy(defaults, current, defaults.Length);
        }

        public static bool TryRebindNamed(Action action, string token)
        {
            if (!Known(token, out string canonical)) return false;
            int index = (int)action;
            for (int i = 0; i < current.Length; i++)
            {
                if (i != index && current[i] == canonical) return false;
            }
            current[index] = canonical;
            return true;
        }

        private static bool Known(string token, out string canonical)
        {
            canonical = "";
            if (string.IsNullOrEmpty(token)) return false;
            for (int i = 0; i < Buttons.Length; i++)
            {
                if (string.Equals(Buttons[i], token, StringComparison.OrdinalIgnoreCase))
                {
                    canonical = Buttons[i];
                    return true;
                }
            }
            return false;
        }
    }
}
