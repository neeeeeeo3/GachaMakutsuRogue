using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MinimapUI : MonoBehaviour
{
    [Header("Auto Create UI")]
    public bool autoCreateUI = true;
    public Canvas targetCanvas;

    [Header("References")]
    public DungeonGridManager dungeonGridManager;
    public bool autoFindDungeonGridManager = true;
    public Camera targetCamera;
    public bool autoFindCamera = true;

    [Header("Generated UI")]
    public RectTransform panelRoot;
    public Image panelBackground;
    public TMP_Text titleText;
    public RawImage mapImage;

    [Header("Layout")]
    public Vector2 panelSize = new Vector2(240f, 240f);
    public Vector2 panelAnchoredPosition = new Vector2(18f, 18f);
    public Vector2 mapPadding = new Vector2(14f, 42f);

    [Header("Grid Range")]
    public int minGridX = -30;
    public int maxGridX = 30;
    public int minGridY = -18;
    public int maxGridY = 18;

    [Header("Refresh")]
    public float refreshInterval = 0.2f;
    public bool refreshEveryFrame = false;

    [Header("Display")]
    public bool showTitle = true;
    public string titleLabel = "MINIMAP";
    public bool showSoil = true;
    public bool showFloor = true;
    public bool showEntranceAndCore = true;
    public bool showPlaceableObjects = true;
    public bool showHeroes = true;
    public bool showCameraView = true;

    [Header("Colors")]
    public Color emptyColor = new Color(0.02f, 0.025f, 0.035f, 1f);
    public Color soilColor = new Color(0.15f, 0.10f, 0.06f, 1f);
    public Color floorColor = new Color(0.42f, 0.34f, 0.24f, 1f);
    public Color entranceCoreColor = new Color(0.25f, 0.9f, 1f, 1f);
    public Color placeableColor = new Color(0.3f, 1f, 0.35f, 1f);
    public Color heroColor = new Color(1f, 0.25f, 0.25f, 1f);
    public Color cameraViewColor = new Color(1f, 1f, 1f, 1f);

    [Header("Debug")]
    public bool showDebugLog = false;

    private Texture2D minimapTexture;
    private float nextRefreshTime;

    private int MapWidth
    {
        get { return Mathf.Max(1, maxGridX - minGridX + 1); }
    }

    private int MapHeight
    {
        get { return Mathf.Max(1, maxGridY - minGridY + 1); }
    }

    private void Awake()
    {
        if (autoCreateUI)
        {
            EnsureUI();
        }
    }

    private void Start()
    {
        AutoFindReferences();
        RebuildTextureIfNeeded();
        RefreshMap();
    }

    private void Update()
    {
        if (refreshEveryFrame)
        {
            RefreshMap();
            return;
        }

        if (Time.time < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.time + Mathf.Max(0.02f, refreshInterval);
        RefreshMap();
    }

    private void OnDestroy()
    {
        if (minimapTexture != null)
        {
            Destroy(minimapTexture);
            minimapTexture = null;
        }
    }

    [ContextMenu("Refresh Map")]
    public void RefreshMap()
    {
        AutoFindReferences();
        EnsureUI();
        RebuildTextureIfNeeded();

        if (minimapTexture == null)
        {
            return;
        }

        ClearTexture();
        DrawDungeonTiles();

        if (showPlaceableObjects)
        {
            DrawPlaceableObjects();
        }

        if (showHeroes)
        {
            DrawHeroes();
        }

        if (showCameraView)
        {
            DrawCameraView();
        }

        minimapTexture.Apply(false);
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
    }

    private void RebuildTextureIfNeeded()
    {
        int width = MapWidth;
        int height = MapHeight;

        if (minimapTexture != null
            && minimapTexture.width == width
            && minimapTexture.height == height)
        {
            return;
        }

        if (minimapTexture != null)
        {
            Destroy(minimapTexture);
            minimapTexture = null;
        }

        minimapTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        minimapTexture.filterMode = FilterMode.Point;
        minimapTexture.wrapMode = TextureWrapMode.Clamp;

        if (mapImage != null)
        {
            mapImage.texture = minimapTexture;
        }

        DebugLog("Minimap texture rebuilt: " + width + " x " + height);
    }

    private void ClearTexture()
    {
        for (int y = 0; y < minimapTexture.height; y++)
        {
            for (int x = 0; x < minimapTexture.width; x++)
            {
                minimapTexture.SetPixel(x, y, emptyColor);
            }
        }
    }

    private void DrawDungeonTiles()
    {
        if (dungeonGridManager == null)
        {
            return;
        }

        for (int gridY = minGridY; gridY <= maxGridY; gridY++)
        {
            for (int gridX = minGridX; gridX <= maxGridX; gridX++)
            {
                DungeonTile tile = dungeonGridManager.GetTileAtGridPosition(gridX, gridY);

                if (tile == null)
                {
                    continue;
                }

                Color pixelColor = emptyColor;

                if (tile.IsFloor)
                {
                    if (!showFloor)
                    {
                        continue;
                    }

                    pixelColor = floorColor;
                }
                else
                {
                    if (!showSoil)
                    {
                        continue;
                    }

                    pixelColor = soilColor;
                }

                if (showEntranceAndCore
                    && dungeonGridManager.IsEntranceOrCoreAtWorldPosition(tile.transform.position))
                {
                    pixelColor = entranceCoreColor;
                }

                SetGridPixel(gridX, gridY, pixelColor);
            }
        }
    }

    private void DrawPlaceableObjects()
    {
        PlaceableObject[] placeableObjects = FindObjectsByType<PlaceableObject>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (PlaceableObject placeableObject in placeableObjects)
        {
            if (placeableObject == null)
            {
                continue;
            }

            if (!placeableObject.countsAsOccupied)
            {
                continue;
            }

            if (TryWorldToGrid(placeableObject.transform.position, out int gridX, out int gridY))
            {
                DrawPoint(gridX, gridY, placeableColor, 0);
            }
        }
    }

    private void DrawHeroes()
    {
        HeroHealth[] heroes = FindObjectsByType<HeroHealth>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (HeroHealth hero in heroes)
        {
            if (hero == null)
            {
                continue;
            }

            if (TryWorldToGrid(hero.transform.position, out int gridX, out int gridY))
            {
                DrawPoint(gridX, gridY, heroColor, 0);
            }
        }
    }

    private void DrawCameraView()
    {
        if (targetCamera == null)
        {
            return;
        }

        float cameraDistance = Mathf.Abs(targetCamera.transform.position.z);

        Vector3 bottomLeft = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0f, cameraDistance));
        Vector3 topRight = targetCamera.ViewportToWorldPoint(new Vector3(1f, 1f, cameraDistance));

        if (!TryWorldToGrid(bottomLeft, out int minX, out int minY))
        {
            minX = Mathf.RoundToInt(bottomLeft.x);
            minY = Mathf.RoundToInt(bottomLeft.y);
        }

        if (!TryWorldToGrid(topRight, out int maxX, out int maxY))
        {
            maxX = Mathf.RoundToInt(topRight.x);
            maxY = Mathf.RoundToInt(topRight.y);
        }

        DrawGridRectOutline(minX, minY, maxX, maxY, cameraViewColor);
    }

    private bool TryWorldToGrid(Vector3 worldPosition, out int gridX, out int gridY)
    {
        gridX = 0;
        gridY = 0;

        if (dungeonGridManager != null
            && dungeonGridManager.TryGetGridPositionFromWorldPosition(worldPosition, out int x, out int y))
        {
            gridX = x;
            gridY = y;
            return IsInsideGridRange(gridX, gridY);
        }

        gridX = Mathf.RoundToInt(worldPosition.x);
        gridY = Mathf.RoundToInt(worldPosition.y);

        return IsInsideGridRange(gridX, gridY);
    }

    private bool IsInsideGridRange(int gridX, int gridY)
    {
        return gridX >= minGridX
            && gridX <= maxGridX
            && gridY >= minGridY
            && gridY <= maxGridY;
    }

    private void DrawPoint(int gridX, int gridY, Color color, int radius)
    {
        for (int offsetY = -radius; offsetY <= radius; offsetY++)
        {
            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                int targetX = gridX + offsetX;
                int targetY = gridY + offsetY;

                if (!IsInsideGridRange(targetX, targetY))
                {
                    continue;
                }

                SetGridPixel(targetX, targetY, color);
            }
        }
    }

    private void DrawGridRectOutline(int gridMinX, int gridMinY, int gridMaxX, int gridMaxY, Color color)
    {
        int left = Mathf.Min(gridMinX, gridMaxX);
        int right = Mathf.Max(gridMinX, gridMaxX);
        int bottom = Mathf.Min(gridMinY, gridMaxY);
        int top = Mathf.Max(gridMinY, gridMaxY);

        left = Mathf.Clamp(left, minGridX, maxGridX);
        right = Mathf.Clamp(right, minGridX, maxGridX);
        bottom = Mathf.Clamp(bottom, minGridY, maxGridY);
        top = Mathf.Clamp(top, minGridY, maxGridY);

        for (int x = left; x <= right; x++)
        {
            SetGridPixel(x, bottom, color);
            SetGridPixel(x, top, color);
        }

        for (int y = bottom; y <= top; y++)
        {
            SetGridPixel(left, y, color);
            SetGridPixel(right, y, color);
        }
    }

    private void SetGridPixel(int gridX, int gridY, Color color)
    {
        int pixelX = gridX - minGridX;
        int pixelY = gridY - minGridY;

        if (pixelX < 0
            || pixelX >= minimapTexture.width
            || pixelY < 0
            || pixelY >= minimapTexture.height)
        {
            return;
        }

        minimapTexture.SetPixel(pixelX, pixelY, color);
    }

    private void EnsureUI()
    {
        if (!autoCreateUI)
        {
            return;
        }

        EnsureCanvas();
        EnsurePanel();
    }

    private void EnsureCanvas()
    {
        if (targetCanvas != null)
        {
            return;
        }

        targetCanvas = FindFirstObjectByType<Canvas>();

        if (targetCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("MinimapCanvas");
        targetCanvas = canvasObject.AddComponent<Canvas>();
        targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        targetCanvas.sortingOrder = 5200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();
    }

    private void EnsurePanel()
    {
        if (panelRoot != null && panelBackground != null && mapImage != null)
        {
            return;
        }

        GameObject panelObject = new GameObject("MinimapPanel");
        panelObject.transform.SetParent(targetCanvas.transform, false);

        panelRoot = panelObject.AddComponent<RectTransform>();
        panelRoot.anchorMin = new Vector2(0f, 0f);
        panelRoot.anchorMax = new Vector2(0f, 0f);
        panelRoot.pivot = new Vector2(0f, 0f);
        panelRoot.anchoredPosition = panelAnchoredPosition;
        panelRoot.sizeDelta = panelSize;

        panelBackground = panelObject.AddComponent<Image>();
        panelBackground.color = new Color(0.03f, 0.04f, 0.06f, 0.78f);
        panelBackground.raycastTarget = false;

        if (showTitle)
        {
            titleText = CreateText(
                panelRoot,
                "MinimapTitleText",
                new Vector2(0f, -10f),
                new Vector2(panelSize.x, 28f),
                18,
                TextAlignmentOptions.Center,
                new Color(0.85f, 0.96f, 1f, 1f)
            );

            titleText.text = titleLabel;
        }

        GameObject mapObject = new GameObject("MinimapImage");
        mapObject.transform.SetParent(panelRoot, false);

        RectTransform mapRect = mapObject.AddComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0f, 0f);
        mapRect.anchorMax = new Vector2(1f, 1f);
        mapRect.pivot = new Vector2(0.5f, 0.5f);
        mapRect.offsetMin = new Vector2(mapPadding.x, mapPadding.x);
        mapRect.offsetMax = new Vector2(-mapPadding.x, -mapPadding.y);

        mapImage = mapObject.AddComponent<RawImage>();
        mapImage.color = Color.white;
        mapImage.raycastTarget = false;
    }

    private TMP_Text CreateText(
        RectTransform parent,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size,
        int fontSize,
        TextAlignmentOptions alignment,
        Color color
    )
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = "";
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = false;

        return text;
    }

    private void DebugLog(string message)
    {
        if (!showDebugLog)
        {
            return;
        }

        Debug.Log("MinimapUI: " + message);
    }
}