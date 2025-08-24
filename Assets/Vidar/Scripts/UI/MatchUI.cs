using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchUI : MonoBehaviour
{
    public TurnManager turnManager;
    public TextMeshProUGUI turnLabel;
    public Button btnMakeMove;
    public Button btnEndTurn;

    void Awake()
    {
        // verrouille les clics tant que le réseau n'a pas spawn l'objet
        btnMakeMove.interactable = false;
        btnEndTurn.interactable  = false;
    }

    void Start()
    {
        // quand le TurnManager est spawné côté net, on branche tout
        turnManager.OnSpawned += OnTurnManagerReady;

        // si on est déjà prêt (ex: Host), on branche tout de suite
        if (turnManager.IsReady) OnTurnManagerReady();
    }

    void OnTurnManagerReady()
    {
        turnManager.OnStateChanged += Render;

        btnMakeMove.onClick.AddListener(() => turnManager.MakeMove());
        btnEndTurn.onClick.AddListener(() => turnManager.EndTurn());

        btnMakeMove.interactable = true;
        btnEndTurn.interactable  = true;

        Render(turnManager.State);
    }

    private void Render(BoardState s)
    {
        string who = (s.activePlayer == 0) ? "Joueur 1" : "Joueur 2";
        turnLabel.text = $"Tour {s.turnIndex} — {who}\nCoups: J1={s.movesP1}  J2={s.movesP2}";
    }
}
