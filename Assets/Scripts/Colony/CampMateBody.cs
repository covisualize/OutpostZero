using UnityEngine;
using UnityEngine.AI;
using OutpostZero.Core;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Walks one colonist's yard body to its goal. On the NavMesh it steers with an agent, so it goes round
    /// modules and walls; off it, or before the mesh is baked, it slides straight there. The model's own
    /// controller plays its walk at the stride that matches the speed.
    /// </summary>
    public class CampMateBody : MonoBehaviour
    {
        public const string ModelId = "Colonist_Survivor";
        public const float Repath = 0.35f;
        public const float SnapReach = 2f;

        private NavMeshAgent agent;
        private Animator animator;
        private Vector3 lastGoal = new Vector3(float.NaN, 0f, 0f);
        private Vector3 lastPosition;
        private bool hasSpeed;
        private bool hasRate;
        private bool hasActivity;
        private int activity = -1;

        public bool Steered => agent != null && agent.enabled && agent.isOnNavMesh;
        /// <summary>True when the model's controller has chore clips, so the yard skips its procedural tilt.</summary>
        public bool Acts => hasActivity;

        public void Dress(bool model)
        {
            lastPosition = transform.position;
            if (model) Rig();
            if (NavMesh.SamplePosition(transform.position, out var hit, SnapReach, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agent = gameObject.AddComponent<NavMeshAgent>();
                agent.radius = 0.3f;
                agent.height = 1.8f;
                agent.acceleration = 8f;
                agent.angularSpeed = 540f;
                agent.stoppingDistance = 0.2f;
                agent.autoBraking = true;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                agent.avoidancePriority = 60;
                agent.updateRotation = false;
            }
        }

        private void Rig()
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null) return;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            if (animator.runtimeAnimatorController == null)
                animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(CharacterRig.ResourcePath(ModelId));
            if (animator.runtimeAnimatorController == null) return;
            foreach (var parameter in animator.parameters)
            {
                if (parameter.name == "Speed") hasSpeed = true;
                if (parameter.name == StrideSheet.MoveRate) hasRate = true;
                if (parameter.name == CharacterRig.Activity) hasActivity = true;
            }
        }

        /// <summary>Moves toward the goal at the pace and returns the planar direction it is heading.</summary>
        public Vector3 Drive(Vector3 goal, float pace)
        {
            if (Steered)
            {
                agent.speed = pace;
                goal.y = transform.position.y;
                if (float.IsNaN(lastGoal.x) || (goal - lastGoal).sqrMagnitude > Repath * Repath)
                {
                    lastGoal = goal;
                    agent.SetDestination(goal);
                }
            }
            else
            {
                goal.y = transform.position.y;
                transform.position = Vector3.MoveTowards(transform.position, goal, pace * Time.deltaTime);
            }

            Vector3 moved = transform.position - lastPosition;
            moved.y = 0f;
            float speed = Time.deltaTime > 0f ? moved.magnitude / Time.deltaTime : 0f;
            lastPosition = transform.position;
            Animate(speed);
            Vector3 heading = Steered && agent.desiredVelocity.sqrMagnitude > 0.01f ? agent.desiredVelocity : goal - transform.position;
            heading.y = 0f;
            return heading;
        }

        /// <summary>Sets the chore the body plays once it stands at its spot (<see cref="CharacterRig.ActivityWork"/> and so on).</summary>
        public void Chore(int code)
        {
            if (!hasActivity || code == activity || animator == null) return;
            activity = code;
            animator.SetInteger(CharacterRig.Activity, code);
        }

        private void Animate(float speed)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            if (hasSpeed) animator.SetFloat("Speed", speed);
            if (hasRate) animator.SetFloat(StrideSheet.MoveRate, StrideSheet.Rate(speed, StrideSheet.GroundSpeed(ModelId, "Walk"), transform.lossyScale.y));
        }
    }
}
