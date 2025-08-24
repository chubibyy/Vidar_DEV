using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class MatchUI : MonoBehaviour
{
    [Header("Refs")]
    public TurnManager turnManager;
    public TextMeshProUGUI turnLabel;
    public Button btnMakeMove;
    public Button btnEndTurn;

    void Awake()
    {
        // Empêche tout clic avant que l'objet réseau soit prêt
        SetButtonsInteractable(false);
    }

    void Start()
    {
        // Quand le TurnManager est "spawn" réseau, on branche les handlers
        turnManager.OnSpawned += OnTurnManagerReady;

        // Si on est déjà prêt (cas Host), on branche tout de suite
        if (turnManager.IsReady) OnTurnManagerReady();
    }

    private void OnTurnManagerReady()
    {
        turnManager.OnStateChanged += Render;

        btnMakeMove.onClick.AddListener(() => turnManager.MakeMove());
        btnEndTurn.onClick.AddListener(() => turnManager.EndTurn());

        Render(turnManager.State);
    }

    private void Render(BoardState s)
    {
        string who = (s.activePlayer == 0) ? "Joueur 1" : "Joueur 2";
        turnLabel.text = $"Tour {s.turnIndex} — {who}\n" +
                         $"Coups: J1={s.movesP1}  |  J2={s.movesP2}";

        // Active les boutons uniquement si c'est mon tour
        bool myTurn = turnManager.IsMyTurn(NetworkManager.Singleton.LocalClientId);
        SetButtonsInteractable(myTurn);
    }

    private void SetButtonsInteractable(bool on)
    {
        if (btnMakeMove) btnMakeMove.interactable = on;
        if (btnEndTurn)  btnEndTurn.interactable  = on;
    }
}
