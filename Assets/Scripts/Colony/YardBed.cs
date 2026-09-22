using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>
    /// The yard has a voice when the machines are actually running.
    /// A fueled generator hums. A lamp on that power buzzes. A finished fire crackles.
    /// A dark lamp and an unbuilt site stay quiet.
    /// </summary>
    public static class YardBed
    {
        public const float Hum = 0.28f;
        public const float Crackle = 0.22f;
        public const float Buzz = 0.14f;
        public const float Reach = 18f;

        public static void Mix(bool generator, bool fire, bool lamp, out float hum, out float crackle, out float buzz)
        {
            hum = generator ? Hum : 0f;
            crackle = fire ? Crackle : 0f;
            buzz = generator && lamp ? Buzz : 0f;
        }

        public static bool Spot(IReadOnlyList<PlacedModule> modules, string kind, out float x, out float z)
        {
            x = 0f;
            z = 0f;
            if (modules == null || string.IsNullOrEmpty(kind)) return false;
            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                if (module == null || module.kind != kind) continue;
                if (!BuildSite.Ready(module.site, module.integrity)) continue;
                x = module.x;
                z = module.z;
                return true;
            }
            return false;
        }
    }
}
