using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer; 

public class MatchmakingManager : MonoBehaviour
{
    public static MatchmakingManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async void FindMatch()
    {
        Debug.Log("[Matchmaking] Searching for sessions...");
        try
        {
            var query = new QuerySessionsOptions { Count = 1 };
            var result = await MultiplayerService.Instance.QuerySessionsAsync(query);

            if (result.Sessions.Count > 0)
            {
                Debug.Log($"[Matchmaking] Found session: {result.Sessions[0].Id}");
                await JoinGame(result.Sessions[0].Id);
            }
            else
            {
                Debug.Log("[Matchmaking] No sessions found. Hosting new...");
                await CreateGame();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Matchmaking] Error: {e.Message}");
        }
    }

    public async Task StartDedicatedServer()
    {
        Debug.Log("[Matchmaking] Starting Dedicated Server...");
        try 
        {
            // 1. Create Session
            var options = new SessionOptions
            {
                Name = $"Server-{Guid.NewGuid()}",
                MaxPlayers = 4, // Server + 2 Clients + Buffer
                IsPrivate = false,
                IsLocked = false
            }.WithRelayNetwork();

            var session = await MultiplayerService.Instance.CreateSessionAsync(options);
            Debug.Log($"[Matchmaking] Dedicated Session Created: {session.Id}");
            
            // 2. Start Server (Not Host)
            NetworkManager.Singleton.StartServer();
            NetworkManager.Singleton.SceneManager.LoadScene("Match", UnityEngine.SceneManagement.LoadSceneMode.Single);
            
            Debug.Log("[Matchmaking] Server Running. Waiting for players...");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Matchmaking] Server Start Failed: {e.Message}");
        }
    }

    private async Task CreateGame()
    {
        try 
        {
            var options = new SessionOptions
            {
                Name = $"Match-{Guid.NewGuid()}",
                MaxPlayers = 4, // Increased to allow testing (1 Host + 2-3 Clients)
                IsPrivate = false,
                IsLocked = false
            }.WithRelayNetwork();

            var session = await MultiplayerService.Instance.CreateSessionAsync(options);
            Debug.Log($"[Matchmaking] Created Session: {session.Id}");
            SetupEvents(session);
            LogPlayers(session);

            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene("Match", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Matchmaking] Create Failed: {e.Message}");
        }
    }

    private async Task JoinGame(string sessionId)
    {
        try
        {
            var session = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
            Debug.Log($"[Matchmaking] Joined Session: {session.Id}");
            SetupEvents(session);
            LogPlayers(session);

            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Matchmaking] Join Failed: {e.Message}");
        }
    }

    private void SetupEvents(ISession session)
    {
        // Note: Event names might vary slightly in SDK versions (e.g. PlayerJoined vs OnPlayerJoined)
        // Assuming standard 1.1 API
        /*
        session.PlayerJoined += (player) => 
        {
            Debug.Log($"[Session Event] Player Joined: {player.Id}");
            LogPlayers(session);
        };
        session.PlayerLeft += (player) => 
        {
            Debug.Log($"[Session Event] Player Left: {player.Id}");
        };
        */
        // Since I cannot verify the exact event API without docs, I will stick to logging current state.
    }

    private void LogPlayers(ISession session)
    {
        Debug.Log($"[Session] Players in Room ({session.Players.Count}):");
        foreach (var p in session.Players)
        {
            Debug.Log($" - {p.Id}");
        }
    }
}
