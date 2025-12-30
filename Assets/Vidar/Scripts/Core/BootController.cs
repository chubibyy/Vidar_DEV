using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class BootController : MonoBehaviour
{
    [SerializeField] private string menuSignScene = "Menu-Sign";

    private async void Start()
    {
        Debug.Log("[Boot] Starting initialization...");

        // 1. Initialize Authentication (Auto-login is handled inside AuthManager)
        if (AuthenticationManager.Instance != null)
        {
            await AuthenticationManager.Instance.InitializeAsync();
        }
        else
        {
            Debug.LogError("[Boot] AuthenticationManager is missing from the scene!");
            return;
        }

        // 2. Load User Data
        if (PlayerDataManager.Instance != null)
        {
            await PlayerDataManager.Instance.LoadProfileAsync();
        }
        else
        {
            Debug.LogError("[Boot] PlayerDataManager is missing from the scene!");
            return;
        }

        Debug.Log($"[Boot] Initialization complete. Loading {menuSignScene}...");
        SceneManager.LoadScene(menuSignScene);
    }
}