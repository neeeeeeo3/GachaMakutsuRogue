using TMPro;
using UnityEngine;

public class RunManager : MonoBehaviour
{
    public enum GamePhase
    {
        DungeonBuild,
        HeroDefense,
        Upgrade,
        GameOver
    }

    public static RunManager Instance { get; private set; }

    [Header("Phase")]
    public GamePhase currentPhase = GamePhase.DungeonBuild;
    public TMP_Text phaseText;
    public GameObject startDefenseButtonObject;

    [Header("Mana")]
    public int mana = 5;
    public TMP_Text manaText;

    [Header("Core")]
    public int coreHp = 10;
    public int maxCoreHp = 10;
    public TMP_Text coreText;

    [Header("Upgrade Status")]
    public TMP_Text upgradeStatusText;

    [Header("Upgrade Values")]
    public int slimeDamageBonus = 0;
    public float slimeRangeBonus = 0f;
    public int slimeHpBonus = 0;
    public int manaRewardBonus = 0;
    public int trapDamageBonus = 0;
    public float trapRangeBonus = 0f;
    public int maxSlimeBonus = 0;
    public int gachaCostReduction = 0;

    public bool isUpgradeSelectionActive;
    public bool isGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        currentPhase = GamePhase.DungeonBuild;

        UpdateManaText();
        UpdateCoreText();
        UpdatePhaseText();
        UpdateStartDefenseButton();
        RefreshUpgradeStatusText();
    }

    public bool IsDungeonBuildPhase()
    {
        return !isGameOver && currentPhase == GamePhase.DungeonBuild;
    }

    public bool IsHeroDefensePhase()
    {
        return !isGameOver && currentPhase == GamePhase.HeroDefense;
    }

    public bool IsUpgradePhase()
    {
        return !isGameOver && currentPhase == GamePhase.Upgrade;
    }

    public void SetPhaseToDungeonBuild()
    {
        if (isGameOver)
        {
            return;
        }

        currentPhase = GamePhase.DungeonBuild;
        isUpgradeSelectionActive = false;

        UpdatePhaseText();
        UpdateStartDefenseButton();

        Debug.Log("Phase: Dungeon Build");
    }

    public void SetPhaseToHeroDefense()
    {
        if (isGameOver)
        {
            return;
        }

        currentPhase = GamePhase.HeroDefense;
        isUpgradeSelectionActive = false;

        UpdatePhaseText();
        UpdateStartDefenseButton();

        Debug.Log("Phase: Hero Defense");
    }

    public void SetPhaseToUpgrade()
    {
        if (isGameOver)
        {
            return;
        }

        currentPhase = GamePhase.Upgrade;
        isUpgradeSelectionActive = true;

        UpdatePhaseText();
        UpdateStartDefenseButton();

        Debug.Log("Phase: Upgrade");
    }

    public void StartHeroDefensePhase()
    {
        if (isGameOver)
        {
            return;
        }

        if (currentPhase != GamePhase.DungeonBuild)
        {
            Debug.Log("Cannot start defense outside Dungeon Build Phase.");
            return;
        }

        WaveManager waveManager = FindFirstObjectByType<WaveManager>();

        if (waveManager == null)
        {
            Debug.LogError("WaveManager not found!");
            return;
        }

        if (waveManager.HasActiveHero())
        {
            Debug.Log("Hero already exists!");
            return;
        }

        SetPhaseToHeroDefense();
        waveManager.SpawnNextHero();
    }

    public bool SpendMana(int amount)
    {
        if (isGameOver)
        {
            return false;
        }

        if (isUpgradeSelectionActive)
        {
            Debug.Log("Cannot roll during upgrade selection!");
            return false;
        }

        if (mana < amount)
        {
            Debug.Log("Not enough mana!");
            return false;
        }

        mana -= amount;
        Debug.Log("MANA: " + mana);
        UpdateManaText();
        return true;
    }

    public int GetFinalGachaCost(int baseCost)
    {
        int finalCost = baseCost - gachaCostReduction;

        if (finalCost < 0)
        {
            finalCost = 0;
        }

        return finalCost;
    }

    public void AddMana(int amount)
    {
        if (isGameOver)
        {
            return;
        }

        mana += amount;
        Debug.Log("Mana +" + amount + " / Current Mana: " + mana);
        UpdateManaText();
    }

    public void TakeCoreDamage(int damage)
    {
        if (isGameOver)
        {
            return;
        }

        coreHp -= damage;
        Debug.Log("Core Damage: " + damage + " / Core HP: " + coreHp);

        if (coreHp <= 0)
        {
            coreHp = 0;
            GameOver();
        }

        UpdateCoreText();
    }

    public void HealCore(int amount)
    {
        if (isGameOver)
        {
            return;
        }

        coreHp += amount;

        if (coreHp > maxCoreHp)
        {
            coreHp = maxCoreHp;
        }

        Debug.Log("Core Heal: " + amount + " / Core HP: " + coreHp);
        UpdateCoreText();
    }

    public void AddSlimeDamageBonus(int amount)
    {
        if (isGameOver)
        {
            return;
        }

        slimeDamageBonus += amount;
        RefreshUpgradeStatusText();
    }

    public void AddSlimeRangeBonus(float amount)
    {
        if (isGameOver)
        {
            return;
        }

        slimeRangeBonus += amount;
        RefreshUpgradeStatusText();
    }

    public void AddSlimeHpBonus(int amount)
    {
        if (isGameOver)
        {
            return;
        }

        slimeHpBonus += amount;

        SlimeHealth[] slimes = FindObjectsByType<SlimeHealth>(FindObjectsSortMode.None);

        foreach (SlimeHealth slime in slimes)
        {
            if (slime != null)
            {
                slime.ApplyMaxHpBonus(amount);
            }
        }

        Debug.Log("Slime HP Bonus +" + amount + " / Total: " + slimeHpBonus);

        RefreshUpgradeStatusText();
    }

    public void AddManaRewardBonus(int amount)
    {
        if (isGameOver)
        {
            return;
        }

        manaRewardBonus += amount;
        RefreshUpgradeStatusText();
    }

    public void AddTrapDamageBonus(int amount)
    {
        if (isGameOver)
        {
            return;
        }

        trapDamageBonus += amount;
        RefreshUpgradeStatusText();
    }

    public void AddTrapRangeBonus(float amount)
    {
        if (isGameOver)
        {
            return;
        }

        trapRangeBonus += amount;
        RefreshUpgradeStatusText();
    }

    public void AddMaxSlimeBonus(int amount)
    {
        if (isGameOver)
        {
            return;
        }

        maxSlimeBonus += amount;
        RefreshUpgradeStatusText();
    }

    public void AddGachaCostReduction(int amount)
    {
        if (isGameOver)
        {
            return;
        }

        gachaCostReduction += amount;
        RefreshUpgradeStatusText();
    }

    public void SetUpgradeSelectionActive(bool active)
    {
        if (isGameOver)
        {
            isUpgradeSelectionActive = false;
            return;
        }

        isUpgradeSelectionActive = active;

        if (active)
        {
            currentPhase = GamePhase.Upgrade;
        }

        UpdatePhaseText();
        UpdateStartDefenseButton();
    }

    private void GameOver()
    {
        isGameOver = true;
        isUpgradeSelectionActive = false;
        currentPhase = GamePhase.GameOver;

        Debug.Log("GAME OVER!");

        UpdatePhaseText();
        UpdateCoreText();
        UpdateStartDefenseButton();
    }

    private void UpdateManaText()
    {
        if (manaText == null)
        {
            Debug.LogWarning("ManaText is not assigned!");
            return;
        }

        manaText.text = "MANA: " + mana;
    }

    private void UpdateCoreText()
    {
        if (coreText == null)
        {
            Debug.LogWarning("CoreText is not assigned!");
            return;
        }

        if (isGameOver)
        {
            coreText.text = "CORE: 0\nGAME OVER";
        }
        else
        {
            coreText.text = "CORE: " + coreHp + " / " + maxCoreHp;
        }
    }

    private void UpdatePhaseText()
    {
        if (phaseText == null)
        {
            return;
        }

        switch (currentPhase)
        {
            case GamePhase.DungeonBuild:
                phaseText.text = "DUNGEON BUILD PHASE";
                break;

            case GamePhase.HeroDefense:
                phaseText.text = "HERO DEFENSE PHASE";
                break;

            case GamePhase.Upgrade:
                phaseText.text = "UPGRADE PHASE";
                break;

            case GamePhase.GameOver:
                phaseText.text = "GAME OVER";
                break;
        }
    }

    private void UpdateStartDefenseButton()
    {
        if (startDefenseButtonObject == null)
        {
            return;
        }

        bool shouldShow = !isGameOver && currentPhase == GamePhase.DungeonBuild;
        startDefenseButtonObject.SetActive(shouldShow);
    }

    private void RefreshUpgradeStatusText()
    {
        if (upgradeStatusText == null)
        {
            return;
        }

        upgradeStatusText.text =
            "SLIME DMG: +" + slimeDamageBonus + "\n" +
            "SLIME RANGE: +" + slimeRangeBonus.ToString("0.0") + "\n" +
            "SLIME HP: +" + slimeHpBonus + "\n" +
            "TRAP DMG: +" + trapDamageBonus + "\n" +
            "TRAP RANGE: +" + trapRangeBonus.ToString("0.0") + "\n" +
            "MANA BONUS: +" + manaRewardBonus + "\n" +
            "MAX SLIME: +" + maxSlimeBonus + "\n" +
            "GACHA COST: -" + gachaCostReduction;
    }
}