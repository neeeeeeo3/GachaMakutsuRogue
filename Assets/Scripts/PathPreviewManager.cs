using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PathPreviewManager : MonoBehaviour
{
    private enum PathState
    {
        NoCore,
        NoPath,
        HasPath
    }

    [Header("References")]
    public DungeonGridManager dungeonGridManager;
    public bool autoFindDungeonGridManager = true;

    [Header("UI")]
    public TMP_Text pathStatusText;
    public bool showPathStatusText = true;

    [Header("Visibility")]
    public bool showOnlyDuringDungeonBuildPhase = true;
    public bool hideWhilePlacingCapsule = true;
    public bool hideDuringRemoveMode = true;
    public bool hideDuringCorePlacementMode = true;

    [Header("Look")]
    public Color pathColor = new Color(1f, 0.9f, 0.15f, 0.55f);
    public Color entrancePathColor = new Color(0.2f, 0.65f, 1f, 0.65f);
    public Color corePathColor = new Color(1f, 0.2f, 0.35f, 0.65f);
    public float cellScaleMultiplier = 0.62f;
    public int sortingOrder = -12;

    [Header("Blink")]
    public bool blinkPath = true;
    public float blinkSpeed = 3.5f;
    public float minAlphaMultiplier = 0.45f;
    public float maxAlphaMultiplier = 1.0f;

    [Header("Update")]
    public float refreshInterval = 0.15f;

    private readonly List<GameObject> previewObjects = new List<GameObject>();
    private readonly List<SpriteRenderer> previewRenderers = new List<SpriteRenderer>();
    private readonly List<Color> previewBaseColors = new List<Color>();

    private readonly Vector2Int[] directions =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private Sprite squareSprite;
    private float refreshTimer;
    private PathState currentPathState = PathState.NoCore;
    private int currentPathLength;

    private void Start()
    {
        AutoFindReferences();
        squareSprite = CreateSquareSprite();
        RefreshPreview();
    }

    private void Update()
    {
        refreshTimer += Time.deltaTime;

        if (refreshTimer >= refreshInterval)
        {
            refreshTimer = 0f;
            RefreshPreview();
        }

        UpdateBlink();
    }

    public void RefreshPreview()
    {
        AutoFindReferences();

        if (!ShouldShowPreview())
        {
            ClearPreview();
            UpdateStatusText("");
            return;
        }

        if (dungeonGridManager == null)
        {
            ClearPreview();
            UpdateStatusText("ROUTE: NO GRID");
            return;
        }

        if (!dungeonGridManager.hasCorePlaced)
        {
            currentPathState = PathState.NoCore;
            currentPathLength = 0;

            ClearPreview();
            UpdateStatusText("ROUTE: NO CORE\nSET CORE FIRST");
            return;
        }

        if (!TryFindPathQuiet(out List<Vector2Int> gridPath))
        {
            currentPathState = PathState.NoPath;
            currentPathLength = 0;

            ClearPreview();
            UpdateStatusText("ROUTE: NO PATH\nDIG TUNNEL TO CORE");
            return;
        }

        currentPathState = PathState.HasPath;
        currentPathLength = gridPath.Count;

        DrawPath(gridPath);
        UpdateStatusText("ROUTE OK\nLENGTH: " + currentPathLength);
    }

    private bool ShouldShowPreview()
    {
        if (showOnlyDuringDungeonBuildPhase)
        {
            if (RunManager.Instance != null && !RunManager.Instance.IsDungeonBuildPhase())
            {
                return false;
            }
        }

        if (hideWhilePlacingCapsule)
        {
            GachaManager gachaManager = FindFirstObjectByType<GachaManager>();

            if (gachaManager != null && gachaManager.HasPendingCapsule())
            {
                return false;
            }
        }

        if (hideDuringRemoveMode)
        {
            if (RemoveModeManager.Instance != null && RemoveModeManager.Instance.IsRemoveModeActive)
            {
                return false;
            }
        }

        if (hideDuringCorePlacementMode)
        {
            if (CorePlacementManager.Instance != null && CorePlacementManager.Instance.IsCorePlacementModeActive)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryFindPathQuiet(out List<Vector2Int> gridPath)
    {
        gridPath = new List<Vector2Int>();

        if (dungeonGridManager == null)
        {
            return false;
        }

        if (!dungeonGridManager.hasCorePlaced)
        {
            return false;
        }

        Vector2Int start = dungeonGridManager.entranceGridPosition;
        Vector2Int goal = dungeonGridManager.coreGridPosition;

        if (!IsWalkable(start))
        {
            return false;
        }

        if (!IsWalkable(goal))
        {
            return false;
        }

        Queue<Vector2Int> openQueue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        openQueue.Enqueue(start);
        visited.Add(start);

        bool found = false;

        while (openQueue.Count > 0)
        {
            Vector2Int current = openQueue.Dequeue();

            if (current == goal)
            {
                found = true;
                break;
            }

            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;

                if (visited.Contains(next))
                {
                    continue;
                }

                if (!IsWalkable(next))
                {
                    continue;
                }

                visited.Add(next);
                cameFrom[next] = current;
                openQueue.Enqueue(next);
            }
        }

        if (!found)
        {
            return false;
        }

        Vector2Int pathPosition = goal;
        gridPath.Add(pathPosition);

        while (pathPosition != start)
        {
            pathPosition = cameFrom[pathPosition];
            gridPath.Add(pathPosition);
        }

        gridPath.Reverse();

        return true;
    }

    private bool IsWalkable(Vector2Int gridPosition)
    {
        if (dungeonGridManager == null)
        {
            return false;
        }

        DungeonTile tile = dungeonGridManager.GetTileAtGridPosition(gridPosition);

        if (tile == null)
        {
            return false;
        }

        return tile.IsFloor;
    }

    private void DrawPath(List<Vector2Int> gridPath)
    {
        ClearPreview();

        if (gridPath == null || gridPath.Count <= 0)
        {
            return;
        }

        if (squareSprite == null)
        {
            squareSprite = CreateSquareSprite();
        }

        for (int i = 0; i < gridPath.Count; i++)
        {
            Vector2Int gridPosition = gridPath[i];
            Vector3 worldPosition = dungeonGridManager.GetWorldPositionFromGridPosition(gridPosition);

            Color color = pathColor;

            if (gridPosition == dungeonGridManager.entranceGridPosition)
            {
                color = entrancePathColor;
            }
            else if (dungeonGridManager.hasCorePlaced && gridPosition == dungeonGridManager.coreGridPosition)
            {
                color = corePathColor;
            }

            CreatePathCell(worldPosition, color);
        }
    }

    private void CreatePathCell(Vector3 position, Color color)
    {
        GameObject cellObject = new GameObject("HeroPathPreviewCell");
        cellObject.transform.SetParent(transform);
        cellObject.transform.position = position;

        float tileSize = 1f;

        if (dungeonGridManager != null)
        {
            tileSize = dungeonGridManager.tileSize;
        }

        cellObject.transform.localScale = new Vector3(
            tileSize * cellScaleMultiplier,
            tileSize * cellScaleMultiplier,
            1f
        );

        SpriteRenderer spriteRenderer = cellObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = squareSprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;

        previewObjects.Add(cellObject);
        previewRenderers.Add(spriteRenderer);
        previewBaseColors.Add(color);
    }

    private void UpdateBlink()
    {
        if (!blinkPath)
        {
            return;
        }

        if (currentPathState != PathState.HasPath)
        {
            return;
        }

        float wave = Mathf.Sin(Time.time * blinkSpeed) * 0.5f + 0.5f;
        float alphaMultiplier = Mathf.Lerp(minAlphaMultiplier, maxAlphaMultiplier, wave);

        for (int i = 0; i < previewRenderers.Count; i++)
        {
            SpriteRenderer renderer = previewRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            Color baseColor = previewBaseColors[i];
            Color blinkColor = baseColor;
            blinkColor.a = baseColor.a * alphaMultiplier;

            renderer.color = blinkColor;
        }
    }

    private void UpdateStatusText(string message)
    {
        if (!showPathStatusText)
        {
            return;
        }

        if (pathStatusText == null)
        {
            return;
        }

        pathStatusText.text = message;
    }

    private void ClearPreview()
    {
        for (int i = previewObjects.Count - 1; i >= 0; i--)
        {
            if (previewObjects[i] != null)
            {
                Destroy(previewObjects[i]);
            }
        }

        previewObjects.Clear();
        previewRenderers.Clear();
        previewBaseColors.Clear();
    }

    private void AutoFindReferences()
    {
        if (!autoFindDungeonGridManager)
        {
            return;
        }

        if (dungeonGridManager == null)
        {
            dungeonGridManager = FindFirstObjectByType<DungeonGridManager>();
        }
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.name = "PathPreviewSquareTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );
    }
}