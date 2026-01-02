using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Vidar/Ability Definition")]
public class AbilityDefinition : ScriptableObject
{
    public string abilityName;
    [TextArea] public string description;
    
    [Header("Settings")]
    public int manaCost; // Cost to use this ability
    public int damage;   // > 0 for damage dealing
    public int healing;  // > 0 for healing
    
    [Tooltip("If true, heals the caster.")]
    public bool isSelfHeal;
    
    [Tooltip("If true, heals allies in range.")]
    public bool isTeamHeal;
    
    public int range;
    public int cooldownTurns;
    
    [Header("Visuals")]
    public GameObject vfxPrefab;
    public AudioClip soundEffect;
}
