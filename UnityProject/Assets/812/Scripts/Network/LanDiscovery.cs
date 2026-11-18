using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Room812.Network
{
    /// <summary>
    /// Zero-cloud LAN discovery. Hosts broadcast a small UDP beacon; clients listen and show
    /// discovered hosts. Direct-IP join always works as a fallback (mobile hotspot friendly).
    /// </summary>
    public class LanDiscovery : MonoBehaviour
    {
        public int port = 47777;
        public float beaconInterval = 2f;
        public string HostName = "812 Host";
        public event Action<string, string> HostFound; // ip, name

        UdpClient beacon;
        UdpClient listener;
        IPEndPoint broadcast;
        float timer;
        readonly Dictionary<string, float> seen = new Dictionary<string, float>();

        public void StartBeacon()
        {
            try
            {
                beacon = new UdpClient { EnableBroadcast = true };
                broadcast = new IPEndPoint(IPAddress.Broadcast, port);
            }
            catch (Exception e) { Debug.LogWarning("[812] UDP beacon unavailable: " + e.Message); }
        }

        public void StartListener()
        {
            try
            {
                listener = new UdpClient(port);
                listener.Client.Blocking = false;
            }
            catch (Exception e) { Debug.LogWarning("[812] UDP listener unavailable: " + e.Message); }
        }

        void Update()
        {
            if (beacon != null)
            {
                timer += Time.deltaTime;
                if (timer >= beaconInterval)
                {
                    timer = 0f;
                    byte[] data = Encoding.UTF8.GetBytes("812|" + HostName);
                    try { beacon.Send(data, data.Length, broadcast); } catch { }
                }
            }

            if (listener != null)
            {
                try
                {
                    while (listener.Available > 0)
                    {
                        IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
                        byte[] data = listener.Receive(ref from);
                        string msg = Encoding.UTF8.GetString(data);
                        if (!msg.StartsWith("812|")) continue;
                        string ip = from.Address.ToString();
                        if (seen.ContainsKey(ip) && Time.realtimeSinceStartup - seen[ip] < 8f) continue;
                        seen[ip] = Time.realtimeSinceStartup;
                        HostFound?.Invoke(ip, msg.Substring(4));
                    }
                }
                catch (SocketException) { }
                catch (Exception) { }
            }
        }

        void OnDestroy()
        {
            if (beacon != null) { beacon.Close(); beacon = null; }
            if (listener != null) { listener.Close(); listener = null; }
        }
    }
}
