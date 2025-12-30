using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitCard", menuName = "Vidar/Unit Card Definition")]
public class CardDefinition : ScriptableObject
{
    [Header("Identity")]
    public int cardId;                 // Unique ID for networking/serialization
    public string displayName;
    [TextArea] public string description;
    public TeamType team;

    [Header("Visuals")]
    public Sprite icon;
    public GameObject unitPrefab;      // The actual spawned network object

    [Header("Stats")]
    public int manaCost;
    public int maxHealth;
    public int attackPower;
}