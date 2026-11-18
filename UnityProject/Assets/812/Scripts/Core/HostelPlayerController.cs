using UnityEngine;
using UnityEngine.InputSystem;

namespace Room812.Core
{
    /// <summary>
    /// Mobile-first first-person investigator controller.
    /// Keyboard/mouse for desktop testing, touch drag for mobile, optional gyroscope lean.
    /// CharacterController keeps stair stepping cheap; footstep events feed the noise bus
    /// and audio layer. Animation Rigging IK will be layered on once the player rig arrives.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class HostelPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 3.2f;
        public float sprintSpeed = 5.4f;
        public float crouchSpeed = 1.6f;
        public float acceleration = 14f;
        public float airControl = 0.35f;
        public float gravity = -22f;
        public float jumpHeight = 1.05f;
        public float stepOffset = 0.35f;
        public float slopeLimit = 48f;

        [Header("Camera / feel")]
        public Camera viewCamera;
        public float mouseSensitivity = 0.12f;
        public float touchSensitivity = 0.004f;
        public float leanAngle = 7f;
        public float bobFrequency = 9f;
        public float bobAmount = 0.035f;
        public bool useGyroscope = true;
        [Range(0f, 1f)] public float gyroInfluence = 0.35f;

        [Header("Footsteps / haptics")]
        public float stepDistance = 2.0f;
        public bool hapticsOnFootstep = false;
        public UnityEngine.Events.UnityEvent onFootstep = new UnityEngine.Events.UnityEvent();

        CharacterController controller;
        Vector3 velocity;
        float pitch;
        float yaw;
        float lean;
        float bobTime;
        float distanceWalked;
        bool crouching;
        bool sprinting;
        UnityEngine.Gyroscope gyro;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            controller.stepOffset = stepOffset;
            controller.slopeLimit = slopeLimit;
            yaw = transform.eulerAngles.y;
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            if (useGyroscope && SystemInfo.supportsGyroscope)
            {
                gyro = Input.gyro;
                gyro.enabled = true;
            }
        }

        void Update()
        {
            Vector2 look = ReadLook();
            Vector2 move = ReadMove();
            ApplyRotation(look);
            ApplyMovement(move);
            ApplyCamera(look);
        }

        Vector2 ReadLook()
        {
            Vector2 look = Vector2.zero;
            if (Mouse.current != null)
                look += Mouse.current.delta.ReadValue() * mouseSensitivity;

            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.isPressed && touch.position.ReadValue().x > Screen.width * 0.45f)
                    look += touch.delta.ReadValue() * touchSensitivity;
            }
            return look;
        }

        Vector2 ReadMove()
        {
            Vector2 move = Vector2.zero;
            bool jump = false;

            if (Keyboard.current != null)
            {
                var kb = Keyboard.current;
                if (kb.wKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed) move.y -= 1f;
                if (kb.aKey.isPressed) move.x -= 1f;
                if (kb.dKey.isPressed) move.x += 1f;
                sprinting = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                crouching = kb.leftCtrlKey.isPressed || kb.cKey.isPressed;
                jump = kb.spaceKey.wasPressedThisFrame;
            }

            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.isPressed && touch.position.ReadValue().x < Screen.width * 0.45f)
                {
                    Vector2 drag = touch.position.ReadValue() - touch.startPosition.ReadValue();
                    move = Vector2.ClampMagnitude(drag / 120f, 1f);
                    crouching = false;
                    sprinting = move.magnitude > 0.85f;
                }
            }

            if (jump && controller.isGrounded)
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            return Vector2.ClampMagnitude(move, 1f);
        }

        void ApplyRotation(Vector2 look)
        {
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            float targetLean = -look.x * 0.6f;
            if (gyro != null)
            {
                float roll = Mathf.DeltaAngle(0f, gyro.attitude.eulerAngles.z);
                targetLean += roll * 0.012f * gyroInfluence;
            }
            lean = Mathf.Lerp(lean, Mathf.Clamp(targetLean, -leanAngle, leanAngle), Time.deltaTime * 6f);
        }

        void ApplyMovement(Vector2 move)
        {
            float speed = crouching ? crouchSpeed : (sprinting ? sprintSpeed : walkSpeed);
            Vector3 wish = transform.forward * move.y + transform.right * move.x;
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            float control = controller.isGrounded ? 1f : airControl;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            horizontal = Vector3.Lerp(horizontal, wish * speed, Time.deltaTime * acceleration * control);
            velocity.x = horizontal.x;
            velocity.z = horizontal.z;

            if (controller.isGrounded && velocity.y < 0f) velocity.y = -2f;
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            if (controller.isGrounded && horizontal.magnitude > 0.5f)
            {
                distanceWalked += horizontal.magnitude * Time.deltaTime;
                if (distanceWalked >= stepDistance)
                {
                    distanceWalked = 0f;
                    onFootstep?.Invoke();
                    NoiseBus.Emit(transform.position, crouching ? 1.5f : 5f, gameObject);
                    if (hapticsOnFootstep)
                    {
#if UNITY_ANDROID || UNITY_IOS
                        Handheld.Vibrate();
#endif
                    }
                }
            }
        }

        void ApplyCamera(Vector2 look)
        {
            if (viewCamera == null) return;
            float speed01 = Mathf.Clamp01(new Vector2(velocity.x, velocity.z).magnitude / walkSpeed);
            bobTime += Time.deltaTime * bobFrequency * speed01;
            float bob = Mathf.Sin(bobTime) * bobAmount * speed01;
            float height = crouching ? -0.35f : 0f;
            Vector3 target = new Vector3(lean * 0.012f, height + bob, 0f);
            viewCamera.transform.localPosition = Vector3.Lerp(viewCamera.transform.localPosition, target, Time.deltaTime * 10f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, -lean);
        }
    }
}
