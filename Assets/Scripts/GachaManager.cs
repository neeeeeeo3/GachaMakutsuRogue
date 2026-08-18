using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class GachaManager : MonoBehaviour
{
    public GameObject slimePrefab;
    public GameObject foodPrefab;
    public GameObject trapPrefab;

    [Header("Capsule")]
    public GameObject capsulePrefab;
    public float capsuleOpenDelay = 0.6f;

    [Header("Real Capsule Visual")]
    public bool useRealCapsuleVisual = true;
    public bool useRealCapsuleVisualForEject = true;
    public bool useRealCapsuleVisualForPlacedCapsule = true;
    public float realCapsuleScale = 1f;
    public bool useGachaMachineSortingLayerForEject = true;
    public string gachaMachineSortingLayerName = "GachaMachine";
    public bool copyRootLayerToCapsuleVisualParts = true;

    [Header("Capsule Colors")]
    public Color slimeCapsuleColor = new Color(0.25f, 1f, 0.35f, 1f);
    public Color foodCapsuleColor = new Color(1f, 0.55f, 0.2f, 1f);
    public Color trapCapsuleColor = new Color(1f, 0.9f, 0.15f, 1f);

    [Header("Rarity Rates")]
    [Range(0f, 1f)]
    public float rareRate = 0.22f;

    [Range(0f, 1f)]
    public float epicRate = 0.06f;

    [Header("Rarity Capsule Color")]
    public bool useRarityColorForCapsule = true;

    [Range(0f, 1f)]
    public float rarityColorBlend = 0.45f;

    public Color commonRarityColor = new Color(1f, 1f, 1f, 1f);
    public Color rareRarityColor = new Color(0.25f, 0.75f, 1f, 1f);
    public Color epicRarityColor = new Color(0.95f, 0.45f, 1f, 1f);

    [Header("Rarity Display")]
    public bool showRarityInPendingText = true;
    public bool showCommonRarityInPendingText = true;
    public bool showRarityInGetEffect = true;
    public bool showCommonRarityInGetEffect = false;

    [Tooltip("現在のドット文字GET演出に対応しやすいよう、EPICは演出上ELITE表示にしています。")]
    public string commonGetEffectRarityName = "COMMON";
    public string rareGetEffectRarityName = "RARE";
    public string epicGetEffectRarityName = "ELITE";

    [Header("Dungeon Tile Placement")]
    public bool requireDugFloorToPlace = true;
    public bool requireEmptyTileToPlace = true;
    public DungeonGridManager dungeonGridManager;
    public bool autoFindDungeonGridManager = true;

    [Header("Gacha Machine Animation")]
    public GachaMachineAnimator gachaMachineAnimator;
    public bool autoFindGachaMachineAnimator = true;
    public bool waitForMachineAnimationBeforeEject = true;

    [Header("Gacha Eject Animation")]
    public Transform gachaMachineExitPoint;
    public Transform capsuleReadyPoint;
    public float ejectDuration = 0.75f;
    public float ejectArcHeight = 0.45f;
    public float ejectSpinSpeed = 980f;
    public float ejectEndPause = 0.12f;

    [Header("Eject Capsule Sorting")]
    public int ejectCapsuleSortingOrder = 1300;

    [Header("Eject Roll Feel")]
    public float ejectRollHopHeight = 0.08f;
    public float ejectPopScaleAmount = 0.16f;
    public float ejectSquashAmount = 0.08f;

    [Header("Dungeon Get Effect")]
    public bool playDungeonGetEffect = true;
    public bool waitForDungeonGetEffectBeforePlacement = true;
    public Transform dungeonGetEffectPoint;
    public Vector2 dungeonGetEffectViewportPosition = new Vector2(0.5f, 0.62f);
    public float dungeonGetEffectScale = 1.65f;
    public float dungeonGetEffectDuration = 0.95f;
    public string dungeonGetEffectSortingLayerName = "Default";
    public int dungeonGetEffectSortingOrder = 3000;
    public string dungeonGetEffectLayerName = "Default";

    [Header("UI")]
    public TMP_Text pendingText;

    [Header("Effect Description UI")]
    public GachaEffectDescriptionUI effectDescriptionUI;
    public bool autoFindEffectDescriptionUI = true;
    public bool showRolledEffectDescription = true;
    public bool showPlacementEffectDescription = true;

    [Header("Placement Message UI")]
    public PlacementMessageUI placementMessageUI;
    public bool autoFindPlacementMessageUI = true;
    public bool showPlacementErrorMessage = true;
    public float placementErrorMessageCooldown = 0.12f;

    [Header("Placement Preview Feedback")]
    public bool tintPreviewWhenInvalid = true;
    public Color invalidPreviewColor = new Color(1f, 0.25f, 0.25f, 1f);
    [Range(0f, 1f)]
    public float invalidPreviewAlphaMultiplier = 0.42f;

    [Header("Gacha History")]
    public bool addResultToGachaHistory = true;
    public GachaHistoryUI gachaHistoryUI;
    public bool autoFindGachaHistoryUI = true;

    [Header("Buttons To Hide While Placing")]
    public GameObject[] buttonsToHideWhilePlacing;

    [Header("Placement Visual")]
    public PlacementAreaVisualizer placementAreaVisualizer;
    public bool autoFindPlacementAreaVisualizer = true;

    public int rollCost = 1;

    [Range(0f, 1f)]
    public float slimeRate = 0.55f;

    [Range(0f, 1f)]
    public float foodRate = 0.30f;

    [Header("Placement Area")]
    public float minPlaceX = -30f;
    public float maxPlaceX = 30f;
    public float minPlaceY = -18f;
    public float maxPlaceY = 18f;

    [Header("Grid Placement")]
    public bool useGridSnap = true;
    public float gridSize = 1f;

    [Header("Preview")]
    public float previewAlpha = 0.5f;

    private GameObject pendingPrefab;
    private string pendingName = "NONE";
    private GachaRarityType pendingRarity = GachaRarityType.Common;
    private Color pendingCapsuleColor = Color.white;

    private GameObject previewObject;
    private SpriteRenderer previewSpriteRenderer;

    private GameObject ejectVisualObject;
    private SpriteRenderer ejectVisualRenderer;

    private bool[] storedButtonActiveStates;
    private bool hasStoredButtonStates;

    private bool isEjecting;
    private float nextPlacementErrorMessageTime;

    private void Start()
    {
        if (autoFindPlacementAreaVisualizer && placementAreaVisualizer == null)
        {
            placementAreaVisualizer = FindFirstObjectByType<PlacementAreaVisualizer>();
        }

        if (autoFindGachaMachineAnimator && gachaMachineAnimator == null)
        {
            gachaMachineAnimator = FindFirstObjectByType<GachaMachineAnimator>();
        }

        if (autoFindDungeonGridManager && dungeonGridManager == null)
        {
            dungeonGridManager = FindFirstObjectByType<DungeonGridManager>();
        }

        if (autoFindGachaHistoryUI && gachaHistoryUI == null)
        {
            if (GachaHistoryUI.Instance != null)
            {
                gachaHistoryUI = GachaHistoryUI.Instance;
            }
            else
            {
                gachaHistoryUI = FindFirstObjectByType<GachaHistoryUI>();
            }
        }

        if (autoFindEffectDescriptionUI && effectDescriptionUI == null)
        {
            effectDescriptionUI = FindFirstObjectByType<GachaEffectDescriptionUI>();
        }

        if (autoFindPlacementMessageUI && placementMessageUI == null)
        {
            placementMessageUI = FindFirstObjectByType<PlacementMessageUI>();
        }

        UpdatePendingText();
        DestroyPreview();
        DestroyEjectVisual();
        HidePlacementArea();
        HideEffectDescription();
        HidePlacementMessage();
    }

    private void Update()
    {
        if (RunManager.Instance != null && RunManager.Instance.isGameOver)
        {
            StopAllCoroutines();

            isEjecting = false;
            pendingPrefab = null;
            pendingName = "NONE";
            pendingRarity = GachaRarityType.Common;
            pendingCapsuleColor = Color.white;

            DestroyPreview();
            DestroyEjectVisual();
            HidePlacementArea();
            HideEffectDescription();
            HidePlacementMessage();
            RestoreButtonsAfterPlacing();
            UpdatePendingText();

            return;
        }

        if (isEjecting)
        {
            HidePlacementArea();
            return;
        }

        if (pendingPrefab == null)
        {
            DestroyPreview();
            HidePlacementArea();
            HidePlacementEffectDescription();
            return;
        }

        ShowPlacementArea();
        ShowPlacementEffectDescriptionForPending();
        UpdatePreviewPosition();

        if (Input.GetMouseButtonDown(0))
        {
            TryPlacePendingCapsule();
        }

        if (Input.GetMouseButtonDown(1))
        {
            CancelPendingCapsule();
        }
    }

    public bool HasPendingCapsule()
    {
        return pendingPrefab != null || isEjecting;
    }

    public void Roll()
    {
        Debug.Log("GACHA button pressed!");

        if (isEjecting)
        {
            Debug.Log("Gacha is already rolling!");
            return;
        }

        if (pendingPrefab != null)
        {
            Debug.Log("Place current capsule first!");
            return;
        }

        if (RunManager.Instance == null)
        {
            Debug.LogError("RunManager not found!");
            return;
        }

        if (!RunManager.Instance.IsDungeonBuildPhase())
        {
            Debug.Log("Gacha is only available during Dungeon Build Phase.");
            return;
        }

        int finalRollCost = RunManager.Instance.GetFinalGachaCost(rollCost);

        if (!RunManager.Instance.SpendMana(finalRollCost))
        {
            Debug.Log("Gacha roll failed. Not enough mana or roll is blocked.");
            return;
        }

        GameObject rolledPrefab = ChoosePrefab();

        string rolledName = pendingName;
        GachaRarityType rolledRarity = ChooseRarity();
        Color baseCapsuleColor = pendingCapsuleColor;
        Color rolledCapsuleColor = ApplyRarityColorToCapsule(baseCapsuleColor, rolledRarity);

        Debug.Log("Gacha Rarity: " + GetRarityName(rolledRarity));
        Debug.Log("Gacha Final Result: " + GetFullDisplayName(rolledName, rolledRarity));

        AddGachaHistoryEntry(rolledName, rolledRarity, rolledCapsuleColor);

        StartCoroutine(EjectCapsuleThenEnterPlacement(
            rolledPrefab,
            rolledName,
            rolledRarity,
            rolledCapsuleColor
        ));
    }

    private IEnumerator EjectCapsuleThenEnterPlacement(
        GameObject rolledPrefab,
        string rolledName,
        GachaRarityType rolledRarity,
        Color rolledColor
    )
    {
        isEjecting = true;

        pendingPrefab = null;
        pendingName = rolledName;
        pendingRarity = rolledRarity;
        pendingCapsuleColor = rolledColor;

        HidePlacementArea();
        HideEffectDescription();
        HidePlacementMessage();
        DestroyPreview();
        HideButtonsWhilePlacing();
        UpdatePendingText();

        if (gachaMachineAnimator == null && autoFindGachaMachineAnimator)
        {
            gachaMachineAnimator = FindFirstObjectByType<GachaMachineAnimator>();
        }

        if (gachaMachineAnimator != null)
        {
            if (waitForMachineAnimationBeforeEject)
            {
                yield return gachaMachineAnimator.PlayRollAnimationRoutine();
            }
            else
            {
                gachaMachineAnimator.PlayRollAnimation();
            }
        }

        CreateEjectVisual(rolledColor);

        Vector3 startPosition = GetEjectStartPosition();
        Vector3 endPosition = GetEjectEndPosition();

        float timer = 0f;
        Vector3 baseScale = Vector3.one;

        if (ejectVisualObject != null)
        {
            baseScale = ejectVisualObject.transform.localScale;
            ejectVisualObject.transform.position = startPosition;
        }

        while (timer < ejectDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, ejectDuration));
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 position = Vector3.Lerp(startPosition, endPosition, easedT);

            float mainArc = Mathf.Sin(t * Mathf.PI) * ejectArcHeight;
            float littleHop = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 5f)) * ejectRollHopHeight * (1f - t);

            position.y += mainArc + littleHop;

            if (ejectVisualObject != null)
            {
                ejectVisualObject.transform.position = position;

                float rotationZ = -ejectSpinSpeed * t * ejectDuration;
                ejectVisualObject.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);

                float popScale = 1f + Mathf.Sin(t * Mathf.PI) * ejectPopScaleAmount;
                float squashWave = Mathf.Sin(t * Mathf.PI * 6f) * ejectSquashAmount * (1f - t);

                ejectVisualObject.transform.localScale = new Vector3(
                    baseScale.x * (popScale + squashWave),
                    baseScale.y * (popScale - squashWave),
                    baseScale.z
                );
            }

            yield return null;
        }

        if (ejectVisualObject != null)
        {
            ejectVisualObject.transform.position = endPosition;
            ejectVisualObject.transform.rotation = Quaternion.identity;
            ejectVisualObject.transform.localScale = baseScale;
        }

        yield return new WaitForSeconds(ejectEndPause);

        DestroyEjectVisual();

        if (playDungeonGetEffect)
        {
            string getEffectName = GetGetEffectDisplayName(rolledName, rolledRarity);

            if (waitForDungeonGetEffectBeforePlacement)
            {
                yield return PlayDungeonGetEffectRoutine(getEffectName, rolledColor);
            }
            else
            {
                StartCoroutine(PlayDungeonGetEffectRoutine(getEffectName, rolledColor));
            }
        }

        pendingPrefab = rolledPrefab;
        pendingName = rolledName;
        pendingRarity = rolledRarity;
        pendingCapsuleColor = rolledColor;

        isEjecting = false;

        CreatePreview();
        ShowPlacementArea();
        UpdatePendingText();

        ShowRolledEffectDescriptionForPending();
        ShowPlacementEffectDescriptionForPending();

        Debug.Log("Capsule is ready to place: " + GetFullDisplayName(pendingName, pendingRarity));
    }

    private GameObject ChoosePrefab()
    {
        float randomValue = Random.value;

        if (randomValue < slimeRate)
        {
            pendingName = "SLIME";
            pendingCapsuleColor = slimeCapsuleColor;
            Debug.Log("Gacha Result: Slime");
            return slimePrefab;
        }

        if (randomValue < slimeRate + foodRate)
        {
            pendingName = "FOOD";
            pendingCapsuleColor = foodCapsuleColor;
            Debug.Log("Gacha Result: Food");
            return foodPrefab;
        }

        pendingName = "TRAP";
        pendingCapsuleColor = trapCapsuleColor;
        Debug.Log("Gacha Result: Trap");
        return trapPrefab;
    }

    private GachaRarityType ChooseRarity()
    {
        float safeEpicRate = Mathf.Clamp01(epicRate);
        float safeRareRate = Mathf.Clamp(rareRate, 0f, 1f - safeEpicRate);

        float randomValue = Random.value;

        if (randomValue < safeEpicRate)
        {
            return GachaRarityType.Epic;
        }

        if (randomValue < safeEpicRate + safeRareRate)
        {
            return GachaRarityType.Rare;
        }

        return GachaRarityType.Common;
    }

    private Color ApplyRarityColorToCapsule(Color baseColor, GachaRarityType rarity)
    {
        if (!useRarityColorForCapsule)
        {
            return baseColor;
        }

        if (rarity == GachaRarityType.Common)
        {
            return baseColor;
        }

        Color rarityColor = GetRarityColor(rarity);
        Color finalColor = Color.Lerp(baseColor, rarityColor, rarityColorBlend);
        finalColor.a = baseColor.a;

        return finalColor;
    }

    private Color GetRarityColor(GachaRarityType rarity)
    {
        switch (rarity)
        {
            case GachaRarityType.Common:
                return commonRarityColor;

            case GachaRarityType.Rare:
                return rareRarityColor;

            case GachaRarityType.Epic:
                return epicRarityColor;
        }

        return Color.white;
    }

    private string GetRarityName(GachaRarityType rarity)
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

    private string GetFullDisplayName(string itemName, GachaRarityType rarity)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            itemName = "ITEM";
        }

        itemName = itemName.ToUpperInvariant();

        if (!showRarityInPendingText)
        {
            return itemName;
        }

        if (rarity == GachaRarityType.Common && !showCommonRarityInPendingText)
        {
            return itemName;
        }

        return GetRarityName(rarity) + " " + itemName;
    }

    private string GetGetEffectDisplayName(string itemName, GachaRarityType rarity)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            itemName = "ITEM";
        }

        itemName = itemName.ToUpperInvariant();

        if (!showRarityInGetEffect)
        {
            return itemName;
        }

        if (rarity == GachaRarityType.Common && !showCommonRarityInGetEffect)
        {
            return itemName;
        }

        string rarityName = GetGetEffectRarityName(rarity);

        if (string.IsNullOrWhiteSpace(rarityName))
        {
            return itemName;
        }

        return rarityName.ToUpperInvariant() + " " + itemName;
    }

    private string GetGetEffectRarityName(GachaRarityType rarity)
    {
        switch (rarity)
        {
            case GachaRarityType.Common:
                return commonGetEffectRarityName;

            case GachaRarityType.Rare:
                return rareGetEffectRarityName;

            case GachaRarityType.Epic:
                return epicGetEffectRarityName;
        }

        return "";
    }

    private void AddGachaHistoryEntry(
        string itemName,
        GachaRarityType rarity,
        Color capsuleColor
    )
    {
        if (!addResultToGachaHistory)
        {
            return;
        }

        if (gachaHistoryUI == null && autoFindGachaHistoryUI)
        {
            if (GachaHistoryUI.Instance != null)
            {
                gachaHistoryUI = GachaHistoryUI.Instance;
            }
            else
            {
                gachaHistoryUI = FindFirstObjectByType<GachaHistoryUI>();
            }
        }

        if (gachaHistoryUI == null)
        {
            return;
        }

        gachaHistoryUI.AddEntry(
            GetFullDisplayName(itemName, rarity),
            rarity,
            capsuleColor
        );
    }

    private void TryPlacePendingCapsule()
    {
        if (pendingPrefab == null)
        {
            return;
        }

        if (RunManager.Instance != null && !RunManager.Instance.IsDungeonBuildPhase())
        {
            return;
        }

        if (IsPointerOverUI())
        {
            return;
        }

        Vector3 placePosition = GetMouseWorldPosition();
        placePosition = SnapPositionIfNeeded(placePosition);

        if (!CanPlaceAtPosition(placePosition, true, out string failureMessage))
        {
            ShowPlacementError(failureMessage);
            return;
        }

        PlaceCapsule(placePosition);

        Debug.Log("Placed capsule containing: " + GetFullDisplayName(pendingName, pendingRarity) + " at " + placePosition);

        pendingPrefab = null;
        pendingName = "NONE";
        pendingRarity = GachaRarityType.Common;
        pendingCapsuleColor = Color.white;

        DestroyPreview();
        HidePlacementArea();
        HideEffectDescription();
        HidePlacementMessage();
        RestoreButtonsAfterPlacing();
        UpdatePendingText();
    }

    private bool CanPlaceAtPosition(Vector3 position, bool showLog)
    {
        return CanPlaceAtPosition(position, showLog, out string failureMessage);
    }

    private bool CanPlaceAtPosition(Vector3 position, bool showLog, out string failureMessage)
    {
        failureMessage = "";

        if (!IsInsidePlaceArea(position))
        {
            return FailPlacement(
                "Outside placement area.",
                showLog,
                out failureMessage
            );
        }

        DungeonGridManager grid = GetDungeonGridManager();

        if (requireDugFloorToPlace)
        {
            if (grid == null)
            {
                return FailPlacement(
                    "Dungeon grid not found.",
                    showLog,
                    out failureMessage
                );
            }

            DungeonTile tile = grid.GetTileAtWorldPosition(position);

            if (tile == null)
            {
                return FailPlacement(
                    "No dungeon tile here.",
                    showLog,
                    out failureMessage
                );
            }

            if (!tile.IsFloor)
            {
                return FailPlacement(
                    "Dig this tile first.",
                    showLog,
                    out failureMessage
                );
            }

            if (grid.IsEntranceOrCoreAtWorldPosition(position))
            {
                return FailPlacement(
                    "Cannot place on entrance or core.",
                    showLog,
                    out failureMessage
                );
            }
        }

        if (requireEmptyTileToPlace)
        {
            if (grid == null)
            {
                return FailPlacement(
                    "Dungeon grid not found.",
                    showLog,
                    out failureMessage
                );
            }

            if (grid.IsTileOccupiedAtWorldPosition(position))
            {
                return FailPlacement(
                    "Tile is already occupied.",
                    showLog,
                    out failureMessage
                );
            }
        }

        return true;
    }

    private bool FailPlacement(
        string message,
        bool showLog,
        out string failureMessage
    )
    {
        failureMessage = message;

        if (showLog)
        {
            Debug.Log("Cannot place: " + message);
        }

        return false;
    }

    private void PlaceCapsule(Vector3 placePosition)
    {
        if (capsulePrefab == null)
        {
            Debug.LogWarning("Capsule Prefab is not assigned. Spawning result directly.");

            GameObject spawnedObject = Instantiate(pendingPrefab, placePosition, Quaternion.identity);
            ApplyRarityHolder(spawnedObject, pendingName, pendingRarity, pendingCapsuleColor);
            EnsurePlaceableObject(spawnedObject, GetFullDisplayName(pendingName, pendingRarity));

            return;
        }

        GameObject capsuleObject = Instantiate(capsulePrefab, placePosition, Quaternion.identity);

        SpriteRenderer capsuleRenderer = capsuleObject.GetComponent<SpriteRenderer>();

        if (capsuleRenderer != null)
        {
            capsuleRenderer.color = pendingCapsuleColor;
        }

        ApplyRealCapsuleVisual(capsuleObject, pendingCapsuleColor, false);
        ApplyRarityHolder(capsuleObject, pendingName, pendingRarity, pendingCapsuleColor);

        EnsurePlaceableObject(capsuleObject, GetFullDisplayName(pendingName, pendingRarity) + " Capsule");

        CapsuleOpener opener = capsuleObject.GetComponent<CapsuleOpener>();

        if (opener == null)
        {
            opener = capsuleObject.AddComponent<CapsuleOpener>();
        }

        opener.Initialize(pendingPrefab, capsuleOpenDelay);
    }

    private void ApplyRarityHolder(
        GameObject targetObject,
        string itemName,
        GachaRarityType rarity,
        Color capsuleColor
    )
    {
        if (targetObject == null)
        {
            return;
        }

        GachaRarityHolder rarityHolder = targetObject.GetComponent<GachaRarityHolder>();

        if (rarityHolder == null)
        {
            rarityHolder = targetObject.AddComponent<GachaRarityHolder>();
        }

        rarityHolder.Initialize(
            itemName,
            rarity,
            GetFullDisplayName(itemName, rarity),
            capsuleColor
        );
    }

    private void EnsurePlaceableObject(GameObject targetObject, string objectLabel)
    {
        if (targetObject == null)
        {
            return;
        }

        PlaceableObject placeableObject = targetObject.GetComponent<PlaceableObject>();

        if (placeableObject == null)
        {
            placeableObject = targetObject.AddComponent<PlaceableObject>();
        }

        placeableObject.objectName = objectLabel;
        placeableObject.countsAsOccupied = true;
    }

    private void CancelPendingCapsule()
    {
        if (pendingPrefab == null)
        {
            return;
        }

        Debug.Log("Pending capsule canceled: " + GetFullDisplayName(pendingName, pendingRarity));

        pendingPrefab = null;
        pendingName = "NONE";
        pendingRarity = GachaRarityType.Common;
        pendingCapsuleColor = Color.white;

        DestroyPreview();
        HidePlacementArea();
        HideEffectDescription();
        HidePlacementMessage();
        RestoreButtonsAfterPlacing();
        UpdatePendingText();
    }

    private void CreateEjectVisual(Color capsuleColor)
    {
        DestroyEjectVisual();

        SpriteRenderer sourceRenderer = null;

        if (capsulePrefab != null)
        {
            sourceRenderer = capsulePrefab.GetComponent<SpriteRenderer>();
        }

        if (sourceRenderer == null)
        {
            Debug.LogWarning("Capsule prefab has no SpriteRenderer. Eject visual skipped.");
            return;
        }

        ejectVisualObject = new GameObject("GachaEjectCapsule");

        int gachaMachineLayer = LayerMask.NameToLayer("GachaMachine");

        if (gachaMachineLayer >= 0)
        {
            ejectVisualObject.layer = gachaMachineLayer;
        }

        ejectVisualRenderer = ejectVisualObject.AddComponent<SpriteRenderer>();

        ejectVisualRenderer.sprite = sourceRenderer.sprite;
        ejectVisualRenderer.color = capsuleColor;
        ejectVisualRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        ejectVisualRenderer.sortingOrder = ejectCapsuleSortingOrder;

        ejectVisualObject.transform.position = GetEjectStartPosition();
        ejectVisualObject.transform.localScale = capsulePrefab.transform.localScale;

        ApplyRealCapsuleVisual(ejectVisualObject, capsuleColor, true);
    }

    private void ApplyRealCapsuleVisual(GameObject capsuleObject, Color bottomColor, bool isEjectVisual)
    {
        if (!useRealCapsuleVisual)
        {
            return;
        }

        if (isEjectVisual && !useRealCapsuleVisualForEject)
        {
            return;
        }

        if (!isEjectVisual && !useRealCapsuleVisualForPlacedCapsule)
        {
            return;
        }

        if (capsuleObject == null)
        {
            return;
        }

        CapsuleVisualBuilder visualBuilder = capsuleObject.GetComponent<CapsuleVisualBuilder>();

        if (visualBuilder == null)
        {
            visualBuilder = capsuleObject.AddComponent<CapsuleVisualBuilder>();
        }

        visualBuilder.hideOriginalSpriteRenderer = true;
        visualBuilder.rebuildOnStart = false;
        visualBuilder.randomizeBottomColorOnStart = false;

        if (isEjectVisual)
        {
            visualBuilder.useOriginalRendererSorting = false;
            visualBuilder.baseSortingOrder = ejectCapsuleSortingOrder;
        }
        else
        {
            visualBuilder.useOriginalRendererSorting = true;
        }

        visualBuilder.capsuleScale = realCapsuleScale;

        if (isEjectVisual && useGachaMachineSortingLayerForEject && SortingLayerExists(gachaMachineSortingLayerName))
        {
            visualBuilder.sortingLayerName = gachaMachineSortingLayerName;
        }
        else
        {
            visualBuilder.sortingLayerName = "";
        }

        visualBuilder.SetBottomColor(bottomColor);

        if (copyRootLayerToCapsuleVisualParts)
        {
            SetLayerRecursively(capsuleObject, capsuleObject.layer);
        }
    }

    private IEnumerator PlayDungeonGetEffectRoutine(string resultName, Color resultColor)
    {
        Vector3 effectPosition = GetDungeonGetEffectPosition();
        int effectLayer = GetLayerByNameOrDefault(dungeonGetEffectLayerName);

        yield return GachaGetEffect.PlayRoutine(
            resultName,
            resultColor,
            effectPosition,
            dungeonGetEffectScale,
            dungeonGetEffectDuration,
            dungeonGetEffectSortingLayerName,
            dungeonGetEffectSortingOrder,
            effectLayer
        );
    }

    private Vector3 GetDungeonGetEffectPosition()
    {
        if (dungeonGetEffectPoint != null)
        {
            return dungeonGetEffectPoint.position;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            float cameraDistance = Mathf.Abs(mainCamera.transform.position.z);

            Vector3 viewportPosition = new Vector3(
                dungeonGetEffectViewportPosition.x,
                dungeonGetEffectViewportPosition.y,
                cameraDistance
            );

            Vector3 worldPosition = mainCamera.ViewportToWorldPoint(viewportPosition);
            worldPosition.z = 0f;

            return worldPosition;
        }

        return new Vector3(0f, 1.5f, 0f);
    }

    private int GetLayerByNameOrDefault(string layerName)
    {
        if (!string.IsNullOrEmpty(layerName))
        {
            int layer = LayerMask.NameToLayer(layerName);

            if (layer >= 0)
            {
                return layer;
            }
        }

        return 0;
    }

    private bool SortingLayerExists(string targetSortingLayerName)
    {
        if (string.IsNullOrEmpty(targetSortingLayerName))
        {
            return false;
        }

        SortingLayer[] sortingLayers = SortingLayer.layers;

        foreach (SortingLayer sortingLayer in sortingLayers)
        {
            if (sortingLayer.name == targetSortingLayerName)
            {
                return true;
            }
        }

        return false;
    }

    private void SetLayerRecursively(GameObject targetObject, int layer)
    {
        if (targetObject == null)
        {
            return;
        }

        targetObject.layer = layer;

        foreach (Transform child in targetObject.transform)
        {
            if (child == null)
            {
                continue;
            }

            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void DestroyEjectVisual()
    {
        if (ejectVisualObject != null)
        {
            Destroy(ejectVisualObject);
            ejectVisualObject = null;
            ejectVisualRenderer = null;
        }
    }

    private Vector3 GetEjectStartPosition()
    {
        if (gachaMachineExitPoint != null)
        {
            return gachaMachineExitPoint.position;
        }

        return new Vector3(-6.5f, -1.4f, 0f);
    }

    private Vector3 GetEjectEndPosition()
    {
        if (capsuleReadyPoint != null)
        {
            return capsuleReadyPoint.position;
        }

        return new Vector3(-4.7f, -1.0f, 0f);
    }

    private void HideButtonsWhilePlacing()
    {
        if (hasStoredButtonStates)
        {
            return;
        }

        if (buttonsToHideWhilePlacing == null)
        {
            return;
        }

        storedButtonActiveStates = new bool[buttonsToHideWhilePlacing.Length];

        for (int i = 0; i < buttonsToHideWhilePlacing.Length; i++)
        {
            GameObject buttonObject = buttonsToHideWhilePlacing[i];

            if (buttonObject == null)
            {
                continue;
            }

            storedButtonActiveStates[i] = buttonObject.activeSelf;
            buttonObject.SetActive(false);
        }

        hasStoredButtonStates = true;
    }

    private void RestoreButtonsAfterPlacing()
    {
        if (!hasStoredButtonStates)
        {
            return;
        }

        if (buttonsToHideWhilePlacing == null || storedButtonActiveStates == null)
        {
            hasStoredButtonStates = false;
            return;
        }

        int count = Mathf.Min(buttonsToHideWhilePlacing.Length, storedButtonActiveStates.Length);

        for (int i = 0; i < count; i++)
        {
            GameObject buttonObject = buttonsToHideWhilePlacing[i];

            if (buttonObject == null)
            {
                continue;
            }

            buttonObject.SetActive(storedButtonActiveStates[i]);
        }

        hasStoredButtonStates = false;
    }

    private void CreatePreview()
    {
        DestroyPreview();

        if (pendingPrefab == null)
        {
            return;
        }

        SpriteRenderer sourceRenderer = null;

        if (capsulePrefab != null)
        {
            sourceRenderer = capsulePrefab.GetComponent<SpriteRenderer>();
        }

        if (sourceRenderer == null)
        {
            sourceRenderer = pendingPrefab.GetComponent<SpriteRenderer>();
        }

        if (sourceRenderer == null)
        {
            Debug.LogWarning("Preview source has no SpriteRenderer.");
            return;
        }

        previewObject = new GameObject("PlacementPreview");
        previewSpriteRenderer = previewObject.AddComponent<SpriteRenderer>();

        previewSpriteRenderer.sprite = sourceRenderer.sprite;
        previewSpriteRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        previewSpriteRenderer.sortingOrder = sourceRenderer.sortingOrder + 10;

        Color previewColor = pendingCapsuleColor;
        previewColor.a = previewAlpha;
        previewSpriteRenderer.color = previewColor;

        if (capsulePrefab != null)
        {
            previewObject.transform.localScale = capsulePrefab.transform.localScale;
        }
        else
        {
            previewObject.transform.localScale = pendingPrefab.transform.localScale;
        }

        UpdatePreviewPosition();
    }

    private void UpdatePreviewPosition()
    {
        if (previewObject == null)
        {
            return;
        }

        Vector3 previewPosition = GetMouseWorldPosition();
        previewPosition = SnapPositionIfNeeded(previewPosition);

        previewObject.transform.position = previewPosition;

        if (previewSpriteRenderer != null)
        {
            bool canPlace = CanPlaceAtPosition(previewPosition, false, out string failureMessage)
                && !IsPointerOverUI();

            Color color;

            if (canPlace)
            {
                color = pendingCapsuleColor;
                color.a = previewAlpha;
            }
            else
            {
                if (tintPreviewWhenInvalid)
                {
                    color = invalidPreviewColor;
                    color.a = previewAlpha * invalidPreviewAlphaMultiplier;
                }
                else
                {
                    color = pendingCapsuleColor;
                    color.a = previewAlpha * 0.25f;
                }
            }

            previewSpriteRenderer.color = color;
        }
    }

    private void DestroyPreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
            previewSpriteRenderer = null;
        }
    }

    private void ShowPlacementArea()
    {
        if (placementAreaVisualizer == null && autoFindPlacementAreaVisualizer)
        {
            placementAreaVisualizer = FindFirstObjectByType<PlacementAreaVisualizer>();
        }

        if (placementAreaVisualizer != null)
        {
            placementAreaVisualizer.ShowVisual();
        }
    }

    private void HidePlacementArea()
    {
        if (placementAreaVisualizer == null && autoFindPlacementAreaVisualizer)
        {
            placementAreaVisualizer = FindFirstObjectByType<PlacementAreaVisualizer>();
        }

        if (placementAreaVisualizer != null)
        {
            placementAreaVisualizer.HideVisual();
        }
    }

    private DungeonGridManager GetDungeonGridManager()
    {
        if (dungeonGridManager == null && autoFindDungeonGridManager)
        {
            dungeonGridManager = FindFirstObjectByType<DungeonGridManager>();
        }

        return dungeonGridManager;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;
        return mouseWorldPosition;
    }

    private Vector3 SnapPositionIfNeeded(Vector3 position)
    {
        DungeonGridManager grid = GetDungeonGridManager();

        if (requireDugFloorToPlace && grid != null)
        {
            return grid.SnapWorldPositionToTileCenter(position);
        }

        if (!useGridSnap)
        {
            return position;
        }

        position.x = Mathf.Round(position.x / gridSize) * gridSize;
        position.y = Mathf.Round(position.y / gridSize) * gridSize;
        position.z = 0f;

        return position;
    }

    private bool IsInsidePlaceArea(Vector3 position)
    {
        DungeonGridManager grid = GetDungeonGridManager();

        if (requireDugFloorToPlace && grid != null)
        {
            return grid.TryGetGridPositionFromWorldPosition(position, out int x, out int y);
        }

        return position.x >= minPlaceX
            && position.x <= maxPlaceX
            && position.y >= minPlaceY
            && position.y <= maxPlaceY;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject();
    }

    private void ShowRolledEffectDescriptionForPending()
    {
        if (!showRolledEffectDescription)
        {
            return;
        }

        GachaEffectDescriptionUI ui = GetEffectDescriptionUI();

        if (ui == null)
        {
            return;
        }

        ui.ShowRolledResult(
            pendingName,
            GetRarityName(pendingRarity),
            pendingPrefab
        );
    }

    private void ShowPlacementEffectDescriptionForPending()
    {
        if (!showPlacementEffectDescription)
        {
            return;
        }

        if (pendingPrefab == null)
        {
            return;
        }

        GachaEffectDescriptionUI ui = GetEffectDescriptionUI();

        if (ui == null)
        {
            return;
        }

        ui.ShowPlacementInfo(
            pendingName,
            GetRarityName(pendingRarity),
            pendingPrefab
        );
    }

    private void HideEffectDescription()
    {
        GachaEffectDescriptionUI ui = GetEffectDescriptionUI();

        if (ui == null)
        {
            return;
        }

        ui.HideAll();
    }

    private void HidePlacementEffectDescription()
    {
        GachaEffectDescriptionUI ui = GetEffectDescriptionUI();

        if (ui == null)
        {
            return;
        }

        ui.HidePlacement();
    }

    private GachaEffectDescriptionUI GetEffectDescriptionUI()
    {
        if (effectDescriptionUI == null && autoFindEffectDescriptionUI)
        {
            effectDescriptionUI = FindFirstObjectByType<GachaEffectDescriptionUI>();
        }

        return effectDescriptionUI;
    }

    private void ShowPlacementError(string message)
    {
        if (!showPlacementErrorMessage)
        {
            return;
        }

        if (Time.time < nextPlacementErrorMessageTime)
        {
            return;
        }

        PlacementMessageUI ui = GetPlacementMessageUI();

        if (ui == null)
        {
            return;
        }

        ui.ShowPlacementError("CAN'T PLACE", message);
        nextPlacementErrorMessageTime = Time.time + placementErrorMessageCooldown;
    }

    private void HidePlacementMessage()
    {
        PlacementMessageUI ui = GetPlacementMessageUI();

        if (ui == null)
        {
            return;
        }

        ui.Hide();
    }

    private PlacementMessageUI GetPlacementMessageUI()
    {
        if (placementMessageUI == null && autoFindPlacementMessageUI)
        {
            placementMessageUI = FindFirstObjectByType<PlacementMessageUI>();
        }

        return placementMessageUI;
    }

    private void UpdatePendingText()
    {
        if (pendingText == null)
        {
            return;
        }

        if (isEjecting)
        {
            pendingText.text = "ROLLING: " + GetFullDisplayName(pendingName, pendingRarity);
            return;
        }

        if (pendingPrefab == null)
        {
            pendingText.text = "PENDING: NONE";
        }
        else
        {
            pendingText.text = "PENDING: "
                + GetFullDisplayName(pendingName, pendingRarity)
                + "\nLEFT CLICK: PLACE ON EMPTY DUG FLOOR / RIGHT CLICK: CANCEL";
        }
    }
}