using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetDebug : MonoBehaviour
{
    void Start()
    {
        var nm = NetworkManager.Singleton;
        var utp = nm?.NetworkConfig.NetworkTransport as UnityTransport;
        Debug.Log($"[NetDebug] isServer={nm?.IsServer} isClient={nm?.IsClient} addr={utp?.ConnectionData.Address} port={utp?.ConnectionData.Port}");
        if (nm != null)
        {
            nm.OnClientConnectedCallback += id => Debug.Log($"[NetDebug] OnClientConnected id={id}");
            nm.OnClientDisconnectCallback += id => Debug.Log($"[NetDebug] OnClientDisconnected id={id}");
        }
    }
}
