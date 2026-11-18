using UnityEngine;
using Room812.Core;

namespace Room812.Audio
{
    /// <summary>Plays footstep and heartbeat clips from the player's live state.</summary>
    public class PlayerAudioFeedback : MonoBehaviour
    {
        public AudioSource footstepSource;
        public AudioSource heartbeatSource;
        public HostelPlayerController controller;
        public SanityManager sanity;

        float heartTimer;

        void Start()
        {
            if (controller == null) controller = GetComponent<HostelPlayerController>();
            if (sanity == null) sanity = FindAnyObjectByType<SanityManager>();
            if (controller != null) controller.onFootstep.AddListener(PlayFootstep);
        }

        void OnDestroy()
        {
            if (controller != null) controller.onFootstep.RemoveListener(PlayFootstep);
        }

        void PlayFootstep()
        {
            if (footstepSource != null) footstepSource.Play();
        }

        void Update()
        {
            if (heartbeatSource == null || sanity == null) return;
            if (sanity.sanity >= 0.7f) return;
            heartTimer -= Time.deltaTime;
            if (heartTimer <= 0f)
            {
                heartTimer = 60f / Mathf.Max(40f, sanity.heartbeatBpm);
                heartbeatSource.Play();
            }
        }
    }
}
