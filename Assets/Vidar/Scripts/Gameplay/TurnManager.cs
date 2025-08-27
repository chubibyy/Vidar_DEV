using System.Text;
using UnityEngine;
using Unity.Netcode;

public class TurnManager : NetworkBehaviour
{
    // ---------- Cartes & Zones ----------
    [Header("Cards Registry")]
    [SerializeField] private CardDefinition[] allCards; // à remplir dans l’inspector
    [SerializeField] private Transform spawnZoneP1;     // BoxCollider requis
    [SerializeField] private Transform spawnZoneP2;     // BoxCollider requis

    private CardDefinition GetCard(int id)
    {
        if (allCards == null) return null;
        foreach (var c in allCards) if (c && c.cardId == id) return c;
        return null;
    }

    // ---------- Etat ----------
    public BoardState State { get; private set; }

    // ---------- Joueurs (réplication NGO) ----------
    // doit être instanciée à la déclaration
    private readonly NetworkList<ulong> _players = new NetworkList<ulong>();

    // ---------- Hooks locaux ----------
    public System.Action<BoardState> OnStateChanged;
    public System.Action OnSpawned;
    public bool IsReady { get; private set; }

    private static readonly Encoding Utf8 = Encoding.UTF8;

    private void Awake()
    {
        State = BoardState.CreateInitial();
        IsReady = false;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // callbacks (après seed ci-dessous)
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            // seed la liste avec les clients déjà connectés (très important)
            _players.Clear();
            SeedPlayersFromConnections();

            // pousse l’état initial
            State = BoardState.CreateInitial();
            NotifyClientsClientRpc(Serialize(State));
            NotifyLocal();
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

    private void SeedPlayersFromConnections()
    {
        foreach (var id in NetworkManager.ConnectedClientsIds)
        {
            if (id == NetworkManager.ServerClientId) continue; // ignore serveur
            AddPlayerIfAbsent(id);
        }
        Debug.Log($"[TurnManager] Seed players: count={_players.Count}");
    }

    private void AddPlayerIfAbsent(ulong clientId)
    {
        for (int i = 0; i < _players.Count; i++)
            if (_players[i] == clientId) return;

        if (_players.Count < 2)
        {
            _players.Add(clientId);
            Debug.Log($"[TurnManager] Add player clientId={clientId} -> index={_players.Count - 1}");
        }
        else
        {
            Debug.Log("[TurnManager] Partie pleine, client ignoré.");
        }
    }

    // ---------- Connexions (serveur) ----------
    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId) return;
        AddPlayerIfAbsent(clientId);

        // (re)pousser l’état
        NotifyClientsClientRpc(Serialize(State));
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
        // TODO: abandon / fin auto / pause
    }

    // ---------- API locale (UI) ----------
    // IMPORTANT : côté serveur, ces méthodes ne font rien (pas d’input serveur)
    public void MakeMove()
    {
        if (!IsReady || !IsClient) return;
        MakeMoveServerRpc();
    }

    public void EndTurn()
    {
        if (!IsReady || !IsClient) return;
        EndTurnServerRpc();
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
        return senderIndex != -1 && senderIndex == State.activePlayer;
    }

    // ---------- Placement de héros ----------
    [ServerRpc(RequireOwnership = false)]
    public void PlaceHeroServerRpc(int cardId, Vector3 requestedPos, ServerRpcParams rpc = default)
    {
        var sender = rpc.Receive.SenderClientId;
        int pIndex = GetPlayerIndexFromClientId(sender);

        if (pIndex == -1)
        {
            PlaceHeroResultClientRpc(false, "joueur non enregistré",
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { sender } } });
            return;
        }
        if (pIndex != State.activePlayer)
        {
            PlaceHeroResultClientRpc(false, "pas ton tour",
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { sender } } });
            return;
        }
        if (!IsInsideSpawnZone(requestedPos, pIndex))
        {
            PlaceHeroResultClientRpc(false, "hors zone de spawn",
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { sender } } });
            return;
        }

        var card = GetCard(cardId);
        if (!card || !card.heroPrefab)
        {
            PlaceHeroResultClientRpc(false, "carte/prefab invalide",
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { sender } } });
            return;
        }

        var spawnPos = requestedPos; spawnPos.y += 0.05f;

        var go = Instantiate(card.heroPrefab, spawnPos, Quaternion.identity);
        var no = go.GetComponent<NetworkObject>();
        if (!no)
        {
            Destroy(go);
            PlaceHeroResultClientRpc(false, "NetworkObject manquant",
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { sender } } });
            return;
        }

        no.Spawn();
        no.ChangeOwnership(sender);

        // Focus TPS chez le client propriétaire
        FocusHeroClientRpc(no.NetworkObjectId,
            new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { sender } } });

        PlaceHeroResultClientRpc(true, "ok",
            new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { sender } } });

        BroadcastState();
    }

    private bool IsInsideSpawnZone(Vector3 worldPos, int playerIndex)
    {
        var tz = (playerIndex == 0) ? spawnZoneP1 : spawnZoneP2;
        if (!tz) return false;

        var bc = tz.GetComponent<BoxCollider>();
        if (!bc) return false;

        var b = bc.bounds; b.Expand(0.01f);
        return b.Contains(worldPos);
    }

    [ClientRpc]
    private void PlaceHeroResultClientRpc(bool ok, string reason, ClientRpcParams target = default)
    {
        if (!ok) Debug.LogWarning($"[Placement] refusé: {reason}");
    }

    [ClientRpc]
    private void FocusHeroClientRpc(ulong heroNetId, ClientRpcParams target = default)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.SpawnManager.SpawnedObjects.TryGetValue(heroNetId, out var netObj)) return;

        var camRig = FindAnyObjectByType<CameraRig>(FindObjectsInactive.Include);
        if (camRig != null)
        {
            camRig.Follow(netObj.transform);
            camRig.SetMode(CameraRig.Mode.TPS);
        }
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
        var packed = Serialize(State);
        NotifyClientsClientRpc(packed);
        NotifyLocal();
    }

    // ---------- Helpers joueurs ----------
    public bool IsMyTurn()
    {
        if (!IsReady || !IsClient) return false; // serveur non joueur
        int myIdx = GetLocalPlayerIndex();
        return myIdx != -1 && myIdx == State.activePlayer;
    }

    public int GetLocalPlayerIndexPublic() => GetLocalPlayerIndex();

    private int GetLocalPlayerIndex()
    {
        if (_players.Count == 0) return -1;
        ulong me = NetworkManager.LocalClientId;
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

    // ---------- Sync ----------
    [ClientRpc]
    private void NotifyClientsClientRpc(byte[] packed)
    {
        State = Deserialize(packed);
        NotifyLocal();
    }

    private void NotifyLocal() => OnStateChanged?.Invoke(State);

    // ---------- Sérialisation ----------
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
