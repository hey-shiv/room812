using System;
using UnityEngine;

namespace Room812.Core
{
    /// <summary>
    /// Lightweight local event bus for gameplay noise. The ghost AI subscribes to this
    /// to build its memory of where players make sound. No network or cloud involved.
    /// </summary>
    public static class NoiseBus
    {
        public static event Action<Vector3, float, GameObject> Emitted;

        public static void Emit(Vector3 position, float radius, GameObject source)
        {
            Emitted?.Invoke(position, radius, source);
        }
    }
}
