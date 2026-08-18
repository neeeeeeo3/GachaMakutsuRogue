using UnityEngine;

public class SlimeHealth : MonoBehaviour
{
    public int maxHp = 3;
    public int currentHp;

    [Header("Rarity Display")]
    public bool showRarityDebugLog = true;
    public bool applyRunManagerHpBonus = true;

    [Header("Damage Popup")]
    public Vector3 damagePopupOffset = new Vector3(0f, 0.35f, 0f);

    public bool IsDead { get; private set; }

    private bool initialized;
    private GachaRarityHolder rarityHolder;

    private void Awake()
    {
        rarityHolder = GetComponent<GachaRarityHolder>();
    }

    private void Start()
    {
        InitializeHp();
    }

    private void InitializeHp()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        if (rarityHolder == null)
        {
            rarityHolder = GetComponent<GachaRarityHolder>();
        }

        if (applyRunManagerHpBonus && RunManager.Instance != null)
        {
            maxHp += RunManager.Instance.slimeHpBonus;
        }

        if (maxHp < 1)
        {
            maxHp = 1;
        }

        currentHp = maxHp;
        IsDead = false;

        if (showRarityDebugLog)
        {
            Debug.Log(
                "Slime initialized. "
                + GetRarityLogPrefix()
                + " HP: "
                + currentHp
                + " / "
                + maxHp
            );
        }
    }

    public void ApplyMaxHpBonus(int amount)
    {
        if (IsDead)
        {
            return;
        }

        if (!initialized)
        {
            InitializeHp();
        }

        maxHp += amount;
        currentHp += amount;

        if (maxHp < 1)
        {
            maxHp = 1;
        }

        if (currentHp < 0)
        {
            currentHp = 0;
        }

        Debug.Log("Existing slime HP increased. HP: " + currentHp + " / " + maxHp);
    }

    public void Heal(int amount)
    {
        if (IsDead)
        {
            return;
        }

        if (!initialized)
        {
            InitializeHp();
        }

        if (amount <= 0)
        {
            return;
        }

        currentHp += amount;

        if (currentHp > maxHp)
        {
            currentHp = maxHp;
        }

        Debug.Log("Slime healed. HP: " + currentHp + " / " + maxHp);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
        {
            return;
        }

        if (!initialized)
        {
            InitializeHp();
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

        Debug.Log(
            "Slime HP: "
            + currentHp
            + " / "
            + maxHp
            + " "
            + GetRarityLogPrefix()
        );

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;

        Debug.Log("Slime defeated! " + GetRarityLogPrefix());

        Destroy(gameObject);
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
            popupColor = GetDamagePopupColor();
        }

        DamagePopup.Create(transform.position + damagePopupOffset, damage, popupColor);
    }

    private Color GetDamagePopupColor()
    {
        if (rarityHolder == null)
        {
            rarityHolder = GetComponent<GachaRarityHolder>();
        }

        if (rarityHolder == null)
        {
            return new Color(1f, 0.9f, 0.25f, 1f);
        }

        switch (rarityHolder.rarity)
        {
            case GachaRarityType.Rare:
                return new Color(0.35f, 0.85f, 1f, 1f);

            case GachaRarityType.Epic:
                return new Color(1f, 0.45f, 1f, 1f);
        }

        return new Color(1f, 0.9f, 0.25f, 1f);
    }

    private string GetRarityLogPrefix()
    {
        if (rarityHolder == null)
        {
            rarityHolder = GetComponent<GachaRarityHolder>();
        }

        if (rarityHolder == null)
        {
            return "[COMMON SLIME]";
        }

        switch (rarityHolder.rarity)
        {
            case GachaRarityType.Rare:
                return "[RARE SLIME]";

            case GachaRarityType.Epic:
                return "[EPIC SLIME]";
        }

        return "[COMMON SLIME]";
    }
}