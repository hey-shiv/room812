using System;
using System.Collections.Generic;
using UnityEngine;

namespace Room812.Audio
{
    /// <summary>
    /// Marks a room acoustic profile (washroom tile reverb vs dampened dorm, etc.).
    /// The active zone is resolved from the listener position each frame; Steam Audio
    /// reverb/occlusion parameters will be driven from this once the SDK is installed.
    /// </summary>
    public class AcousticZone : MonoBehaviour
    {
        public enum RoomType { Dorm, Washroom, Corridor, WardenOffice, Room812, Rooftop, Stairwell }

        public RoomType roomType = RoomType.Dorm;
        [Range(0f, 3f)] public float reverbTime = 0.5f;
        [Range(100f, 20000f)] public float lowPassHz = 14000f;
        [Range(0f, 1f)] public float muffling = 0f;

        static readonly List<AcousticZone> All = new List<AcousticZone>();
        Collider zoneCollider;

        public static AcousticZone Current { get; private set; }
        public static event Action<AcousticZone> Changed;

        void OnEnable()
        {
            zoneCollider = GetComponent<Collider>();
            if (All.IndexOf(this) < 0) All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
            if (Current == this) Current = null;
        }

        public static void Resolve(Vector3 listenerPosition)
        {
            AcousticZone found = null;
            for (int i = 0; i < All.Count; i++)
            {
                var zone = All[i];
                if (zone == null || zone.zoneCollider == null) continue;
                if (zone.zoneCollider.bounds.Contains(listenerPosition))
                {
                    found = zone;
                    break;
                }
            }
            if (found != Current)
            {
                Current = found;
                Changed?.Invoke(found);
            }
        }
    }
}
