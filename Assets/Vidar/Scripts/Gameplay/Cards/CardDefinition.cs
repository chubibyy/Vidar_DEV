using UnityEngine;

[CreateAssetMenu(fileName = "CardDefinition", menuName = "Vidar/Card Definition")]
public class CardDefinition : ScriptableObject
{
    public int cardId;                 // identifiant stable
    public string displayName;
    public GameObject heroPrefab;      // le prefab réseau "Hero" (avec NetworkObject)
    // Plus tard: stats, classe, skin, capacités, coût, etc.
}
