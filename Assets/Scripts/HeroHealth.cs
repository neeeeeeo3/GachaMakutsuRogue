using UnityEngine;

public class HeroHealth : MonoBehaviour
{
    public int maxHp = 10;
    public int currentHp;
    public int manaReward = 3;

    [Header("Hero Type")]
    public string heroName = "Normal Hero";
    public float trapDamageMultiplier = 1f;

    [Header("Nutrients On Death")]
    public bool dropNutrientsOnDeath = true;
    public HeroNutrientDropper nutrientDropper;
    public bool autoAddNutrientDropper = true;

    [Header("Damage Popup")]
    public Vector3 damagePopupOffset = new Vector3(0f, 0.45f, 0f);

    private bool isDead;
    private HeroFloatingHud floatingHud;

    private void Start()
    {
        if (currentHp <= 0)
        {
            currentHp = maxHp;
        }

        EnsureFloatingHud();
        EnsureNutrientDropper();
        RefreshFloatingHud();
    }

    public void ConfigureHero(
        string newHeroName,
        int newMaxHp,
        int newManaReward,
        float newTrapDamageMultiplier,
        Color heroColor
    )
    {
        heroName = newHeroName;
        maxHp = newMaxHp;
        currentHp = maxHp;
        manaReward = newManaReward;
        trapDamageMultiplier = newTrapDamageMultiplier;
        isDead = false;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = heroColor;
        }

        EnsureFloatingHud();
        EnsureNutrientDropper();
        RefreshFloatingHud();

        Debug.Log(heroName + " configured. HP: " + maxHp + " / Trap Multiplier: " + trapDamageMultiplier);
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        if (damage < 0)
        {
            damage = 0;
        }

        currentHp -= damage;

        if (currentHp < 0)
        {
            currentHp = 0;
        }

        ShowDamagePopup(damage);
        RefreshFloatingHud();

        Debug.Log(heroName + " HP: " + currentHp);

        if (currentHp <= 0)
        {
            Die();
        }
    }

    public void TakeTrapDamage(int damage)
    {
        int finalDamage = Mathf.RoundToInt(damage * trapDamageMultiplier);

        if (finalDamage < 0)
        {
            finalDamage = 0;
        }

        Debug.Log(heroName + " took trap damage. Base: " + damage + " Final: " + finalDamage);

        TakeDamage(finalDamage);
    }

    private void Die()
    {
        isDead = true;

        Debug.Log(heroName + " defeated!");

        DropNutrients();

        int finalManaReward = manaReward;

        if (RunManager.Instance != null)
        {
            finalManaReward += RunManager.Instance.manaRewardBonus;
            RunManager.Instance.AddMana(finalManaReward);
        }
        else
        {
            Debug.LogWarning("RunManager.Instance is null!");
        }

        if (UpgradeManager.Instance != null)
        {
            Debug.Log("Show upgrade choices!");
            UpgradeManager.Instance.ShowUpgradeChoices();
        }
        else
        {
            Debug.LogWarning("UpgradeManager.Instance is null!");
        }

        Destroy(gameObject);
    }

    private void DropNutrients()
    {
        if (!dropNutrientsOnDeath)
        {
            return;
        }

        EnsureNutrientDropper();

        if (nutrientDropper == null)
        {
            Debug.LogWarning("HeroHealth: NutrientDropper not found.");
            return;
        }

        nutrientDropper.DropNutrientsAtHeroPosition(heroName);
    }

    private void ShowDamagePopup(int damage)
    {
        Color popupColor;

        if (damage <= 0)
        {
            popupColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        }
        else
        {
            popupColor = new Color(1f, 0.35f, 0.25f, 1f);
        }

        DamagePopup.Create(transform.position + damagePopupOffset, damage, popupColor);
    }

    private void EnsureFloatingHud()
    {
        floatingHud = GetComponent<HeroFloatingHud>();

        if (floatingHud == null)
        {
            floatingHud = gameObject.AddComponent<HeroFloatingHud>();
        }
    }

    private void EnsureNutrientDropper()
    {
        if (!dropNutrientsOnDeath)
        {
            return;
        }

        if (nutrientDropper == null)
        {
            nutrientDropper = GetComponent<HeroNutrientDropper>();
        }

        if (nutrientDropper == null && autoAddNutrientDropper)
        {
            nutrientDropper = gameObject.AddComponent<HeroNutrientDropper>();
        }
    }

    private void RefreshFloatingHud()
    {
        if (floatingHud != null)
        {
            floatingHud.Refresh();
        }
    }
}