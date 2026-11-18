using Unity.Netcode;
using UnityEngine;
using Room812.AI;

namespace Room812.Network
{
    /// <summary>Server-authoritative ghost transform sync. Only the host runs the AI brain.</summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkedGhost : NetworkBehaviour
    {
        public NetworkVariable<Vector3> NetPosition = new NetworkVariable<Vector3>(
            Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> NetState = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        GhostBrain brain;

        void Awake() { brain = GetComponent<GhostBrain>(); }

        public override void OnNetworkSpawn()
        {
            if (brain != null) brain.enabled = IsServer;
        }

        void Update()
        {
            if (IsServer)
            {
                NetPosition.Value = transform.position;
                if (brain != null) NetState.Value = (int)brain.state;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, NetPosition.Value, Time.deltaTime * 10f);
            }
        }
    }
}
