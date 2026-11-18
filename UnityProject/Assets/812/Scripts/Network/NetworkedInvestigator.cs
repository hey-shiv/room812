using Unity.Netcode;
using UnityEngine;
using Room812.Core;

namespace Room812.Network
{
    /// <summary>
    /// Owner-authoritative transform + sanity sync for an investigator. Character movement
    /// stays local (responsive on mobile); the host validates/relays state to other clients.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkedInvestigator : NetworkBehaviour
    {
        public NetworkVariable<Vector3> NetPosition = new NetworkVariable<Vector3>(
            Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<Vector3> NetRotation = new NetworkVariable<Vector3>(
            Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<float> NetSanity = new NetworkVariable<float>(
            1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        HostelPlayerController controller;

        void Awake() { controller = GetComponent<HostelPlayerController>(); }

        public override void OnNetworkSpawn()
        {
            if (controller != null) controller.enabled = IsOwner;
            if (IsOwner)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    cam.transform.SetParent(transform, false);
                    cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                }
            }
        }

        void Update()
        {
            if (IsOwner)
            {
                NetPosition.Value = transform.position;
                NetRotation.Value = transform.eulerAngles;
                var sanity = SanityManager.Instance;
                if (sanity != null) NetSanity.Value = sanity.sanity;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, NetPosition.Value, Time.deltaTime * 14f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(NetRotation.Value), Time.deltaTime * 14f);
            }
        }
    }
}
