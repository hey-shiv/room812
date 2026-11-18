using UnityEngine;

namespace Room812.Audio
{
    /// <summary>
    /// Bridges the room acoustic zones to Steam Audio. Resolves the listener's current zone
    /// each frame and applies reverb time / reflection mix / occlusion settings to all sources.
    /// Washrooms ring; dorm rooms stay dampened.
    /// </summary>
    public class SteamAudioZoneDriver : MonoBehaviour
    {
        public bool enableOcclusion = true;
        public bool enableReflections = true;
        public int occlusionSamples = 16;

        AcousticZone lastZone;

        void OnEnable() { AcousticZone.Changed += Apply; }
        void OnDisable() { AcousticZone.Changed -= Apply; }

        void Update()
        {
            var cam = Camera.main;
            if (cam != null) AcousticZone.Resolve(cam.transform.position);
            if (AcousticZone.Current != lastZone) Apply(AcousticZone.Current);
        }

        void Apply(AcousticZone zone)
        {
            lastZone = zone;
            if (zone == null) return;

            foreach (var listener in FindObjectsByType<SteamAudio.SteamAudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                listener.applyReverb = zone.reverbTime > 0.05f;

            foreach (var source in FindObjectsByType<SteamAudio.SteamAudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                source.occlusion = enableOcclusion;
                source.occlusionSamples = occlusionSamples;
                source.occlusionRadius = 0.4f;
                source.reflections = enableReflections;
                source.reflectionsMixLevel = Mathf.Clamp01(zone.reverbTime / 2f);
                source.reverbTimeLow = zone.reverbTime;
                source.reverbTimeMid = zone.reverbTime * 0.8f;
                source.reverbTimeHigh = zone.reverbTime * 0.5f;
            }
        }
    }
}
