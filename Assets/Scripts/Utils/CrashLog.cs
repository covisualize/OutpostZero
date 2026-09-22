using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace OutpostZero.Utils
{
    /// <summary>File names, rotation and line format for the session log. No Unity calls, so it is testable.</summary>
    public static class LogRotation
    {
        public const string Stem = "outpost";
        public const int Keep = 5;
        public const long Cap = 2L * 1024 * 1024;

        /// <summary>outpost.log is the live session; outpost.1.log is the one before, up to outpost.{Keep-1}.log.</summary>
        public static string Name(int index) => index <= 0 ? Stem + ".log" : Stem + "." + index + ".log";

        /// <summary>The renames that push every log one slot older, oldest first so nothing is overwritten.</summary>
        public static List<KeyValuePair<string, string>> Moves(ICollection<string> present, int keep)
        {
            var moves = new List<KeyValuePair<string, string>>();
            if (keep < 1) keep = 1;
            for (int i = keep - 2; i >= 0; i--)
            {
                string from = Name(i);
                if (present != null && present.Contains(from)) moves.Add(new KeyValuePair<string, string>(from, Name(i + 1)));
            }
            return moves;
        }

        public static bool ShouldRoll(long size, long cap) => cap > 0 && size >= cap;

        public static bool Serious(LogType type) => type == LogType.Exception || type == LogType.Error || type == LogType.Assert;

        public static string Line(DateTime utc, LogType type, string message, string stack)
        {
            var text = new StringBuilder();
            text.Append(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")).Append(" [").Append(type).Append("] ").Append((message ?? "").TrimEnd());
            if (Serious(type) && !string.IsNullOrEmpty(stack))
            {
                foreach (var frame in stack.Split('\n'))
                {
                    string trimmed = frame.TrimEnd('\r', ' ');
                    if (trimmed.Length > 0) text.Append("\n    ").Append(trimmed);
                }
            }
            return text.Append('\n').ToString();
        }

        /// <summary>A session ended badly if it logged an exception or never wrote its closing line (a crash or a kill).</summary>
        public static bool Crashed(string log)
        {
            if (string.IsNullOrEmpty(log)) return false;
            bool faulted = log.Contains("[" + LogType.Exception + "]");
            bool closed = log.TrimEnd().EndsWith(Closing, StringComparison.Ordinal);
            return faulted || !closed;
        }

        public const string Closing = "session closed";
    }

    /// <summary>
    /// Mirrors every log message to persistentDataPath/Logs/outpost.log, keeps the last five sessions,
    /// and rolls mid-session at 2 MB. Crash upload is opt-in and has no endpoint yet, so it only reports what it would send.
    /// </summary>
    public static class CrashLog
    {
        public const string OptInKey = "outpost.crash_upload";

        private static readonly object Gate = new object();
        private static StreamWriter writer;
        private static string directory;
        private static long written;

        public static string Directory => directory ?? "";
        public static string Current => directory != null ? Path.Combine(directory, LogRotation.Name(0)) : "";
        public static string Previous => directory != null ? Path.Combine(directory, LogRotation.Name(1)) : "";
        public static bool PreviousCrashed { get; private set; }

        public static bool UploadOptIn
        {
            get => PlayerPrefs.GetInt(OptInKey, 0) == 1;
            set => PlayerPrefs.SetInt(OptInKey, value ? 1 : 0);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (writer != null) return;
            try
            {
                directory = Path.Combine(Application.persistentDataPath, "Logs");
                System.IO.Directory.CreateDirectory(directory);
                Roll();
                PreviousCrashed = File.Exists(Previous) && LogRotation.Crashed(File.ReadAllText(Previous));
                Open();
                Application.logMessageReceivedThreaded += Record;
                Application.quitting += Close;
                Write(LogRotation.Line(DateTime.UtcNow, LogType.Log, "session opened " + Application.version + " " + Application.platform, null));
                if (PreviousCrashed) Debug.LogWarning("[CrashLog] The last session ended badly. Its log is at " + Previous + (UploadOptIn ? "." : ". Crash upload is off."));
            }
            catch (Exception exception)
            {
                writer = null;
                Debug.LogWarning("[CrashLog] Disabled: " + exception.Message);
            }
        }

        /// <summary>Placeholder until there is a crash endpoint: says whether the previous log would be sent.</summary>
        public static bool TryUpload(out string reason)
        {
            if (!UploadOptIn) { reason = "crash upload is off"; return false; }
            if (!PreviousCrashed) { reason = "the last session closed cleanly"; return false; }
            reason = "no crash endpoint is configured; attach " + Previous + " to a bug report";
            return false;
        }

        private static void Record(string message, string stack, LogType type)
        {
            Write(LogRotation.Line(DateTime.UtcNow, type, message, stack));
        }

        private static void Write(string line)
        {
            lock (Gate)
            {
                if (writer == null) return;
                try
                {
                    writer.Write(line);
                    written += line.Length;
                    if (LogRotation.ShouldRoll(written, LogRotation.Cap))
                    {
                        writer.Dispose();
                        writer = null;
                        Roll();
                        Open();
                    }
                }
                catch (Exception)
                {
                    writer = null;
                }
            }
        }

        private static void Roll()
        {
            var present = new HashSet<string>();
            foreach (var path in System.IO.Directory.GetFiles(directory, LogRotation.Stem + "*.log")) present.Add(Path.GetFileName(path));
            string oldest = LogRotation.Name(LogRotation.Keep - 1);
            if (present.Contains(oldest)) File.Delete(Path.Combine(directory, oldest));
            foreach (var move in LogRotation.Moves(present, LogRotation.Keep))
            {
                string to = Path.Combine(directory, move.Value);
                if (File.Exists(to)) File.Delete(to);
                File.Move(Path.Combine(directory, move.Key), to);
            }
        }

        private static void Open()
        {
            writer = new StreamWriter(Current, false, new UTF8Encoding(false)) { AutoFlush = true };
            written = 0;
        }

        private static void Close()
        {
            Write(LogRotation.Line(DateTime.UtcNow, LogType.Log, LogRotation.Closing, null));
            lock (Gate)
            {
                Application.logMessageReceivedThreaded -= Record;
                writer?.Dispose();
                writer = null;
            }
        }
    }
}
