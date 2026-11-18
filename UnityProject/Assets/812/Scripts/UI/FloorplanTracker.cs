using UnityEngine;
using UnityEngine.UI;

namespace Room812.UI
{
    /// <summary>Drives the player dot on the diegetic tablet floorplan from world position.</summary>
    public class FloorplanTracker : MonoBehaviour
    {
        public Transform player;
        public RectTransform dot;
        public RectTransform map;
        public float worldSizeX = 18f;
        public float worldSizeZ = 14f;

        void LateUpdate()
        {
            if (player == null || dot == null || map == null) return;
            float nx = Mathf.Clamp01((player.position.x + worldSizeX * 0.5f) / worldSizeX);
            float nz = Mathf.Clamp01((player.position.z + worldSizeZ * 0.5f) / worldSizeZ);
            Vector2 size = map.rect.size;
            dot.anchoredPosition = new Vector2((nx - 0.5f) * (size.x - 16f), (nz - 0.5f) * (size.y - 16f));
        }
    }
}
