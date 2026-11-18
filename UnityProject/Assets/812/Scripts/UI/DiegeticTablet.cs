using UnityEngine;
using UnityEngine.InputSystem;

namespace Room812.UI
{
    /// <summary>
    /// Raises a rugged in-world tablet to inspect the journal, floorplan and vitals.
    /// Nothing is drawn in screen space — the phone itself becomes the gear.
    /// </summary>
    public class DiegeticTablet : MonoBehaviour
    {
        public Transform tablet;
        public Vector3 loweredPosition = new Vector3(0.35f, -0.55f, 0.6f);
        public Vector3 raisedPosition = new Vector3(0f, -0.1f, 0.55f);
        public float raiseSpeed = 7f;
        public bool raised;

        void Awake()
        {
            if (tablet == null) tablet = transform;
            tablet.localPosition = loweredPosition;
        }

        void Update()
        {
            bool pressed = false;
            if (Keyboard.current != null) pressed = Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.tKey.wasPressedThisFrame;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                if (Touchscreen.current.primaryTouch.position.ReadValue().y > Screen.height * 0.82f) pressed = true;
            }
            if (pressed) raised = !raised;

            Vector3 target = raised ? raisedPosition : loweredPosition;
            tablet.localPosition = Vector3.Lerp(tablet.localPosition, target, Time.deltaTime * raiseSpeed);
            Quaternion rot = raised ? Quaternion.Euler(-12f, 0f, 0f) : Quaternion.Euler(65f, 0f, 0f);
            tablet.localRotation = Quaternion.Slerp(tablet.localRotation, rot, Time.deltaTime * raiseSpeed);
        }
    }
}
