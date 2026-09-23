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
    }
}
