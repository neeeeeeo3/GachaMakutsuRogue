using UnityEngine;
using UnityEngine.EventSystems;

public class CorePlacementManager : MonoBehaviour
{
    public static CorePlacementManager Instance { get; private set; }

    [Header("References")]
    public DungeonGridManager dungeonGridManager;
    public bool autoFindDungeonGridManager = true;

    public Camera targetCamera;
    public bool autoFindCamera = true;

    public GachaManager gachaManager;
    public bool autoFindGachaManager = true;

    public PlacementAreaVisualizer placementAreaVisualizer;
    public bool autoFindPlacementAreaVisualizer = true;

    public PlacementMessageUI placementMessageUI;
    public bool autoFindPlacementMessageUI = true;

    [Header("Core Placement")]
    public bool isPlacingCore = false;
    public bool allowMoveExistingCore = true;
    public bool onlyDuringDungeonBuildPhase = true;
    public bool blockWhenGachaHasPendingCapsule = true;
    public bool cancelWithRightClick = true;
    public bool endPlacementAfterSuccess = true;

    [Header("Preview")]
    public bool showPreview = true;

    [Tooltip("任意。ここにCore用Prefabを入れると、そのSpriteをプレビューに使います。空なら自動で四角プレビューを作ります。")]
    public GameObject corePreviewSourcePrefab;

    public bool useCorePreviewSourceScale = true;
    public float fallbackPreviewSize = 0.72f;
    public float previewScaleMultiplier = 1f;

    public Color validPreviewColor = new Color(1f, 0.2f, 0.35f, 0.55f);
    public Color invalidPreviewColor = new Color(1f, 0.18f, 0.18f, 0.24f);

    public string previewSortingLayerName = "Default";
    public int previewSortingOrder = 1800;

    [Header("Messages")]
    public bool showPlacementMessages = true;
    public float messageCooldown = 0.12f;

    [Header("Debug")]
    public bool showDebugLog = true;

    public bool IsCorePlacementModeActive
    {
        get
        {
            return isPlacingCore;
        }
    }

    private GameObject previewObject;
    private SpriteRenderer previewRenderer;
    private Sprite fallbackSquareSprite;

    private float nextMessageTime;

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
        AutoFindReferences();
        DestroyPreview();
    }

    private void Update()
    {
        AutoFindReferences();

        if (RunManager.Instance != null && RunManager.Instance.isGameOver)
        {
            CancelCorePlacement();
            return;
        }

        if (!isPlacingCore)
        {
            DestroyPreview();
            return;
        }

        if (!CanStayInCorePlacementMode(out string stopReason))
        {
            ShowMessage("CORE BLOCKED", stopReason);
            CancelCorePlacement();
            return;
        }

        ShowPlacementArea();
        UpdatePreview();

        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceCoreAtMouse();
        }

        if (cancelWithRightClick && Input.GetMouseButtonDown(1))
        {
            CancelCorePlacement();
        }
    }

    public bool IsPlacingCore()
    {
        return isPlacingCore;
    }

    public void BeginCorePlacement()
    {
        AutoFindReferences();

        if (!CanStartCorePlacement(out string reason))
        {
            ShowMessage("CORE BLOCKED", reason);
            DebugLog("Core placement blocked: " + reason);
            return;
        }

        isPlacingCore = true;
        CreatePreviewIfNeeded();
        ShowPlacementArea();

        DebugLog("Core placement started.");
    }

    public void CancelCorePlacement()
    {
        isPlacingCore = false;
        DestroyPreview();
        HidePlacementArea();

        DebugLog("Core placement canceled.");
    }

    public void ToggleCorePlacementMode()
    {
        if (isPlacingCore)
        {
            CancelCorePlacement();
        }
        else
        {
            BeginCorePlacement();
        }
    }

    // Button compatibility methods.
    // 古いButton OnClickがどの名前を呼んでいても拾えるようにしてあります。
    public void SetCore()
    {
        BeginCorePlacement();
    }

    public void SetCoreMode()
    {
        BeginCorePlacement();
    }

    public void StartSetCore()
    {
        BeginCorePlacement();
    }

    public void StartSetCoreMode()
    {
        BeginCorePlacement();
    }

    public void StartCorePlacement()
    {
        BeginCorePlacement();
    }

    public void EnableCorePlacement()
    {
        BeginCorePlacement();
    }

    public void StartPlacingCore()
    {
        BeginCorePlacement();
    }

    public void OnClickPlaceCore()
    {
        BeginCorePlacement();
    }

    public void OnSetCoreButton()
    {
        BeginCorePlacement();
    }

    public void OnSetCoreButtonClicked()
    {
        BeginCorePlacement();
    }

    public void PlaceCoreMode()
    {
        BeginCorePlacement();
    }

    public void EnterCorePlacementMode()
    {
        BeginCorePlacement();
    }

    public void ExitCorePlacementMode()
    {
        CancelCorePlacement();
    }

    public void StopCorePlacement()
    {
        CancelCorePlacement();
    }

    public void CancelSetCore()
    {
        CancelCorePlacement();
    }

    private void TryPlaceCoreAtMouse()
    {
        if (IsPointerOverUI())
        {
            return;
        }

        if (dungeonGridManager == null)
        {
            ShowMessage("CAN'T PLACE", "Dungeon grid not found.");
            return;
        }

        Vector3 placePosition = GetMouseWorldPosition();
        placePosition = dungeonGridManager.SnapWorldPositionToTileCenter(placePosition);

        if (!CanPlaceCoreAtPosition(placePosition, out string failureMessage))
        {
            ShowMessage("CAN'T PLACE", failureMessage);
            return;
        }

        bool success = dungeonGridManager.TrySetCoreAtWorldPosition(placePosition);

        if (!success)
        {
            ShowMessage("CAN'T PLACE", "Core placement failed.");
            return;
        }

        ShowMessage("CORE SET", "Core placed.");

        DebugLog("Core placed at " + placePosition);

        if (endPlacementAfterSuccess)
        {
            isPlacingCore = false;
            DestroyPreview();
            HidePlacementArea();
        }
    }

    private bool CanStartCorePlacement(out string reason)
    {
        reason = "";

        if (dungeonGridManager == null)
        {
            reason = "Dungeon grid not found.";
            return false;
        }

        if (onlyDuringDungeonBuildPhase)
        {
            if (RunManager.Instance != null && !RunManager.Instance.IsDungeonBuildPhase())
            {
                reason = "Core can be placed only during build phase.";
                return false;
            }
        }

        if (!allowMoveExistingCore && dungeonGridManager.hasCorePlaced)
        {
            reason = "Core is already placed.";
            return false;
        }

        if (blockWhenGachaHasPendingCapsule && gachaManager != null && gachaManager.HasPendingCapsule())
        {
            reason = "Place or cancel current capsule first.";
            return false;
        }

        return true;
    }

    private bool CanStayInCorePlacementMode(out string reason)
    {
        return CanStartCorePlacement(out reason);
    }

    private bool CanPlaceCoreAtPosition(Vector3 worldPosition, out string failureMessage)
    {
        failureMessage = "";

        if (dungeonGridManager == null)
        {
            failureMessage = "Dungeon grid not found.";
            return false;
        }

        if (!dungeonGridManager.TryGetGridPositionFromWorldPosition(worldPosition, out int x, out int y))
        {
            failureMessage = "No dungeon tile here.";
            return false;
        }

        Vector2Int gridPosition = new Vector2Int(x, y);

        if (dungeonGridManager.IsEntranceGridPosition(gridPosition))
        {
            failureMessage = "Cannot place core on entrance.";
            return false;
        }

        DungeonTile tile = dungeonGridManager.GetTileAtGridPosition(gridPosition);

        if (tile == null)
        {
            failureMessage = "No dungeon tile here.";
            return false;
        }

        if (!tile.IsFloor)
        {
            failureMessage = "Dig this tile first.";
            return false;
        }

        if (dungeonGridManager.IsTileOccupiedAtWorldPosition(worldPosition))
        {
            failureMessage = "Tile is already occupied.";
            return false;
        }

        return true;
    }

    private void UpdatePreview()
    {
        if (!showPreview)
        {
            DestroyPreview();
            return;
        }

        CreatePreviewIfNeeded();

        if (previewObject == null)
        {
            return;
        }

        if (dungeonGridManager == null)
        {
            previewObject.SetActive(false);
            return;
        }

        Vector3 previewPosition = GetMouseWorldPosition();
        previewPosition = dungeonGridManager.SnapWorldPositionToTileCenter(previewPosition);

        previewObject.transform.position = previewPosition;
        previewObject.SetActive(true);

        bool canPlace = CanPlaceCoreAtPosition(previewPosition, out string failureMessage)
            && !IsPointerOverUI();

        if (previewRenderer != null)
        {
            previewRenderer.color = canPlace ? validPreviewColor : invalidPreviewColor;
        }
    }

    private void CreatePreviewIfNeeded()
    {
        if (!showPreview)
        {
            return;
        }

        if (previewObject != null)
        {
            return;
        }

        Sprite sourceSprite = null;
        Vector3 sourceScale = Vector3.one;

        if (corePreviewSourcePrefab != null)
        {
            SpriteRenderer sourceRenderer = corePreviewSourcePrefab.GetComponent<SpriteRenderer>();

            if (sourceRenderer != null)
            {
                sourceSprite = sourceRenderer.sprite;
            }

            sourceScale = corePreviewSourcePrefab.transform.localScale;
        }

        if (sourceSprite == null)
        {
            sourceSprite = GetFallbackSquareSprite();
            float size = GetFallbackPreviewWorldSize();
            sourceScale = new Vector3(size, size, 1f);
        }

        previewObject = new GameObject("CorePlacementPreview");
        previewRenderer = previewObject.AddComponent<SpriteRenderer>();

        previewRenderer.sprite = sourceSprite;
        previewRenderer.color = validPreviewColor;
        previewRenderer.sortingOrder = previewSortingOrder;

        if (SortingLayerExists(previewSortingLayerName))
        {
            previewRenderer.sortingLayerName = previewSortingLayerName;
        }

        if (useCorePreviewSourceScale && corePreviewSourcePrefab != null)
        {
            previewObject.transform.localScale = sourceScale * previewScaleMultiplier;
        }
        else
        {
            float size = GetFallbackPreviewWorldSize();
            previewObject.transform.localScale = new Vector3(size, size, 1f) * previewScaleMultiplier;
        }
    }

    private float GetFallbackPreviewWorldSize()
    {
        if (dungeonGridManager != null)
        {
            return dungeonGridManager.tileSize * fallbackPreviewSize;
        }

        return fallbackPreviewSize;
    }

    private Sprite GetFallbackSquareSprite()
    {
        if (fallbackSquareSprite != null)
        {
            return fallbackSquareSprite;
        }

        Texture2D texture = new Texture2D(1, 1);
        texture.name = "CorePlacementPreviewSquareTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        fallbackSquareSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );

        return fallbackSquareSprite;
    }

    private void DestroyPreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
            previewRenderer = null;
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

    private Vector3 GetMouseWorldPosition()
    {
        Camera cameraToUse = targetCamera;

        if (cameraToUse == null)
        {
            cameraToUse = Camera.main;
        }

        if (cameraToUse == null)
        {
            return Vector3.zero;
        }

        Vector3 mouseWorldPosition = cameraToUse.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;

        return mouseWorldPosition;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject();
    }

    private void ShowMessage(string title, string body)
    {
        if (!showPlacementMessages)
        {
            DebugLog(title + ": " + body);
            return;
        }

        if (Time.time < nextMessageTime)
        {
            return;
        }

        if (placementMessageUI == null && autoFindPlacementMessageUI)
        {
            placementMessageUI = FindFirstObjectByType<PlacementMessageUI>();
        }

        if (placementMessageUI != null)
        {
            placementMessageUI.ShowPlacementError(title, body);
        }
        else
        {
            Debug.Log(title + ": " + body);
        }

        nextMessageTime = Time.time + messageCooldown;
    }

    private void AutoFindReferences()
    {
        if (autoFindDungeonGridManager && dungeonGridManager == null)
        {
            if (DungeonGridManager.Instance != null)
            {
                dungeonGridManager = DungeonGridManager.Instance;
            }
            else
            {
                dungeonGridManager = FindFirstObjectByType<DungeonGridManager>();
            }
        }

        if (autoFindCamera && targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (autoFindGachaManager && gachaManager == null)
        {
            gachaManager = FindFirstObjectByType<GachaManager>();
        }

        if (autoFindPlacementAreaVisualizer && placementAreaVisualizer == null)
        {
            placementAreaVisualizer = FindFirstObjectByType<PlacementAreaVisualizer>();
        }

        if (autoFindPlacementMessageUI && placementMessageUI == null)
        {
            placementMessageUI = FindFirstObjectByType<PlacementMessageUI>();
        }
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

    private void DebugLog(string message)
    {
        if (!showDebugLog)
        {
            return;
        }

        Debug.Log("CorePlacementManager: " + message);
    }
}