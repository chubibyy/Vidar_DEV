using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;  


public class DeckInvokeUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerDeck deck;
    public TurnManager turnManager;
    public Button invokeButton;
    public Button[] cardButtons; // 5 boutons cartes (index = index de la carte)
    public Color selectedColor = new Color(0.2f, 0.7f, 1f, 1f);

    private int _selectedIndex = -1;
    private ColorBlock[] _originalBlocks;

    void Awake()
    {
        if (turnManager == null)
            turnManager = Object.FindAnyObjectByType<TurnManager>(FindObjectsInactive.Include);

        if (cardButtons == null || cardButtons.Length == 0)
        {
            // tentative d'auto-détection (boutons dont le nom contient "Card")
            cardButtons = GetComponentsInChildren<Button>(includeInactive: true);
            var list = new System.Collections.Generic.List<Button>();
            foreach (var b in cardButtons)
                if (b.name.ToLower().Contains("card"))
                    list.Add(b);
            cardButtons = list.ToArray();
        }

        // branche les callbacks des cartes
        if (cardButtons != null && cardButtons.Length > 0)
        {
            _originalBlocks = new ColorBlock[cardButtons.Length];
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int idx = i;
                _originalBlocks[i] = cardButtons[i].colors;
                cardButtons[i].onClick.AddListener(() => OnClickCard(idx));
            }
        }

        if (invokeButton != null)
            invokeButton.onClick.AddListener(OnClickInvoke);

        UpdateInvokeState();
    }

    void OnDestroy()
    {
        if (cardButtons != null)
            foreach (var b in cardButtons) if (b != null) b.onClick.RemoveAllListeners();
        if (invokeButton != null) invokeButton.onClick.RemoveAllListeners();
    }

    void Update()
    {
        // Active/désactive le bouton selon tour + sélection
        UpdateInvokeState();
    }

    private void UpdateInvokeState()
    {
        bool hasSelection = _selectedIndex >= 0 && deck != null && deck.startingCards != null &&
                            _selectedIndex < deck.startingCards.Length && deck.startingCards[_selectedIndex] != null;

        bool myTurn = (turnManager != null) && turnManager.IsMyTurn() && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient;

        if (invokeButton != null)
            invokeButton.interactable = hasSelection && myTurn;
    }

    private void OnClickCard(int index)
    {
        if (deck == null || deck.startingCards == null) return;
        if (index < 0 || index >= deck.startingCards.Length) return;

        _selectedIndex = index;
        HighlightSelected(index);

        var card = deck.startingCards[index];
        Debug.Log($"[DeckUI] Carte sélectionnée: {card.displayName}");
    }

    private void HighlightSelected(int index)
    {
        if (cardButtons == null || cardButtons.Length == 0) return;

        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (cardButtons[i] == null) continue;

            var cb = cardButtons[i].colors;
            if (_originalBlocks != null && _originalBlocks.Length == cardButtons.Length)
                cb = _originalBlocks[i];

            if (i == index)
            {
                cb.normalColor = selectedColor;
                cb.selectedColor = selectedColor;
                cb.highlightedColor = selectedColor;
            }
            cardButtons[i].colors = cb;
        }
    }

    private void OnClickInvoke()
    {
        if (deck == null || deck.startingCards == null || turnManager == null) return;
        if (_selectedIndex < 0 || _selectedIndex >= deck.startingCards.Length) return;

        var card = deck.startingCards[_selectedIndex];
        if (card == null) return;

        Debug.Log($"[DeckUI] Invoquer {card.displayName} (cardId={card.cardId})");
        turnManager.SummonHeroServerRpc(card.cardId);

        // Optionnel: désélectionne après invocation
        _selectedIndex = -1;
        HighlightSelected(-1);
        UpdateInvokeState();
    }
}
