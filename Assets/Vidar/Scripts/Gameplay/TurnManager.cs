using Unity.Netcode;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    public BoardState State { get; private set; }
    public System.Action<BoardState> OnStateChanged;
    public System.Action OnSpawned;   // <- nouveau
    public bool IsReady { get; private set; }  // <- nouveau

    void Awake()
    {
        State = BoardState.CreateInitial();
    }

    public override void OnNetworkSpawn()
    {
        IsReady = true;
        OnSpawned?.Invoke();          // avertit l'UI
        Notify();                      // 1er rendu
    }

    public override void OnNetworkDespawn()
    {
        IsReady = false;
    }

    public void MakeMove()
    {
        if (!IsReady) return;         // bloque les clics trop tôt

        if (!IsServer) { MakeMoveServerRpc(); }
        else           { ApplyMove(); }
    }

    public void EndTurn()
    {
        if (!IsReady) return;

        if (!IsServer) { EndTurnServerRpc(); }
        else           { ApplyEndTurn(); }
    }

    [ServerRpc(RequireOwnership = false)] private void MakeMoveServerRpc() => ApplyMove();
    [ServerRpc(RequireOwnership = false)] private void EndTurnServerRpc()  => ApplyEndTurn();

    private void ApplyMove()
    {
        if (State.activePlayer == 0) State.movesP1++; else State.movesP2++;
        NotifyClientsClientRpc(Serialize(State));
    }

    private void ApplyEndTurn()
    {
        State.turnIndex++;
        State.activePlayer = (State.activePlayer == 0) ? 1 : 0;
        NotifyClientsClientRpc(Serialize(State));
    }

    [ClientRpc] private void NotifyClientsClientRpc(byte[] packed)
    {
        State = Deserialize(packed);
        OnStateChanged?.Invoke(State);
    }

    private void Notify() => OnStateChanged?.Invoke(State);

// --- Helpers de sérialisation ---
private static byte[] Serialize(BoardState s)
{
    string json = UnityEngine.JsonUtility.ToJson(s);
    return System.Text.Encoding.UTF8.GetBytes(json);
}

private static BoardState Deserialize(byte[] b)
{
    string json = System.Text.Encoding.UTF8.GetString(b);
    return UnityEngine.JsonUtility.FromJson<BoardState>(json);
}
}
