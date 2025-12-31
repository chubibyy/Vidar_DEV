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
    [SerializeField] private Transform deckGridContainer; // The parent (GridLayoutGroup)
    
    [Header("Shop UI")]
    [SerializeField] private TextMeshProUGUI lastPullText; // Text to show result

    [Header("Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject deckPanel;
    [SerializeField] private GameObject shopPanel;

    private void Start()
    {
        // Force Menu Panel on Start
        ShowMenu();
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
        if (deckGridContainer == null || cardUiPrefab == null || PlayerDataManager.Instance == null) return;

        // Clear existing items
        foreach (Transform child in deckGridContainer) 
        {
            Destroy(child.gameObject);
        }

        // Populate
        var profile = PlayerDataManager.Instance.CurrentProfile;
        if (profile == null) return;

        foreach (int cardId in profile.UnlockedHeroIds)
        {
            var def = PlayerDataManager.Instance.GetCardDef(cardId);
            if (def != null)
            {
                var go = Instantiate(cardUiPrefab, deckGridContainer);
                
                // Simple setup: Find Image component for icon, TextMeshPro for name
                var img = go.GetComponent<Image>();
                if (img && def.icon) img.sprite = def.icon;

                var txt = go.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) 
                {
                    txt.text = string.IsNullOrEmpty(def.displayName) ? $"#{def.cardId}" : def.displayName;
                }
            }
        }
    }
}
