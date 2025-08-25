using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class MatchUI : MonoBehaviour
{
    // Champs optionnels : si tu ne les renseignes pas dans l'Inspector,
    // le script les retrouvera automatiquement au Start().
    [Header("Optional (auto-detected if null)")]
    public TurnManager turnManager;
    public TextMeshProUGUI turnLabel;
    public Button btnMakeMove;
    public Button btnEndTurn;

    void Awake()
    {
        // Au cas où l'UI apparaisse avant la connexion réseau
        SetButtons(false);
    }

    void Start()
    {
        // ---- Auto-detect des références manquantes ----
        if (turnManager == null)
            turnManager = Object.FindAnyObjectByType<TurnManager>(FindObjectsInactive.Include);

        if (turnLabel == null)
            turnLabel = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);

        if (btnMakeMove == null)
            btnMakeMove = FindButtonByNameContains("MakeMove") ?? FindButtonByTextContains("Jouer");

        if (btnEndTurn == null)
            btnEndTurn  = FindButtonByNameContains("EndTurn")  ?? FindButtonByTextContains("End");

        // Sécurise : si on n'a toujours pas les refs, on log et on sort proprement
        if (turnManager == null)
        {
            Debug.LogError("[MatchUI] TurnManager introuvable dans la scène.");
            return;
        }
        if (turnLabel == null)  Debug.LogWarning("[MatchUI] turnLabel introuvable (texte d'état non affiché).");
        if (btnMakeMove == null) Debug.LogWarning("[MatchUI] btnMakeMove introuvable.");
        if (btnEndTurn == null)  Debug.LogWarning("[MatchUI] btnEndTurn introuvable.");

        // ---- Branchement des événements par code ----
        if (btnMakeMove != null) btnMakeMove.onClick.AddListener(OnClickMakeMove);
        if (btnEndTurn  != null) btnEndTurn .onClick.AddListener(OnClickEndTurn);

        // Render à chaque changement d'état
        turnManager.OnStateChanged += Render;

        // Premier rendu
        Render(turnManager.State);
    }

    // --- Callbacks Boutons ---
    public void OnClickMakeMove() => turnManager.MakeMove();
    public void OnClickEndTurn()  => turnManager.EndTurn();

    // --- Rendu UI ---
    private void Render(TurnManager.BoardState s)
    {
        if (turnLabel != null)
        {
            string who = (s.activePlayer == 0) ? "Joueur 1" : "Joueur 2";
            turnLabel.text = $"Tour {s.turnIndex} — {who}\n" +
                             $"Coups: J1={s.movesP1} | J2={s.movesP2}";
        }

        bool myTurn = turnManager.IsMyTurn();
        SetButtons(myTurn);
    }

    private void SetButtons(bool on)
    {
        if (btnMakeMove != null) btnMakeMove.interactable = on;
        if (btnEndTurn  != null) btnEndTurn .interactable = on;
    }

    // --- Helpers pour retrouver des boutons ---
    private Button FindButtonByNameContains(string contains)
    {
        foreach (var b in GetComponentsInChildren<Button>(includeInactive: true))
            if (b.name.ToLower().Contains(contains.ToLower()))
                return b;
        return null;
    }

    private Button FindButtonByTextContains(string contains)
    {
        foreach (var b in GetComponentsInChildren<Button>(includeInactive: true))
        {
            var txt = b.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (txt != null && txt.text.ToLower().Contains(contains.ToLower()))
                return b;
        }
        return null;
    }
}
