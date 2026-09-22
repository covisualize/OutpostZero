#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

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
            DirectoryEnsure();
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }
            EnsureParameters(controller);
            EnsureGraph(controller);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        public static void AssignMotions(IReadOnlyList<AnimationClip> clips)
        {
            if (clips == null || clips.Count == 0) return;
            var controller = Build();
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
            foreach (var child in machine.states)
            {
                string state = child.state.name;
                if (byName.TryGetValue(state, out var clip)) child.state.motion = clip;
            }
            MotionOr(machine, "Crouch", byName, "CrouchIdle", "Idle");
            MotionOr(machine, "CrouchWalk", byName, "CrouchWalk", "Walk");
            MotionOr(machine, "Sprint", byName, "Sprint", "Walk");
            MotionOr(machine, "Attack", byName, "Attack", "Melee", "Fire");
            MotionOr(machine, "Hit", byName, "Hit", "Stagger");
            MotionOr(machine, "Death", byName, "Death", "DeathB");
            MotionOr(machine, "Reload", byName, "Reload");
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
            var attack = FindOrAdd(machine, "Attack", new Vector3(280, 180, 0));
            var hit = FindOrAdd(machine, "Hit", new Vector3(520, 180, 0));
            var reload = FindOrAdd(machine, "Reload", new Vector3(760, 180, 0));
            var death = FindOrAdd(machine, "Death", new Vector3(280, 280, 0));
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

            TriggerFrom(idle, attack, "Attack");
            TriggerFrom(walk, attack, "Attack");
            TriggerFrom(sprint, attack, "Attack");
            ExitTo(attack, idle);
            TriggerFrom(idle, hit, "Hit");
            TriggerFrom(walk, hit, "Hit");
            ExitTo(hit, idle);
            TriggerFrom(idle, reload, "Reload");
            TriggerFrom(walk, reload, "Reload");
            ExitTo(reload, idle);
            AnyTrigger(machine, death, "Death");
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
