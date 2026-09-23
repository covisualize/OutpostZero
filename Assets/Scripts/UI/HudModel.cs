using System;
using System.Collections.Generic;
using OutpostZero.Expedition;

namespace OutpostZero.UI
{
    /// <summary>
    /// Every named HUD element, its parent and its USS classes. <c>Assets/UI/Resources/HUD.uxml</c> declares the same
    /// tree, and the controller builds it from this table when the UXML is missing, so both paths expose the same names.
    /// </summary>
    public static class HudTree
    {
        public enum Kind { Box, Text, Radial, Picture }

        public struct Node
        {
            public string Name;
            public string Parent;
            public Kind Kind;
            public string Classes;
            /// <summary>Font size as a multiple of <see cref="HudFit.BaseFont"/>; zero inherits.</summary>
            public float Font;
        }

        public const string Root = "hud-root";
        public const int Slots = 4;

        public static readonly string[] Needs = { "hunger", "thirst", "fatigue" };
        public static readonly string[] Status = { "bleed", "poison", "infect", "adrenaline", "burn", "hungry", "thirsty", "tired", "lamp", "watch" };
        public static readonly string[] Edges = { "top", "right", "bottom", "left" };
        public static readonly string[] Cardinals = { "n", "e", "s", "w" };
        public const int Toasts = 4;

        private static Node[] nodes;

        public static Node[] Nodes
        {
            get
            {
                if (nodes == null) nodes = Build();
                return nodes;
            }
        }

        public static string SlotName(int slot) => "slot-" + slot;
        public static string SlotIcon(int slot) => "slot-" + slot + "-icon";
        public static string SlotKey(int slot) => "slot-" + slot + "-key";
        public static string SlotLabel(int slot) => "slot-" + slot + "-name";
        public static string WheelName(int slot) => "wheel-" + slot;
        public static string ToastName(int index) => "toast-" + index;

        private static Node[] Build()
        {
            var list = new List<Node>();
            void Add(string name, string parent, Kind kind, string classes, float font = 0f)
            {
                list.Add(new Node { Name = name, Parent = parent, Kind = kind, Classes = classes, Font = font });
            }

            Add("veil", Root, Kind.Box, "hud-fill hud-veil");
            Add("vignette", Root, Kind.Box, "hud-fill");
            foreach (var edge in Edges) Add("edge-" + edge, "vignette", Kind.Box, "hud-edge hud-edge-" + edge);
            Add("popups", Root, Kind.Box, "hud-fill");

            Add("vitals", Root, Kind.Box, "hud-panel hud-vitals");
            Add("health-row", "vitals", Kind.Box, "hud-row");
            Add("health-caption", "health-row", Kind.Text, "hud-caption", 0.85f);
            Add("health-value", "health-row", Kind.Text, "hud-value", 1.1f);
            Add("health-max", "health-row", Kind.Text, "hud-dim", 0.85f);
            Add("health-track", "vitals", Kind.Box, "hud-track hud-track-main");
            Add("health-ghost", "health-track", Kind.Box, "hud-bar hud-ghost");
            Add("health-fill", "health-track", Kind.Box, "hud-bar hud-health");
            Add("stamina-track", "vitals", Kind.Box, "hud-track hud-track-thin");
            Add("stamina-fill", "stamina-track", Kind.Box, "hud-bar hud-stamina");
            Add("needs", "vitals", Kind.Box, "hud-row hud-needs");
            foreach (var need in Needs)
            {
                Add("need-" + need, "needs", Kind.Box, "hud-need");
                Add("need-" + need + "-label", "need-" + need, Kind.Text, "hud-caption", 0.85f);
                Add("need-" + need + "-track", "need-" + need, Kind.Box, "hud-track hud-track-mini");
                Add("need-" + need + "-fill", "need-" + need + "-track", Kind.Box, "hud-bar hud-need-fill");
            }
            Add("status", "vitals", Kind.Box, "hud-row hud-status");
            foreach (var flag in Status) Add("status-" + flag, "status", Kind.Text, "hud-pill hud-pill-" + flag, 0.85f);

            Add("objectives", Root, Kind.Box, "hud-panel hud-objectives");
            Add("objective-quota", "objectives", Kind.Text, "hud-line", 0.95f);
            Add("objective-poi", "objectives", Kind.Text, "hud-line", 0.9f);
            Add("objective-board", "objectives", Kind.Text, "hud-line", 0.9f);
            Add("objective-rescue", "objectives", Kind.Text, "hud-line", 0.9f);
            Add("objective-district", "objectives", Kind.Text, "hud-line hud-dim", 0.85f);
            Add("objective-clock", "objectives", Kind.Text, "hud-line hud-dim", 0.85f);
            Add("objective-timer", "objectives", Kind.Text, "hud-line hud-warn", 0.95f);

            Add("compass", Root, Kind.Box, "hud-compass");
            Add("compass-strip", "compass", Kind.Box, "hud-strip");
            foreach (var point in Cardinals) Add("compass-" + point, "compass-strip", Kind.Text, "hud-cardinal", 0.95f);
            Add("compass-poi", "compass-strip", Kind.Text, "hud-marker hud-marker-poi", 0.85f);
            Add("compass-gate", "compass-strip", Kind.Text, "hud-marker hud-marker-gate", 0.85f);
            Add("compass-center", "compass-strip", Kind.Box, "hud-compass-center");

            Add("feed", Root, Kind.Box, "hud-feed");
            Add("kill-feed", "feed", Kind.Text, "hud-line hud-right", 0.9f);
            Add("threats", "feed", Kind.Text, "hud-line hud-right hud-strong", 1f);
            Add("watch", "feed", Kind.Text, "hud-panel hud-watch", 0.85f);

            Add("toasts", Root, Kind.Box, "hud-toasts");
            for (int i = 0; i < Toasts; i++) Add(ToastName(i), "toasts", Kind.Text, "hud-toast", 0.95f);
            Add("subtitle", Root, Kind.Text, "hud-subtitle", 0.95f);
            Add("hurt", Root, Kind.Text, "hud-hurt", 0.9f);
            Add("tutorial", Root, Kind.Text, "hud-tutorial", 0.95f);
            Add("prompt", Root, Kind.Text, "hud-prompt", 0.9f);

            Add("hit-marker", Root, Kind.Box, "hud-hit");
            foreach (var edge in Edges) Add("hit-" + edge, "hit-marker", Kind.Box, "hud-hit-tick hud-hit-" + edge);

            Add("meters", Root, Kind.Box, "hud-panel hud-meters");
            Add("noise-row", "meters", Kind.Box, "hud-row");
            Add("noise-label", "noise-row", Kind.Text, "hud-caption", 0.85f);
            Add("noise-track", "noise-row", Kind.Box, "hud-track hud-track-meter");
            Add("noise-fill", "noise-track", Kind.Box, "hud-bar");
            Add("noise-mark", "noise-row", Kind.Text, "hud-caption hud-strong", 0.85f);
            Add("exposure-row", "meters", Kind.Box, "hud-row");
            Add("exposure-label", "exposure-row", Kind.Text, "hud-caption", 0.85f);
            Add("exposure-track", "exposure-row", Kind.Box, "hud-track hud-track-meter");
            Add("exposure-fill", "exposure-track", Kind.Box, "hud-bar hud-exposure");
            Add("tension-row", "meters", Kind.Box, "hud-row");
            Add("tension-beat", "tension-row", Kind.Box, "hud-beat");
            Add("tension-label", "tension-row", Kind.Text, "hud-caption", 0.85f);

            Add("loadout", Root, Kind.Box, "hud-panel hud-loadout");
            Add("ammo-row", "loadout", Kind.Box, "hud-row hud-ammo");
            Add("reload-radial", "ammo-row", Kind.Radial, "hud-radial");
            Add("ammo-mag", "ammo-row", Kind.Text, "hud-mag", 2.2f);
            Add("ammo-split", "ammo-row", Kind.Text, "hud-dim", 1.2f);
            Add("ammo-reserve", "ammo-row", Kind.Text, "hud-reserve", 1.2f);
            Add("weapon-name", "loadout", Kind.Text, "hud-line hud-right", 0.9f);
            Add("slots", "loadout", Kind.Box, "hud-row hud-slots");
            for (int i = 0; i < Slots; i++)
            {
                Add(SlotName(i), "slots", Kind.Box, "hud-slot");
                Add(SlotIcon(i), SlotName(i), Kind.Picture, "hud-slot-icon");
                Add(SlotKey(i), SlotName(i), Kind.Text, "hud-slot-key", 0.85f);
                Add(SlotLabel(i), SlotName(i), Kind.Text, "hud-slot-name", 0.85f);
            }
            Add("belt", "loadout", Kind.Text, "hud-line hud-right hud-dim", 0.85f);

            Add("wheel", Root, Kind.Box, "hud-wheel");
            for (int i = 0; i < Slots; i++) Add(WheelName(i), "wheel", Kind.Text, "hud-wheel-slot hud-wheel-" + i, 1f);
            return list.ToArray();
        }

        /// <summary>The UXML text for this tree; <c>HUD.uxml</c> must equal it.</summary>
        public static string Uxml()
        {
            var text = new System.Text.StringBuilder();
            text.Append("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" editor-extension-mode=\"False\">\n");
            text.Append("    <Style src=\"HUD.uss\" />\n");
            text.Append("    <ui:VisualElement name=\"").Append(Root).Append("\" class=\"hud\" picking-mode=\"Ignore\">\n");
            Write(text, Root, 2);
            text.Append("    </ui:VisualElement>\n");
            text.Append("</ui:UXML>\n");
            return text.ToString();
        }

        private static void Write(System.Text.StringBuilder text, string parent, int depth)
        {
            foreach (var node in Nodes)
            {
                if (node.Parent != parent) continue;
                string pad = new string(' ', depth * 4);
                string tag = node.Kind == Kind.Text ? "ui:Label" : "ui:VisualElement";
                text.Append(pad).Append('<').Append(tag).Append(" name=\"").Append(node.Name).Append("\" class=\"").Append(node.Classes).Append("\" picking-mode=\"Ignore\"");
                bool parentOfAny = false;
                foreach (var child in Nodes) if (child.Parent == node.Name) { parentOfAny = true; break; }
                if (!parentOfAny)
                {
                    text.Append(" />\n");
                    continue;
                }
                text.Append(">\n");
                Write(text, node.Name, depth + 1);
                text.Append(pad).Append("</").Append(tag).Append(">\n");
            }
        }

        /// <summary>The smallest font multiple any text node uses.</summary>
        public static float SmallestFont()
        {
            float smallest = 1f;
            foreach (var node in Nodes)
            {
                if (node.Kind == Kind.Text && node.Font > 0f && node.Font < smallest) smallest = node.Font;
            }
            return smallest;
        }
    }

    /// <summary>
    /// The HUD panel scales with screen height from a 1920×1080 reference so ultrawide keeps element sizes,
    /// and the interface-size setting multiplies on top. The side columns and the compass must fit side by side.
    /// </summary>
    public static class HudFit
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;
        public const float BaseFont = 18f;
        public const float MinReadablePixels = 10f;
        public const float Margin = 16f;
        public const float LeftColumn = 340f;
        public const float RightColumn = 300f;
        public const float Compass = 480f;

        public static float Scale(float screenHeight, float uiScale)
        {
            if (screenHeight <= 0f) return 1f;
            return screenHeight / ReferenceHeight * (uiScale <= 0f ? 1f : uiScale);
        }

        public static float LogicalWidth(float screenWidth, float screenHeight, float uiScale)
        {
            return screenWidth / Scale(screenHeight, uiScale);
        }

        public static float Pixels(float fontMultiple, float textScale, float screenHeight, float uiScale)
        {
            return BaseFont * fontMultiple * (textScale <= 0f ? 1f : textScale) * Scale(screenHeight, uiScale);
        }

        public static bool Fits(float screenWidth, float screenHeight, float uiScale)
        {
            return LogicalWidth(screenWidth, screenHeight, uiScale) >= LeftColumn + RightColumn + Compass + Margin * 4f;
        }

        public static bool Readable(float screenHeight, float uiScale, float textScale)
        {
            return Pixels(HudTree.SmallestFont(), textScale, screenHeight, uiScale) >= MinReadablePixels;
        }
    }

    /// <summary>Numbers the HUD shows, formatted once so a changing counter never allocates.</summary>
    public static class HudNumbers
    {
        public const int Cached = 1000;
        private static readonly string[] table = new string[Cached];

        public static string Of(int value)
        {
            if (value < 0) value = 0;
            if (value >= Cached) return value.ToString();
            return table[value] ?? (table[value] = value.ToString());
        }
    }

    /// <summary>
    /// Pickup and event toasts stack newest on top, at most <see cref="HudTree.Toasts"/>, each for
    /// <see cref="Life"/> seconds. A repeat of the newest line refreshes it instead of stacking.
    /// </summary>
    public sealed class ToastStack
    {
        public const float Life = 2.4f;
        public const float FadeTail = 0.4f;

        private readonly string[] lines;
        private readonly float[] until;
        private int count;

        public ToastStack(int capacity = HudTree.Toasts)
        {
            lines = new string[capacity];
            until = new float[capacity];
        }

        public int Count => count;
        public int Capacity => lines.Length;
        public int Version { get; private set; }

        /// <summary>Newest first.</summary>
        public string Line(int index) => index >= 0 && index < count ? lines[index] : null;

        public void Push(string text, float now)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (count > 0 && lines[0] == text)
            {
                until[0] = now + Life;
                return;
            }
            int keep = count < lines.Length ? count : lines.Length - 1;
            for (int i = keep; i > 0; i--)
            {
                lines[i] = lines[i - 1];
                until[i] = until[i - 1];
            }
            lines[0] = text;
            until[0] = now + Life;
            count = keep + 1;
            Version++;
        }

        public bool Prune(float now)
        {
            int before = count;
            while (count > 0 && now > until[count - 1])
            {
                lines[count - 1] = null;
                count--;
            }
            if (count != before) Version++;
            return count != before;
        }

        public float Alpha(int index, float now)
        {
            if (index < 0 || index >= count) return 0f;
            float left = until[index] - now;
            if (left <= 0f) return 0f;
            return left >= FadeTail ? 1f : left / FadeTail;
        }

        public void Clear()
        {
            for (int i = 0; i < count; i++) lines[i] = null;
            if (count > 0) Version++;
            count = 0;
        }
    }

    /// <summary>
    /// The damage vignette lights the screen edge a hit came from: top is in front, right is to the right.
    /// A hit between two edges splits across both.
    /// </summary>
    public static class DamageEdges
    {
        public const float Hold = 0.9f;

        public static float Weight(int edge, float incoming)
        {
            float centre = edge * 90f;
            float off = Math.Abs(StreetHeading.Wrap(incoming - centre));
            if (off >= 90f) return 0f;
            return (float)Math.Cos(off * Math.PI / 180.0);
        }

        public static float Fade(float age, float strength)
        {
            if (age < 0f || age >= Hold) return 0f;
            float left = 1f - age / Hold;
            float s = strength < 0f ? 0f : strength > 1f ? 1f : strength;
            return left * left * s;
        }

        /// <summary>Damage as a share of max health, floored so a scratch still shows.</summary>
        public static float Strength(float amount, float maxHealth)
        {
            if (amount <= 0f) return 0f;
            float share = amount / (maxHealth <= 1f ? 1f : maxHealth);
            float s = 0.35f + share * 2.5f;
            return s > 1f ? 1f : s;
        }
    }

    /// <summary>The compass strip shows ±90° around the facing; markers beyond it pin to the nearer edge.</summary>
    public static class CompassMarks
    {
        public static readonly float[] CardinalYaw = { 0f, 90f, 180f, 270f };

        public static float Cardinal(int index, float faceYaw)
        {
            return StreetHeading.Wrap(CardinalYaw[index] - faceYaw);
        }

        public static bool Place(float delta, float halfWidth, out float x)
        {
            if (StreetHeading.OnStrip(delta, halfWidth, out x)) return true;
            x = delta > 0f ? halfWidth : -halfWidth;
            return false;
        }

        /// <summary>Cardinal letters fade toward the strip ends.</summary>
        public static float Fade(float delta)
        {
            float span = Math.Abs(delta);
            if (span >= StreetHeading.Half) return 0f;
            float t = span / StreetHeading.Half;
            return 1f - t * t;
        }
    }

    /// <summary>A subtle lub-dub in the tension pip while the horde director sits at Peak.</summary>
    public static class Heartbeat
    {
        public const float Period = 60f / 76f;
        public const float Swell = 0.35f;

        public static float Pulse(float time, bool peak)
        {
            if (!peak) return 0f;
            float phase = time % Period;
            if (phase < 0f) phase += Period;
            float lub = Bump(phase, 0f);
            float dub = Bump(phase, 0.2f) * 0.6f;
            return lub > dub ? lub : dub;
        }

        public static float Scale(float time, bool peak) => 1f + Swell * Pulse(time, peak);

        private static float Bump(float phase, float at)
        {
            float d = (phase - at) / 0.06f;
            return (float)Math.Exp(-d * d);
        }
    }

    /// <summary>The hit marker pops and fades; a kill holds longer and opens wider.</summary>
    public static class HitPip
    {
        public const float HitLife = 0.18f;
        public const float KillLife = 0.4f;

        public static float Alpha(float age, bool kill)
        {
            float life = kill ? KillLife : HitLife;
            if (age < 0f || age >= life) return 0f;
            return 1f - age / life;
        }

        public static float Spread(float age, bool kill)
        {
            float open = kill ? 14f : 8f;
            float t = age <= 0f ? 0f : age >= 0.08f ? 1f : age / 0.08f;
            return open + (kill ? 6f : 3f) * t;
        }
    }

    /// <summary>The reload ring sweeps clockwise from twelve o'clock.</summary>
    public static class ReloadArc
    {
        public static float Sweep(float fill)
        {
            if (fill <= 0f) return 0f;
            return fill >= 1f ? 360f : fill * 360f;
        }

        /// <summary>Redraw only when the sweep moves a visible step.</summary>
        public static bool Moved(float shown, float fill) => Math.Abs(Sweep(fill) - shown) >= 3f;
    }
}
