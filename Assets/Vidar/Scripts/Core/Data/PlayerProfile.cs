using System;
using System.Collections.Generic;

[Serializable]
public class PlayerProfile
{
    public string Username;
    public int Level;
    public int Gold;
    
    // The list of Card IDs that the player has unlocked and can use in their deck
    public List<int> UnlockedHeroIds;

    // The current active deck (list of card IDs)
    public List<int> CurrentDeckIds;

    // Constructor for a fresh account
    public PlayerProfile()
    {
        Username = "New Player";
        Level = 1;
        Gold = 0;
        UnlockedHeroIds = new List<int>();
        CurrentDeckIds = new List<int>();
    }

    /// <summary>
    /// Checks if the player owns a specific hero card.
    /// </summary>
    public bool IsHeroUnlocked(int cardId)
    {
        return UnlockedHeroIds.Contains(cardId);
    }
}
