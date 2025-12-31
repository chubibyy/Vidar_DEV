using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

[RequireComponent(typeof(EdgegapLauncher))]
public class MatchmakingManager : MonoBehaviour
{
    public static MatchmakingManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (GetComponent<EdgegapLauncher>() == null) gameObject.AddComponent<EdgegapLauncher>();
    }

    public async void FindMatch()
    {
        Debug.Log("[Matchmaking] Requesting Server via EdgegapLauncher...");
        
        if (AuthenticationManager.Instance == null || string.IsNullOrEmpty(AuthenticationManager.Instance.PlayerId))
        {
            Debug.LogError("Not Authenticated!");
            return;
        }

        try
        {
            string publicIp = await GetPublicIpAsync();
            Debug.Log($"[Matchmaking] Client IP: {publicIp}");

            EdgegapLauncher.Instance.DeployServer(
                publicIp,
                OnServerAllocated,
                OnMatchmakingFailed
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[Matchmaking] Error: {e.Message}");
        }
    }

    private async void OnServerAllocated(string ip, int port)
    {
        Debug.Log($"[Matchmaking] Server Ready at {ip}:{port}. Waiting 2s for warmup...");
        await Task.Delay(2000); 
        Debug.Log($"[Matchmaking] Connecting...");
        ConnectToServer(ip, port);
    }

    private void OnMatchmakingFailed(string error)
    {
        Debug.LogError($"[Matchmaking] Failed: {error}");
    }

    private async Task<string> GetPublicIpAsync()
    {
        using (UnityWebRequest request = UnityWebRequest.Get("https://api.ipify.org"))
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                return request.downloadHandler.text;
            }
        }
        return "0.0.0.0"; 
    }

    private void ConnectToServer(string ip, int port)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, (ushort)port);
        NetworkManager.Singleton.StartClient();
    }
}