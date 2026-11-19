using UnityEngine;
using UnityEngine.InputSystem;

namespace Room812.Core
{
    /// <summary>
    /// Makes the carried gear functional: F toggles the flashlight, the EMF reader reacts to
    /// ghost proximity, and low sanity produces rhythmic heartbeat haptics.
    /// </summary>
    public class GearController : MonoBehaviour
    {
        [Header("Flashlight")]
        public Transform flashlightRoot;
        public Light flashlight;
        public bool flashlightOn = true;
        public float lightIntensity = 6f;
        public float lightRange = 14f;
        public float lightAngle = 48f;

        [Header("EMF reader")]
        public Transform emfRoot;
        public Renderer emfScreen;
        public Renderer[] emfLeds;
        public Transform ghost;
        public float emfRange = 12f;
        public Color emfIdle = new Color(0.05f, 0.35f, 0.2f);
        public Color emfHot = new Color(1f, 0.1f, 0.05f);

        [Header("Heartbeat haptics")]
        public bool heartbeatHaptics = true;

        SanityManager sanity;
        float heartTimer;
        float emfPulseCooldown;

        void Start()
        {
            sanity = FindAnyObjectByType<SanityManager>();
            if (ghost == null)
            {
                var brain = FindAnyObjectByType<Room812.AI.GhostBrain>();
                if (brain != null) ghost = brain.transform;
            }
            if (flashlight == null && flashlightRoot != null)
                flashlight = flashlightRoot.GetComponentInChildren<Light>(true);
            if (flashlight == null && flashlightRoot != null)
            {
                var go = new GameObject("FlashlightBeam");
                go.transform.SetParent(flashlightRoot, false);
                flashlight = go.AddComponent<Light>();
            }
            if (flashlight != null)
            {
                flashlight.type = LightType.Spot;
                flashlight.range = lightRange;
                flashlight.spotAngle = lightAngle;
                flashlight.intensity = lightIntensity;
                flashlight.shadows = LightShadows.None; // mobile budget
                flashlight.transform.localRotation = Quaternion.identity;
            }
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                flashlightOn = !flashlightOn;
            if (flashlight != null) flashlight.enabled = flashlightOn;

            float field = 0f;
            if (ghost != null)
                field = 1f - Mathf.Clamp01(Vector3.Distance(transform.position, ghost.position) / emfRange);

            emfPulseCooldown -= Time.deltaTime;
            if (field > 0.8f && emfPulseCooldown <= 0f)
            {
                emfPulseCooldown = 1.2f;
                NativeHaptics.Pulse(2);
            }

            if (emfScreen != null)
            {
                var mat = emfScreen.material;
                mat.SetColor("_BaseColor", Color.Lerp(emfIdle, emfHot, field));
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.Lerp(emfIdle, emfHot, field) * 2f);
            }
            if (emfLeds != null)
            {
                int lit = Mathf.RoundToInt(field * emfLeds.Length);
                for (int i = 0; i < emfLeds.Length; i++)
                {
                    if (emfLeds[i] == null) continue;
                    var m = emfLeds[i].material;
                    bool on = i < lit;
                    m.SetColor("_BaseColor", on ? emfHot : new Color(0.12f, 0.02f, 0.02f));
                    if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", on ? emfHot * 2f : Color.black);
                }
            }

            if (heartbeatHaptics && sanity != null && sanity.sanity < 0.65f)
            {
                heartTimer -= Time.deltaTime;
                if (heartTimer <= 0f)
                {
                    heartTimer = 60f / Mathf.Max(40f, sanity.heartbeatBpm);
#if UNITY_ANDROID || UNITY_IOS
                    Handheld.Vibrate();
#endif
                }
            }
        }
    }
}
