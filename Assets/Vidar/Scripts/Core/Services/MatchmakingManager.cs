using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

[RequireComponent(typeof(EdgegapLauncher))]
public class MatchmakingManager : MonoBehaviour
{
    public static MatchmakingManager Instance { get; private set; }

    private Lobby _currentLobby;
    private bool _isHost = false;
    private const string KEY_IP = "ServerIP";
    private const string KEY_PORT = "ServerPort";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (GetComponent<EdgegapLauncher>() == null) gameObject.AddComponent<EdgegapLauncher>();
    }

    public async void FindMatch()
    {
        Debug.Log("[Matchmaking] Looking for a match...");
        
        if (AuthenticationManager.Instance == null || string.IsNullOrEmpty(AuthenticationManager.Instance.PlayerId))
        {
            Debug.LogError("Not Authenticated!");
            return;
        }

        try
        {
            // 1. Try to Join Existing Lobby
            Debug.Log("[Matchmaking] Attempting QuickJoin...");
            var options = new QuickJoinLobbyOptions();
            _currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            
            Debug.Log($"[Matchmaking] Joined Lobby: {_currentLobby.Id}");
            _isHost = false;
            StartCoroutine(ClientLobbyPollRoutine());
        }
        catch (LobbyServiceException e)
        {
            // 2. If no lobby found, Create One
            Debug.Log($"[Matchmaking] No lobbies found ({e.ErrorCode}). Creating new one...");
            await CreateLobbyAndDeploy();
        }
    }

    private async Task CreateLobbyAndDeploy()
    {
        try
        {
            string lobbyName = "VidarMatch_" + Guid.NewGuid().ToString().Substring(0, 5);
            int maxPlayers = 2;
            
            var lobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = new Player(AuthenticationManager.Instance.PlayerId),
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_IP, new DataObject(DataObject.VisibilityOptions.Public, "0.0.0.0") },
                    { KEY_PORT, new DataObject(DataObject.VisibilityOptions.Public, "0") }
                }
            };

            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, lobbyOptions);
            _isHost = true;
            Debug.Log($"[Matchmaking] Lobby Created: {_currentLobby.Id}. Deploying Server...");

            // Start Heartbeat to keep lobby alive
            StartCoroutine(HostLobbyHeartbeatRoutine());

            // Deploy Edgegap Server
            string publicIp = await GetPublicIpAsync();
            EdgegapLauncher.Instance.DeployServer(publicIp, OnServerDeployed, OnDeployFailed);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Matchmaking] Create Lobby Failed: {e.Message}");
        }
    }

    // --- Host Logic ---

    private async void OnServerDeployed(string ip, int port)
    {
        Debug.Log($"[Matchmaking] Server Deployed at {ip}:{port}. Updating Lobby...");

        // Update Lobby with IP/Port so Client can find it
        try
        {
            var updateOptions = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_IP, new DataObject(DataObject.VisibilityOptions.Public, ip) },
                    { KEY_PORT, new DataObject(DataObject.VisibilityOptions.Public, port.ToString()) }
                }
            };

            _currentLobby = await LobbyService.Instance.UpdateLobbyAsync(_currentLobby.Id, updateOptions);
            
            // Connect Host
            Debug.Log("[Matchmaking] Connecting Host...");
            await Task.Delay(2000); // Warmup
            ConnectToServer(ip, port);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Matchmaking] Failed to update Lobby: {e.Message}");
        }
    }

    private void OnDeployFailed(string error)
    {
        Debug.LogError($"[Matchmaking] Deployment Failed: {error}. Deleting Lobby.");
        if (_currentLobby != null)
        {
            LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
            _currentLobby = null;
        }
    }

    private IEnumerator HostLobbyHeartbeatRoutine()
    {
        var wait = new WaitForSeconds(15);
        while (_currentLobby != null && _isHost)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
            yield return wait;
        }
    }

    // --- Client Logic ---

    private IEnumerator ClientLobbyPollRoutine()
    {
        var wait = new WaitForSeconds(2);
        while (_currentLobby != null && !_isHost)
        {
            var task = LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Exception != null)
            {
                Debug.LogError("[Matchmaking] Failed to poll Lobby.");
                _currentLobby = null;
                yield break;
            }

            _currentLobby = task.Result;

            // Check if IP is ready
            if (_currentLobby.Data.ContainsKey(KEY_IP) && _currentLobby.Data.ContainsKey(KEY_PORT))
            {
                string ip = _currentLobby.Data[KEY_IP].Value;
                string portStr = _currentLobby.Data[KEY_PORT].Value;

                if (ip != "0.0.0.0" && int.TryParse(portStr, out int port) && port > 0)
                {
                    Debug.Log($"[Matchmaking] Server Info Found: {ip}:{port}. Connecting...");
                    ConnectToServer(ip, port);
                    yield break; // Stop polling
                }
            }
            
            Debug.Log("[Matchmaking] Waiting for Host to provision server...");
            yield return wait;
        }
    }

    // --- Helpers ---

    private void ConnectToServer(string ip, int port)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, (ushort)port);
        NetworkManager.Singleton.StartClient();
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
    
    private void OnDestroy()
    {
        // Cleanup if Host
        if (_currentLobby != null && _isHost)
        {
            LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
        }
    }
}
