using System;
using System.Collections.Generic;
using UnityEngine;

namespace Room812.AI
{
    /// <summary>
    /// Local-only memory for the ghost. Tracks noise events and learns which hiding spots
    /// the squad keeps returning to. No network, no external services.
    /// </summary>
    public class GhostBlackboard : MonoBehaviour
    {
        [Serializable]
        public class HidingSpotMemory
        {
            public string id;
            public Vector3 position;
            public float weight = 1f;
            public int visits;
            public float lastRemembered;
        }

        [Range(0f, 1f)] public float aggression = 0.15f;
        [Range(0f, 1f)] public float squadSanity = 1f;
        public Vector3 lastNoisePosition;
        public float lastNoiseRadius;
        public float lastNoiseTime = -999f;
        public Vector3 target;
        public bool hasTarget;

        readonly Dictionary<string, HidingSpotMemory> hidingSpots = new Dictionary<string, HidingSpotMemory>();

        public void RememberNoise(Vector3 position, float radius)
        {
            lastNoisePosition = position;
            lastNoiseRadius = radius;
            lastNoiseTime = Time.time;
            target = position;
            hasTarget = true;
        }

        public void RememberHidingSpot(string id, Vector3 position)
        {
            HidingSpotMemory spot;
            if (!hidingSpots.TryGetValue(id, out spot))
            {
                spot = new HidingSpotMemory { id = id, position = position };
                hidingSpots[id] = spot;
            }
            spot.visits++;
            spot.weight = Mathf.Min(5f, spot.weight + 0.35f);
            spot.lastRemembered = Time.time;
        }

        public HidingSpotMemory MostWeightedSpot()
        {
            HidingSpotMemory best = null;
            foreach (var kv in hidingSpots)
                if (best == null || kv.Value.weight > best.weight) best = kv.Value;
            return best;
        }

        public int RememberedSpotCount { get { return hidingSpots.Count; } }
    }
}
