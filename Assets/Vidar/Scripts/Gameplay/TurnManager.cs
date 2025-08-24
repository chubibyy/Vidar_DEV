using UnityEngine;
using Unity.Netcode;
using System.Text; // pour Encoding UTF8

public class TurnManager : NetworkBehaviour
{
    public BoardState State { get; private set; }

    // Events simples pour brancher l'UI
    public System.Action<BoardState> OnStateChanged;
    public System.Action OnSpawned;

    public bool IsReady { get; private set; } = false;

    void Awake()
    {
        // Côté client, on attendra le premier SyncState du serveur
        State = BoardState.CreateInitial();
    }

    public override void OnNetworkSpawn()
    {
        IsReady = true;

        if (IsServer)
        {
            // Le serveur (Host) est la source de vérité : initialise et pousse l'état
            State = BoardState.CreateInitial();
            NotifyClientsClientRpc(Serialize(State));
        }

        OnSpawned?.Invoke();
        // Sur Host, on peut déjà rendre; sur Client, Render sera appelé au premier Sync
        if (IsServer) Notify();
    }

    public override void OnNetworkDespawn()
    {
        IsReady = false;
    }

    // ---------- API appelée par l'UI ----------
    public void MakeMove()
    {
        if (!IsReady) return;

        if (!IsServer) { MakeMoveServerRpc(); }
        else           { ApplyMove(); }
    }

    public void EndTurn()
    {
        if (!IsReady) return;

        if (!IsServer) { EndTurnServerRpc(); }
        else           { ApplyEndTurn(); }
    }

    // ---------- RPC (côté serveur uniquement) ----------
    [ServerRpc(RequireOwnership = false)]
    private void MakeMoveServerRpc(ServerRpcParams rpc = default)
    {
        // sécurité: seul le joueur actif peut jouer
        if (!IsSenderActive(rpc)) return;
        ApplyMove();
    }

    [ServerRpc(RequireOwnership = false)]
    private void EndTurnServerRpc(ServerRpcParams rpc = default)
    {
        if (!IsSenderActive(rpc)) return;
        ApplyEndTurn();
    }

    // ---------- Logic ----------
    private void ApplyMove()
    {
        if (State.activePlayer == 0) State.movesP1++;
        else                         State.movesP2++;

        // push état à tous
        NotifyClientsClientRpc(Serialize(State));
    }

    private void ApplyEndTurn()
    {
        State.turnIndex++;
        State.activePlayer = (State.activePlayer == 0) ? 1 : 0;

        NotifyClientsClientRpc(Serialize(State));
    }

    private bool IsSenderActive(ServerRpcParams rpc)
    {
        // mapping simple 2 joueurs: Host=J1(index 0), Client=J2(index 1)
        var senderId = rpc.Receive.SenderClientId;
        int senderIndex = GetPlayerIndexFromClientId(senderId);
        return senderIndex == State.activePlayer;
    }

    // ---------- Utils Tour ----------
    public bool IsMyTurn(ulong localClientId)
    {
        if (!IsReady) return false;
        int myIdx = GetLocalPlayerIndex();
        return myIdx == State.activePlayer;
    }

    private int GetLocalPlayerIndex()
    {
        // 0 = Host (serveur), 1 = premier client
        return IsServer ? 0 : 1;
    }

    private int GetPlayerIndexFromClientId(ulong clientId)
    {
        // Dans ce MVP à 2 joueurs, on considère:
        // - le clientId du serveur (Host) => index 0
        // - tout autre client => index 1
        return (clientId == NetworkManager.ServerClientId) ? 0 : 1;
    }

    // ---------- Notify / Sync ----------
    private void Notify() => OnStateChanged?.Invoke(State);

    [ClientRpc]
    private void NotifyClientsClientRpc(byte[] packed)
    {
        State = Deserialize(packed);
        Notify();
    }

    // ---------- Sérialisation simple (Unity JsonUtility) ----------
    private static byte[] Serialize(BoardState s)
    {
        string json = UnityEngine.JsonUtility.ToJson(s);
        return Encoding.UTF8.GetBytes(json);
    }

    private static BoardState Deserialize(byte[] b)
    {
        string json = Encoding.UTF8.GetString(b);
        return UnityEngine.JsonUtility.FromJson<BoardState>(json);
    }
}
