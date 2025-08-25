using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Netcode;

public class TurnManager : NetworkBehaviour
{
    // --- Etat de jeu (simple) ---
    [System.Serializable]
    public class BoardState
    {
        public int turnIndex;        // 0,1,2...
        public int activePlayer;     // 0 = Joueur 1, 1 = Joueur 2
        public int movesP1;
        public int movesP2;

        public static BoardState CreateInitial()
        {
            return new BoardState { turnIndex = 0, activePlayer = 0, movesP1 = 0, movesP2 = 0 };
        }
    }

    public BoardState State { get; private set; }

    // --- Sync: ordre des clients (2 joueurs) ---
    // Serveur remplit _players[0] et _players[1] avec les clientId des deux joueurs.
    private NetworkList<ulong> _players;

    // --- Hooks UI ---
    public System.Action<BoardState> OnStateChanged;
    public System.Action OnSpawned;

    public bool IsReady { get; private set; } = false;

    void Awake()
    {
        State = BoardState.CreateInitial();
    }

    public override void OnNetworkSpawn()
    {
        // Init NetworkList
        _players = new NetworkList<ulong>();
        if (IsServer)
        {
            // S'abonner aux connexions/déconnexions côté serveur
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            // Clean si déjà rempli (cas de reload)
            _players.Clear();
        }

        IsReady = true;
        OnSpawned?.Invoke();

        if (IsServer)
        {
            // Le serveur est source de vérité : pousse l'état initial
            State = BoardState.CreateInitial();
            NotifyClientsClientRpc(Serialize(State));
            NotifyLocal();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        IsReady = false;
    }

    // -------- Connexions (serveur) --------
    private void OnClientConnected(ulong clientId)
    {
        // Remplit l'ordre des joueurs (2 max). Le serveur n'est pas joueur.
        if (_players.Count < 2)
        {
            _players.Add(clientId);
            Debug.Log($"[Server] Client {clientId} enregistré en playerIndex={_players.Count - 1}");

            // Quand les 2 joueurs sont là, on peut (optionnel) renvoyer un sync complet
            if (_players.Count == 2)
            {
                // Reset/confirm état de départ si tu veux
                NotifyClientsClientRpc(Serialize(State));
            }
        }
        else
        {
            // Partie pleine -> kick (ou spectateur si tu gères)
            Debug.Log("[Server] Partie pleine, on refuse un 3e client.");
            NetworkManager.DisconnectClient(clientId);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Libère le slot si un joueur part
        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i] == clientId)
            {
                _players.RemoveAt(i);
                break;
            }
        }
        // Option: pause/abandon de partie ici
    }

    // -------- API UI (appelée par le client local) --------
    public void MakeMove()
    {
        if (!IsReady) return;
        if (!IsServer) MakeMoveServerRpc();
        else           ApplyMove();           // utile si tu fais un client "loopback" local, mais ici le serveur n'est pas joueur
    }

    public void EndTurn()
    {
        if (!IsReady) return;
        if (!IsServer) EndTurnServerRpc();
        else           ApplyEndTurn();
    }

    // -------- RPC côté serveur --------
    [ServerRpc(RequireOwnership = false)]
    private void MakeMoveServerRpc(ServerRpcParams rpc = default)
    {
        if (!IsSenderActive(rpc)) return;   // sécurité tour
        ApplyMove();
    }

    [ServerRpc(RequireOwnership = false)]
    private void EndTurnServerRpc(ServerRpcParams rpc = default)
    {
        if (!IsSenderActive(rpc)) return;
        ApplyEndTurn();
    }

    private bool IsSenderActive(ServerRpcParams rpc)
    {
        var senderId = rpc.Receive.SenderClientId;
        int senderIndex = GetPlayerIndexFromClientId(senderId);
        return senderIndex == State.activePlayer && senderIndex != -1;
    }

    // -------- Logique serveur --------
    private void ApplyMove()
    {
        if (State.activePlayer == 0) State.movesP1++; else State.movesP2++;
        NotifyClientsClientRpc(Serialize(State));
        NotifyLocal();
    }

    private void ApplyEndTurn()
    {
        State.turnIndex++;
        State.activePlayer = (State.activePlayer == 0) ? 1 : 0;
        NotifyClientsClientRpc(Serialize(State));
        NotifyLocal();
    }

    // -------- Utils joueurs --------
    public bool IsMyTurn()
    {
        if (!IsReady) return false;
        int myIdx = GetLocalPlayerIndex();
        return myIdx != -1 && myIdx == State.activePlayer;
    }

    private int GetLocalPlayerIndex()
    {
        if (_players == null || _players.Count == 0) return -1;
        ulong me = NetworkManager.LocalClientId;   // côté client
        for (int i = 0; i < _players.Count; i++)
            if (_players[i] == me) return i;
        return -1;
    }

    private int GetPlayerIndexFromClientId(ulong clientId)
    {
        if (_players == null) return -1;
        for (int i = 0; i < _players.Count; i++)
            if (_players[i] == clientId) return i;
        return -1;
    }

    // -------- Sync vers clients --------
    [ClientRpc]
    private void NotifyClientsClientRpc(byte[] packed)
    {
        State = Deserialize(packed);
        NotifyLocal();
    }

    private void NotifyLocal() => OnStateChanged?.Invoke(State);

    // -------- Sérialisation simple --------
    private static byte[] Serialize(BoardState s)
    {
        string json = JsonUtility.ToJson(s);
        return Encoding.UTF8.GetBytes(json);
    }

    private static BoardState Deserialize(byte[] b)
    {
        string json = Encoding.UTF8.GetString(b);
        return JsonUtility.FromJson<BoardState>(json);
    }
}
