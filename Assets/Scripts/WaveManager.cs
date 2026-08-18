using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    private enum HeroKind
    {
        Normal,
        Fast,
        Tank,
        Thief
    }

    public GameObject heroPrefab;

    [Header("Optional UI")]
    public TMP_Text heroInfoText;

    [Header("Build Phase Next Hero Preview")]
    public bool prepareNextHeroDuringDungeonBuild = true;
    public bool showNextHeroPreviewUI = true;
    public WaveWarningUI waveWarningUI;
    public bool autoFindWaveWarningUI = true;
    public string entranceDisplayName = "ENTRANCE";
    public string previewStatusText = "READY";

    [Tooltip("ダンジョン作成フェーズ中、表示を定期的に更新します。")]
    public float previewRefreshInterval = 0.5f;

    [Header("Start Block Warning")]
    public bool showStartBlockedWarning = true;

    [Tooltip("警告ウィンドウを何秒表示するか。WaveWarningUI側の折りたたみ時間より短めがおすすめです。")]
    public float startBlockedWarningSeconds = 2.4f;

    public string startBlockedWarningTitle = "WARNING";

    [Header("No Core Warning")]
    public string noCoreWarningMainLine = "NO CORE";
    public string noCoreWarningDetailLine = "SET CORE FIRST";
    public string noCoreWarningStatusLine = "DEFENSE BLOCKED";

    [Header("No Path Warning")]
    public string noPathWarningMainLine = "NO PATH";
    public string noPathWarningDetailLine = "DIG TUNNEL TO CORE";
    public string noPathWarningStatusLine = "DEFENSE BLOCKED";

    [Header("No Grid Warning")]
    public string noGridWarningMainLine = "NO DUNGEON GRID";
    public string noGridWarningDetailLine = "GRID MANAGER NOT FOUND";
    public string noGridWarningStatusLine = "DEFENSE BLOCKED";

    [Header("No Hero Prefab Warning")]
    public string noHeroPrefabWarningMainLine = "NO HERO PREFAB";
    public string noHeroPrefabWarningDetailLine = "ASSIGN HERO PREFAB";
    public string noHeroPrefabWarningStatusLine = "DEFENSE BLOCKED";

    [Header("Hero Visual Mapping")]
    public HeroVisualBuilder.HeroVisualType normalHeroVisualType = HeroVisualBuilder.HeroVisualType.BraveHero;
    public HeroVisualBuilder.HeroVisualType fastHeroVisualType = HeroVisualBuilder.HeroVisualType.Ranger;
    public HeroVisualBuilder.HeroVisualType tankHeroVisualType = HeroVisualBuilder.HeroVisualType.HeavyWarrior;
    public HeroVisualBuilder.HeroVisualType thiefHeroVisualType = HeroVisualBuilder.HeroVisualType.Thief;

    [Header("Debug")]
    public bool showDebugLog = false;

    private GameObject currentHero;
    private int waveNumber;

    private bool hasPreparedNextHero;
    private int preparedWaveNumber;
    private HeroKind preparedHeroKind;

    private float nextPreviewRefreshTime;

    private bool isShowingStartBlockedWarning;
    private Coroutine startBlockedWarningCoroutine;

    private void Start()
    {
        AutoFindReferences();
        ClearHeroInfoText();
        EnsurePreparedNextHeroPreview(true);
    }

    private void Update()
    {
        if (RunManager.Instance != null && RunManager.Instance.isGameOver)
        {
            HideNextHeroPreview();
            return;
        }

        if (RunManager.Instance == null)
        {
            return;
        }

        if (!RunManager.Instance.IsDungeonBuildPhase())
        {
            return;
        }

        if (currentHero != null)
        {
            return;
        }

        if (isShowingStartBlockedWarning)
        {
            return;
        }

        if (Time.time < nextPreviewRefreshTime)
        {
            return;
        }

        nextPreviewRefreshTime = Time.time + Mathf.Max(0.05f, previewRefreshInterval);

        EnsurePreparedNextHeroPreview(false);
    }

    public bool HasActiveHero()
    {
        return currentHero != null;
    }

    public void SpawnNextHero()
    {
        if (RunManager.Instance != null && RunManager.Instance.isGameOver)
        {
            return;
        }

        if (currentHero != null)
        {
            Debug.Log("Cannot spawn hero. Current hero still exists.");
            return;
        }

        DungeonGridManager dungeonGridManager = GetDungeonGridManager();

        if (dungeonGridManager == null)
        {
            Debug.LogError("DungeonGridManager not found!");

            if (heroInfoText != null)
            {
                heroInfoText.text = "NO DUNGEON GRID";
            }

            ShowStartBlockedWarning(
                noGridWarningMainLine,
                noGridWarningDetailLine,
                noGridWarningStatusLine
            );

            ReturnToDungeonBuildPhase();
            return;
        }

        if (!dungeonGridManager.hasCorePlaced)
        {
            Debug.Log("Cannot start defense. No core placed.");

            if (heroInfoText != null)
            {
                heroInfoText.text = "NO CORE\nSET CORE FIRST";
            }

            ShowStartBlockedWarning(
                noCoreWarningMainLine,
                noCoreWarningDetailLine,
                noCoreWarningStatusLine
            );

            ReturnToDungeonBuildPhase();
            return;
        }

        if (!dungeonGridManager.TryFindPathFromEntranceToCore(out List<Vector3> pathPoints))
        {
            Debug.Log("Cannot start defense. No connected path from entrance to core.");

            if (heroInfoText != null)
            {
                heroInfoText.text = "NO PATH\nDIG TUNNEL TO CORE";
            }

            ShowStartBlockedWarning(
                noPathWarningMainLine,
                noPathWarningDetailLine,
                noPathWarningStatusLine
            );

            ReturnToDungeonBuildPhase();
            return;
        }

        if (heroPrefab == null)
        {
            Debug.LogError("Hero Prefab is not assigned!");

            if (heroInfoText != null)
            {
                heroInfoText.text = "NO HERO PREFAB";
            }

            ShowStartBlockedWarning(
                noHeroPrefabWarningMainLine,
                noHeroPrefabWarningDetailLine,
                noHeroPrefabWarningStatusLine
            );

            ReturnToDungeonBuildPhase();
            return;
        }

        EnsurePreparedNextHeroPreview(false);

        int nextWaveNumber = hasPreparedNextHero ? preparedWaveNumber : waveNumber + 1;
        HeroKind nextHeroKind = hasPreparedNextHero
            ? preparedHeroKind
            : GetRandomHeroKind(waveNumber + 1);

        HideNextHeroPreview();
        SpawnPreparedHero(nextWaveNumber, nextHeroKind, pathPoints);

        hasPreparedNextHero = false;
    }

    private void EnsurePreparedNextHeroPreview(bool forceRefreshUI)
    {
        if (!prepareNextHeroDuringDungeonBuild)
        {
            return;
        }

        int nextWaveNumber = waveNumber + 1;

        if (!hasPreparedNextHero || preparedWaveNumber != nextWaveNumber)
        {
            preparedWaveNumber = nextWaveNumber;
            preparedHeroKind = GetRandomHeroKind(preparedWaveNumber);
            hasPreparedNextHero = true;

            DebugLog("Prepared next hero: Wave " + preparedWaveNumber + " / " + preparedHeroKind);
        }

        if (showNextHeroPreviewUI || forceRefreshUI)
        {
            ShowNextHeroPreview();
        }

        UpdatePreparingHeroInfoText(preparedWaveNumber, preparedHeroKind);
    }

    private void ShowNextHeroPreview()
    {
        AutoFindReferences();

        if (waveWarningUI == null)
        {
            DebugLog("WaveWarningUI not found. Skipping preview.");
            return;
        }

        int normalCount = preparedHeroKind == HeroKind.Normal ? 1 : 0;
        int fastCount = preparedHeroKind == HeroKind.Fast ? 1 : 0;
        int tankCount = preparedHeroKind == HeroKind.Tank ? 1 : 0;
        int thiefCount = preparedHeroKind == HeroKind.Thief ? 1 : 0;

        waveWarningUI.ShowPreview(
            preparedWaveNumber,
            normalCount,
            fastCount,
            tankCount,
            thiefCount,
            entranceDisplayName,
            previewStatusText
        );
    }

    private void HideNextHeroPreview()
    {
        AutoFindReferences();

        if (waveWarningUI != null)
        {
            waveWarningUI.Hide();
        }
    }

    private void ShowStartBlockedWarning(
        string mainLine,
        string detailLine,
        string statusLine
    )
    {
        if (!showStartBlockedWarning)
        {
            return;
        }

        AutoFindReferences();

        if (waveWarningUI == null)
        {
            DebugLog("WaveWarningUI not found. Cannot show blocked start warning.");
            return;
        }

        if (startBlockedWarningCoroutine != null)
        {
            StopCoroutine(startBlockedWarningCoroutine);
            startBlockedWarningCoroutine = null;
        }

        isShowingStartBlockedWarning = true;

        waveWarningUI.ShowStaticText(
            startBlockedWarningTitle,
            mainLine,
            detailLine,
            statusLine
        );

        startBlockedWarningCoroutine = StartCoroutine(HideStartBlockedWarningRoutine());
    }

    private IEnumerator HideStartBlockedWarningRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, startBlockedWarningSeconds));

        if (waveWarningUI != null)
        {
            waveWarningUI.Hide();
        }

        isShowingStartBlockedWarning = false;
        startBlockedWarningCoroutine = null;

        nextPreviewRefreshTime = 0f;
    }

    private void SpawnPreparedHero(
        int nextWaveNumber,
        HeroKind heroKind,
        List<Vector3> pathPoints
    )
    {
        waveNumber = nextWaveNumber;

        Vector3 spawnPosition = pathPoints[0];

        currentHero = Instantiate(heroPrefab, spawnPosition, Quaternion.identity);

        ApplyHeroKind(currentHero, heroKind, pathPoints);

        Debug.Log("Wave " + waveNumber + " spawned immediately: " + heroKind);
    }

    private HeroKind GetRandomHeroKind(int targetWaveNumber)
    {
        if (targetWaveNumber <= 1)
        {
            return HeroKind.Normal;
        }

        int randomValue = Random.Range(0, 100);

        if (targetWaveNumber < 4)
        {
            if (randomValue < 50)
            {
                return HeroKind.Normal;
            }

            if (randomValue < 75)
            {
                return HeroKind.Fast;
            }

            return HeroKind.Tank;
        }

        if (randomValue < 35)
        {
            return HeroKind.Normal;
        }

        if (randomValue < 60)
        {
            return HeroKind.Fast;
        }

        if (randomValue < 82)
        {
            return HeroKind.Tank;
        }

        return HeroKind.Thief;
    }

    private void ApplyHeroKind(GameObject heroObject, HeroKind heroKind, List<Vector3> pathPoints)
    {
        HeroHealth heroHealth = heroObject.GetComponent<HeroHealth>();
        HeroMover heroMover = heroObject.GetComponent<HeroMover>();
        HeroAttack heroAttack = heroObject.GetComponent<HeroAttack>();

        if (heroAttack == null)
        {
            heroAttack = heroObject.AddComponent<HeroAttack>();
        }

        int baseHp = 5 + (waveNumber - 1) * 3;
        float baseSpeed = 2f + (waveNumber - 1) * 0.2f;
        int baseReward = 3 + waveNumber;

        string heroName = "Normal Hero";
        int hp = baseHp;
        float speed = baseSpeed;
        int reward = baseReward;
        int coreDamage = 1;
        float trapDamageMultiplier = 1f;
        Color color = Color.white;
        HeroVisualBuilder.HeroVisualType visualType = normalHeroVisualType;

        float slimeAttackRange = 1.15f;
        int slimeAttackDamage = 1;
        float slimeAttackInterval = 0.8f;

        switch (heroKind)
        {
            case HeroKind.Normal:
                heroName = "Normal Hero";
                hp = baseHp;
                speed = baseSpeed;
                reward = baseReward;
                coreDamage = 1;
                trapDamageMultiplier = 1f;
                color = new Color(0.45f, 0.75f, 1f, 1f);
                visualType = normalHeroVisualType;

                slimeAttackRange = 1.15f;
                slimeAttackDamage = 1;
                slimeAttackInterval = 0.8f;
                break;

            case HeroKind.Fast:
                heroName = "Fast Hero";
                hp = Mathf.Max(3, baseHp - 2);
                speed = baseSpeed + 1.2f;
                reward = baseReward;
                coreDamage = 1;
                trapDamageMultiplier = 1f;
                color = new Color(1f, 0.95f, 0.25f, 1f);
                visualType = fastHeroVisualType;

                slimeAttackRange = 1.05f;
                slimeAttackDamage = 1;
                slimeAttackInterval = 0.65f;
                break;

            case HeroKind.Tank:
                heroName = "Tank Hero";
                hp = baseHp + 8 + waveNumber * 2;
                speed = baseSpeed * 0.65f;
                reward = baseReward + 2;
                coreDamage = 2;
                trapDamageMultiplier = 1f;
                color = new Color(1f, 0.35f, 0.25f, 1f);
                visualType = tankHeroVisualType;

                slimeAttackRange = 1.25f;
                slimeAttackDamage = 2;
                slimeAttackInterval = 1.05f;
                break;

            case HeroKind.Thief:
                heroName = "Thief Hero";
                hp = baseHp + 1;
                speed = baseSpeed + 0.45f;
                reward = baseReward + 1;
                coreDamage = 1;
                trapDamageMultiplier = 0.35f;
                color = new Color(0.75f, 0.35f, 1f, 1f);
                visualType = thiefHeroVisualType;

                slimeAttackRange = 1.15f;
                slimeAttackDamage = 1;
                slimeAttackInterval = 0.55f;
                break;
        }

        ApplyHeroVisual(heroObject, visualType);

        if (heroHealth != null)
        {
            heroHealth.ConfigureHero(heroName, hp, reward, trapDamageMultiplier, color);
        }

        if (heroMover != null)
        {
            heroMover.ConfigureMovement(speed, coreDamage);
            heroMover.SetPath(pathPoints);
        }

        if (heroAttack != null)
        {
            heroAttack.ConfigureAttack(slimeAttackRange, slimeAttackDamage, slimeAttackInterval);
        }

        UpdateHeroInfoText(
            heroName,
            hp,
            speed,
            reward,
            coreDamage,
            trapDamageMultiplier,
            slimeAttackDamage,
            slimeAttackInterval,
            visualType
        );
    }

    private void ApplyHeroVisual(GameObject heroObject, HeroVisualBuilder.HeroVisualType visualType)
    {
        if (heroObject == null)
        {
            return;
        }

        HeroVisualBuilder visualBuilder = heroObject.GetComponent<HeroVisualBuilder>();

        if (visualBuilder == null)
        {
            visualBuilder = heroObject.GetComponentInChildren<HeroVisualBuilder>();
        }

        if (visualBuilder == null)
        {
            Debug.LogWarning("HeroVisualBuilder not found on spawned hero.");
            return;
        }

        visualBuilder.heroVisualType = visualType;
        visualBuilder.RebuildVisual();
    }

    private void UpdatePreparingHeroInfoText(int nextWaveNumber, HeroKind heroKind)
    {
        if (heroInfoText == null)
        {
            return;
        }

        if (isShowingStartBlockedWarning)
        {
            return;
        }

        heroInfoText.text =
            "NEXT WAVE: " + nextWaveNumber + "\n" +
            "NEXT HERO: " + GetHeroKindDisplayName(heroKind) + "\n" +
            "PRESS START DEFENSE";
    }

    private void UpdateHeroInfoText(
        string heroName,
        int hp,
        float speed,
        int reward,
        int coreDamage,
        float trapDamageMultiplier,
        int slimeAttackDamage,
        float slimeAttackInterval,
        HeroVisualBuilder.HeroVisualType visualType
    )
    {
        if (heroInfoText == null)
        {
            return;
        }

        heroInfoText.text =
            "WAVE: " + waveNumber + "\n" +
            "HERO: " + heroName + "\n" +
            "LOOK: " + visualType + "\n" +
            "HP: " + hp + "\n" +
            "SPEED: " + speed.ToString("0.0") + "\n" +
            "REWARD: " + reward + "\n" +
            "CORE DMG: " + coreDamage + "\n" +
            "TRAP DMG RATE: " + trapDamageMultiplier.ToString("0.00") + "\n" +
            "SLIME ATK: " + slimeAttackDamage + "\n" +
            "ATK INTERVAL: " + slimeAttackInterval.ToString("0.00");
    }

    private string GetHeroKindDisplayName(HeroKind heroKind)
    {
        switch (heroKind)
        {
            case HeroKind.Normal:
                return "NORMAL HERO";

            case HeroKind.Fast:
                return "FAST HERO";

            case HeroKind.Tank:
                return "TANK HERO";

            case HeroKind.Thief:
                return "THIEF HERO";
        }

        return "UNKNOWN HERO";
    }

    private void ClearHeroInfoText()
    {
        if (heroInfoText != null)
        {
            heroInfoText.text = "NO HERO\nBUILD YOUR DUNGEON";
        }
    }

    private DungeonGridManager GetDungeonGridManager()
    {
        DungeonGridManager dungeonGridManager = DungeonGridManager.Instance;

        if (dungeonGridManager == null)
        {
            dungeonGridManager = FindFirstObjectByType<DungeonGridManager>();
        }

        return dungeonGridManager;
    }

    private void AutoFindReferences()
    {
        if (autoFindWaveWarningUI && waveWarningUI == null)
        {
            if (WaveWarningUI.Instance != null)
            {
                waveWarningUI = WaveWarningUI.Instance;
            }
            else
            {
                waveWarningUI = FindFirstObjectByType<WaveWarningUI>();
            }
        }
    }

    private void ReturnToDungeonBuildPhase()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.SetPhaseToDungeonBuild();
        }
    }

    private void DebugLog(string message)
    {
        if (!showDebugLog)
        {
            return;
        }

        Debug.Log("WaveManager: " + message);
    }
}