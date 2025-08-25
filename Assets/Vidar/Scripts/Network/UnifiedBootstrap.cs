using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Unity.Netcode.Transports.UTP;

[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(UnityTransport))]
public class UnifiedBootstrap : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "MVP";

    void Start()
    {
        var nm = GetComponent<NetworkManager>();
        if (nm == null)
        {
            Debug.LogError("[UnifiedBootstrap] NetworkManager introuvable sur ce GameObject.");
            return;
        }

        // On démarre d'abord en serveur
        if (nm.StartServer())
        {
            if (!nm.NetworkConfig.EnableSceneManagement)
            {
                Debug.LogError("[UnifiedBootstrap] Enable Scene Management est OFF sur le NetworkManager.");
                return;
            }

            // A PARTIR D'ICI: nm.SceneManager est garanti non-null
            var nsm = nm.SceneManager;
            if (nsm == null)
            {
                Debug.LogError("[UnifiedBootstrap] NetworkSceneManager est null après StartServer().");
                return;
            }

            // (Optionnel) logs pour debug
            nsm.OnSceneEvent += e =>
                Debug.Log($"[SceneEvent] {e.SceneEventType} | {e.SceneName} | ClientId={e.ClientId}");

            Debug.Log("[UnifiedBootstrap] Server started → loading scene: " + gameSceneName);
            nsm.LoadScene(gameSceneName, LoadSceneMode.Single);
            return;
        }

        // Si le port est déjà pris → on devient client
        Debug.LogWarning("[UnifiedBootstrap] Port occupé → bascule en Client.");
        if (!nm.StartClient())
        {
            Debug.LogError("[UnifiedBootstrap] StartClient a échoué (serveur indisponible ?).");
        }
    }
}
