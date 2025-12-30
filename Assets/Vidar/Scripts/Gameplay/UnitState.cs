using UnityEngine;
using Unity.Netcode;

public class UnitState : NetworkBehaviour
{
    // Network Variables for synced data
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>();
    public NetworkVariable<TeamType> Team = new NetworkVariable<TeamType>();
    
    // Events for UI/VFX to react
    public System.Action<int> OnHealthChanged;
    public System.Action<TeamType> OnTeamChanged;

    private int _maxHealth;

    public void Initialize(CardDefinition data, TeamType overrideTeam = TeamType.Neutral)
    {
        if (!IsServer) return;

        _maxHealth = data.maxHealth;
        CurrentHealth.Value = _maxHealth;
        Team.Value = (overrideTeam != TeamType.Neutral) ? overrideTeam : data.team;
    }

    public override void OnNetworkSpawn()
    {
        CurrentHealth.OnValueChanged += (oldVal, newVal) => OnHealthChanged?.Invoke(newVal);
        Team.OnValueChanged += (oldVal, newVal) => OnTeamChanged?.Invoke(newVal);
        
        // Initial invoke for late joiners
        OnHealthChanged?.Invoke(CurrentHealth.Value);
        OnTeamChanged?.Invoke(Team.Value);
    }

    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= (oldVal, newVal) => OnHealthChanged?.Invoke(newVal);
        Team.OnValueChanged -= (oldVal, newVal) => OnTeamChanged?.Invoke(newVal);
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageServerRpc(int amount)
    {
        int newHealth = CurrentHealth.Value - amount;
        CurrentHealth.Value = Mathf.Max(0, newHealth);
        
        if (CurrentHealth.Value == 0)
        {
            // Die Logic
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
