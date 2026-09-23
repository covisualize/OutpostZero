using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OutpostZero.Graphics
{
    /// <summary>What a sampled stretch of play measured, and which thresholds it broke.</summary>
    public sealed class PerfReport
    {
        public string Scenario = "";
        public string Tier = "";
        public float Seconds;
        public int Frames;
        public int Zombies;
        public int Shots;
        public int Kills;
        public float BudgetMs;
        public float MedianMs;
        public float P95Ms;
        public float WorstMs;
        public double AllocPerFrame;
        public long AllocWorst;
        public int DrawCallsP95;
        public int DrawCallsWorst;
        public int SetPassP95;
        public readonly List<string> Failures = new List<string>();

        public bool Passed => Failures.Count == 0;

        public override string ToString() =>
            $"{Frames} frames, {Zombies} zombies: median {MedianMs:0.0} ms, p95 {P95Ms:0.0} ms, worst {WorstMs:0.0} ms (budget {BudgetMs:0.0} ms), {AllocPerFrame / 1024.0:0.0} KB GC per frame, {DrawCallsP95} draw calls p95"
            + (Passed ? "" : " -- " + string.Join("; ", Failures));

        /// <summary>Flat JSON for the CI artifact; invariant culture so a German runner still writes dots.</summary>
        public string ToJson()
        {
            var c = CultureInfo.InvariantCulture;
            var b = new StringBuilder(512);
            b.Append("{\n");
            b.Append("  \"scenario\": \"").Append(Escape(Scenario)).Append("\",\n");
            b.Append("  \"tier\": \"").Append(Escape(Tier)).Append("\",\n");
            b.Append("  \"passed\": ").Append(Passed ? "true" : "false").Append(",\n");
            b.Append("  \"seconds\": ").Append(Seconds.ToString("0.0", c)).Append(",\n");
            b.Append("  \"frames\": ").Append(Frames.ToString(c)).Append(",\n");
            b.Append("  \"zombies\": ").Append(Zombies.ToString(c)).Append(",\n");
            b.Append("  \"shots\": ").Append(Shots.ToString(c)).Append(",\n");
            b.Append("  \"kills\": ").Append(Kills.ToString(c)).Append(",\n");
            b.Append("  \"budgetMs\": ").Append(BudgetMs.ToString("0.00", c)).Append(",\n");
            b.Append("  \"medianMs\": ").Append(MedianMs.ToString("0.00", c)).Append(",\n");
            b.Append("  \"p95Ms\": ").Append(P95Ms.ToString("0.00", c)).Append(",\n");
            b.Append("  \"worstMs\": ").Append(WorstMs.ToString("0.00", c)).Append(",\n");
            b.Append("  \"gcBytesPerFrame\": ").Append(AllocPerFrame.ToString("0", c)).Append(",\n");
            b.Append("  \"gcBytesWorstFrame\": ").Append(AllocWorst.ToString(c)).Append(",\n");
            b.Append("  \"drawCallsP95\": ").Append(DrawCallsP95.ToString(c)).Append(",\n");
            b.Append("  \"drawCallsWorst\": ").Append(DrawCallsWorst.ToString(c)).Append(",\n");
            b.Append("  \"setPassP95\": ").Append(SetPassP95.ToString(c)).Append(",\n");
            b.Append("  \"failures\": [");
            for (int i = 0; i < Failures.Count; i++)
            {
                if (i > 0) b.Append(", ");
                b.Append('"').Append(Escape(Failures[i])).Append('"');
            }
            b.Append("]\n}\n");
            return b.ToString();
        }

        private static string Escape(string text) => (text ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    /// <summary>
    /// Pass/fail rules for the PlayMode perf test: the median frame fits the quality tier's budget, the slow tail
    /// stays within twice that, no single frame hitches past a quarter second, steady play barely allocates,
    /// and the busiest frames stay under <see cref="DrawCallBudget"/> draw calls.
    /// </summary>
    public static class PerfGate
    {
        public const int MinFrames = 120;
        public const float TailShare = 2f;
        public const float HitchMs = 250f;
        public const long AllocPerFrameBytes = 16 * 1024;
        public const int DrawCallBudget = 300;
        public const int SampleFrames = 600;
        public const int TestZombies = 30;
        public const string ReportFolder = "Artifacts/Perf";
        public const string ReportFile = "perf-report.json";

        public static PerfReport Judge(IReadOnlyList<float> frameMs, IReadOnlyList<long> allocBytes, float budgetMs, int zombies) =>
            Judge(frameMs, allocBytes, null, null, budgetMs, zombies);

        public static PerfReport Judge(IReadOnlyList<float> frameMs, IReadOnlyList<long> allocBytes, IReadOnlyList<int> drawCalls, IReadOnlyList<int> setPass, float budgetMs, int zombies)
        {
            var report = new PerfReport { BudgetMs = budgetMs, Zombies = zombies, Frames = frameMs?.Count ?? 0 };
            if (report.Frames < MinFrames)
            {
                report.Failures.Add($"only {report.Frames} frames sampled, need {MinFrames}");
                return report;
            }
            var sorted = new float[report.Frames];
            for (int i = 0; i < sorted.Length; i++) sorted[i] = frameMs[i];
            Array.Sort(sorted);
            report.MedianMs = Percentile(sorted, 0.5f);
            report.P95Ms = Percentile(sorted, 0.95f);
            report.WorstMs = sorted[sorted.Length - 1];
            long total = 0;
            int counted = 0;
            for (int i = 0; allocBytes != null && i < allocBytes.Count; i++)
            {
                total += allocBytes[i];
                if (allocBytes[i] > report.AllocWorst) report.AllocWorst = allocBytes[i];
                counted++;
            }
            report.AllocPerFrame = counted > 0 ? total / (double)counted : 0.0;
            report.DrawCallsP95 = IntPercentile(drawCalls, 0.95f, out report.DrawCallsWorst);
            report.SetPassP95 = IntPercentile(setPass, 0.95f, out _);

            if (report.MedianMs > budgetMs) report.Failures.Add($"median {report.MedianMs:0.0} ms over {budgetMs:0.0}");
            if (report.P95Ms > budgetMs * TailShare) report.Failures.Add($"p95 {report.P95Ms:0.0} ms over {budgetMs * TailShare:0.0}");
            if (report.WorstMs > HitchMs) report.Failures.Add($"hitch {report.WorstMs:0.0} ms over {HitchMs:0}");
            if (report.AllocPerFrame > AllocPerFrameBytes) report.Failures.Add($"{report.AllocPerFrame / 1024.0:0.0} KB GC per frame over {AllocPerFrameBytes / 1024} KB");
            if (report.DrawCallsP95 > DrawCallBudget) report.Failures.Add($"draw calls p95 {report.DrawCallsP95} over {DrawCallBudget}");
            return report;
        }

        /// <summary>Nearest-rank percentile of an ascending array.</summary>
        public static float Percentile(float[] sorted, float share)
        {
            if (sorted == null || sorted.Length == 0) return 0f;
            int rank = (int)Math.Ceiling(share * sorted.Length) - 1;
            if (rank < 0) rank = 0;
            if (rank >= sorted.Length) rank = sorted.Length - 1;
            return sorted[rank];
        }

        private static int IntPercentile(IReadOnlyList<int> values, float share, out int worst)
        {
            worst = 0;
            if (values == null || values.Count == 0) return 0;
            var sorted = new int[values.Count];
            for (int i = 0; i < sorted.Length; i++) sorted[i] = values[i];
            Array.Sort(sorted);
            worst = sorted[sorted.Length - 1];
            int rank = (int)Math.Ceiling(share * sorted.Length) - 1;
            if (rank < 0) rank = 0;
            return sorted[rank];
        }
    }

    /// <summary>
    /// The perf test's scripted firefight: sixty seconds against a crowd held at <see cref="PerfGate.TestZombies"/>,
    /// cycling the loadout so every muzzle, impact, and gore effect lands in the sample.
    /// </summary>
    public static class FirefightPlan
    {
        public const float Seconds = 60f;
        public const float WarmSeconds = 3f;
        public const float SwitchEvery = 12f;
        public const float RefillEvery = 0.5f;
        public const int ReserveFloor = 30;
        public const int ReserveTopUp = 90;

        public static int SlotAt(float elapsed, int slots)
        {
            if (slots <= 1 || elapsed <= 0f) return 0;
            return (int)(elapsed / SwitchEvery) % slots;
        }

        public static int Refill(int alive, int target) => alive >= target ? 0 : target - alive;

        public static int TopUp(int reserve) => reserve < ReserveFloor ? ReserveTopUp : 0;

        public static bool Done(float elapsed) => elapsed >= Seconds;
    }
}
