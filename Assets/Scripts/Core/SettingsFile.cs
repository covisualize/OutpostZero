using System;
using System.Globalization;
using System.Text;

namespace OutpostZero.Core
{
    /// <summary>
    /// Flat settings document for persistentDataPath/settings.json.
    /// Missing keys keep the defaults. A written zero stays zero.
    /// </summary>
    public static class SettingsFile
    {
        public const string FileName = "settings.json";

        public struct Snapshot
        {
            public float shake;
            public float master;
            public float sfx;
            public float music;
            public float ambience;
            public float ui;
            public float text;
            public float fov;
            public float opacity;
            public float brightness;
            public int colorblind;
            public int quality;
            public int vsync;
            public int difficulty;
            public int gore;
            public int hitStop;
            public int numbers;
            public int blur;
            public int window;
            public int aim;
            public int invert;
            public int crouch;
            public int sprint;
            public int frame;
            public int resolution;
            public int render;
            public bool subtitles;
            public bool merciful;
            public bool quietFlash;
            public string language;
            public string keys;
            public string pad;
        }

        public static Snapshot Defaults()
        {
            return new Snapshot
            {
                shake = 1f,
                master = 1f,
                sfx = 1f,
                music = 0.7f,
                ambience = 0.8f,
                ui = 1f,
                text = 1f,
                fov = 55f,
                opacity = 1f,
                brightness = 1f,
                colorblind = 0,
                quality = 1,
                vsync = 1,
                difficulty = 2,
                gore = 1,
                hitStop = 1,
                numbers = 1,
                blur = 0,
                window = 0,
                aim = 0,
                invert = 0,
                crouch = 0,
                sprint = 0,
                frame = 0,
                resolution = 0,
                render = 0,
                subtitles = true,
                merciful = false,
                quietFlash = false,
                language = "en",
                keys = "",
                pad = ""
            };
        }

        public static string ToJson(Snapshot snap)
        {
            var builder = new StringBuilder(512);
            builder.Append('{');
            Num(builder, "shake", snap.shake, true);
            Num(builder, "master", snap.master, false);
            Num(builder, "sfx", snap.sfx, false);
            Num(builder, "music", snap.music, false);
            Num(builder, "ambience", snap.ambience, false);
            Num(builder, "ui", snap.ui, false);
            Num(builder, "text", snap.text, false);
            Num(builder, "fov", snap.fov, false);
            Num(builder, "opacity", snap.opacity, false);
            Num(builder, "brightness", snap.brightness, false);
            Int(builder, "colorblind", snap.colorblind);
            Int(builder, "quality", snap.quality);
            Int(builder, "vsync", snap.vsync);
            Int(builder, "difficulty", snap.difficulty);
            Int(builder, "gore", snap.gore);
            Int(builder, "hitStop", snap.hitStop);
            Int(builder, "numbers", snap.numbers);
            Int(builder, "blur", snap.blur);
            Int(builder, "window", snap.window);
            Int(builder, "aim", snap.aim);
            Int(builder, "invert", snap.invert);
            Int(builder, "crouch", snap.crouch);
            Int(builder, "sprint", snap.sprint);
            Int(builder, "frame", snap.frame);
            Int(builder, "resolution", snap.resolution);
            Int(builder, "render", snap.render);
            Int(builder, "subtitles", snap.subtitles ? 1 : 0);
            Int(builder, "merciful", snap.merciful ? 1 : 0);
            Int(builder, "flash", snap.quietFlash ? 1 : 0);
            Str(builder, "language", snap.language);
            Str(builder, "keys", snap.keys);
            Str(builder, "pad", snap.pad);
            builder.Append('}');
            return builder.ToString();
        }

        public static bool TryFromJson(string json, out Snapshot snap)
        {
            snap = Defaults();
            if (string.IsNullOrEmpty(json) || json.IndexOf("\"shake\"", StringComparison.Ordinal) < 0) return false;
            snap.shake = Num(json, "shake", snap.shake);
            snap.master = Num(json, "master", snap.master);
            snap.sfx = Num(json, "sfx", snap.sfx);
            snap.music = Num(json, "music", snap.music);
            snap.ambience = Num(json, "ambience", snap.ambience);
            snap.ui = Num(json, "ui", snap.ui);
            snap.text = Num(json, "text", snap.text);
            snap.fov = Num(json, "fov", snap.fov);
            snap.opacity = Num(json, "opacity", snap.opacity);
            snap.brightness = Num(json, "brightness", snap.brightness);
            snap.colorblind = (int)Num(json, "colorblind", snap.colorblind);
            snap.quality = (int)Num(json, "quality", snap.quality);
            snap.vsync = (int)Num(json, "vsync", snap.vsync);
            snap.difficulty = (int)Num(json, "difficulty", snap.difficulty);
            snap.gore = (int)Num(json, "gore", snap.gore);
            snap.hitStop = (int)Num(json, "hitStop", snap.hitStop);
            snap.numbers = (int)Num(json, "numbers", snap.numbers);
            snap.blur = (int)Num(json, "blur", snap.blur);
            snap.window = (int)Num(json, "window", snap.window);
            snap.aim = (int)Num(json, "aim", snap.aim);
            snap.invert = (int)Num(json, "invert", snap.invert);
            snap.crouch = (int)Num(json, "crouch", snap.crouch);
            snap.sprint = (int)Num(json, "sprint", snap.sprint);
            snap.frame = (int)Num(json, "frame", snap.frame);
            snap.resolution = (int)Num(json, "resolution", snap.resolution);
            snap.render = (int)Num(json, "render", snap.render);
            snap.subtitles = Num(json, "subtitles", 1f) != 0f;
            snap.merciful = Num(json, "merciful", 0f) != 0f;
            snap.quietFlash = Num(json, "flash", 0f) != 0f;
            snap.language = Str(json, "language", snap.language);
            snap.keys = Str(json, "keys", snap.keys);
            snap.pad = Str(json, "pad", snap.pad);
            return true;
        }

        private static void Num(StringBuilder builder, string key, float value, bool first)
        {
            if (!first) builder.Append(',');
            builder.Append('"').Append(key).Append("\":");
            builder.Append(value.ToString("0.####", CultureInfo.InvariantCulture));
        }

        private static void Int(StringBuilder builder, string key, int value)
        {
            builder.Append(",\"").Append(key).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void Str(StringBuilder builder, string key, string value)
        {
            builder.Append(",\"").Append(key).Append("\":\"");
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    if (c == '\\' || c == '"') builder.Append('\\');
                    builder.Append(c);
                }
            }
            builder.Append('"');
        }

        private static float Num(string json, string key, float fallback)
        {
            string token = "\"" + key + "\":";
            int at = json.IndexOf(token, StringComparison.Ordinal);
            if (at < 0) return fallback;
            int start = at + token.Length;
            int end = start;
            while (end < json.Length && json[end] != ',' && json[end] != '}') end++;
            string raw = json.Substring(start, end - start).Trim();
            if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)) return fallback;
            return value;
        }

        private static string Str(string json, string key, string fallback)
        {
            string token = "\"" + key + "\":\"";
            int at = json.IndexOf(token, StringComparison.Ordinal);
            if (at < 0) return fallback ?? "";
            int start = at + token.Length;
            var builder = new StringBuilder();
            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    builder.Append(json[i + 1]);
                    i++;
                    continue;
                }
                if (c == '"') return builder.ToString();
                builder.Append(c);
            }
            return fallback ?? "";
        }
    }
}
