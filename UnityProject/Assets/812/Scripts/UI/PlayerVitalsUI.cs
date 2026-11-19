using UnityEngine;
using UnityEngine.UI;
using Room812.Core;

namespace Room812.UI
{
    /// <summary>Shows sanity and heart rate on the diegetic tablet, updated live.</summary>
    public class PlayerVitalsUI : MonoBehaviour
    {
        public Text readout;
        public SanityManager sanity;

        void Start()
        {
            if (sanity == null) sanity = FindAnyObjectByType<SanityManager>();
            if (readout == null)
            {
                var t = transform.Find("VitalsText");
                if (t != null) readout = t.GetComponent<Text>();
            }
        }

        void Update()
        {
            if (readout == null || sanity == null) return;
            string state = sanity.sanity > 0.75f ? "STABLE" : sanity.sanity > 0.4f ? "SHAKY" : "CRITICAL";
            readout.text = "SANITY " + Mathf.RoundToInt(sanity.sanity * 100f) + "%  |  HR " + Mathf.RoundToInt(sanity.heartbeatBpm) + " BPM  " + state;
        }
    }
}
