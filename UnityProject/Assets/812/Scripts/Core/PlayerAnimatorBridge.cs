using UnityEngine;

namespace Room812.Core
{
    /// <summary>
    /// Drives the investigator Animator from actual movement, so locomotion is responsive
    /// rather than stiff. Uses the rig-matched clips authored by CHAT-B.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimatorBridge : MonoBehaviour
    {
        public Transform trackedRoot;
        public float walkThreshold = 0.15f;
        public float runThreshold = 3.8f;
        public float stairThreshold = 0.25f;
        public float turnThreshold = 24f;

        Animator animator;
        Vector3 lastPosition;
        float lastYaw;

        void Awake()
        {
            animator = GetComponent<Animator>();
            if (trackedRoot == null) trackedRoot = transform.root;
            lastPosition = trackedRoot.position;
            lastYaw = trackedRoot.eulerAngles.y;
        }

        void Update()
        {
            if (animator == null || trackedRoot == null) return;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 delta = trackedRoot.position - lastPosition;
            lastPosition = trackedRoot.position;

            float horizontal = new Vector2(delta.x, delta.z).magnitude / dt;
            animator.SetFloat("Speed", horizontal);

            bool crouch = UnityEngine.InputSystem.Keyboard.current != null &&
                          (UnityEngine.InputSystem.Keyboard.current.leftCtrlKey.isPressed ||
                           UnityEngine.InputSystem.Keyboard.current.cKey.isPressed);
            animator.SetBool("Crouch", crouch);

            int stairs = 0;
            if (delta.y > stairThreshold * dt * 10f) stairs = 1;
            else if (delta.y < -stairThreshold * dt * 10f) stairs = 2;
            animator.SetInteger("Stairs", stairs);

            float yaw = trackedRoot.eulerAngles.y;
            float turn = Mathf.DeltaAngle(lastYaw, yaw);
            lastYaw = yaw;
            if (turn > turnThreshold) animator.SetTrigger("TurnLeft");
            else if (turn < -turnThreshold) animator.SetTrigger("TurnRight");
        }
    }
}
