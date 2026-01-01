using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }
    
    // The player's current loaded deck for the match
    public List<int> LoadedDeck { get; private set; } = new List<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        LoadDeckFromProfile();
    }

    /// <summary>
    /// Loads the deck from the persistent PlayerDataManager.
    /// This ensures we have the deck ready for the match.
    /// </summary>
    public void LoadDeckFromProfile()
    {
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.CurrentProfile != null)
        {
            LoadedDeck = new List<int>(PlayerDataManager.Instance.CurrentProfile.CurrentDeckIds);
            Debug.Log($"[DeckManager] Loaded {LoadedDeck.Count} cards.");
        }
        else
        {
            Debug.LogWarning("[DeckManager] No PlayerDataManager or Profile found!");
        }
    }

    public CardDefinition GetCardDef(int cardId)
    {
        return PlayerDataManager.Instance ? PlayerDataManager.Instance.GetCardDef(cardId) : null;
    }
}
