using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// Puts a weapon's generated prefab in the hand at the definition's grip. The model is
    /// purely visual: its colliders go, so it never blocks shots or the player's own capsule.
    /// </summary>
    public static class HeldModel
    {
        public const string Name = "Held_Mesh";

        public static GameObject Mount(Transform weapon, WeaponDefinition definition)
        {
            if (weapon == null || definition == null || definition.heldPrefab == null) return null;
            var existing = weapon.Find(Name);
            if (existing != null) return existing.gameObject;
            if (weapon.GetComponentInChildren<Renderer>(true) != null) return null;
            var model = Object.Instantiate(definition.heldPrefab, weapon, false);
            model.name = Name;
            Place(model.transform, definition);
            foreach (var solid in model.GetComponentsInChildren<Collider>(true)) Object.Destroy(solid);
            GameLayers.ApplyRecursively(model, weapon.gameObject.layer);
            return model;
        }

        public static void Place(Transform model, WeaponDefinition definition)
        {
            if (model == null || definition == null) return;
            model.localPosition = definition.holdOffset;
            model.localRotation = Quaternion.Euler(definition.holdEuler);
            model.localScale = Vector3.one;
        }
    }
}
