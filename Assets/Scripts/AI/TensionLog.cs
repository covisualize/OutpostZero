using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OutpostZero.AI
{
    /// <summary>
    /// The tension curve of one street, sampled at a fixed step, for the pacing check and its CSV.
    /// </summary>
    public class TensionLog
    {
        public struct Sample
        {
            public float Time;
            public float Tension;
            public TensionState State;
            public int Alive;
        }

        private readonly List<Sample> samples = new List<Sample>();

        public IReadOnlyList<Sample> Samples => samples;

        public void Add(float time, float tension, TensionState state, int alive)
        {
            samples.Add(new Sample { Time = time, Tension = tension, State = state, Alive = alive });
        }

        public float Min()
        {
            float min = float.MaxValue;
            for (int i = 0; i < samples.Count; i++) if (samples[i].Tension < min) min = samples[i].Tension;
            return samples.Count == 0 ? 0f : min;
        }

        public float Max()
        {
            float max = float.MinValue;
            for (int i = 0; i < samples.Count; i++) if (samples[i].Tension > max) max = samples[i].Tension;
            return samples.Count == 0 ? 0f : max;
        }

        /// <summary>Peaks counted as climbs into Peak from any lower state.</summary>
        public int Peaks()
        {
            int peaks = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i].State != TensionState.Peak) continue;
                if (i == 0 || samples[i - 1].State != TensionState.Peak) peaks++;
            }
            return peaks;
        }

        /// <summary>True when the curve reached Peak and later fell back to Relax or Calm.</summary>
        public bool Ebbs()
        {
            bool peaked = false;
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i].State == TensionState.Peak) peaked = true;
                else if (peaked && (samples[i].State == TensionState.Relax || samples[i].State == TensionState.Calm)) return true;
            }
            return false;
        }

        public string Csv()
        {
            var text = new StringBuilder("seconds,tension,state,alive\n");
            for (int i = 0; i < samples.Count; i++)
            {
                var s = samples[i];
                text.Append(s.Time.ToString("0.0", CultureInfo.InvariantCulture)).Append(',')
                    .Append(s.Tension.ToString("0.0", CultureInfo.InvariantCulture)).Append(',')
                    .Append(s.State).Append(',')
                    .Append(s.Alive).Append('\n');
            }
            return text.ToString();
        }
    }
}
