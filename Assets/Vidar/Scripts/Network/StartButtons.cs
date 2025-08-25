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
            NetworkManager.Singleton.StartServer();
            // Charger la scène "MVP" en mode Single
            NetworkManager.Singleton.SceneManager.LoadScene(
                "MVP",
                LoadSceneMode.Single);
        });

        btnClient.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });
    }
}
