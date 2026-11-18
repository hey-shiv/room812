using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Room812.Network
{
    /// <summary>
    /// Push-to-talk squad walkie-talkie over NGO custom messages. No external voice server —
    /// audio travels over the same local UDP connection as the game. Hold V to transmit.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class WalkieTalkie : NetworkBehaviour
    {
        const string Channel = "812Voice";
        const int SampleRate = 22050;
        const int Chunk = 1024;
        const float SendInterval = 0.12f;

        AudioSource speaker;
        AudioClip micClip;
        string device;
        bool transmitting;
        float sendTimer;
        readonly float[] buffer = new float[Chunk * 2];

        void Awake()
        {
            speaker = GetComponent<AudioSource>();
            if (speaker == null) speaker = gameObject.AddComponent<AudioSource>();
            speaker.spatialBlend = 0f;
            speaker.playOnAwake = false;
        }

        public override void OnNetworkSpawn()
        {
            if (NetworkManager != null && NetworkManager.CustomMessagingManager != null)
                NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(Channel, OnVoice);
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager != null && NetworkManager.CustomMessagingManager != null)
                NetworkManager.CustomMessagingManager.UnregisterNamedMessageHandler(Channel);
            StopTransmission();
        }

        void Update()
        {
            if (!IsSpawned) return;
            if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
            {
                if (transmitting) StopTransmission();
                else StartTransmission();
            }
            if (!transmitting || micClip == null || string.IsNullOrEmpty(device)) return;

            sendTimer += Time.deltaTime;
            if (sendTimer < SendInterval) return;
            sendTimer = 0f;

            int pos = Microphone.GetPosition(device);
            if (pos <= 0) return;
            micClip.GetData(buffer, Mathf.Max(0, pos - Chunk));

            var pcm = new short[Chunk];
            for (int i = 0; i < Chunk; i++) pcm[i] = (short)(Mathf.Clamp(buffer[i], -1f, 1f) * 32767f);

            var targets = new List<ulong>();
            foreach (var id in NetworkManager.ConnectedClientsIds)
                if (id != NetworkManager.LocalClientId) targets.Add(id);
            if (targets.Count == 0) return;

            using (var writer = new FastBufferWriter(Chunk * 2 + 16, Allocator.Temp))
            {
                writer.WriteValueSafe(pcm);
                NetworkManager.CustomMessagingManager.SendNamedMessage(Channel, targets, writer, NetworkDelivery.UnreliableSequenced);
            }
        }

        void StartTransmission()
        {
            if (Microphone.devices.Length == 0) return;
            device = Microphone.devices[0];
            micClip = Microphone.Start(device, true, 1, SampleRate);
            transmitting = true;
        }

        void StopTransmission()
        {
            if (!string.IsNullOrEmpty(device)) Microphone.End(device);
            micClip = null;
            transmitting = false;
        }

        void OnVoice(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out short[] pcm);
            if (pcm == null || pcm.Length == 0) return;
            var samples = new float[pcm.Length];
            for (int i = 0; i < pcm.Length; i++) samples[i] = pcm[i] / 32767f;
            var clip = AudioClip.Create("WalkieTalkie", samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            if (speaker != null) speaker.PlayOneShot(clip);
            Destroy(clip, 2f);
        }

        void OnDestroy() { StopTransmission(); }
    }
}
