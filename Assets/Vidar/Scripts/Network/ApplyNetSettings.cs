using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(UnityTransport))]
public class ApplyNetSettings : MonoBehaviour
{
    [SerializeField] private NetSettings settings;

    void Awake()
    {
        if (!settings)
        {
            Debug.LogError("[ApplyNetSettings] NetSettings manquant.");
            return;
        }

        // Prend les composants sur le même GameObject
        var nm  = GetComponent<NetworkManager>();
        var utp = GetComponent<UnityTransport>();

        // Options de transport
        utp.UseWebSockets = settings.useWebSockets;

        // Port (int dans l'asset, cast en ushort)
        int p = Mathf.Clamp(settings.port, 1, 65535);
        ushort portU16 = (ushort)p;

        // Configure l’adresse de connexion (client) + l’adresse d’écoute (serveur)
        utp.SetConnectionData(settings.serverAddress, portU16, settings.serverListenAddress);

        Debug.Log($"[ApplyNetSettings] Config → connect {settings.serverAddress}:{p} | listen {settings.serverListenAddress}:{p}");
    }
    
}
