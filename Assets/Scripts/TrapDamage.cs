using UnityEngine;

public class TrapDamage : MonoBehaviour
{
    [Header("Base Trap")]
    public float triggerRange = 0.8f;
    public int damage = 3;
    public bool destroyAfterTrigger = true;

    [Header("Rarity Bonus")]
    public bool applyRarityBonus = true;

    [Tooltip("RARE TRAPの追加ダメージです。")]
    public int rareDamageBonus = 2;

    [Tooltip("EPIC TRAPの追加ダメージです。")]
    public int epicDamageBonus = 5;

    [Tooltip("RARE TRAPの追加発動範囲です。")]
    public float rareTriggerRangeBonus = 0.15f;

    [Tooltip("EPIC TRAPの追加発動範囲です。")]
    public float epicTriggerRangeBonus = 0.25f;

    [Header("Epic Area Damage")]
    public bool epicUsesAreaDamage = true;

    [Tooltip("EPIC TRAPが周囲にも当てる範囲です。")]
    public float epicAreaRange = 1.35f;

    [Tooltip("EPIC TRAPの範囲ダメージ倍率です。中心以外の勇者に使います。")]
    [Range(0.1f, 1f)]
    public float epicAreaDamageMultiplier = 0.65f;

    [Tooltip("範囲ダメージの最低値です。")]
    public int epicMinimumAreaDamage = 1;

    [Header("Safety")]
    public bool ignoreInactiveHeroes = true;
    public bool triggerOnlyDuringHeroDefense = true;

    [Header("Visual Feedback")]
    public bool createActivateEffect = true;
    public float effectDuration = 0.35f;
    public float commonEffectSize = 0.75f;
    public float rareEffectSize = 1.0f;
    public float epicEffectSize = 1.45f;
    public int effectSortingOrder = 2900;

    public Color commonEffectColor = new Color(1f, 0.85f, 0.25f, 0.75f);
    public Color rareEffectColor = new Color(0.25f, 0.85f, 1f, 0.82f);
    public Color epicEffectColor = new Color(1f, 0.35f, 1f, 0.88f);

    [Header("Debug")]
    public bool showDebugLog = true;

    private bool hasTriggered;
    private GachaRarityHolder rarityHolder;
    private Sprite circleSprite;

    private void Awake()
    {
        rarityHolder = GetComponent<GachaRarityHolder>();
        circleSprite = CreateCircleSprite(64);
    }

    private void Update()
    {
        if (hasTriggered)
        {
            return;
        }

        if (!CanTriggerNow())
        {
            return;
        }

        HeroHealth targetHero = FindTriggerTargetHero();

        if (targetHero == null)
        {
            return;
        }

        ActivateTrap(targetHero);
    }

    private bool CanTriggerNow()
    {
        if (RunManager.Instance != null && RunManager.Instance.isGameOver)
        {
            return false;
        }

        if (triggerOnlyDuringHeroDefense)
        {
            if (RunManager.Instance != null && RunManager.Instance.IsDungeonBuildPhase())
            {
                return false;
            }
        }

        return true;
    }

    private HeroHealth FindTriggerTargetHero()
    {
        HeroHealth[] heroes = FindObjectsByType<HeroHealth>(FindObjectsSortMode.None);

        HeroHealth bestHero = null;
        float bestDistance = float.MaxValue;
        float finalTriggerRange = GetFinalTriggerRange();

        foreach (HeroHealth hero in heroes)
        {
            if (hero == null)
            {
                continue;
            }

            if (ignoreInactiveHeroes && !hero.gameObject.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, hero.transform.position);

            if (distance > finalTriggerRange)
            {
                continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestHero = hero;
            }
        }

        return bestHero;
    }

    private void ActivateTrap(HeroHealth primaryHero)
    {
        if (primaryHero == null)
        {
            return;
        }

        hasTriggered = true;

        int finalDamage = GetFinalDamage();
        GachaRarityType rarity = GetRarity();

        if (showDebugLog)
        {
            Debug.Log(
                GetTrapLabel()
                + " activated! Damage: "
                + finalDamage
                + " Range: "
                + GetFinalTriggerRange().ToString("0.00")
            );
        }

        primaryHero.TakeTrapDamage(finalDamage);

        if (rarity == GachaRarityType.Epic && epicUsesAreaDamage)
        {
            ApplyEpicAreaDamage(primaryHero, finalDamage);
        }

        if (createActivateEffect)
        {
            CreateActivateEffect(rarity);
        }

        if (destroyAfterTrigger)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyEpicAreaDamage(HeroHealth primaryHero, int centerDamage)
    {
        HeroHealth[] heroes = FindObjectsByType<HeroHealth>(FindObjectsSortMode.None);

        int areaDamage = Mathf.RoundToInt(centerDamage * epicAreaDamageMultiplier);

        if (areaDamage < epicMinimumAreaDamage)
        {
            areaDamage = epicMinimumAreaDamage;
        }

        foreach (HeroHealth hero in heroes)
        {
            if (hero == null)
            {
                continue;
            }

            if (hero == primaryHero)
            {
                continue;
            }

            if (ignoreInactiveHeroes && !hero.gameObject.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, hero.transform.position);

            if (distance > epicAreaRange)
            {
                continue;
            }

            hero.TakeTrapDamage(areaDamage);

            if (showDebugLog)
            {
                Debug.Log("EPIC TRAP area hit! Damage: " + areaDamage);
            }
        }
    }

    private int GetFinalDamage()
    {
        int finalDamage = damage;

        if (RunManager.Instance != null)
        {
            finalDamage += RunManager.Instance.trapDamageBonus;
        }

        if (!applyRarityBonus)
        {
            return Mathf.Max(0, finalDamage);
        }

        switch (GetRarity())
        {
            case GachaRarityType.Rare:
                finalDamage += rareDamageBonus;
                break;

            case GachaRarityType.Epic:
                finalDamage += epicDamageBonus;
                break;
        }

        return Mathf.Max(0, finalDamage);
    }

    private float GetFinalTriggerRange()
    {
        float finalTriggerRange = triggerRange;

        if (RunManager.Instance != null)
        {
            finalTriggerRange += RunManager.Instance.trapRangeBonus;
        }

        if (!applyRarityBonus)
        {
            return Mathf.Max(0.01f, finalTriggerRange);
        }

        switch (GetRarity())
        {
            case GachaRarityType.Rare:
                finalTriggerRange += rareTriggerRangeBonus;
                break;

            case GachaRarityType.Epic:
                finalTriggerRange += epicTriggerRangeBonus;
                break;
        }

        return Mathf.Max(0.01f, finalTriggerRange);
    }

    private GachaRarityType GetRarity()
    {
        if (rarityHolder == null)
        {
            rarityHolder = GetComponent<GachaRarityHolder>();
        }

        if (rarityHolder == null)
        {
            return GachaRarityType.Common;
        }

        return rarityHolder.rarity;
    }

    private string GetTrapLabel()
    {
        switch (GetRarity())
        {
            case GachaRarityType.Rare:
                return "RARE TRAP";

            case GachaRarityType.Epic:
                return "EPIC TRAP";
        }

        return "COMMON TRAP";
    }

    private Color GetEffectColor(GachaRarityType rarity)
    {
        switch (rarity)
        {
            case GachaRarityType.Rare:
                return rareEffectColor;

            case GachaRarityType.Epic:
                return epicEffectColor;
        }

        return commonEffectColor;
    }

    private float GetEffectSize(GachaRarityType rarity)
    {
        switch (rarity)
        {
            case GachaRarityType.Rare:
                return rareEffectSize;

            case GachaRarityType.Epic:
                return epicEffectSize;
        }

        return commonEffectSize;
    }

    private void CreateActivateEffect(GachaRarityType rarity)
    {
        if (circleSprite == null)
        {
            circleSprite = CreateCircleSprite(64);
        }

        GameObject effectObject = new GameObject(GetTrapLabel() + "_ActivateEffect");
        effectObject.transform.position = transform.position;
        effectObject.transform.localScale = Vector3.one * GetEffectSize(rarity);

        SpriteRenderer spriteRenderer = effectObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = circleSprite;
        spriteRenderer.color = GetEffectColor(rarity);
        spriteRenderer.sortingOrder = effectSortingOrder;

        TrapActivateEffectAnimation animation = effectObject.AddComponent<TrapActivateEffectAnimation>();
        animation.Initialize(effectDuration);
    }

    private Sprite CreateCircleSprite(int textureSize)
    {
        int safeSize = Mathf.Max(16, textureSize);

        Texture2D texture = new Texture2D(safeSize, safeSize, TextureFormat.RGBA32, false);
        texture.name = "TrapActivateCircleTexture";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((safeSize - 1) * 0.5f, (safeSize - 1) * 0.5f);
        float radius = safeSize * 0.48f;

        for (int y = 0; y < safeSize; y++)
        {
            for (int x = 0; x < safeSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - distance);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, safeSize, safeSize),
            new Vector2(0.5f, 0.5f),
            safeSize
        );
    }
}

public class TrapActivateEffectAnimation : MonoBehaviour
{
    private float duration = 0.35f;
    private float timer;
    private Vector3 startScale;
    private SpriteRenderer spriteRenderer;

    public void Initialize(float newDuration)
    {
        duration = Mathf.Max(0.01f, newDuration);
        startScale = transform.localScale;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startScale = transform.localScale;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float progress = Mathf.Clamp01(timer / duration);
        float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

        transform.localScale = Vector3.Lerp(
            startScale,
            startScale * 1.7f,
            easedProgress
        );

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(color.a, 0f, easedProgress);
            spriteRenderer.color = color;
        }

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }
}