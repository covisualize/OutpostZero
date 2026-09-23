#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Builds the locomotion and combat state machine the rigs target once clips exist.
    /// </summary>
    public static class SurvivorAnimatorBuilder
    {
        public const string ControllerPath = "Assets/Resources/SurvivorLocomotion.controller";

        public static AnimatorController Build()
        {
            return Build(ControllerPath);
        }

        /// <summary>Creates or refreshes the shared graph at <paramref name="path"/>; each character model gets its own copy.</summary>
        public static AnimatorController Build(string path)
        {
            DirectoryEnsure();
            string folder = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
            }
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }
            EnsureParameters(controller);
            EnsureGraph(controller);
            EnsureUpperLayer(controller);
            EnsureRig(controller);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        public static void AssignMotions(IReadOnlyList<AnimationClip> clips)
        {
            AssignMotions(ControllerPath, clips);
        }

        public static void AssignMotions(string path, IReadOnlyList<AnimationClip> clips)
        {
            AssignMotions(path, clips, null);
        }

        /// <summary>Fills the graph with the model's clips; <paramref name="model"/> supplies the bone paths for the upper-body mask.</summary>
        public static void AssignMotions(string path, IReadOnlyList<AnimationClip> clips, GameObject model)
        {
            if (clips == null || clips.Count == 0) return;
            var controller = Build(path);
            if (model != null) EnsureUpperMask(controller, model);
            var byName = new Dictionary<string, AnimationClip>();
            for (int i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                if (clip == null) continue;
                string name = BareName(clip.name);
                if (!byName.ContainsKey(name)) byName[name] = clip;
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = Loops(name);
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }

            var machine = controller.layers[0].stateMachine;
            var upper = UpperMachine(controller);
            foreach (var layer in controller.layers)
            {
                foreach (var child in layer.stateMachine.states)
                {
                    string state = child.state.name;
                    if (child.state.motion is BlendTree) continue;
                    if (byName.TryGetValue(state, out var clip)) child.state.motion = clip;
                }
            }
            MotionOr(machine, "Crouch", byName, "CrouchIdle", "Idle");
            MotionOr(machine, "CrouchWalk", byName, "CrouchWalk", "Walk");
            MotionOr(machine, "Sprint", byName, "Sprint", "Walk");
            MotionOr(upper, "Attack", byName, "Attack", "Melee", "Fire");
            MotionOr(machine, "Hit", byName, "Hit", "Stagger");
            MotionOr(machine, "Death", byName, "Death", "DeathB");
            MotionOr(upper, "Reload", byName, "Reload");
            MotionOr(machine, CharacterRig.Windup, byName, "Roar", "Scream");
            MotionOr(machine, CharacterRig.Dash, byName, "Charge", "Lunge", "Sprint");
            Variants(controller, machine, "Idle", CharacterRig.IdleVariant, byName, "Idle", "IdleB");
            Variants(controller, machine, "Walk", CharacterRig.GaitVariant, byName, CrowdVariant.WalkTakes);
            Variants(controller, machine, "Death", CharacterRig.DeathVariant, byName, "Death", "DeathB", "DeathC");
            if (upper != null) Variants(controller, upper, "Reload", CharacterRig.ReloadVariant, byName, CharacterRig.ReloadTakes);
            if (byName.TryGetValue(CharacterRig.Dissolve, out var heap)) Melt(machine, heap);
            if (byName.TryGetValue(CharacterRig.Stagger, out var reel)) Reel(machine, reel);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        public static void AssignMotions(AnimationClip idle, AnimationClip walk)
        {
            var list = new List<AnimationClip>();
            if (idle != null) list.Add(idle);
            if (walk != null) list.Add(walk);
            AssignMotions(list);
        }

        private static void EnsureParameters(AnimatorController controller)
        {
            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "Crouch", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Sprint", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Death", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Reload", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, ReloadSpeed, AnimatorControllerParameterType.Float);
            EnsureParameter(controller, StrideSheet.MoveRate, AnimatorControllerParameterType.Float);
            EnsureParameter(controller, CharacterRig.IdleVariant, AnimatorControllerParameterType.Float);
            EnsureParameter(controller, CharacterRig.GaitVariant, AnimatorControllerParameterType.Float);
            EnsureParameter(controller, CharacterRig.DeathVariant, AnimatorControllerParameterType.Float);
            EnsureParameter(controller, CharacterRig.ReloadVariant, AnimatorControllerParameterType.Float);
            EnsureParameter(controller, CharacterRig.Stagger, AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, CharacterRig.Windup, AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, CharacterRig.Dash, AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, CharacterRig.Activity, AnimatorControllerParameterType.Int);
            var parameters = controller.parameters;
            bool changed = false;
            for (int i = 0; i < parameters.Length; i++)
            {
                bool rate = parameters[i].name == ReloadSpeed || parameters[i].name == StrideSheet.MoveRate;
                if (rate && parameters[i].defaultFloat != 1f)
                {
                    parameters[i].defaultFloat = 1f;
                    changed = true;
                }
            }
            if (changed) controller.parameters = parameters;
        }

        public const string ReloadSpeed = "ReloadSpeed";

        /// <summary>The reload plays at the gun's reload speed; gaits play at the body's stride rate.</summary>
        private static void EnsureRig(AnimatorController controller)
        {
            foreach (var layer in controller.layers)
            {
                foreach (var child in layer.stateMachine.states)
                {
                    string rate = SpeedParameter(child.state.name);
                    if (rate == null) continue;
                    child.state.speedParameterActive = true;
                    child.state.speedParameter = rate;
                }
            }
        }

        /// <summary>Only a model with the heap take gets the state; a survivor's fall holds its last frame.</summary>
        private static void Melt(AnimatorStateMachine machine, AnimationClip heap)
        {
            AnimatorState death = null;
            foreach (var child in machine.states)
                if (child.state.name == "Death") death = child.state;
            if (death == null) return;
            var melt = FindOrAdd(machine, CharacterRig.Dissolve, new Vector3(280, 380, 0));
            melt.motion = heap;
            ExitTo(death, melt);
        }

        /// <summary>A long stun reels from any standing or moving state, then settles back to the idle.</summary>
        private static void Reel(AnimatorStateMachine machine, AnimationClip clip)
        {
            var from = new List<AnimatorState>();
            AnimatorState idle = null;
            foreach (var child in machine.states)
            {
                string name = child.state.name;
                if (name == "Idle") idle = child.state;
                if (name == "Idle" || name == "Walk" || name == "Sprint" || name == CharacterRig.Windup || name == CharacterRig.Dash) from.Add(child.state);
            }
            if (idle == null) return;
            var reel = FindOrAdd(machine, CharacterRig.Stagger, new Vector3(760, 180, 0));
            reel.motion = clip;
            for (int i = 0; i < from.Count; i++) TriggerFrom(from[i], reel, CharacterRig.Stagger);
            ExitTo(reel, idle);
        }

        public const string UpperMaskName = "UpperBodyMask";

        private static AnimatorStateMachine UpperMachine(AnimatorController controller)
        {
            foreach (var layer in controller.layers)
                if (layer.name == CharacterRig.UpperLayer) return layer.stateMachine;
            return null;
        }

        /// <summary>
        /// Swings and reloads live on an override layer masked to the Spine subtree, so a survivor can
        /// fire while walking and a zombie can bite mid-shamble. Death and flinches clear it.
        /// </summary>
        private static void EnsureUpperLayer(AnimatorController controller)
        {
            var baseMachine = controller.layers[0].stateMachine;
            foreach (var child in baseMachine.states)
            {
                foreach (var transition in child.state.transitions)
                {
                    var to = transition.destinationState;
                    if (to != null && CharacterRig.IsUpperState(to.name)) child.state.RemoveTransition(transition);
                }
            }
            foreach (var child in baseMachine.states)
            {
                if (CharacterRig.IsUpperState(child.state.name)) baseMachine.RemoveState(child.state);
            }

            if (UpperMachine(controller) == null) controller.AddLayer(CharacterRig.UpperLayer);
            var layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name != CharacterRig.UpperLayer) continue;
                layers[i].defaultWeight = 1f;
                layers[i].blendingMode = AnimatorLayerBlendingMode.Override;
            }
            controller.layers = layers;

            var machine = UpperMachine(controller);
            var rest = FindOrAdd(machine, CharacterRig.UpperRest, new Vector3(280, 0, 0));
            machine.defaultState = rest;
            for (int i = 0; i < CharacterRig.UpperStates.Length; i++)
            {
                string name = CharacterRig.UpperStates[i];
                var state = FindOrAdd(machine, name, new Vector3(520, 80 * i, 0));
                TriggerFrom(rest, state, name);
                ExitTo(state, rest);
            }
            for (int i = 0; i < CharacterRig.UpperClears.Length; i++)
            {
                AnyTrigger(machine, rest, CharacterRig.UpperClears[i]);
            }
            foreach (var transition in machine.anyStateTransitions) transition.canTransitionToSelf = false;
        }

        private static void EnsureUpperMask(AnimatorController controller, GameObject model)
        {
            AvatarMask mask = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(controller)))
            {
                if (asset is AvatarMask found && found.name == UpperMaskName) mask = found;
            }
            if (mask == null)
            {
                mask = new AvatarMask { name = UpperMaskName };
                AssetDatabase.AddObjectToAsset(mask, controller);
            }
            var bones = model.GetComponentsInChildren<Transform>(true);
            mask.transformCount = bones.Length;
            for (int i = 0; i < bones.Length; i++)
            {
                string path = AnimationUtility.CalculateTransformPath(bones[i], model.transform);
                mask.SetTransformPath(i, path);
                mask.SetTransformActive(i, CharacterRig.UpperBody(path));
            }
            EditorUtility.SetDirty(mask);
            var layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == CharacterRig.UpperLayer) layers[i].avatarMask = mask;
            }
            controller.layers = layers;
        }

        public static string SpeedParameter(string state)
        {
            switch (state)
            {
                case "Reload": return ReloadSpeed;
                case "Walk":
                case "Sprint":
                case "CrouchWalk":
                case CharacterRig.Dash:
                    return StrideSheet.MoveRate;
                default: return null;
            }
        }

        /// <summary>
        /// Crowd variety: a state with alternate takes becomes a 1D blend on its variant parameter, so each
        /// zombie can hold its own idle or gait. Rebuilt in place on re-import.
        /// </summary>
        private static void Variants(AnimatorController controller, AnimatorStateMachine machine, string state, string parameter, Dictionary<string, AnimationClip> clips, params string[] names)
        {
            var takes = new List<AnimationClip>();
            for (int i = 0; i < names.Length; i++)
                if (clips.TryGetValue(names[i], out var clip)) takes.Add(clip);
            if (takes.Count < 2) return;
            AnimatorState target = null;
            foreach (var child in machine.states)
                if (child.state.name == state) target = child.state;
            if (target == null) return;
            var tree = target.motion as BlendTree;
            if (tree == null)
            {
                tree = new BlendTree { name = state + "Variants", hideFlags = HideFlags.HideInHierarchy };
                AssetDatabase.AddObjectToAsset(tree, controller);
                target.motion = tree;
            }
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = parameter;
            tree.useAutomaticThresholds = false;
            var children = new ChildMotion[takes.Count];
            for (int i = 0; i < takes.Count; i++)
            {
                children[i] = new ChildMotion { motion = takes[i], threshold = CrowdVariant.Threshold(i, takes.Count), timeScale = 1f };
            }
            tree.children = children;
            EditorUtility.SetDirty(tree);
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (var parameter in controller.parameters)
            {
                if (parameter.name == name) return;
            }
            controller.AddParameter(name, type);
        }

        private static void EnsureGraph(AnimatorController controller)
        {
            var machine = controller.layers[0].stateMachine;
            var idle = FindOrAdd(machine, "Idle", new Vector3(280, 0, 0));
            var walk = FindOrAdd(machine, "Walk", new Vector3(280, 80, 0));
            var crouch = FindOrAdd(machine, "Crouch", new Vector3(520, 0, 0));
            var crouchWalk = FindOrAdd(machine, "CrouchWalk", new Vector3(760, 0, 0));
            var sprint = FindOrAdd(machine, "Sprint", new Vector3(520, 80, 0));
            var hit = FindOrAdd(machine, "Hit", new Vector3(520, 180, 0));
            var death = FindOrAdd(machine, "Death", new Vector3(280, 280, 0));
            var windup = FindOrAdd(machine, CharacterRig.Windup, new Vector3(520, 280, 0));
            var dash = FindOrAdd(machine, CharacterRig.Dash, new Vector3(760, 280, 0));
            if (machine.defaultState == null) machine.defaultState = idle;

            if (TryLink(idle, walk, false, out var toWalk))
            {
                toWalk.AddCondition(AnimatorConditionMode.Greater, 0.2f, "Speed");
                toWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "Crouch");
            }
            if (TryLink(walk, idle, false, out var toIdle)) toIdle.AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");
            if (TryLink(idle, crouch, false, out var toCrouch)) toCrouch.AddCondition(AnimatorConditionMode.If, 0f, "Crouch");
            if (TryLink(crouch, idle, false, out var fromCrouch)) fromCrouch.AddCondition(AnimatorConditionMode.IfNot, 0f, "Crouch");
            if (TryLink(crouch, crouchWalk, false, out var toCrouchWalk)) toCrouchWalk.AddCondition(AnimatorConditionMode.Greater, 0.2f, "Speed");
            if (TryLink(crouchWalk, crouch, false, out var fromCrouchWalk)) fromCrouchWalk.AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");
            if (TryLink(crouchWalk, walk, false, out var stand)) stand.AddCondition(AnimatorConditionMode.IfNot, 0f, "Crouch");
            if (TryLink(walk, sprint, false, out var toSprint))
            {
                toSprint.AddCondition(AnimatorConditionMode.If, 0f, "Sprint");
                toSprint.AddCondition(AnimatorConditionMode.IfNot, 0f, "Crouch");
            }
            if (TryLink(sprint, walk, false, out var fromSprint)) fromSprint.AddCondition(AnimatorConditionMode.IfNot, 0f, "Sprint");

            TriggerFrom(idle, hit, "Hit");
            TriggerFrom(walk, hit, "Hit");
            ExitTo(hit, idle);
            TriggerFrom(idle, windup, CharacterRig.Windup);
            TriggerFrom(walk, windup, CharacterRig.Windup);
            TriggerFrom(sprint, windup, CharacterRig.Windup);
            ExitTo(windup, idle);
            foreach (var from in new[] { idle, walk, sprint, windup })
            {
                if (TryLink(from, dash, false, out var toDash)) toDash.AddCondition(AnimatorConditionMode.If, 0f, CharacterRig.Dash);
            }
            if (TryLink(dash, walk, false, out var fromDash)) fromDash.AddCondition(AnimatorConditionMode.IfNot, 0f, CharacterRig.Dash);
            for (int i = 0; i < CharacterRig.ActivityStates.Length; i++)
            {
                var chore = FindOrAdd(machine, CharacterRig.ActivityStates[i], new Vector3(40, 80 * i, 0));
                Chore(idle, walk, hit, chore, i + 1);
            }
            AnyTrigger(machine, death, "Death");
        }

        /// <summary>A standing body settles into its chore; walking off or dropping the chore returns it to the idle.</summary>
        private static void Chore(AnimatorState idle, AnimatorState walk, AnimatorState hit, AnimatorState chore, int code)
        {
            if (TryLink(idle, chore, false, out var start))
            {
                start.duration = 0.25f;
                start.AddCondition(AnimatorConditionMode.Equals, code, CharacterRig.Activity);
                start.AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");
            }
            if (TryLink(chore, idle, false, out var stop))
            {
                stop.duration = 0.25f;
                stop.AddCondition(AnimatorConditionMode.NotEqual, code, CharacterRig.Activity);
            }
            if (TryLink(chore, walk, false, out var leave)) leave.AddCondition(AnimatorConditionMode.Greater, 0.2f, "Speed");
            TriggerFrom(chore, hit, "Hit");
        }

        private static AnimatorState FindOrAdd(AnimatorStateMachine machine, string name, Vector3 position)
        {
            foreach (var child in machine.states)
            {
                if (child.state.name == name) return child.state;
            }
            return machine.AddState(name, position);
        }

        private static bool TryLink(AnimatorState from, AnimatorState to, bool exitTime, out AnimatorStateTransition transition)
        {
            foreach (var existing in from.transitions)
            {
                if (existing.destinationState == to)
                {
                    transition = existing;
                    return false;
                }
            }
            transition = from.AddTransition(to);
            transition.hasExitTime = exitTime;
            transition.duration = 0.08f;
            return true;
        }

        private static void TriggerFrom(AnimatorState from, AnimatorState to, string trigger)
        {
            foreach (var existing in from.transitions)
            {
                if (existing.destinationState != to) continue;
                foreach (var condition in existing.conditions)
                {
                    if (condition.parameter == trigger) return;
                }
            }
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.05f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void ExitTo(AnimatorState from, AnimatorState to)
        {
            foreach (var existing in from.transitions)
            {
                if (existing.destinationState == to && existing.hasExitTime) return;
            }
            var transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 0.9f;
            transition.duration = 0.08f;
        }

        private static void AnyTrigger(AnimatorStateMachine machine, AnimatorState to, string trigger)
        {
            foreach (var existing in machine.anyStateTransitions)
            {
                if (existing.destinationState != to) continue;
                foreach (var condition in existing.conditions)
                {
                    if (condition.parameter == trigger) return;
                }
            }
            var transition = machine.AddAnyStateTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.05f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void MotionOr(AnimatorStateMachine machine, string state, Dictionary<string, AnimationClip> clips, params string[] names)
        {
            AnimatorState target = null;
            foreach (var child in machine.states)
            {
                if (child.state.name == state) target = child.state;
            }
            if (target == null || target.motion != null) return;
            for (int i = 0; i < names.Length; i++)
            {
                if (clips.TryGetValue(names[i], out var clip))
                {
                    target.motion = clip;
                    return;
                }
            }
        }

        public static string BareName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            int bar = name.LastIndexOf('|');
            return bar >= 0 ? name.Substring(bar + 1) : name;
        }

        private static bool Loops(string name)
        {
            switch (name)
            {
                case "Idle":
                case "IdleB":
                case "Walk":
                case "WalkB":
                case "Sprint":
                case "CrouchIdle":
                case "CrouchWalk":
                case "Shamble":
                case "Aim":
                case "Charge":
                case "Work":
                case "Talk":
                    return true;
                default:
                    return false;
            }
        }

        private static void DirectoryEnsure()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
        }
    }
}
#endif
