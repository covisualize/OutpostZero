#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Builds a small Animator state machine the locomotion driver can target once clips exist.
    /// </summary>
    public static class SurvivorAnimatorBuilder
    {
        public const string ControllerPath = "Assets/Resources/SurvivorLocomotion.controller";

        public static AnimatorController Build()
        {
            DirectoryEnsure();
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null) return existing;

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Crouch", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Sprint", AnimatorControllerParameterType.Bool);
            var machine = controller.layers[0].stateMachine;
            var idle = machine.AddState("Idle", new Vector3(280, 0, 0));
            var walk = machine.AddState("Walk", new Vector3(280, 70, 0));
            var crouch = machine.AddState("Crouch", new Vector3(520, 0, 0));
            var sprint = machine.AddState("Sprint", new Vector3(520, 70, 0));
            machine.defaultState = idle;
            var toWalk = idle.AddTransition(walk);
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.2f, "Speed");
            toWalk.hasExitTime = false;
            toWalk.duration = 0.1f;
            var toIdle = walk.AddTransition(idle);
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");
            toIdle.hasExitTime = false;
            toIdle.duration = 0.1f;
            var toCrouch = idle.AddTransition(crouch);
            toCrouch.AddCondition(AnimatorConditionMode.If, 0f, "Crouch");
            toCrouch.hasExitTime = false;
            var toSprint = walk.AddTransition(sprint);
            toSprint.AddCondition(AnimatorConditionMode.If, 0f, "Sprint");
            toSprint.hasExitTime = false;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        public static void AssignMotions(AnimationClip idle, AnimationClip walk)
        {
            var controller = Build();
            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states)
            {
                if (child.state.name == "Idle" && idle != null) child.state.motion = idle;
                if (child.state.name == "Walk" && walk != null) child.state.motion = walk;
                if (child.state.name == "Sprint" && walk != null) child.state.motion = walk;
                if (child.state.name == "Crouch" && idle != null) child.state.motion = idle;
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
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
