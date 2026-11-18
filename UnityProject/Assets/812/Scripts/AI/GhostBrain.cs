using UnityEngine;
using UnityEngine.AI;
using Room812.Core;

namespace Room812.AI
{
    /// <summary>
    /// Offline utility-AI ghost controller. Patrols, investigates noise, hunts, manifests and
    /// retreats. Aggression is driven by squad sanity and proximity to Room 812. Learned
    /// hiding spots bias investigation targets over time.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class GhostBrain : MonoBehaviour
    {
        public enum GhostState { Dormant, Patrol, Investigate, Hunt, Manifest, Retreat }

        public GhostState state = GhostState.Dormant;
        public GhostBlackboard blackboard;
        public Vector3 room812Position = new Vector3(5f, 8f, 4.5f);
        public float patrolSpeed = 1.1f;
        public float investigateSpeed = 1.8f;
        public float huntSpeed = 2.8f;
        public float hearingRadius = 18f;
        public float huntRadius = 12f;
        public float thinkInterval = 0.3f;

        NavMeshAgent agent;
        float nextThink;
        float stateTimer;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (blackboard == null) blackboard = GetComponent<GhostBlackboard>();
            if (blackboard == null) blackboard = gameObject.AddComponent<GhostBlackboard>();
            NoiseBus.Emitted += OnNoise;
        }

        void OnDestroy()
        {
            NoiseBus.Emitted -= OnNoise;
        }

        void Start()
        {
            SetState(GhostState.Patrol);
        }

        void OnNoise(Vector3 position, float radius, GameObject source)
        {
            float distance = Vector3.Distance(transform.position, position);
            if (distance > Mathf.Max(radius, hearingRadius)) return;
            blackboard.RememberNoise(position, radius);
            if (state == GhostState.Patrol || state == GhostState.Dormant)
                SetState(GhostState.Investigate);
        }

        void Update()
        {
            if (agent == null || !agent.isOnNavMesh) return;
            stateTimer += Time.deltaTime;
            if (Time.time < nextThink) return;
            nextThink = Time.time + thinkInterval;
            Think();
        }

        void Think()
        {
            float sanity = Mathf.Clamp01(blackboard.squadSanity);
            float proximity = 1f - Mathf.Clamp01(Vector3.Distance(transform.position, room812Position) / 22f);
            blackboard.aggression = Mathf.Clamp01(0.1f + proximity * 0.45f + (1f - sanity) * 0.45f);

            switch (state)
            {
                case GhostState.Patrol:
                    agent.speed = patrolSpeed;
                    if (!blackboard.hasTarget || Vector3.Distance(transform.position, blackboard.target) < 1f)
                    {
                        var spot = blackboard.MostWeightedSpot();
                        blackboard.target = spot != null ? spot.position : room812Position + Random.insideUnitSphere * 4f;
                        blackboard.target.y = transform.position.y;
                        blackboard.hasTarget = true;
                        GoTo(blackboard.target);
                    }
                    if (blackboard.aggression > 0.65f && sanity < 0.6f) SetState(GhostState.Hunt);
                    break;

                case GhostState.Investigate:
                    agent.speed = investigateSpeed;
                    if (blackboard.hasTarget) GoTo(blackboard.target);
                    if (Vector3.Distance(transform.position, blackboard.target) < 1.2f || stateTimer > 12f)
                    {
                        var spot = blackboard.MostWeightedSpot();
                        if (spot != null)
                        {
                            blackboard.RememberHidingSpot(spot.id, spot.position);
                            blackboard.target = spot.position;
                        }
                        SetState(GhostState.Manifest);
                    }
                    break;

                case GhostState.Hunt:
                    agent.speed = huntSpeed;
                    if (blackboard.hasTarget) GoTo(blackboard.target);
                    if (stateTimer > 18f || sanity > 0.85f) SetState(GhostState.Retreat);
                    break;

                case GhostState.Manifest:
                    agent.speed = 0f;
                    if (stateTimer > 3f) SetState(blackboard.aggression > 0.5f ? GhostState.Hunt : GhostState.Patrol);
                    break;

                case GhostState.Retreat:
                    agent.speed = patrolSpeed;
                    GoTo(room812Position);
                    if (stateTimer > 10f) SetState(GhostState.Patrol);
                    break;
            }
        }

        void GoTo(Vector3 point)
        {
            Vector3 destination = point;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(destination, out hit, 4f, NavMesh.AllAreas))
                destination = hit.position;
            agent.SetDestination(destination);
        }

        void SetState(GhostState next)
        {
            state = next;
            stateTimer = 0f;
        }
    }
}
