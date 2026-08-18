using UnityEngine;

public class SlimeFoodItem : MonoBehaviour
{
    [Header("Food")]
    public int foodValue = 1;

    [Header("Rarity Food Effects")]
    public bool applyRarityFoodEffects = true;

    [Tooltip("COMMON FOODで増えるスライム数です。通常は1。")]
    public int commonReproductionCount = 1;

    [Tooltip("RARE FOODで追加される増殖数です。Common 1 + Rare Bonus 1 = 合計2体など。")]
    public int rareExtraReproductionCount = 1;

    [Tooltip("EPIC FOODで追加される増殖数です。Common 1 + Epic Bonus 2 = 合計3体など。")]
    public int epicExtraReproductionCount = 2;

    [Header("Heal Effects")]
    public bool healSlimeWhenEaten = true;
    public int commonHealAmount = 0;
    public int rareHealAmount = 1;
    public int epicHealAmount = 3;

    [Header("Max HP Bonus")]
    public bool increaseMaxHpWhenEaten = true;
    public int commonMaxHpBonus = 0;
    public int rareMaxHpBonus = 0;
    public int epicMaxHpBonus = 1;

    [Header("Attack Bonus")]
    public bool strengthenAttackWhenEaten = true;
    public int commonAttackDamageBonus = 0;
    public int rareAttackDamageBonus = 0;
    public int epicAttackDamageBonus = 1;

    [Tooltip("RARE FOODを食べた時の攻撃間隔倍率です。0.95なら少し速くなります。")]
    public float rareAttackIntervalMultiplier = 0.95f;

    [Tooltip("EPIC FOODを食べた時の攻撃間隔倍率です。0.85なら速くなります。")]
    public float epicAttackIntervalMultiplier = 0.85f;

    [Tooltip("餌強化後の攻撃力上限です。強くなりすぎ防止。")]
    public int maxAttackDamageAfterFood = 6;

    [Tooltip("餌強化後の攻撃間隔下限です。速くなりすぎ防止。")]
    public float minimumAttackIntervalAfterFood = 0.35f;

    [Header("Visual Feedback")]
    public bool createEatEffect = true;
    public float eatEffectDuration = 0.35f;
    public float eatEffectStartScale = 0.25f;
    public float eatEffectEndScale = 0.75f;
    public int eatEffectSortingOrder = 2800;

    [Header("Debug")]
    public bool showDebugLog = false;

    public bool IsConsumed { get; private set; }

    private MonoBehaviour reservedBy;
    private GachaRarityHolder rarityHolder;
    private Sprite squareSprite;

    private void Awake()
    {
        rarityHolder = GetComponent<GachaRarityHolder>();
        squareSprite = CreateSquareSprite();
    }

    public bool IsAvailableFor(MonoBehaviour seeker)
    {
        if (IsConsumed)
        {
            return false;
        }

        return reservedBy == null || reservedBy == seeker;
    }

    public bool TryReserve(MonoBehaviour seeker)
    {
        if (!IsAvailableFor(seeker))
        {
            return false;
        }

        reservedBy = seeker;
        return true;
    }

    public void ReleaseReservation(MonoBehaviour seeker)
    {
        if (reservedBy == seeker)
        {
            reservedBy = null;
        }
    }

    public int GetReproductionCount()
    {
        int baseCount = Mathf.Max(1, commonReproductionCount);
        int valueBonus = Mathf.Max(0, foodValue - 1);

        if (!applyRarityFoodEffects)
        {
            return Mathf.Max(1, foodValue);
        }

        switch (GetRarity())
        {
            case GachaRarityType.Rare:
                return baseCount + valueBonus + Mathf.Max(0, rareExtraReproductionCount);

            case GachaRarityType.Epic:
                return baseCount + valueBonus + Mathf.Max(0, epicExtraReproductionCount);
        }

        return baseCount + valueBonus;
    }

    public void ApplyFoodEffectToSlime(GameObject slimeObject)
    {
        if (slimeObject == null)
        {
            return;
        }

        GachaRarityType rarity = GetRarity();

        ApplyHealthEffect(slimeObject, rarity);
        ApplyAttackEffect(slimeObject, rarity);

        if (createEatEffect)
        {
            CreateEatEffect(slimeObject.transform.position, rarity);
        }

        if (showDebugLog)
        {
            Debug.Log(
                "Food effect applied: "
                + GetFoodEffectLabel()
                + " reproduction="
                + GetReproductionCount()
            );
        }
    }

    public string GetFoodEffectLabel()
    {
        switch (GetRarity())
        {
            case GachaRarityType.Rare:
                return "RARE FOOD";

            case GachaRarityType.Epic:
                return "EPIC FOOD";
        }

        return "COMMON FOOD";
    }

    public void Consume()
    {
        if (IsConsumed)
        {
            return;
        }

        IsConsumed = true;
        Destroy(gameObject);
    }

    private void ApplyHealthEffect(GameObject slimeObject, GachaRarityType rarity)
    {
        if (!healSlimeWhenEaten && !increaseMaxHpWhenEaten)
        {
            return;
        }

        SlimeHealth slimeHealth = slimeObject.GetComponent<SlimeHealth>();

        if (slimeHealth == null)
        {
            return;
        }

        int maxHpBonus = GetMaxHpBonus(rarity);
        int healAmount = GetHealAmount(rarity);

        if (increaseMaxHpWhenEaten && maxHpBonus > 0)
        {
            slimeHealth.ApplyMaxHpBonus(maxHpBonus);
        }

        if (healSlimeWhenEaten && healAmount > 0)
        {
            slimeHealth.Heal(healAmount);
        }
    }

    private void ApplyAttackEffect(GameObject slimeObject, GachaRarityType rarity)
    {
        if (!strengthenAttackWhenEaten)
        {
            return;
        }

        SlimeAttack slimeAttack = slimeObject.GetComponent<SlimeAttack>();

        if (slimeAttack == null)
        {
            return;
        }

        int damageBonus = GetAttackDamageBonus(rarity);

        if (damageBonus > 0)
        {
            slimeAttack.attackDamage += damageBonus;

            if (slimeAttack.attackDamage > maxAttackDamageAfterFood)
            {
                slimeAttack.attackDamage = maxAttackDamageAfterFood;
            }
        }

        float intervalMultiplier = GetAttackIntervalMultiplier(rarity);

        if (intervalMultiplier > 0f && intervalMultiplier < 1f)
        {
            slimeAttack.attackInterval *= intervalMultiplier;

            if (slimeAttack.attackInterval < minimumAttackIntervalAfterFood)
            {
                slimeAttack.attackInterval = minimumAttackIntervalAfterFood;
            }
        }
    }

    private int GetHealAmount(GachaRarityType rarity)
    {
        switch (rarity)
        {
            case GachaRarityType.Rare:
                return rareHealAmount;

            case GachaRarityType.Epic:
                return epicHealAmount;
        }

        return commonHealAmount;
    }

    private int GetMaxHpBonus(GachaRarityType rarity)
    {
        switch (rarity)
        {
            case GachaRarityType.Rare:
                return rareMaxHpBonus;

            case GachaRarityType.Epic:
                return epicMaxHpBonus;
        }

        return commonMaxHpBonus;
    }

    private int GetAttackDamageBonus(GachaRarityType rarity)
    {
        switch (rarity)
        {
            case GachaRarityType.Rare:
                return rareAttackDamageBonus;

            case GachaRarityType.Epic:
                return epicAttackDamageBonus;
        }

        return commonAttackDamageBonus;
    }

    private float GetAttackIntervalMultiplier(GachaRarityType rarity)
    {
        switch (rarity)
        {
            case GachaRarityType.Rare:
                return rareAttackIntervalMultiplier;

            case GachaRarityType.Epic:
                return epicAttackIntervalMultiplier;
        }

        return 1f;
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

    private Color GetEffectColor(GachaRarityType rarity)
    {
        switch (rarity)
        {
            case GachaRarityType.Rare:
                return new Color(0.3f, 0.85f, 1f, 0.82f);

            case GachaRarityType.Epic:
                return new Color(1f, 0.45f, 1f, 0.88f);
        }

        return new Color(1f, 0.75f, 0.28f, 0.78f);
    }

    private void CreateEatEffect(Vector3 position, GachaRarityType rarity)
    {
        if (squareSprite == null)
        {
            squareSprite = CreateSquareSprite();
        }

        GameObject effectObject = new GameObject("FoodEatEffect_" + GetFoodEffectLabel());
        effectObject.transform.position = position;
        effectObject.transform.localScale = Vector3.one * eatEffectStartScale;

        SpriteRenderer spriteRenderer = effectObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = squareSprite;
        spriteRenderer.color = GetEffectColor(rarity);
        spriteRenderer.sortingOrder = eatEffectSortingOrder;

        FoodEatEffectAnimation animation = effectObject.AddComponent<FoodEatEffectAnimation>();
        animation.Initialize(
            eatEffectDuration,
            eatEffectStartScale,
            eatEffectEndScale
        );
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.name = "FoodEatEffectSquareTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );
    }

    private void OnDisable()
    {
        reservedBy = null;
    }
}

public class FoodEatEffectAnimation : MonoBehaviour
{
    private float duration = 0.35f;
    private float startScale = 0.25f;
    private float endScale = 0.75f;

    private float timer;
    private SpriteRenderer spriteRenderer;

    public void Initialize(
        float newDuration,
        float newStartScale,
        float newEndScale
    )
    {
        duration = Mathf.Max(0.01f, newDuration);
        startScale = Mathf.Max(0.01f, newStartScale);
        endScale = Mathf.Max(startScale, newEndScale);

        transform.localScale = Vector3.one * startScale;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float progress = Mathf.Clamp01(timer / duration);
        float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

        float scale = Mathf.Lerp(startScale, endScale, easedProgress);
        transform.localScale = Vector3.one * scale;

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