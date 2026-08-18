using System.Collections.Generic;
using UnityEngine;

public class PlacementAreaVisualizer : MonoBehaviour
{
    private enum CellVisualState
    {
        Placeable,
        SoilBlocked,
        OccupiedBlocked,
        SpecialBlocked
    }

    [Header("Auto Match Dungeon Grid")]
    public bool autoMatchDungeonGrid = true;
    public DungeonGridManager dungeonGridManager;
    public bool autoFindDungeonGridManager = true;

    [Header("Fallback Area")]
    public GachaManager gachaManager;
    public bool autoFindGachaManager = true;

    [Header("Cell Colors")]
    public Color placeableCellColor = new Color(0.2f, 1f, 0.35f, 0.28f);
    public Color soilBlockedCellColor = new Color(0.15f, 0.15f, 0.18f, 0.12f);
    public Color occupiedCellColor = new Color(1f, 0.55f, 0.1f, 0.35f);
    public Color specialBlockedCellColor = new Color(1f, 0.15f, 0.25f, 0.38f);

    [Header("Lines")]
    public Color lineColor = new Color(0.2f, 0.9f, 1f, 0.35f);
    public Color borderColor = new Color(0.4f, 1f, 1f, 0.8f);

    [Header("Sorting")]
    public int sortingOrder = -20;

    [Header("Options")]
    public bool visibleOnStart = false;
    public bool rebuildEveryTimeShown = true;
    public bool showBlockedCells = true;

    private readonly List<GameObject> visualObjects = new List<GameObject>();
    private Sprite squareSprite;
    private bool isVisible;

    private int lastWidth;
    private int lastHeight;
    private float lastTileSize;
    private Vector2 lastOrigin;

    private void Start()
    {
        AutoFindReferences();

        squareSprite = CreateSquareSprite();

        if (visibleOnStart)
        {
            ShowVisual();
        }
        else
        {
            HideVisual();
        }
    }

    public void ShowVisual()
    {
        AutoFindReferences();

        if (rebuildEveryTimeShown || IsDungeonGridChanged())
        {
            RebuildVisual();
        }

        SetVisualObjectsActive(true);
        isVisible = true;
    }

    public void HideVisual()
    {
        SetVisualObjectsActive(false);
        isVisible = false;
    }

    public void SetVisible(bool visible)
    {
        if (visible)
        {
            ShowVisual();
        }
        else
        {
            HideVisual();
        }
    }

    public void RebuildVisual()
    {
        ClearVisual();

        if (squareSprite == null)
        {
            squareSprite = CreateSquareSprite();
        }

        if (autoMatchDungeonGrid && GetDungeonGridManager() != null)
        {
            BuildFromDungeonGrid(GetDungeonGridManager());
        }
        else
        {
            BuildFromGachaManagerFallback();
        }

        SetVisualObjectsActive(isVisible);

        Debug.Log("Placement area visual rebuilt.");
    }

    private void BuildFromDungeonGrid(DungeonGridManager grid)
    {
        if (grid == null)
        {
            return;
        }

        lastWidth = grid.width;
        lastHeight = grid.height;
        lastTileSize = grid.tileSize;
        lastOrigin = grid.origin;

        for (int x = 0; x < grid.width; x++)
        {
            for (int y = 0; y < grid.height; y++)
            {
                Vector3 position = grid.GetWorldPositionFromGridPosition(x, y);
                CellVisualState state = GetCellVisualState(grid, position);

                if (!showBlockedCells && state != CellVisualState.Placeable)
                {
                    continue;
                }

                CreateCell(position, grid.tileSize, GetColorForState(state), state);
            }
        }

        float minX = grid.origin.x - grid.tileSize * 0.5f;
        float maxX = grid.origin.x + (grid.width - 1) * grid.tileSize + grid.tileSize * 0.5f;
        float minY = grid.origin.y - grid.tileSize * 0.5f;
        float maxY = grid.origin.y + (grid.height - 1) * grid.tileSize + grid.tileSize * 0.5f;

        CreateGridLines(minX, maxX, minY, maxY, grid.tileSize);
        CreateBorder(minX, maxX, minY, maxY);
    }

    private CellVisualState GetCellVisualState(DungeonGridManager grid, Vector3 position)
    {
        DungeonTile tile = grid.GetTileAtWorldPosition(position);

        if (tile == null)
        {
            return CellVisualState.SoilBlocked;
        }

        if (!tile.IsFloor)
        {
            return CellVisualState.SoilBlocked;
        }

        if (grid.IsEntranceOrCoreAtWorldPosition(position))
        {
            return CellVisualState.SpecialBlocked;
        }

        GachaManager manager = GetGachaManager();

        bool shouldCheckOccupied = true;

        if (manager != null)
        {
            shouldCheckOccupied = manager.requireEmptyTileToPlace;
        }

        if (shouldCheckOccupied && grid.IsTileOccupiedAtWorldPosition(position))
        {
            return CellVisualState.OccupiedBlocked;
        }

        return CellVisualState.Placeable;
    }

    private Color GetColorForState(CellVisualState state)
    {
        switch (state)
        {
            case CellVisualState.Placeable:
                return placeableCellColor;

            case CellVisualState.SoilBlocked:
                return soilBlockedCellColor;

            case CellVisualState.OccupiedBlocked:
                return occupiedCellColor;

            case CellVisualState.SpecialBlocked:
                return specialBlockedCellColor;

            default:
                return soilBlockedCellColor;
        }
    }

    private void BuildFromGachaManagerFallback()
    {
        GachaManager manager = GetGachaManager();

        if (manager == null)
        {
            return;
        }

        float gridSize = manager.gridSize;

        if (gridSize <= 0f)
        {
            gridSize = 1f;
        }

        float minX = manager.minPlaceX;
        float maxX = manager.maxPlaceX;
        float minY = manager.minPlaceY;
        float maxY = manager.maxPlaceY;

        for (float x = minX; x <= maxX; x += gridSize)
        {
            for (float y = minY; y <= maxY; y += gridSize)
            {
                CreateCell(
                    new Vector3(x, y, 0f),
                    gridSize,
                    placeableCellColor,
                    CellVisualState.Placeable
                );
            }
        }

        CreateGridLines(
            minX - gridSize * 0.5f,
            maxX + gridSize * 0.5f,
            minY - gridSize * 0.5f,
            maxY + gridSize * 0.5f,
            gridSize
        );

        CreateBorder(
            minX - gridSize * 0.5f,
            maxX + gridSize * 0.5f,
            minY - gridSize * 0.5f,
            maxY + gridSize * 0.5f
        );
    }

    private void CreateCell(Vector3 position, float size, Color color, CellVisualState state)
    {
        GameObject cellObject = new GameObject("PlacementCell_" + state);
        cellObject.transform.SetParent(transform);
        cellObject.transform.position = position;

        float scaleMultiplier = 0.92f;

        if (state == CellVisualState.SoilBlocked)
        {
            scaleMultiplier = 0.84f;
        }

        if (state == CellVisualState.OccupiedBlocked)
        {
            scaleMultiplier = 0.72f;
        }

        if (state == CellVisualState.SpecialBlocked)
        {
            scaleMultiplier = 0.82f;
        }

        cellObject.transform.localScale = new Vector3(size * scaleMultiplier, size * scaleMultiplier, 1f);

        SpriteRenderer spriteRenderer = cellObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = squareSprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;

        visualObjects.Add(cellObject);
    }

    private void CreateGridLines(float minX, float maxX, float minY, float maxY, float gridSize)
    {
        for (float x = minX; x <= maxX + 0.01f; x += gridSize)
        {
            CreateLine(
                new Vector3(x, (minY + maxY) * 0.5f, 0f),
                new Vector3(0.025f, maxY - minY, 1f),
                lineColor,
                sortingOrder + 1,
                "PlacementGridLineVertical"
            );
        }

        for (float y = minY; y <= maxY + 0.01f; y += gridSize)
        {
            CreateLine(
                new Vector3((minX + maxX) * 0.5f, y, 0f),
                new Vector3(maxX - minX, 0.025f, 1f),
                lineColor,
                sortingOrder + 1,
                "PlacementGridLineHorizontal"
            );
        }
    }

    private void CreateBorder(float minX, float maxX, float minY, float maxY)
    {
        float width = maxX - minX;
        float height = maxY - minY;
        float thickness = 0.07f;

        CreateLine(
            new Vector3((minX + maxX) * 0.5f, maxY, 0f),
            new Vector3(width, thickness, 1f),
            borderColor,
            sortingOrder + 2,
            "PlacementBorderTop"
        );

        CreateLine(
            new Vector3((minX + maxX) * 0.5f, minY, 0f),
            new Vector3(width, thickness, 1f),
            borderColor,
            sortingOrder + 2,
            "PlacementBorderBottom"
        );

        CreateLine(
            new Vector3(minX, (minY + maxY) * 0.5f, 0f),
            new Vector3(thickness, height, 1f),
            borderColor,
            sortingOrder + 2,
            "PlacementBorderLeft"
        );

        CreateLine(
            new Vector3(maxX, (minY + maxY) * 0.5f, 0f),
            new Vector3(thickness, height, 1f),
            borderColor,
            sortingOrder + 2,
            "PlacementBorderRight"
        );
    }

    private void CreateLine(Vector3 position, Vector3 scale, Color color, int order, string objectName)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(transform);
        lineObject.transform.position = position;
        lineObject.transform.localScale = scale;

        SpriteRenderer spriteRenderer = lineObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = squareSprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = order;

        visualObjects.Add(lineObject);
    }

    private bool IsDungeonGridChanged()
    {
        DungeonGridManager grid = GetDungeonGridManager();

        if (grid == null)
        {
            return false;
        }

        if (lastWidth != grid.width)
        {
            return true;
        }

        if (lastHeight != grid.height)
        {
            return true;
        }

        if (Mathf.Abs(lastTileSize - grid.tileSize) > 0.001f)
        {
            return true;
        }

        if (Vector2.Distance(lastOrigin, grid.origin) > 0.001f)
        {
            return true;
        }

        return false;
    }

    private void AutoFindReferences()
    {
        GetDungeonGridManager();
        GetGachaManager();
    }

    private DungeonGridManager GetDungeonGridManager()
    {
        if (dungeonGridManager == null && autoFindDungeonGridManager)
        {
            dungeonGridManager = FindFirstObjectByType<DungeonGridManager>();
        }

        return dungeonGridManager;
    }

    private GachaManager GetGachaManager()
    {
        if (gachaManager == null && autoFindGachaManager)
        {
            gachaManager = FindFirstObjectByType<GachaManager>();
        }

        return gachaManager;
    }

    private void SetVisualObjectsActive(bool active)
    {
        foreach (GameObject visualObject in visualObjects)
        {
            if (visualObject != null)
            {
                visualObject.SetActive(active);
            }
        }
    }

    private void ClearVisual()
    {
        for (int i = visualObjects.Count - 1; i >= 0; i--)
        {
            if (visualObjects[i] != null)
            {
                Destroy(visualObjects[i]);
            }
        }

        visualObjects.Clear();
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.name = "PlacementAreaSquareTexture";
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