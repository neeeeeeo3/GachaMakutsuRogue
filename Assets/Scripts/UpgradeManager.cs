using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    public GameObject upgradePanel;

    public TMP_Text option1Text;
    public TMP_Text option2Text;
    public TMP_Text option3Text;

    private UpgradeType option1;
    private UpgradeType option2;
    private UpgradeType option3;

    private enum UpgradeType
    {
        SlimeDamage,
        SlimeRange,
        SlimeHp,
        TrapDamage,
        TrapRange,
        ManaReward,
        CoreHeal,
        MaxSlime,
        GachaCostDown
    }

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
        HideUpgradePanelOnly();
    }

    public void ShowUpgradeChoices()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.SetPhaseToUpgrade();
        }

        List<UpgradeType> choices = CreateRandomChoices();

        option1 = choices[0];
        option2 = choices[1];
        option3 = choices[2];

        if (option1Text != null)
        {
            option1Text.text = GetUpgradeLabel(option1);
        }

        if (option2Text != null)
        {
            option2Text.text = GetUpgradeLabel(option2);
        }

        if (option3Text != null)
        {
            option3Text.text = GetUpgradeLabel(option3);
        }

        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
            upgradePanel.transform.SetAsLastSibling();
        }

        Debug.Log("Upgrade choices shown: "
            + GetUpgradeLabel(option1) + " / "
            + GetUpgradeLabel(option2) + " / "
            + GetUpgradeLabel(option3));
    }

    public void ChooseOption1()
    {
        ApplyUpgrade(option1);
        FinishUpgradeSelection();
    }

    public void ChooseOption2()
    {
        ApplyUpgrade(option2);
        FinishUpgradeSelection();
    }

    public void ChooseOption3()
    {
        ApplyUpgrade(option3);
        FinishUpgradeSelection();
    }

    private List<UpgradeType> CreateRandomChoices()
    {
        List<UpgradeType> pool = new List<UpgradeType>
        {
            UpgradeType.SlimeDamage,
            UpgradeType.SlimeRange,
            UpgradeType.SlimeHp,
            UpgradeType.TrapDamage,
            UpgradeType.TrapRange,
            UpgradeType.ManaReward,
            UpgradeType.CoreHeal,
            UpgradeType.MaxSlime,
            UpgradeType.GachaCostDown
        };

        List<UpgradeType> result = new List<UpgradeType>();

        while (result.Count < 3 && pool.Count > 0)
        {
            int randomIndex = Random.Range(0, pool.Count);
            result.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }

        return result;
    }

    private string GetUpgradeLabel(UpgradeType upgradeType)
    {
        switch (upgradeType)
        {
            case UpgradeType.SlimeDamage:
                return "SLIME DMG +1";

            case UpgradeType.SlimeRange:
                return "SLIME RANGE +0.5";

            case UpgradeType.SlimeHp:
                return "SLIME HP +1";

            case UpgradeType.TrapDamage:
                return "TRAP DMG +2";

            case UpgradeType.TrapRange:
                return "TRAP RANGE +0.3";

            case UpgradeType.ManaReward:
                return "MANA REWARD +1";

            case UpgradeType.CoreHeal:
                return "CORE HEAL +1";

            case UpgradeType.MaxSlime:
                return "MAX SLIME +5";

            case UpgradeType.GachaCostDown:
                return "GACHA COST -1";

            default:
                return "UNKNOWN";
        }
    }

    private void ApplyUpgrade(UpgradeType upgradeType)
    {
        if (RunManager.Instance == null)
        {
            Debug.LogError("RunManager not found!");
            return;
        }

        switch (upgradeType)
        {
            case UpgradeType.SlimeDamage:
                RunManager.Instance.AddSlimeDamageBonus(1);
                Debug.Log("Upgrade selected: SLIME DMG +1");
                break;

            case UpgradeType.SlimeRange:
                RunManager.Instance.AddSlimeRangeBonus(0.5f);
                Debug.Log("Upgrade selected: SLIME RANGE +0.5");
                break;

            case UpgradeType.SlimeHp:
                RunManager.Instance.AddSlimeHpBonus(1);
                Debug.Log("Upgrade selected: SLIME HP +1");
                break;

            case UpgradeType.TrapDamage:
                RunManager.Instance.AddTrapDamageBonus(2);
                Debug.Log("Upgrade selected: TRAP DMG +2");
                break;

            case UpgradeType.TrapRange:
                RunManager.Instance.AddTrapRangeBonus(0.3f);
                Debug.Log("Upgrade selected: TRAP RANGE +0.3");
                break;

            case UpgradeType.ManaReward:
                RunManager.Instance.AddManaRewardBonus(1);
                Debug.Log("Upgrade selected: MANA REWARD +1");
                break;

            case UpgradeType.CoreHeal:
                RunManager.Instance.HealCore(1);
                Debug.Log("Upgrade selected: CORE HEAL +1");
                break;

            case UpgradeType.MaxSlime:
                RunManager.Instance.AddMaxSlimeBonus(5);
                Debug.Log("Upgrade selected: MAX SLIME +5");
                break;

            case UpgradeType.GachaCostDown:
                RunManager.Instance.AddGachaCostReduction(1);
                Debug.Log("Upgrade selected: GACHA COST -1");
                break;
        }
    }

    private void FinishUpgradeSelection()
    {
        HideUpgradePanelOnly();

        if (RunManager.Instance != null)
        {
            RunManager.Instance.SetPhaseToDungeonBuild();
        }
    }

    private void HideUpgradePanelOnly()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
    }
}