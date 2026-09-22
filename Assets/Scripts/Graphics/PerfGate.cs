using System;
using System.Collections.Generic;

namespace OutpostZero.Graphics
{
    /// <summary>What a sampled stretch of play measured, and which thresholds it broke.</summary>
    public sealed class PerfReport
    {
        public int Frames;
        public int Zombies;
        public float BudgetMs;
        public float MedianMs;
        public float P95Ms;
        public float WorstMs;
        public double AllocPerFrame;
        public readonly List<string> Failures = new List<string>();

        public bool Passed => Failures.Count == 0;

        public override string ToString() =>
            $"{Frames} frames, {Zombies} zombies: median {MedianMs:0.0} ms, p95 {P95Ms:0.0} ms, worst {WorstMs:0.0} ms (budget {BudgetMs:0.0} ms), {AllocPerFrame / 1024.0:0.0} KB GC per frame"
            + (Passed ? "" : " -- " + string.Join("; ", Failures));
    }

    /// <summary>
    /// Pass/fail rules for the PlayMode perf test: the median frame fits the quality tier's budget, the slow tail
    /// stays within twice that, no single frame hitches past a quarter second, and steady play barely allocates.
    /// </summary>
    public static class PerfGate
    {
        public const int MinFrames = 120;
        public const float TailShare = 2f;
        public const float HitchMs = 250f;
        public const long AllocPerFrameBytes = 16 * 1024;
        public const int SampleFrames = 600;
        public const int TestZombies = 30;

        public static PerfReport Judge(IReadOnlyList<float> frameMs, IReadOnlyList<long> allocBytes, float budgetMs, int zombies)
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
                counted++;
            }
            report.AllocPerFrame = counted > 0 ? total / (double)counted : 0.0;

            if (report.MedianMs > budgetMs) report.Failures.Add($"median {report.MedianMs:0.0} ms over {budgetMs:0.0}");
            if (report.P95Ms > budgetMs * TailShare) report.Failures.Add($"p95 {report.P95Ms:0.0} ms over {budgetMs * TailShare:0.0}");
            if (report.WorstMs > HitchMs) report.Failures.Add($"hitch {report.WorstMs:0.0} ms over {HitchMs:0}");
            if (report.AllocPerFrame > AllocPerFrameBytes) report.Failures.Add($"{report.AllocPerFrame / 1024.0:0.0} KB GC per frame over {AllocPerFrameBytes / 1024} KB");
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
    }
}
