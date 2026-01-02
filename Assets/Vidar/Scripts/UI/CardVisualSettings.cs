using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CardVisualSettings", menuName = "Vidar/Card Visual Settings")]
public class CardVisualSettings : ScriptableObject
{
    [Header("Rarity Colors")]
    public Color commonColor = Color.gray;
    public Color rareColor = Color.blue;
    public Color epicColor = new Color(0.6f, 0f, 0.8f); // Purple
    public Color legendaryColor = new Color(1f, 0.84f, 0f); // Gold

    [Header("Class Icons")]
    public Sprite tankIcon;
    public Sprite dpsRangeIcon;
    public Sprite dpsMeleeIcon;
    public Sprite priestessIcon;

    public Color GetRarityColor(RarityType rarity)
    {
        switch (rarity)
        {
            case RarityType.Common: return commonColor;
            case RarityType.Rare: return rareColor;
            case RarityType.Epic: return epicColor;
            case RarityType.Legendary: return legendaryColor;
            default: return Color.white;
        }
    }

    public Sprite GetClassIcon(HeroClassType heroClass)
    {
        switch (heroClass)
        {
            case HeroClassType.Tank: return tankIcon;
            case HeroClassType.DPS_Range: return dpsRangeIcon;
            case HeroClassType.DPS_Melee: return dpsMeleeIcon;
            case HeroClassType.Priestess: return priestessIcon;
            default: return null;
        }
    }
}
