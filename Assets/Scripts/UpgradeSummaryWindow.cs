using TMPro;
using UnityEngine;

public class UpgradeSummaryWindow : MonoBehaviour
{
    public static UpgradeSummaryWindow Instance { get; private set; }

    public GameObject summaryPanel;
    public TMP_Text summaryText;

    [Header("Buttons To Hide While Open")]
    public GameObject toggleButtonObject;
    public GameObject startDefenseButtonObject;

    public float refreshInterval = 0.2f;

    private float refreshTimer;
    private bool isVisible;

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
        HideWindow();
    }

    private void Update()
    {
        if (!isVisible)
        {
            return;
        }

        refreshTimer += Time.deltaTime;

        if (refreshTimer >= refreshInterval)
        {
            refreshTimer = 0f;
            Refresh();
        }
    }

    public void ToggleWindow()
    {
        if (isVisible)
        {
            HideWindow();
        }
        else
        {
            ShowWindow();
        }
    }

    public void ShowWindow()
    {
        isVisible = true;
        refreshTimer = 0f;

        if (summaryPanel != null)
        {
            summaryPanel.SetActive(true);
            summaryPanel.transform.SetAsLastSibling();
        }

        if (toggleButtonObject != null)
        {
            toggleButtonObject.SetActive(false);
        }

        if (startDefenseButtonObject != null)
        {
            startDefenseButtonObject.SetActive(false);
        }

        Refresh();
    }

    public void HideWindow()
    {
        isVisible = false;

        if (summaryPanel != null)
        {
            summaryPanel.SetActive(false);
        }

        if (toggleButtonObject != null)
        {
            toggleButtonObject.SetActive(true);
        }

        RefreshStartDefenseButtonVisibility();
    }

    public void Refresh()
    {
        if (summaryText == null)
        {
            Debug.LogWarning("UpgradeSummaryText is not assigned!");
            return;
        }

        if (RunManager.Instance == null)
        {
            summaryText.text = "RUN MANAGER NOT FOUND";
            return;
        }

        RunManager run = RunManager.Instance;

        int currentGachaCost = GetCurrentGachaCost();

        summaryText.text =
            "UPGRADE SUMMARY\n\n" +
            "SLIME DMG      +" + run.slimeDamageBonus + "\n" +
            "SLIME RANGE    +" + run.slimeRangeBonus.ToString("0.0") + "\n" +
            "SLIME HP       +" + run.slimeHpBonus + "\n" +
            "TRAP DMG       +" + run.trapDamageBonus + "\n" +
            "TRAP RANGE     +" + run.trapRangeBonus.ToString("0.0") + "\n" +
            "MANA BONUS     +" + run.manaRewardBonus + "\n" +
            "MAX SLIME      +" + run.maxSlimeBonus + "\n" +
            "GACHA COST     " + currentGachaCost + "\n" +
            "CORE HP        " + run.coreHp + " / " + run.maxCoreHp;
    }

    public void RefreshIfVisible()
    {
        if (!isVisible)
        {
            return;
        }

        Refresh();
    }

    private void RefreshStartDefenseButtonVisibility()
    {
        if (startDefenseButtonObject == null)
        {
            return;
        }

        if (RunManager.Instance == null)
        {
            startDefenseButtonObject.SetActive(true);
            return;
        }

        bool shouldShowStartDefenseButton =
            !RunManager.Instance.isGameOver &&
            RunManager.Instance.currentPhase == RunManager.GamePhase.DungeonBuild;

        startDefenseButtonObject.SetActive(shouldShowStartDefenseButton);
    }

    private int GetCurrentGachaCost()
    {
        if (RunManager.Instance == null)
        {
            return 0;
        }

        GachaManager gachaManager = FindFirstObjectByType<GachaManager>();

        int baseCost = 1;

        if (gachaManager != null)
        {
            baseCost = gachaManager.rollCost;
        }

        return RunManager.Instance.GetFinalGachaCost(baseCost);
    }
}