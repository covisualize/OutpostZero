using UnityEngine;

namespace OutpostZero.Core
{
    /// <summary>
    /// Finds a component or adds one. The editor hands back a fake null for a missing
    /// component, and C# ?? treats that as present, so lookups go through TryGetComponent.
    /// </summary>
    public static class Attach
    {
        public static T Ensure<T>(GameObject host) where T : Component
        {
            if (host == null) return null;
            return host.TryGetComponent(out T found) ? found : host.AddComponent<T>();
        }

        public static T Near<T>(Component self) where T : Component
        {
            if (self == null) return null;
            return self.TryGetComponent(out T found) ? found : self.GetComponentInChildren<T>();
        }
    }
}
