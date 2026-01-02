using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitCard", menuName = "Vidar/Unit Card Definition")]
public class CardDefinition : ScriptableObject
{
    [Header("Identity")]
    public int cardId;                 // Unique ID for networking/serialization
    public string displayName;
    [TextArea] public string description;

    [Header("Classification")]
    public RarityType rarity;
    public HeroClassType heroClass;
    public TeamType team;

    [Header("Visuals")]
    public Sprite icon;
    public GameObject unitPrefab;      // The actual spawned network object

    [Header("UI Representation")]
    [Tooltip("Optional: Assign a specific UI Prefab for this card. If null, the default template is used.")]
    public GameObject overrideCardPrefab;

    [Header("Stats")]
    public int manaCost;
    public int maxHealth;
    public int endurance;
    
    [Header("Abilities")]
    public PassiveType passiveType;
    public float passiveValue; // e.g., 50 (Health), 5 (Attack), 1.5 (Speed)
    
    public AbilityDefinition basicAttack;
    public AbilityDefinition ultimateAbility;
}