using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Linq;

public class BootController : MonoBehaviour
{
    [SerializeField] private string menuSignScene = "Menu-Sign";

    private async void Start()
    {
        Debug.Log("[Boot] Starting initialization...");

        // 1. Initialize Authentication
        if (AuthenticationManager.Instance != null)
        {
            await AuthenticationManager.Instance.InitializeAsync();
        }
        else
        {
            Debug.LogError("[Boot] AuthenticationManager is missing from the scene!");
            return;
        }

        // 2. Dedicated Server Check
#if UNITY_SERVER
        Debug.Log("[Boot] Dedicated Server Build detected (UNITY_SERVER). Initializing Multiplay...");
        if (DedicatedServerManager.Instance != null)
        {
            await DedicatedServerManager.Instance.StartServerService();
            return; // Stop here, Server waits in this scene
        }
#endif

        // 3. Fallback: Check for manual "-mode server" arg (for local testing without Server Build)
        string[] args = System.Environment.GetCommandLineArgs();
        if (args.Contains("-mode") && args.Contains("server"))
        {
            Debug.Log("[Boot] Argument '-mode server' detected.");
            if (DedicatedServerManager.Instance != null)
            {
                await DedicatedServerManager.Instance.StartServerService();
                return;
            }
        }

        // 4. Client Flow
        if (PlayerDataManager.Instance != null)
        {
            await PlayerDataManager.Instance.LoadProfileAsync();
        }

        Debug.Log($"[Boot] Client Mode. Loading {menuSignScene}...");
        SceneManager.LoadScene(menuSignScene);
    }
}