using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class DedicatedServerManager : MonoBehaviour
{
    public static DedicatedServerManager Instance { get; private set; }
    
    private bool _isServerRunning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task StartServerService()
    {
        Debug.Log("[Server] Initializing Dedicated Server...");
        
        if (_isServerRunning) 
        {
            Debug.LogWarning("[Server] Server already running.");
            return;
        }

        // Configure Port from Args or Env
        ushort port = 7777; 
        
        // 1. Check Command Line Args
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-port" && i + 1 < args.Length)
                ushort.TryParse(args[i + 1], out port);
        }

        // 2. Check Environment Variables (Edgegap/Docker friendly)
        string envPort = System.Environment.GetEnvironmentVariable("SERVER_PORT");
        if (!string.IsNullOrEmpty(envPort) && ushort.TryParse(envPort, out ushort parsedPort))
        {
            port = parsedPort;
        }

        Debug.Log($"[Server] Starting Netcode Server on Port {port}");
        
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[Server] NetworkManager.Singleton is NULL! Is the NetworkManager prefab in the scene?");
            return;
        }

        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (utp == null)
        {
            Debug.LogError("[Server] UnityTransport component missing on NetworkManager!");
            return;
        }

        // Bind to all interfaces
        utp.SetConnectionData("0.0.0.0", port);

        NetworkManager.Singleton.StartServer();
        
        // Load Match Scene immediately
        NetworkManager.Singleton.SceneManager.LoadScene("Match", UnityEngine.SceneManagement.LoadSceneMode.Single);
        
        _isServerRunning = true;
        
        await Task.CompletedTask;
    }
}

