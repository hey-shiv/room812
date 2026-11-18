using UnityEngine;
using UnityEngine.InputSystem;
using Room812.Core;

namespace Room812.Audio
{
    /// <summary>
    /// Optional microphone input: real-world whispers/gasps become gameplay noise events that
    /// the ghost hears. Disabled by default; toggle with M. All processing is local.
    /// </summary>
    public class MicListener : MonoBehaviour
    {
        public bool micEnabled = false;
        public float amplitudeThreshold = 0.06f;
        public float emitCooldown = 0.7f;
        public float noiseRadius = 14f;

        string device;
        AudioClip clip;
        float[] buffer;
        float cooldown;

        void Start()
        {
            if (micEnabled) StartMic();
        }

        public void StartMic()
        {
            if (Microphone.devices.Length == 0) return;
            device = Microphone.devices[0];
            clip = Microphone.Start(device, true, 1, 44100);
            buffer = new float[4096];
            micEnabled = true;
        }

        public void StopMic()
        {
            if (!string.IsNullOrEmpty(device)) Microphone.End(device);
            clip = null;
            micEnabled = false;
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                if (micEnabled) StopMic(); else StartMic();
            }
            if (!micEnabled || clip == null || string.IsNullOrEmpty(device)) return;

            cooldown -= Time.deltaTime;
            int pos = Microphone.GetPosition(device);
            if (pos < 0) return;
            clip.GetData(buffer, Mathf.Max(0, pos - buffer.Length));
            float sum = 0f;
            for (int i = 0; i < buffer.Length; i++) sum += buffer[i] * buffer[i];
            float rms = Mathf.Sqrt(sum / buffer.Length);
            if (rms > amplitudeThreshold && cooldown <= 0f)
            {
                cooldown = emitCooldown;
                NoiseBus.Emit(transform.position, noiseRadius * Mathf.Clamp01(rms * 4f), gameObject);
            }
        }

        void OnDestroy() { StopMic(); }
    }
}
