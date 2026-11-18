using UnityEngine;

namespace Room812.FX
{
    /// <summary>Marks a floor area as wet (water or blood) so footsteps leave fading prints.</summary>
    public class WetSurface : MonoBehaviour
    {
        public enum WetType { Water, Blood }
        public WetType wetType = WetType.Water;
        public float dryness = 1f;
    }
}
