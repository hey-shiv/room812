using UnityEngine;

namespace Room812.FX
{
    /// <summary>
    /// Cheap deterministic tube-light flicker. Drives both the point light and (optionally)
    /// the emissive material on the tube mesh. Keep GPU cost at zero — CPU only.
    /// </summary>
    public class TubeLightFlicker : MonoBehaviour
    {
        public Light target;
        public Renderer tubeRenderer;
        public float baseIntensity = 0.8f;
        [Range(0f, 1f)] public float flickerAmount = 0.35f;
        public float flickerRate = 7f;
        public bool broken = false;
        public bool syncedToGhost = false;

        float clock;
        float seed;

        void Awake()
        {
            if (target == null) target = GetComponentInChildren<Light>();
            if (tubeRenderer == null) tubeRenderer = GetComponentInChildren<Renderer>();
            seed = Mathf.Abs(transform.position.x * 0.13f + transform.position.z * 0.07f);
        }

        void Update()
        {
            clock += Time.deltaTime * flickerRate;
            float noise = Mathf.PerlinNoise(clock, seed);
            float factor = broken
                ? (noise > 0.66f ? 0f : 1f)
                : (1f - flickerAmount + flickerAmount * noise);

            if (target != null) target.intensity = baseIntensity * factor;
        }
    }
}
