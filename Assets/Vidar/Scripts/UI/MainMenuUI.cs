using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnifiedBootstrap networkBootstrap;
    [SerializeField] private TextMeshProUGUI goldText;
    
    [Header("Collection UI")]
    [SerializeField] private GameObject cardUiPrefab; // Prefab with Image & Text
    [SerializeField] private Transform deckGridContainer; // The parent (GridLayoutGroup) for Collection
    [SerializeField] private Transform currentDeckContainer; // The parent for Selected Deck
    [SerializeField] private Button saveDeckButton;
    [SerializeField] private CardVisualSettings visualSettings;

    [Header("Shop UI")]
    [SerializeField] private TextMeshProUGUI lastPullText; // Text to show result

    [Header("Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject deckPanel;
    [SerializeField] private GameObject shopPanel;

    private System.Collections.Generic.List<int> localDeck = new System.Collections.Generic.List<int>();

    private void Start()
    {
        // Force Menu Panel on Start
        ShowMenu();
        
        if (saveDeckButton)
        {
            saveDeckButton.onClick.AddListener(OnSaveDeckClicked);
        }
    }

    private void UpdateUI()
    {
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.CurrentProfile != null)
        {
            goldText.text = $"Gold: {PlayerDataManager.Instance.CurrentProfile.Gold}";
        }
    }

    public void OnPlayClicked()
    {
        if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.FindMatch();
        }
        else
        {
            Debug.LogError("MatchmakingManager not found!");
        }
    }

    public void ShowMenu() => ShowPanel(menuPanel);
    
    public void ShowDeck() 
    {
        // Init local deck from profile
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.CurrentProfile != null)
        {
            localDeck = new System.Collections.Generic.List<int>(PlayerDataManager.Instance.CurrentProfile.CurrentDeckIds);
        }
        
        ShowPanel(deckPanel);
        RefreshDeckView();
    }
    
    public void ShowShop() => ShowPanel(shopPanel);

    private void ShowPanel(GameObject panel)
    {
        if (menuPanel) menuPanel.SetActive(false);
        if (deckPanel) deckPanel.SetActive(false);
        if (shopPanel) shopPanel.SetActive(false);
        
        if (panel) panel.SetActive(true);
        UpdateUI();
    }
    
    public async void OnBuyPackClicked()
    {
        if (PlayerDataManager.Instance == null) return;
        
        // Visual feedback start
        if (lastPullText) lastPullText.text = "Opening...";

        var card = await PlayerDataManager.Instance.BuyPack();
        
        if (card != null)
        {
            Debug.Log($"Pulled: {card.displayName}");
            // Display result
            if (lastPullText) 
            {
                string name = string.IsNullOrEmpty(card.displayName) ? $"Card #{card.cardId}" : card.displayName;
                lastPullText.text = $"You got: {name}!";
            }
        }
        else
        {
            if (lastPullText) lastPullText.text = "Failed (Not enough gold?)";
        }
        UpdateUI();
    }

    private void RefreshDeckView()
    {
        if (PlayerDataManager.Instance == null) return;
        var profile = PlayerDataManager.Instance.CurrentProfile;
        if (profile == null) return;

        // 1. Render Collection (Unlocked but NOT in localDeck)
        if (deckGridContainer)
        {
            foreach (Transform child in deckGridContainer) Destroy(child.gameObject);
            
            foreach (int cardId in profile.UnlockedHeroIds)
            {
                // Optional: Check if already in deck if you want to hide it
                // if (localDeck.Contains(cardId)) continue; 

                var def = PlayerDataManager.Instance.GetCardDef(cardId);
                if (def != null)
                {
                    CreateCardUI(def, deckGridContainer, () => OnCollectionCardClicked(cardId));
                }
            }
        }

        // 2. Render Current Deck
        if (currentDeckContainer)
        {
            foreach (Transform child in currentDeckContainer) Destroy(child.gameObject);
            
            foreach (int cardId in localDeck)
            {
                var def = PlayerDataManager.Instance.GetCardDef(cardId);
                if (def != null)
                {
                     CreateCardUI(def, currentDeckContainer, () => OnDeckCardClicked(cardId));
                }
            }
        }
    }

    private void CreateCardUI(CardDefinition def, Transform parent, UnityEngine.Events.UnityAction onClick)
    {
        // Use specific prefab if defined, otherwise default
        GameObject prefabToUse = (def.overrideCardPrefab != null) ? def.overrideCardPrefab : cardUiPrefab;
        if (prefabToUse == null) return;
        
        var go = Instantiate(prefabToUse, parent);
        
        // Smart UI Update
        var ui = go.GetComponent<CardUIController>();
        if (ui != null && visualSettings != null)
        {
            ui.Setup(def, visualSettings);
        }
        else
        {
            // Fallback
            var img = go.GetComponent<Image>();
            if (img && def.icon) img.sprite = def.icon;

            var txt = go.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) 
            {
                txt.text = string.IsNullOrEmpty(def.displayName) ? $"#{def.cardId}" : def.displayName;
            }
        }
        
        var btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
    }

    private void OnCollectionCardClicked(int cardId)
    {
        if (localDeck.Contains(cardId)) return; // Already in deck
        if (localDeck.Count >= 4) return; // Full

        localDeck.Add(cardId);
        RefreshDeckView();
    }

    private void OnDeckCardClicked(int cardId)
    {
        localDeck.Remove(cardId);
        RefreshDeckView();
    }

    public async void OnSaveDeckClicked()
    {
        if (PlayerDataManager.Instance != null)
        {
            bool success = await PlayerDataManager.Instance.SaveDeck(localDeck);
            Debug.Log(success ? "Deck Saved!" : "Deck Save Failed");
        }
    }
}
