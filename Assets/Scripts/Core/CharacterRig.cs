namespace OutpostZero.Core
{
    /// <summary>
    /// Where each character model's Animator controller lives. The player keeps the
    /// original SurvivorLocomotion controller; every other model gets its own copy of the
    /// graph under Resources/Animators so its clips don't overwrite the player's.
    /// </summary>
    public static class CharacterRig
    {
        public const string PlayerModel = "Survivor_Leader";
        public const string PlayerController = "SurvivorLocomotion";
        public const string Folder = "Animators";

        /// <summary>Blend parameters that pick one body's idle, gait and death takes.</summary>
        public const string IdleVariant = "IdleVariant";
        public const string GaitVariant = "GaitVariant";
        public const string DeathVariant = "DeathVariant";
        /// <summary>Trigger for the brace before a special: the brute's roar, the walker's scream.</summary>
        public const string Windup = "Windup";
        /// <summary>Held while a lunge or charge carries the body.</summary>
        public const string Dash = "Dash";

        /// <summary>The model id is the FBX file stem, e.g. "Zombie_Walker".</summary>
        public static string ModelId(string modelPath)
        {
            if (string.IsNullOrEmpty(modelPath)) return "";
            string path = modelPath.Replace('\\', '/');
            int slash = path.LastIndexOf('/');
            if (slash >= 0) path = path.Substring(slash + 1);
            int dot = path.LastIndexOf('.');
            return dot > 0 ? path.Substring(0, dot) : path;
        }

        /// <summary>Path for Resources.Load, or "" when there is no model.</summary>
        public static string ResourcePath(string modelId)
        {
            if (string.IsNullOrEmpty(modelId)) return "";
            return modelId == PlayerModel ? PlayerController : Folder + "/" + modelId;
        }

        public static string ControllerAsset(string modelId)
        {
            string resource = ResourcePath(modelId);
            return string.IsNullOrEmpty(resource) ? "" : "Assets/Resources/" + resource + ".controller";
        }

        /// <summary>Second animator layer: swings and reloads play above the waist while the legs keep their gait.</summary>
        public const string UpperLayer = "UpperBody";
        /// <summary>The upper layer's resting state; it has no motion, so the base layer shows through.</summary>
        public const string UpperRest = "Empty";
        /// <summary>The rig bone whose subtree (torso, head, arms) the upper layer owns.</summary>
        public const string UpperRoot = "Spine";
        /// <summary>Base-layer states that move to the upper layer.</summary>
        public static readonly string[] UpperStates = { "Attack", "Reload" };
        /// <summary>Full-body triggers that clear the upper layer so the whole body shows the pose.</summary>
        public static readonly string[] UpperClears = { "Death", "Hit" };

        public static bool IsUpperState(string state)
        {
            for (int i = 0; i < UpperStates.Length; i++)
                if (UpperStates[i] == state) return true;
            return false;
        }

        /// <summary>True when a transform path (relative to the model root) lies in the Spine subtree.</summary>
        public static bool UpperBody(string transformPath)
        {
            if (string.IsNullOrEmpty(transformPath)) return false;
            var parts = transformPath.Split('/');
            for (int i = 0; i < parts.Length; i++)
                if (parts[i] == UpperRoot) return true;
            return false;
        }
    }
}
