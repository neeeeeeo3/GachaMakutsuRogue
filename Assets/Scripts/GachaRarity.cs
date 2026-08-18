using UnityEngine;

public enum GachaRarityType
{
    Common,
    Rare,
    Epic
}

public class GachaRarityHolder : MonoBehaviour
{
    [Header("Gacha Result")]
    public string itemName = "ITEM";
    public GachaRarityType rarity = GachaRarityType.Common;
    public string displayName = "COMMON ITEM";
    public Color capsuleColor = Color.white;

    [Header("Future Stat Multipliers")]
    public float hpMultiplier = 1f;
    public float attackMultiplier = 1f;
    public float rewardMultiplier = 1f;
    public float specialMultiplier = 1f;

    public void Initialize(
        string newItemName,
        GachaRarityType newRarity,
        string newDisplayName,
        Color newCapsuleColor
    )
    {
        itemName = string.IsNullOrWhiteSpace(newItemName) ? "ITEM" : newItemName;
        rarity = newRarity;
        displayName = string.IsNullOrWhiteSpace(newDisplayName) ? itemName : newDisplayName;
        capsuleColor = newCapsuleColor;

        ApplyDefaultMultipliers();
    }

    public void ApplyDefaultMultipliers()
    {
        switch (rarity)
        {
            case GachaRarityType.Common:
                hpMultiplier = 1f;
                attackMultiplier = 1f;
                rewardMultiplier = 1f;
                specialMultiplier = 1f;
                break;

            case GachaRarityType.Rare:
                hpMultiplier = 1.35f;
                attackMultiplier = 1.20f;
                rewardMultiplier = 1.25f;
                specialMultiplier = 1.25f;
                break;

            case GachaRarityType.Epic:
                hpMultiplier = 1.80f;
                attackMultiplier = 1.50f;
                rewardMultiplier = 1.60f;
                specialMultiplier = 1.75f;
                break;
        }
    }
}

public static class GachaRarityUtility
{
    public static string GetRarityName(GachaRarityType rarity)
    {
        switch (rarity)
        {
            case GachaRarityType.Common:
                return "COMMON";

            case GachaRarityType.Rare:
                return "RARE";

            case GachaRarityType.Epic:
                return "EPIC";
        }

        return "COMMON";
    }

    public static string GetDisplayName(string itemName, GachaRarityType rarity)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            itemName = "ITEM";
        }

        return GetRarityName(rarity) + " " + itemName.ToUpperInvariant();
    }

    public static Color GetDefaultRarityColor(GachaRarityType rarity)
    {
        switch (rarity)
        {
            case GachaRarityType.Common:
                return new Color(1f, 1f, 1f, 1f);

            case GachaRarityType.Rare:
                return new Color(0.25f, 0.75f, 1f, 1f);

            case GachaRarityType.Epic:
                return new Color(0.95f, 0.45f, 1f, 1f);
        }

        return Color.white;
    }
}