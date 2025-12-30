using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class AuthenticationManager : MonoBehaviour
{
    public static AuthenticationManager Instance { get; private set; }
    public string PlayerId { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task InitializeAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("[Auth] Unity Services Initialized");

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await SignInAnonymouslyAsync();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Auth] Initialization Failed: {e.Message}");
        }
    }

    private async Task SignInAnonymouslyAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("[Auth] Signed in Anonymously");
            Debug.Log($"[Auth] PlayerID: {AuthenticationService.Instance.PlayerId}");
            PlayerId = AuthenticationService.Instance.PlayerId;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"[Auth] Sign In Failed: {ex.ErrorCode} - {ex.Message}");
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"[Auth] Request Failed: {ex.ErrorCode} - {ex.Message}");
        }
    }
}
