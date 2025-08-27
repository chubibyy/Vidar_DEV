using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Netcode;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;   // nouveau module UI (Input System)
#endif

public class MatchUI : MonoBehaviour
{
    [Header("Optional (auto-detected if null)")]
    public TurnManager turnManager;
    public TextMeshProUGUI turnLabel;
    public Button btnMakeMove;
    public Button btnEndTurn;
    private CameraRig _camRig;

    [Header("Debug")]
    public bool forceInteractableForDebug = false;
    public bool verboseLogs = true;

    void Awake()
    {
        EnsureEventSystem();
        SetButtons(false);
    }

    void Start()
    {
        // Auto-bind basique
        if (turnManager == null)
            turnManager = Object.FindAnyObjectByType<TurnManager>(FindObjectsInactive.Include);
        if (turnLabel == null)
            turnLabel = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
        if (btnMakeMove == null)
            btnMakeMove = FindButtonByNameContains("MakeMove") ?? FindButtonByTextContains("jouer");
        if (btnEndTurn == null)
            btnEndTurn = FindButtonByNameContains("EndTurn") ?? FindButtonByTextContains("end");

        if (turnManager == null)
        {
            Debug.LogError("[MatchUI] TurnManager introuvable.");
            enabled = false;
            return;
        }

        if (_camRig == null)
            _camRig = Object.FindAnyObjectByType<CameraRig>(FindObjectsInactive.Include);

        if (btnMakeMove != null) btnMakeMove.onClick.AddListener(OnClickMakeMove);
        if (btnEndTurn  != null) btnEndTurn .onClick.AddListener(OnClickEndTurn);

        // Ecoute les changements d’état
        turnManager.OnStateChanged += Render;

        // Premier rendu
        Render(turnManager.State);
    }

    void OnDestroy()
    {
        if (turnManager != null)
            turnManager.OnStateChanged -= Render;

        if (btnMakeMove != null) btnMakeMove.onClick.RemoveListener(OnClickMakeMove);
        if (btnEndTurn  != null) btnEndTurn .onClick.RemoveListener(OnClickEndTurn);
    }

    // --- Boutons ---
    public void OnClickMakeMove()
    {
        if (verboseLogs) Debug.Log("[MatchUI] Click MakeMove");
        // côté serveur : TurnManager.MakeMove() ne fait rien (nous avons blindé côté TM)
        turnManager.MakeMove();
    }

    public void OnClickEndTurn()
    {
        if (verboseLogs) Debug.Log("[MatchUI] Click EndTurn");
        turnManager.EndTurn();
    }

    // --- Rendu / Interactabilité ---
    // IMPORTANT : BoardState est maintenant un type global (fichier séparé),
    // pas TurnManager.BoardState
    private void Render(BoardState s)
    {
        if (turnLabel != null && s != null)
        {
            string who = (s.activePlayer == 0) ? "Joueur 1" : "Joueur 2";
            turnLabel.text = $"Tour {s.turnIndex} — {who}\nCoups: J1={s.movesP1} | J2={s.movesP2}";
        }

        bool myTurn = turnManager.IsMyTurn();
        if (forceInteractableForDebug) myTurn = true;

        if (verboseLogs)
        {
            string mode =
                NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient ? "Server(DED)"
              : NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient ? "Client"
              : "Unknown";
            Debug.Log($"[MatchUI] Mode={mode} | IsReady={turnManager.IsReady} | MyTurn={myTurn}");
        }

        // Activer les boutons seulement côté client et à son tour
        SetButtons(myTurn && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient);

        // Revenir en vue Master quand ce n'est plus mon tour
        if (!myTurn && _camRig != null) _camRig.SetMode(CameraRig.Mode.Master);
    }

    private void SetButtons(bool on)
    {
        if (btnMakeMove != null) btnMakeMove.interactable = on;
        if (btnEndTurn  != null) btnEndTurn .interactable = on;
    }

    // --- EventSystem compatible New Input System ---
    private void EnsureEventSystem()
    {
        var es = EventSystem.current;
        if (es == null)
            es = new GameObject("EventSystem").AddComponent<EventSystem>();

        #if ENABLE_INPUT_SYSTEM
        var legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy) Destroy(legacy);
        if (!es.TryGetComponent<InputSystemUIInputModule>(out _))
            es.gameObject.AddComponent<InputSystemUIInputModule>();
        #else
        if (!es.TryGetComponent<StandaloneInputModule>(out _))
            es.gameObject.AddComponent<StandaloneInputModule>();
        #endif
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
