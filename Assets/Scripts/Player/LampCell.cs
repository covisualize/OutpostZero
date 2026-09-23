namespace OutpostZero.Player
{
    /// <summary>
    /// The flashlight spends a cell while it is on and fills while it is off.
    /// A full cell is 100. One second on spends 4. One second off puts 2 back.
    /// An old save with no spent field starts full. A dead cell is dark.
    /// </summary>
    public static class LampCell
    {
        public const float Full = 100f;
        public const float Drain = 4f;
        public const float Charge = 2f;
        public const float Peak = 2.8f;
        public const float Pack = 60f;

        public static float Tick(float charge, bool on, float dt)
        {
            if (dt < 0f) dt = 0f;
            if (charge < 0f) charge = 0f;
            if (charge > Full) charge = Full;
            if (on) charge -= Drain * dt;
            else charge += Charge * dt;
            if (charge < 0f) charge = 0f;
            if (charge > Full) charge = Full;
            return charge;
        }

        public static bool Live(float charge)
        {
            return charge > 0.5f;
        }

        public static float Beam(float charge)
        {
            if (!Live(charge)) return 0f;
            float t = charge / Full;
            if (t > 1f) t = 1f;
            return 0.35f + 0.65f * t;
        }

        public static float Intensity(float charge)
        {
            return Peak * Beam(charge);
        }

        public static float Spent(float charge)
        {
            if (charge < 0f) charge = 0f;
            if (charge > Full) charge = Full;
            return Full - charge;
        }

        public static float FromSpent(float spent)
        {
            if (spent < 0f) spent = 0f;
            if (spent > Full) spent = Full;
            return Full - spent;
        }

        public static bool Tops(float charge)
        {
            return charge >= Full;
        }

        public static float Fill(float charge, float amount)
        {
            if (charge < 0f) charge = 0f;
            if (charge > Full) charge = Full;
            if (amount < 0f) amount = 0f;
            float next = charge + amount;
            if (next > Full) return Full;
            return next;
        }
    }
}
