using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaEffectDescriptionUI : MonoBehaviour
{
    [Header("Auto Create UI")]
    public bool autoCreateUI = true;
    public Canvas targetCanvas;

    [Header("Result Toast")]
    public bool showResultToast = true;
    public Vector2 resultPanelSize = new Vector2(430f, 150f);
    public Vector2 resultAnchoredPosition = new Vector2(0f, -92f);
    public float resultShowSeconds = 2.2f;

    [Header("Placement Panel")]
    public bool showPlacementPanel = true;
    public Vector2 placementPanelSize = new Vector2(390f, 190f);
    public Vector2 placementAnchoredPosition = new Vector2(18f, 18f);

    [Header("Text")]
    public int titleFontSize = 22;
    public int bodyFontSize = 15;
    public int hintFontSize = 13;

    [Header("Layout Heights")]
    public float resultTitleHeight = 30f;
    public float resultBodyHeight = 96f;
    public float placementTitleHeight = 32f;
    public float placementBodyHeight = 96f;
    public float placementHintHeight = 42f;

    [Header("Colors")]
    public Color panelColor = new Color(0.035f, 0.04f, 0.06f, 0.88f);
    public Color commonColor = new Color(1f, 0.86f, 0.35f, 1f);
    public Color rareColor = new Color(0.32f, 0.82f, 1f, 1f);
    public Color epicColor = new Color(1f, 0.42f, 1f, 1f);
    public Color normalTextColor = new Color(0.92f, 0.95f, 1f, 1f);
    public Color hintTextColor = new Color(0.72f, 0.78f, 0.88f, 1f);

    [Header("Animation")]
    public bool animateToast = true;
    public float popScale = 1.08f;
    public float popDuration = 0.12f;

    [Header("Debug")]
    public bool showDebugLog = false;

    private RectTransform resultRoot;
    private Image resultBackground;
    private TMP_Text resultTitleText;
    private TMP_Text resultBodyText;

    private RectTransform placementRoot;
    private Image placementBackground;
    private TMP_Text placementTitleText;
    private TMP_Text placementBodyText;
    private TMP_Text placementHintText;

    private Coroutine resultRoutine;

    private void Start()
    {
        if (autoCreateUI)
        {
            CreateUIIfNeeded();
        }

        HideResultImmediate();
        HidePlacement();
    }

    public void ShowRolledResult(string itemName, string rarityName, GameObject itemPrefab)
    {
        CreateUIIfNeeded();

        if (!showResultToast || resultRoot == null)
        {
            return;
        }

        string safeItemName = NormalizeItemName(itemName);
        string safeRarityName = NormalizeRarityName(rarityName);

        resultTitleText.text = "GACHA RESULT  " + GetRarityBadge(safeRarityName);
        resultBodyText.text =
            GetColoredRarityLabel(safeRarityName)
            + " "
            + safeItemName
            + "\n"
            + BuildDescription(safeItemName, safeRarityName, itemPrefab);

        resultBackground.color = GetPanelColorWithRarityTint(safeRarityName);
        resultRoot.gameObject.SetActive(true);

        if (resultRoutine != null)
        {
            StopCoroutine(resultRoutine);
        }

        resultRoutine = StartCoroutine(ResultToastRoutine());

        DebugLog("Show rolled result: " + safeRarityName + " " + safeItemName);
    }

    public void ShowPlacementInfo(string itemName, string rarityName, GameObject itemPrefab)
    {
        CreateUIIfNeeded();

        if (!showPlacementPanel || placementRoot == null)
        {
            return;
        }

        string safeItemName = NormalizeItemName(itemName);
        string safeRarityName = NormalizeRarityName(rarityName);

        placementTitleText.text =
            "PLACING  "
            + GetRarityBadge(safeRarityName)
            + " "
            + safeItemName;

        placementBodyText.text = BuildDescription(safeItemName, safeRarityName, itemPrefab);
        placementHintText.text = BuildPlacementHint(safeItemName);

        placementBackground.color = GetPanelColorWithRarityTint(safeRarityName);
        placementRoot.gameObject.SetActive(true);

        DebugLog("Show placement info: " + safeRarityName + " " + safeItemName);
    }

    public void HidePlacement()
    {
        if (placementRoot != null)
        {
            placementRoot.gameObject.SetActive(false);
        }
    }

    public void HideAll()
    {
        HideResultImmediate();
        HidePlacement();
    }

    private IEnumerator ResultToastRoutine()
    {
        if (resultRoot == null)
        {
            yield break;
        }

        if (animateToast)
        {
            Vector3 startScale = Vector3.one * popScale;
            Vector3 endScale = Vector3.one;

            float timer = 0f;

            while (timer < popDuration)
            {
                timer += Time.deltaTime;

                float progress = Mathf.Clamp01(timer / Mathf.Max(0.01f, popDuration));
                float eased = Mathf.SmoothStep(0f, 1f, progress);

                resultRoot.localScale = Vector3.Lerp(startScale, endScale, eased);

                yield return null;
            }

            resultRoot.localScale = Vector3.one;
        }

        yield return new WaitForSeconds(resultShowSeconds);

        HideResultImmediate();
        resultRoutine = null;
    }

    private void HideResultImmediate()
    {
        if (resultRoutine != null)
        {
            StopCoroutine(resultRoutine);
            resultRoutine = null;
        }

        if (resultRoot != null)
        {
            resultRoot.localScale = Vector3.one;
            resultRoot.gameObject.SetActive(false);
        }
    }

    private string BuildDescription(string itemName, string rarityName, GameObject itemPrefab)
    {
        string upperItemName = NormalizeItemName(itemName);
        string upperRarityName = NormalizeRarityName(rarityName);

        if (upperItemName.Contains("SLIME"))
        {
            return BuildSlimeDescription(upperRarityName, itemPrefab);
        }

        if (upperItemName.Contains("FOOD"))
        {
            return BuildFoodDescription(upperRarityName, itemPrefab);
        }

        if (upperItemName.Contains("TRAP"))
        {
            return BuildTrapDescription(upperRarityName, itemPrefab);
        }

        return "Unknown capsule.\nPlace it to reveal its effect.";
    }

    private string BuildSlimeDescription(string rarityName, GameObject itemPrefab)
    {
        int hp = 3;
        int damage = 1;
        float interval = 0.8f;

        if (itemPrefab != null)
        {
            SlimeHealth health = itemPrefab.GetComponent<SlimeHealth>();

            if (health != null)
            {
                hp = health.maxHp;
            }

            SlimeAttack attack = itemPrefab.GetComponent<SlimeAttack>();

            if (attack != null)
            {
                damage = attack.attackDamage;
                interval = attack.attackInterval;

                if (rarityName == "RARE")
                {
                    interval *= attack.rareAttackIntervalMultiplier;
                }
                else if (rarityName == "EPIC")
                {
                    interval *= attack.epicAttackIntervalMultiplier;
                }

                interval = Mathf.Max(attack.minimumAttackInterval, interval);
            }
        }

        if (rarityName == "EPIC")
        {
            return
                "Main frontline unit.\n"
                + "HP: " + hp + " / ATK: " + damage + " / SPD: " + interval.ToString("0.00") + "s\n"
                + "EPIC: Much faster attack speed.";
        }

        if (rarityName == "RARE")
        {
            return
                "Stronger frontline slime.\n"
                + "HP: " + hp + " / ATK: " + damage + " / SPD: " + interval.ToString("0.00") + "s\n"
                + "RARE: Slightly faster attack speed.";
        }

        return
            "Basic defense unit.\n"
            + "HP: " + hp + " / ATK: " + damage + " / SPD: " + interval.ToString("0.00") + "s\n"
            + "Place near the hero path.";
    }

    private string BuildFoodDescription(string rarityName, GameObject itemPrefab)
    {
        int reproductionCount = 1;
        int healAmount = 0;
        int maxHpBonus = 0;
        int attackBonus = 0;

        SlimeFoodItem food = null;

        if (itemPrefab != null)
        {
            food = itemPrefab.GetComponent<SlimeFoodItem>();
        }

        if (food != null)
        {
            reproductionCount = GetFoodReproductionCount(food, rarityName);
            healAmount = GetFoodHealAmount(food, rarityName);
            maxHpBonus = GetFoodMaxHpBonus(food, rarityName);
            attackBonus = GetFoodAttackBonus(food, rarityName);
        }
        else
        {
            if (rarityName == "RARE")
            {
                reproductionCount = 2;
                healAmount = 1;
            }
            else if (rarityName == "EPIC")
            {
                reproductionCount = 3;
                healAmount = 3;
                maxHpBonus = 1;
                attackBonus = 1;
            }
        }

        if (rarityName == "EPIC")
        {
            return
                "Power food for slimes.\n"
                + "Split: +" + reproductionCount + " / Heal: " + healAmount + "\n"
                + "EPIC: MaxHP+" + maxHpBonus + " / ATK+" + attackBonus + " / Speed UP.";
        }

        if (rarityName == "RARE")
        {
            return
                "Better food with extra growth.\n"
                + "Split: +" + reproductionCount + " / Heal: " + healAmount + "\n"
                + "RARE: Slight attack speed UP.";
        }

        return
            "Basic slime food.\n"
            + "Split: +" + reproductionCount + "\n"
            + "Place near slimes.";
    }

    private string BuildTrapDescription(string rarityName, GameObject itemPrefab)
    {
        int damage = 3;
        float triggerRange = 0.8f;
        float areaRange = 0f;

        TrapDamage trap = null;

        if (itemPrefab != null)
        {
            trap = itemPrefab.GetComponent<TrapDamage>();
        }

        if (trap != null)
        {
            damage = trap.damage;
            triggerRange = trap.triggerRange;

            if (rarityName == "RARE")
            {
                damage += trap.rareDamageBonus;
                triggerRange += trap.rareTriggerRangeBonus;
            }
            else if (rarityName == "EPIC")
            {
                damage += trap.epicDamageBonus;
                triggerRange += trap.epicTriggerRangeBonus;
                areaRange = trap.epicAreaRange;
            }
        }
        else
        {
            if (rarityName == "RARE")
            {
                damage += 2;
                triggerRange += 0.15f;
            }
            else if (rarityName == "EPIC")
            {
                damage += 5;
                triggerRange += 0.25f;
                areaRange = 1.35f;
            }
        }

        if (rarityName == "EPIC")
        {
            return
                "High power trap.\n"
                + "Damage: " + damage + " / Range: " + triggerRange.ToString("0.00") + "\n"
                + "EPIC: Area damage within " + areaRange.ToString("0.00") + ".";
        }

        if (rarityName == "RARE")
        {
            return
                "Improved trap.\n"
                + "Damage: " + damage + " / Range: " + triggerRange.ToString("0.00") + "\n"
                + "RARE: More damage and range.";
        }

        return
            "Basic trap.\n"
            + "Damage: " + damage + " / Range: " + triggerRange.ToString("0.00") + "\n"
            + "Best at corners and narrow paths.";
    }

    private int GetFoodReproductionCount(SlimeFoodItem food, string rarityName)
    {
        int baseCount = Mathf.Max(1, food.commonReproductionCount);
        int valueBonus = Mathf.Max(0, food.foodValue - 1);

        if (rarityName == "RARE")
        {
            return baseCount + valueBonus + Mathf.Max(0, food.rareExtraReproductionCount);
        }

        if (rarityName == "EPIC")
        {
            return baseCount + valueBonus + Mathf.Max(0, food.epicExtraReproductionCount);
        }

        return baseCount + valueBonus;
    }

    private int GetFoodHealAmount(SlimeFoodItem food, string rarityName)
    {
        if (rarityName == "RARE")
        {
            return food.rareHealAmount;
        }

        if (rarityName == "EPIC")
        {
            return food.epicHealAmount;
        }

        return food.commonHealAmount;
    }

    private int GetFoodMaxHpBonus(SlimeFoodItem food, string rarityName)
    {
        if (rarityName == "RARE")
        {
            return food.rareMaxHpBonus;
        }

        if (rarityName == "EPIC")
        {
            return food.epicMaxHpBonus;
        }

        return food.commonMaxHpBonus;
    }

    private int GetFoodAttackBonus(SlimeFoodItem food, string rarityName)
    {
        if (rarityName == "RARE")
        {
            return food.rareAttackDamageBonus;
        }

        if (rarityName == "EPIC")
        {
            return food.epicAttackDamageBonus;
        }

        return food.commonAttackDamageBonus;
    }

    private string BuildPlacementHint(string itemName)
    {
        string upperItemName = NormalizeItemName(itemName);

        if (upperItemName.Contains("SLIME"))
        {
            return "Left Click: Place / Right Click: Cancel\nBest: Hero path, entrance, corners.";
        }

        if (upperItemName.Contains("FOOD"))
        {
            return "Left Click: Place / Right Click: Cancel\nBest: Near slimes you want to power up.";
        }

        if (upperItemName.Contains("TRAP"))
        {
            return "Left Click: Place / Right Click: Cancel\nBest: Narrow paths, corners, near core.";
        }

        return "Left Click: Place / Right Click: Cancel";
    }

    private string NormalizeItemName(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return "UNKNOWN";
        }

        return itemName.Trim().ToUpperInvariant();
    }

    private string NormalizeRarityName(string rarityName)
    {
        if (string.IsNullOrWhiteSpace(rarityName))
        {
            return "COMMON";
        }

        string upper = rarityName.Trim().ToUpperInvariant();

        if (upper.Contains("EPIC"))
        {
            return "EPIC";
        }

        if (upper.Contains("ELITE"))
        {
            return "EPIC";
        }

        if (upper.Contains("RARE"))
        {
            return "RARE";
        }

        return "COMMON";
    }

    private string GetRarityBadge(string rarityName)
    {
        if (rarityName == "EPIC")
        {
            return "[EPIC]";
        }

        if (rarityName == "RARE")
        {
            return "[RARE]";
        }

        return "[COMMON]";
    }

    private string GetColoredRarityLabel(string rarityName)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(GetRarityColor(rarityName));
        return "<color=#" + colorHex + ">" + rarityName + "</color>";
    }

    private Color GetRarityColor(string rarityName)
    {
        if (rarityName == "EPIC")
        {
            return epicColor;
        }

        if (rarityName == "RARE")
        {
            return rareColor;
        }

        return commonColor;
    }

    private Color GetPanelColorWithRarityTint(string rarityName)
    {
        Color rarityColor = GetRarityColor(rarityName);

        Color color = Color.Lerp(panelColor, rarityColor, 0.12f);
        color.a = panelColor.a;

        return color;
    }

    private void CreateUIIfNeeded()
    {
        if (!autoCreateUI)
        {
            return;
        }

        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }

        if (targetCanvas == null)
        {
            Debug.LogWarning("GachaEffectDescriptionUI: Canvas not found.");
            return;
        }

        if (resultRoot == null)
        {
            CreateResultPanel();
        }

        if (placementRoot == null)
        {
            CreatePlacementPanel();
        }
    }

    private void CreateResultPanel()
    {
        GameObject rootObject = new GameObject("GachaResultDescriptionPanel");
        rootObject.transform.SetParent(targetCanvas.transform, false);

        resultRoot = rootObject.AddComponent<RectTransform>();
        resultRoot.anchorMin = new Vector2(0.5f, 1f);
        resultRoot.anchorMax = new Vector2(0.5f, 1f);
        resultRoot.pivot = new Vector2(0.5f, 1f);
        resultRoot.sizeDelta = resultPanelSize;
        resultRoot.anchoredPosition = resultAnchoredPosition;

        resultBackground = rootObject.AddComponent<Image>();
        resultBackground.color = panelColor;

        VerticalLayoutGroup layout = rootObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 12, 12);
        layout.spacing = 4;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        resultTitleText = CreateText(
            "Title",
            rootObject.transform,
            titleFontSize,
            normalTextColor,
            resultTitleHeight
        );

        resultBodyText = CreateText(
            "Body",
            rootObject.transform,
            bodyFontSize,
            normalTextColor,
            resultBodyHeight
        );

        resultBodyText.enableWordWrapping = true;
        resultBodyText.richText = true;
    }

    private void CreatePlacementPanel()
    {
        GameObject rootObject = new GameObject("GachaPlacementDescriptionPanel");
        rootObject.transform.SetParent(targetCanvas.transform, false);

        placementRoot = rootObject.AddComponent<RectTransform>();
        placementRoot.anchorMin = new Vector2(0f, 0f);
        placementRoot.anchorMax = new Vector2(0f, 0f);
        placementRoot.pivot = new Vector2(0f, 0f);
        placementRoot.sizeDelta = placementPanelSize;
        placementRoot.anchoredPosition = placementAnchoredPosition;

        placementBackground = rootObject.AddComponent<Image>();
        placementBackground.color = panelColor;

        VerticalLayoutGroup layout = rootObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 5;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        placementTitleText = CreateText(
            "Title",
            rootObject.transform,
            titleFontSize,
            normalTextColor,
            placementTitleHeight
        );

        placementBodyText = CreateText(
            "Body",
            rootObject.transform,
            bodyFontSize,
            normalTextColor,
            placementBodyHeight
        );

        placementHintText = CreateText(
            "Hint",
            rootObject.transform,
            hintFontSize,
            hintTextColor,
            placementHintHeight
        );

        placementBodyText.enableWordWrapping = true;
        placementBodyText.richText = true;

        placementHintText.enableWordWrapping = true;
        placementHintText.richText = true;
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        int fontSize,
        Color color,
        float preferredHeight
    )
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0f, preferredHeight);

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.richText = true;
        text.overflowMode = TextOverflowModes.Ellipsis;

        LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = preferredHeight;
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleHeight = 0f;

        return text;
    }

    private void DebugLog(string message)
    {
        if (!showDebugLog)
        {
            return;
        }

        Debug.Log("GachaEffectDescriptionUI: " + message);
    }
}