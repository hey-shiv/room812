using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Room812.Network
{
    /// <summary>
    /// Minimal LAN/Wi-Fi-hotspot lobby. Host or join by IP, with UDP broadcast discovery.
    /// No Relay, no cloud, no internet required.
    /// </summary>
    public class LanLobby : MonoBehaviour
    {
        public GameObject playerPrefab;
        public GameObject ghostObject;
        public ushort port = 7777;
        public string joinIp = "127.0.0.1";

        NetworkManager manager;
        LanDiscovery discovery;
        string status = "Offline";
        bool showLobby = true;
        string autoJoinIp = "";

        void Start()
        {
            manager = NetworkManager.Singleton;
            discovery = gameObject.GetComponent<LanDiscovery>();
            if (discovery == null) discovery = gameObject.AddComponent<LanDiscovery>();
            if (discovery != null)
            {
                discovery.StartListener();
                discovery.HostFound += (ip, name) => { if (string.IsNullOrEmpty(joinIp) || joinIp == "127.0.0.1") joinIp = ip; };
            }
            if (manager != null)
            {
                manager.OnClientConnectedCallback += id => Debug.Log("[812] client connected: " + id);
                manager.OnClientDisconnectCallback += id => Debug.Log("[812] client disconnected: " + id);
                var args = System.Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++)
                    if (args[i] == "-join") autoJoinIp = args[i + 1];
                if (!string.IsNullOrEmpty(autoJoinIp)) Invoke("AutoJoin", 1f);
                var transport = manager.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    manager.NetworkConfig.NetworkTransport = transport;
                    transport.SetConnectionData("127.0.0.1", port);
                }
                if (playerPrefab != null && manager.NetworkConfig != null)
                    manager.NetworkConfig.PlayerPrefab = playerPrefab;
            }
        }

        void AutoJoin() { Join(autoJoinIp); }

        public void Host()
        {
            if (manager == null) return;
            var scenePlayer = GameObject.Find("Player_Investigator");
            if (scenePlayer != null) scenePlayer.SetActive(false);
            if (discovery != null) discovery.StartBeacon();
            var transport = manager.GetComponent<UnityTransport>();
            if (transport != null) transport.SetConnectionData("0.0.0.0", port);
            manager.StartHost();
            status = "Hosting on UDP " + port;
            showLobby = false;
        }

        public void Join(string ip)
        {
            if (manager == null || string.IsNullOrEmpty(ip)) return;
            var scenePlayer = GameObject.Find("Player_Investigator");
            if (scenePlayer != null) scenePlayer.SetActive(false);
            var transport = manager.GetComponent<UnityTransport>();
            if (transport != null) transport.SetConnectionData(ip, port);
            manager.StartClient();
            status = "Joining " + ip + ":" + port;
            showLobby = false;
        }

        public void Shutdown()
        {
            if (manager != null && manager.IsListening) manager.Shutdown();
            var scenePlayer = GameObject.Find("Player_Investigator");
            if (scenePlayer != null) scenePlayer.SetActive(true);
            status = "Offline";
            showLobby = true;
        }

        void OnGUI()
        {
            if (!showLobby) return;
            GUILayout.BeginArea(new Rect(20f, 20f, 320f, 300f), GUI.skin.box);
            GUILayout.Label("812 // LAN INVESTIGATION");
            GUILayout.Label(status);
            GUILayout.Space(10f);
            if (GUILayout.Button("Host game (Wi-Fi / hotspot)")) Host();
            GUILayout.Label("Join IP:");
            joinIp = GUILayout.TextField(joinIp);
            if (GUILayout.Button("Join")) Join(joinIp);
            GUILayout.Label("Broadcast discovery port 47777");
            GUILayout.EndArea();
        }
    }
}
