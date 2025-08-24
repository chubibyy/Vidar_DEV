using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class StartButtons : MonoBehaviour
{
    public Button btnHost;
    public Button btnClient;

    void Start()
    {
        btnHost.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();

            // Une fois qu'on est Host, on charge la scène Sandbox
            if (NetworkManager.Singleton.IsServer)
            {
                // Charger la scène "MVP" en mode Single
                NetworkManager.Singleton.SceneManager.LoadScene(
                    "MVP",
                    LoadSceneMode.Single
                );
            }
        });

        btnClient.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });
    }
}
