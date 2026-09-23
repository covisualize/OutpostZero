using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>
    /// The yard's power. A finished generator with fuel makes power, and lamps and turrets draw it, handed out in the
    /// order they were placed until the supply runs out; the rest stay dark. The old scene generator, with no placed
    /// one, still makes one generator's worth.
    /// </summary>
    public static class PowerGrid
    {
        public const int GeneratorOutput = 6;

        public struct Plug
        {
            public string Kind;
            public bool Ready;
        }

        /// <summary>Positive makes power, negative draws it. A loaded module book answers first.</summary>
        public static int Power(string kind)
        {
            return ModuleTable.TryRow(kind, out var row) ? row.Power : CodePower(kind);
        }

        public static int CodePower(string kind)
        {
            switch (kind)
            {
                case "Generator": return GeneratorOutput;
                case "Lamp": return -1;
                case "Turret": return -2;
                default: return 0;
            }
        }

        public static int Supply(IList<Plug> plugs, bool running)
        {
            if (!running) return 0;
            int made = 0;
            if (plugs != null)
            {
                for (int i = 0; i < plugs.Count; i++)
                {
                    int power = Power(plugs[i].Kind);
                    if (plugs[i].Ready && power > 0) made += power;
                }
            }
            return made > 0 ? made : GeneratorOutput;
        }

        public static int Demand(IList<Plug> plugs)
        {
            int drawn = 0;
            if (plugs == null) return 0;
            for (int i = 0; i < plugs.Count; i++)
            {
                int power = Power(plugs[i].Kind);
                if (plugs[i].Ready && power < 0) drawn -= power;
            }
            return drawn;
        }

        /// <summary>Which plugs have power: every ready module that draws none, and each drawing one in turn while the supply lasts.</summary>
        public static bool[] Allot(IList<Plug> plugs, bool running)
        {
            if (plugs == null) return new bool[0];
            var fed = new bool[plugs.Count];
            int left = Supply(plugs, running);
            for (int i = 0; i < plugs.Count; i++)
            {
                if (!plugs[i].Ready) continue;
                int power = Power(plugs[i].Kind);
                if (power >= 0)
                {
                    fed[i] = power == 0 || running;
                    continue;
                }
                if (left < -power) continue;
                left += power;
                fed[i] = true;
            }
            return fed;
        }

        /// <summary>How much of the supply is drawn, for the camp board.</summary>
        public static int Used(IList<Plug> plugs, bool running)
        {
            var fed = Allot(plugs, running);
            int used = 0;
            for (int i = 0; i < fed.Length; i++)
            {
                int power = Power(plugs[i].Kind);
                if (fed[i] && power < 0) used -= power;
            }
            return used;
        }
    }
}
