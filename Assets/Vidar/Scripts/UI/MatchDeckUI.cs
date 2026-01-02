using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MatchDeckUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform handContainer;
    [SerializeField] private GameObject cardButtonPrefab;
    [SerializeField] private TurnManager turnManager;

    // Optional: Visual feedback for selected card
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;

    private int _selectedCardId = -1;
    private List<GameObject> _spawnedButtons = new List<GameObject>();

    private void Start()
    {
        // Wait for DeckManager to initialize
        Invoke(nameof(InitializeUI), 0.1f);
        
        if (turnManager == null)
             turnManager = FindAnyObjectByType<TurnManager>();
    }

    private void InitializeUI()
    {
        if (DeckManager.Instance == null || handContainer == null || cardButtonPrefab == null) return;

        // Clear
        foreach (Transform child in handContainer) Destroy(child.gameObject);
        _spawnedButtons.Clear();

        var deck = DeckManager.Instance.LoadedDeck;
        foreach (int cardId in deck)
        {
            var def = DeckManager.Instance.GetCardDef(cardId);
            if (def != null)
            {
                CreateCardButton(def);
            }
        }
    }

    private void CreateCardButton(CardDefinition def)
    {
        var go = Instantiate(cardButtonPrefab, handContainer);
        _spawnedButtons.Add(go);

        var img = go.GetComponent<Image>();
        if (img && def.icon) img.sprite = def.icon;
        
        var txt = go.GetComponentInChildren<TextMeshProUGUI>();
        if (txt) txt.text = def.displayName;

        // Ensure Button component exists
        var btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();

        if (btn)
        {
            btn.onClick.AddListener(() => OnCardClicked(def.cardId, btn));
        }
    }

    private void OnCardClicked(int cardId, Button btn)
    {
        Debug.Log($"[MatchDeckUI] Clicked card {cardId}");

        if (turnManager == null || !turnManager.IsMyTurn())
        {
            Debug.Log("[MatchDeckUI] Not your turn or TurnManager missing!");
            return;
        }

        // Toggle selection or Immediate Cast?
        // For now: Immediate Cast (as per original requirement, but cleaner)
        // If you want "Select -> Place", we would store _selectedCardId here.
        
        Debug.Log($"[MatchDeckUI] Summoning {cardId}...");
        turnManager.SummonHeroServerRpc(cardId);
        
        // TODO: Cooldown visual logic here
    }
}
