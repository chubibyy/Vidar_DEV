using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(UnityTransport))]
public class UnifiedBootstrap : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "MVP";
    [SerializeField] private NetSettings fallbackSettings; // assigne ton NetSettings.asset ici

    void Start()
    {
        var nm  = GetComponent<NetworkManager>();
        var utp = GetComponent<UnityTransport>();

        // Sécurité: si ApplyNetSettings n’a pas tourné, on pousse la config ici
        EnsureTransportConfigured(utp);

        // 1) Essayer serveur
        if (nm.StartServer())
        {
            if (!nm.NetworkConfig.EnableSceneManagement)
            {
                Debug.LogError("[UnifiedBootstrap] Enable Scene Management est OFF.");
                return;
            }

            var nsm = nm.SceneManager;
            if (nsm == null)
            {
                Debug.LogError("[UnifiedBootstrap] NetworkSceneManager null après StartServer().");
                return;
            }

            nsm.OnSceneEvent += e => Debug.Log($"[SceneEvent] {e.SceneEventType} | {e.SceneName} | ClientId={e.ClientId}");
            Debug.Log("[UnifiedBootstrap] Server started → loading scene: " + gameSceneName);
            nsm.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            return;
        }

        // 2) Port déjà pris → patienter que le shutdown interne se termine puis démarrer Client proprement
        Debug.LogWarning("[UnifiedBootstrap] Port occupé → bascule en Client (reset transport)...");
        StartCoroutine(StartClientNextFrame(nm, utp));
    }

    private IEnumerator StartClientNextFrame(NetworkManager nm, UnityTransport utp)
    {
        // Le StartServer() raté déclenche un shutdown interne asynchrone : on attend 1 frame
        yield return null;

        // On re-pousse *explicitement* l’adresse/port côté client (sans listenAddress)
        if (fallbackSettings != null)
        {
            int p = Mathf.Clamp(fallbackSettings.port, 1, 65535);
            utp.SetConnectionData(fallbackSettings.serverAddress, (ushort)p);
            Debug.Log($"[UnifiedBootstrap] Client config → connect {fallbackSettings.serverAddress}:{p}");
        }

        // Safety: si Netcode était encore actif, on le stoppe avant de relancer le client
        if (nm.IsListening) nm.Shutdown();

        // Maintenant on démarre le client
        if (!nm.StartClient())
        {
            Debug.LogError("[UnifiedBootstrap] StartClient a échoué (serveur indisponible ?).");
        }
    }

    private void EnsureTransportConfigured(UnityTransport utp)
    {
        string addr = utp.ConnectionData.Address;
        bool invalid = string.IsNullOrEmpty(addr) || utp.ConnectionData.Port == 0;
        if (invalid && fallbackSettings != null)
        {
            int p = Mathf.Clamp(fallbackSettings.port, 1, 65535);
            utp.SetConnectionData(fallbackSettings.serverAddress, (ushort)p, fallbackSettings.serverListenAddress);
            Debug.LogWarning($"[UnifiedBootstrap] Transport non configuré → fallback {fallbackSettings.serverAddress}:{p} | listen {fallbackSettings.serverListenAddress}:{p}");
        }
    }
}
