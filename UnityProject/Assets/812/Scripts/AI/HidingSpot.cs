using UnityEngine;

namespace Room812.AI
{
    /// <summary>
    /// Marks a wardrobe / under-bunk hiding place. When the player stays near it long enough,
    /// the ghost's blackboard weights that spot higher for future hunts.
    /// </summary>
    public class HidingSpot : MonoBehaviour
    {
        public string spotId = "Wardrobe";
        public float detectionRadius = 1.4f;
        public float lingerSeconds = 2.5f;

        GhostBlackboard blackboard;
        float linger;

        void Start()
        {
            blackboard = FindAnyObjectByType<GhostBlackboard>();
        }

        void Update()
        {
            if (blackboard == null) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= detectionRadius)
            {
                linger += Time.deltaTime;
                if (linger >= lingerSeconds)
                {
                    blackboard.RememberHidingSpot(spotId, transform.position);
                    linger = 0f;
                }
            }
            else
            {
                linger = Mathf.Max(0f, linger - Time.deltaTime);
            }
        }
    }
}
