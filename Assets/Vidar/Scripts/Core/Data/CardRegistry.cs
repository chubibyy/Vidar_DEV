using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Vidar/Card Registry", fileName = "CardRegistry")]
public class CardRegistry : ScriptableObject
{
    public List<CardDefinition> allCards;

    /// <summary>
    /// Returns a random card definition.
    /// </summary>
    public CardDefinition GetRandomCard()
    {
        if (allCards.Count == 0) return null;
        return allCards[Random.Range(0, allCards.Count)];
    }

    public CardDefinition GetCardById(int id)
    {
        return allCards.Find(c => c.cardId == id);
    }

    public List<CardDefinition> GetCardsByRarity(RarityType rarity)
    {
        return allCards.FindAll(c => c.rarity == rarity);
    }
}
