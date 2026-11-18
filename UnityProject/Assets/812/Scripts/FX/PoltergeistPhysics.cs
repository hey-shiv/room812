using UnityEngine;
using UnityEngine.InputSystem;
using Room812.Core;

namespace Room812.FX
{
    /// <summary>
    /// Throws nearby dynamic props during a hunt. Uses a simple radius search so it can be
    /// driven by the AI now and by RPCs later in LAN multiplayer.
    /// </summary>
    public class PoltergeistPhysics : MonoBehaviour
    {
        public float radius = 9f;
        public float throwForce = 7f;
        public float upwardBias = 2.5f;
        public bool debugKeyEnabled = true;

        void Update()
        {
            if (!debugKeyEnabled || Keyboard.current == null) return;
            if (Keyboard.current.gKey.wasPressedThisFrame) ThrowNearest();
        }

        public void ThrowNearest()
        {
            Rigidbody nearest = null;
            float best = radius * radius;
            foreach (var rb in FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (rb == null || rb.isKinematic) continue;
                float d = (rb.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; nearest = rb; }
            }
            if (nearest == null) return;

            Vector3 dir = (nearest.position - transform.position).normalized + Vector3.up * upwardBias;
            nearest.AddForce(dir.normalized * throwForce, ForceMode.Impulse);
            NoiseBus.Emit(nearest.position, 6f, gameObject);
        }
    }
}
