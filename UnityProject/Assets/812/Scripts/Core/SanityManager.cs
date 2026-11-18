using System;
using UnityEngine;

namespace Room812.Core
{
    /// <summary>
    /// Squad sanity + heartbeat. Drives ghost aggression and phone haptics. Local-only for now;
    /// will be synchronised by Netcode in Phase 5.
    /// </summary>
    public class SanityManager : MonoBehaviour
    {
        public static SanityManager Instance { get; private set; }

        [Range(0f, 1f)] public float sanity = 1f;
        public float heartbeatBpm = 72f;
        public float hapticThreshold = 0.45f;
        public event Action<float> OnSanityChanged;

        void Awake()
        {
            Instance = this;
        }

        public void Drain(float amount)
        {
            sanity = Mathf.Clamp01(sanity - amount);
            OnSanityChanged?.Invoke(sanity);
            var bb = FindAnyObjectByType<Room812.AI.GhostBlackboard>();
            if (bb != null) bb.squadSanity = sanity;
            heartbeatBpm = 72f + (1f - sanity) * 88f;
            if (sanity <= hapticThreshold) Pulse();
        }

        public void Restore(float amount)
        {
            sanity = Mathf.Clamp01(sanity + amount);
            OnSanityChanged?.Invoke(sanity);
            var bb = FindAnyObjectByType<Room812.AI.GhostBlackboard>();
            if (bb != null) bb.squadSanity = sanity;
        }

        void Pulse()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
