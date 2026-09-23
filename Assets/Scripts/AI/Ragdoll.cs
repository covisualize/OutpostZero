using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.AI
{
    /// <summary>
    /// Turns a dead body's bones into jointed rigid limbs for a blast kill, then puts the
    /// animated rig back when the pool hands the body out again. Built at runtime from
    /// <see cref="RagdollSheet"/>, so the generated prefabs carry no physics.
    /// </summary>
    public class Ragdoll : MonoBehaviour
    {
        private struct Part
        {
            public GameObject Host;
            public Rigidbody Body;
            public Collider Shape;
            public CharacterJoint Joint;
            public int Layer;
        }

        private readonly List<Part> parts = new List<Part>();
        private Animator animator;
        private readonly List<SkinnedMeshRenderer> skins = new List<SkinnedMeshRenderer>();

        public bool Active => parts.Count > 0;

        public static Ragdoll Ensure(GameObject root)
        {
            return Attach.Ensure<Ragdoll>(root);
        }

        /// <summary>Goes limp and throws the body. Returns false for a mesh with no rig.</summary>
        public bool Fall(Vector3 push, float mass)
        {
            if (Active) return true;
            animator = GetComponentInChildren<Animator>();
            Transform rig = animator != null ? animator.transform : transform;
            var bones = new Dictionary<string, Transform>();
            Collect(rig, bones);
            if (!bones.ContainsKey("Hips")) return false;

            if (animator != null) animator.enabled = false;
            var bodies = new Dictionary<string, Rigidbody>();
            foreach (var limb in RagdollSheet.Limbs)
            {
                if (!bones.TryGetValue(limb.Bone, out var bone)) continue;
                var part = new Part { Host = bone.gameObject, Layer = bone.gameObject.layer };
                part.Body = bone.gameObject.AddComponent<Rigidbody>();
                part.Body.mass = Mathf.Max(0.5f, mass * limb.Mass);
                part.Body.interpolation = RigidbodyInterpolation.Interpolate;
                part.Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                part.Shape = Shape(bone, limb, bones);
                if (limb.Parent != null && bodies.TryGetValue(limb.Parent, out var parentBody))
                {
                    part.Joint = bone.gameObject.AddComponent<CharacterJoint>();
                    part.Joint.connectedBody = parentBody;
                    part.Joint.enableProjection = true;
                    part.Joint.lowTwistLimit = new SoftJointLimit { limit = -RagdollSheet.Twist };
                    part.Joint.highTwistLimit = new SoftJointLimit { limit = RagdollSheet.Twist };
                    part.Joint.swing1Limit = new SoftJointLimit { limit = limb.Swing };
                    part.Joint.swing2Limit = new SoftJointLimit { limit = limb.Swing * 0.5f };
                }
                bone.gameObject.layer = GameLayers.Corpse;
                part.Body.linearVelocity = push;
                bodies[limb.Bone] = part.Body;
                parts.Add(part);
            }
            skins.Clear();
            GetComponentsInChildren(skins);
            for (int i = 0; i < skins.Count; i++) skins[i].updateWhenOffscreen = true;
            return true;
        }

        /// <summary>Removes the limbs and hands the bones back to the animator.</summary>
        public void Stand()
        {
            if (!Active) return;
            for (int i = 0; i < parts.Count; i++)
                if (parts[i].Joint != null) DestroyImmediate(parts[i].Joint);
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (part.Shape != null) DestroyImmediate(part.Shape);
                if (part.Body != null) DestroyImmediate(part.Body);
                if (part.Host != null) part.Host.layer = part.Layer;
            }
            parts.Clear();
            for (int i = 0; i < skins.Count; i++)
                if (skins[i] != null) skins[i].updateWhenOffscreen = false;
            skins.Clear();
            if (animator != null)
            {
                animator.enabled = true;
                animator.Rebind();
            }
        }

        private static Collider Shape(Transform bone, RagdollSheet.Limb limb, Dictionary<string, Transform> bones)
        {
            if (limb.Toward == null || !bones.TryGetValue(limb.Toward, out var child))
            {
                var ball = bone.gameObject.AddComponent<SphereCollider>();
                ball.radius = limb.Radius;
                ball.center = Vector3.up * limb.Radius * 0.5f;
                return ball;
            }
            Vector3 local = bone.InverseTransformPoint(child.position);
            var capsule = bone.gameObject.AddComponent<CapsuleCollider>();
            capsule.direction = RagdollSheet.Axis(local.x, local.y, local.z);
            capsule.radius = limb.Radius;
            capsule.height = local.magnitude + limb.Radius;
            capsule.center = local * 0.5f;
            return capsule;
        }

        private static void Collect(Transform node, Dictionary<string, Transform> bones)
        {
            if (!bones.ContainsKey(node.name)) bones[node.name] = node;
            foreach (Transform child in node) Collect(child, bones);
        }
    }
}
