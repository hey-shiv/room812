using UnityEngine;

namespace Room812.FX
{
    /// <summary>Frost slowly creeps across window panes instead of appearing instantly.</summary>
    public class FrostCreeper : MonoBehaviour
    {
        public float fullSizeSeconds = 8f;
        public Vector3 fullScale = new Vector3(0.8f, 0.6f, 1f);

        float t;

        void Start()
        {
            transform.localScale = new Vector3(0.01f, 0.01f, 1f);
        }

        void Update()
        {
            if (t >= fullSizeSeconds) return;
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / fullSizeSeconds);
            f = f * f * (3f - 2f * f);
            transform.localScale = Vector3.Lerp(new Vector3(0.01f, 0.01f, 1f), fullScale, f);
        }
    }
}
