using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image cardFrame;      // The border
    [SerializeField] private Image heroIcon;       // The hero's face
    [SerializeField] private Image classIcon;      // The small class symbol
    [SerializeField] private TextMeshProUGUI nameText;
    
    // Optional: Background, Cost Text, Stats Text...

    public void Setup(CardDefinition def, CardVisualSettings settings)
    {
        if (def == null || settings == null) return;

        // 1. Text
        if (nameText) nameText.text = def.displayName;

        // 2. Hero Icon
        if (heroIcon) heroIcon.sprite = def.icon;

        // 3. Rarity Color (Frame)
        if (cardFrame)
        {
            cardFrame.color = settings.GetRarityColor(def.rarity);
        }

        // 4. Class Icon
        if (classIcon)
        {
            Sprite cIcon = settings.GetClassIcon(def.heroClass);
            classIcon.sprite = cIcon;
            classIcon.gameObject.SetActive(cIcon != null);
        }
    }
}
