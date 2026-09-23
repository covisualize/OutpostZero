namespace OutpostZero.AI
{
    /// <summary>
    /// Tension drain shared by the director and the five-minute pacing check.
    /// Two scripted fights, at 25 s and 170 s, push the meter to a peak; quiet drains it.
    /// </summary>
    public static class PressureClock
    {
        public struct Report
        {
            public float Min;
            public float Max;
            public int Calm;
            public int Peak;
            public int Spawns;
        }

        public static float Advance(float tension, TensionState state, float dt, float noise)
        {
            float drain = state == TensionState.Relax ? 4f : 1.2f;
            float next = tension - drain * dt + noise;
            if (next < 0f) next = 0f;
            if (next > 100f) next = 100f;
            return next;
        }

        public static Report Run(int difficulty, float opening, float interval, int tier, bool night = false)
        {
            const int steps = 600;
            const float dt = 0.5f;
            float tension = opening;
            var state = HordeDirector.Evaluate(tension);
            int spawns = 0;
            float timer = 2f;
            float elapsed = 0f;
            float cursor = 0f;
            bool runnersOut = false;
            float min = tension;
            float max = tension;
            int calm = state == TensionState.Calm ? 1 : 0;
            int peak = state == TensionState.Peak ? 1 : 0;
            for (int step = 1; step <= steps; step++)
            {
                float prev = elapsed;
                elapsed = step * dt;
                float noise = 0f;
                if (prev < 25f && elapsed >= 25f) noise += 80f;
                if (prev < 170f && elapsed >= 170f) noise += 80f;
                tension = Advance(tension, state, dt, noise);
                var next = HordeDirector.Evaluate(tension);
                if (next != state)
                {
                    state = next;
                    if (state == TensionState.Calm) calm++;
                    if (state == TensionState.Peak) peak++;
                }
                if (tension < min) min = tension;
                if (tension > max) max = tension;
                for (int n = 0; n < 3; n++)
                {
                    float advanced = HordeSchedule.Advance(elapsed, cursor, tier, out string kind);
                    if (advanced == cursor) break;
                    cursor = advanced;
                    if (!string.IsNullOrEmpty(kind)) spawns += HordeSchedule.Count(kind);
                }
                if (HordeSchedule.RunnerPack(elapsed, night, tier, runnersOut))
                {
                    runnersOut = true;
                    spawns += HordeSchedule.Count("runners");
                }
                timer -= dt;
                if (timer <= 0f)
                {
                    spawns += DifficultyProfile.Batch((int)state, difficulty);
                    timer += interval;
                }
            }
            return new Report { Min = min, Max = max, Calm = calm, Peak = peak, Spawns = spawns };
        }
    }
}
