using System.Text;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Tour par tour autorité serveur (serveur dédié + 2 clients).
/// - Le serveur maintient l'état et l'ordre des joueurs dans _players[0..1].
/// - Les clients envoient leurs actions via ServerRpc.
/// - Le serveur valide puis diffuse l'état via ClientRpc.
/// </summary>
public class TurnManager : NetworkBehaviour
{
    // ---------- Etat de jeu ----------
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

    // ---------- Joueurs (répliqué par NGO) ----------
    // IMPORTANT: NetworkList doit être instanciée à la déclaration (sinon erreur NGO).
    private readonly NetworkList<ulong> _players = new NetworkList<ulong>();

    // ---------- Hooks UI locaux (non réseau) ----------
    public System.Action<BoardState> OnStateChanged;
    public System.Action OnSpawned;

    public bool IsReady { get; private set; }

    private static readonly Encoding Utf8 = Encoding.UTF8;

    private void Awake()
    {
        // Valeur locale pour éviter le null côté client avant le premier sync
        State = BoardState.CreateInitial();
        IsReady = false;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            // Nettoie la liste en cas de reload serveur
            _players.Clear();

            // Source de vérité initiale
            State = BoardState.CreateInitial();
            NotifyClientsClientRpc(Serialize(State)); // pousse vers les clients
            NotifyLocal();                            // et met à jour local (monitoring serveur éventuel)
        }

        IsReady = true;
        OnSpawned?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            _players.Clear();
        }
        IsReady = false;
    }

    // ---------- Connexions (serveur) ----------
    private void OnClientConnected(ulong clientId)
    {
        // Enregistre jusqu'à 2 joueurs (serveur n'est pas joueur)
        if (_players.Count < 2)
        {
            _players.Add(clientId);
            Debug.Log($"[Server] Client {clientId} -> playerIndex={_players.Count - 1}");

            // Option: renvoyer l'état actuel (utile si un client arrive en cours)
            NotifyClientsClientRpc(Serialize(State));
        }
        else
        {
            Debug.Log("[Server] Partie pleine, on refuse un 3e client.");
            NetworkManager.DisconnectClient(clientId);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i] == clientId)
            {
                _players.RemoveAt(i);
                break;
            }
        }
        // TODO: gérer abandon, victoire auto, etc.
    }

    // ---------- API appelée par l'UI locale ----------
    public void MakeMove()
    {
        if (!IsReady) return;

        if (IsServer) ApplyMove();           // (utile si tu ajoutes plus tard un client local d'observation)
        else          MakeMoveServerRpc();
    }

    public void EndTurn()
    {
        if (!IsReady) return;

        if (IsServer) ApplyEndTurn();
        else          EndTurnServerRpc();
    }

    // ---------- RPC côté serveur ----------
    [ServerRpc(RequireOwnership = false)]
    private void MakeMoveServerRpc(ServerRpcParams rpc = default)
    {
        if (!IsLegalSender(rpc)) return;
        ApplyMove();
    }

    [ServerRpc(RequireOwnership = false)]
    private void EndTurnServerRpc(ServerRpcParams rpc = default)
    {
        if (!IsLegalSender(rpc)) return;
        ApplyEndTurn();
    }

    private bool IsLegalSender(ServerRpcParams rpc)
    {
        var senderId = rpc.Receive.SenderClientId;
        int senderIndex = GetPlayerIndexFromClientId(senderId);

        // refuse si:
        // - sender non mappé (-1)
        // - pas son tour
        // - (option) si les 2 joueurs ne sont pas encore connectés, autoriser seulement player 0
        if (senderIndex == -1) return false;

        // Empêche de jouer si l'adversaire n'est pas encore connecté (optionnel)
        // if (_players.Count < 2 && senderIndex != 0) return false;

        return senderIndex == State.activePlayer;
    }

    // ---------- Logique serveur ----------
    private void ApplyMove()
    {
        if (State.activePlayer == 0) State.movesP1++;
        else                         State.movesP2++;

        BroadcastState();
    }

    private void ApplyEndTurn()
    {
        State.turnIndex++;
        State.activePlayer = (State.activePlayer == 0) ? 1 : 0;

        BroadcastState();
    }

    private void BroadcastState()
    {
        // Diffuse l'état aux clients; le serveur met aussi à jour sa vue locale
        var packed = Serialize(State);
        NotifyClientsClientRpc(packed);
        NotifyLocal();
    }

    // ---------- Helpers joueurs ----------
    public bool IsMyTurn()
    {
        if (!IsReady) return false;
        int myIdx = GetLocalPlayerIndex();
        return myIdx != -1 && myIdx == State.activePlayer;
    }

    private int GetLocalPlayerIndex()
    {
        if (_players.Count == 0) return -1;

        ulong me = NetworkManager.LocalClientId; // côté client
        for (int i = 0; i < _players.Count; i++)
            if (_players[i] == me) return i;

        return -1;
    }

    private int GetPlayerIndexFromClientId(ulong clientId)
    {
        for (int i = 0; i < _players.Count; i++)
            if (_players[i] == clientId) return i;

        return -1;
    }

    // ---------- Sync vers clients ----------
    [ClientRpc]
    private void NotifyClientsClientRpc(byte[] packed)
    {
        State = Deserialize(packed);
        NotifyLocal();
    }

    private void NotifyLocal() => OnStateChanged?.Invoke(State);

    // ---------- Sérialisation (simple, Unity) ----------
    private static byte[] Serialize(BoardState s)
    {
        string json = JsonUtility.ToJson(s);
        return Utf8.GetBytes(json);
    }

    private static BoardState Deserialize(byte[] b)
    {
        string json = Utf8.GetString(b);
        return JsonUtility.FromJson<BoardState>(json);
    }
}
