using UnityEngine;

namespace Room812.AI
{
    /// <summary>Maps GhostBrain states to Animator parameters (no code generation needed).</summary>
    [RequireComponent(typeof(Animator))]
    public class GhostAnimatorBridge : MonoBehaviour
    {
        public GhostBrain brain;
        public string parameterName = "GhostState";

        Animator animator;
        int last = -999;

        void Awake()
        {
            animator = GetComponent<Animator>();
            if (brain == null) brain = GetComponent<GhostBrain>();
        }

        void Update()
        {
            if (animator == null || brain == null) return;
            int value = (int)brain.state;
            if (value == last) return;
            last = value;
            animator.SetInteger(parameterName, value);
        }
    }
}
