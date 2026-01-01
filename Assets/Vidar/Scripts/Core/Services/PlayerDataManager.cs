using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudSave;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }
    
    [Header("Config")]
    [SerializeField] private CardRegistry cardRegistry;
    [SerializeField] private int packCost = 100;

    // The locally cached profile
    public PlayerProfile CurrentProfile { get; private set; }

    private const string PROFILE_KEY = "player_profile";

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

    /// <summary>
    /// Loads the player profile from the Cloud Database.
    /// If none exists, creates a new one.
    /// </summary>
    public async Task LoadProfileAsync()
    {
        try
        {
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { PROFILE_KEY });

            if (data.TryGetValue(PROFILE_KEY, out var item))
            {
                // Deserialize the JSON string back into our object
                CurrentProfile = item.Value.GetAs<PlayerProfile>();
                
                // Migration: Ensure new fields are initialized
                if (CurrentProfile.CurrentDeckIds == null)
                    CurrentProfile.CurrentDeckIds = new List<int>();
                    
                Debug.Log($"[Database] Profile loaded: {CurrentProfile.Username} (Lvl {CurrentProfile.Level})");
            }
            else
            {
                Debug.Log("[Database] No profile found. Creating new...");
                CurrentProfile = new PlayerProfile();
                // No starter heroes - Player must buy packs
                CurrentProfile.Gold = 500; // Starter Gold for Gacha testing
                await SaveProfileAsync();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Database] Load Failed: {e.Message}");
            // Fallback for offline testing
            CurrentProfile = new PlayerProfile(); 
            CurrentProfile.Gold = 1000; // Debug gold
        }
    }

    /// <summary>
    /// Saves the current local profile to the Cloud Database.
    /// </summary>
    public async Task SaveProfileAsync()
    {
        if (CurrentProfile == null) return;

        try
        {
            var data = new Dictionary<string, object> { { PROFILE_KEY, CurrentProfile } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log("[Database] Profile saved successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Database] Save Failed: {e.Message}");
        }
    }
    
    // --- Deck Management ---

    public async Task<bool> SaveDeck(List<int> newDeckIds)
    {
        if (CurrentProfile == null) return false;

        // Validation: Max 4 cards
        if (newDeckIds.Count > 4)
        {
            Debug.LogWarning("Deck cannot exceed 4 cards.");
            return false;
        }

        // Validation: Ownership
        foreach (var id in newDeckIds)
        {
            if (!CurrentProfile.UnlockedHeroIds.Contains(id))
            {
                Debug.LogError($"Player does not own card {id}");
                return false;
            }
        }

        CurrentProfile.CurrentDeckIds = new List<int>(newDeckIds);
        await SaveProfileAsync();
        return true;
    }

    // --- Economy & Gacha ---

    public async Task<CardDefinition> BuyPack()
    {
        if (CurrentProfile.Gold < packCost)
        {
            Debug.LogWarning("Not enough gold!");
            return null;
        }

        if (cardRegistry == null || cardRegistry.allCards.Count == 0)
        {
            Debug.LogError("Card Registry not assigned or empty!");
            return null;
        }

        CurrentProfile.Gold -= packCost;
        
        // Gacha Logic: Pick random
        CardDefinition pulledCard = cardRegistry.GetRandomCard();
        
        if (pulledCard != null)
        {
             UnlockHero(pulledCard.cardId);
        }
        
        await SaveProfileAsync();
        return pulledCard;
    }

    public void UnlockHero(int cardId)
    {
        if (CurrentProfile.UnlockedHeroIds.Contains(cardId))
        {
            Debug.Log($"[Database] Duplicate Hero ID: {cardId} -> +50 Gold refund");
            CurrentProfile.Gold += 50;
        }
        else
        {
            CurrentProfile.UnlockedHeroIds.Add(cardId);
            Debug.Log($"[Database] Unlocked Hero ID: {cardId}");
        }
    }
    
    public CardDefinition GetCardDef(int id)
    {
        return cardRegistry ? cardRegistry.GetCardById(id) : null;
    }
}