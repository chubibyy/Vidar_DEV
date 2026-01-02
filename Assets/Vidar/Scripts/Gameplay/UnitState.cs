using UnityEngine;
using Unity.Netcode;

public class UnitState : NetworkBehaviour
{
    // Network Variables for synced data
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>();
    public NetworkVariable<int> CurrentAttack = new NetworkVariable<int>();
    public NetworkVariable<float> MoveSpeed = new NetworkVariable<float>();
    public NetworkVariable<TeamType> Team = new NetworkVariable<TeamType>();
    
    // Events for UI/VFX to react
    public System.Action<int> OnHealthChanged;
    public System.Action<TeamType> OnTeamChanged;

    private int _maxHealth;

    public void Initialize(CardDefinition data, TeamType overrideTeam = TeamType.Neutral)
    {
        if (!IsServer) return;

        // 1. Calculate Base Stats
        int hp = data.maxHealth;
        // Use Basic Ability damage as the "Attack Power" stat
        int atk = (data.basicAttack != null) ? data.basicAttack.damage : 0;
        float speed = 5.0f; // Default base speed

        // 2. Apply Passive (Legendary Only)
        if (data.rarity == RarityType.Legendary)
        {
            switch (data.passiveType)
            {
                case PassiveType.BonusHealth:
                    hp += Mathf.RoundToInt(data.passiveValue);
                    break;
                case PassiveType.BonusAttack:
                    atk += Mathf.RoundToInt(data.passiveValue);
                    break;
                case PassiveType.BonusSpeed:
                    speed += data.passiveValue;
                    break;
            }
        }

        // 3. Set Network Variables
        _maxHealth = hp;
        CurrentHealth.Value = _maxHealth;
        CurrentAttack.Value = atk;
        MoveSpeed.Value = speed;
        
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
