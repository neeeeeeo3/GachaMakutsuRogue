using System.Collections.Generic;
using UnityEngine;

public class DungeonGridManager : MonoBehaviour
{
    public static DungeonGridManager Instance { get; private set; }

    [Header("Grid Size")]
    public int width = 10;
    public int height = 6;
    public float tileSize = 1f;

    [Header("Grid Position")]
    public Vector2 origin = new Vector2(-4.5f, -2.5f);

    [Header("Entrance / Core")]
    public bool forceEntranceToTopCenter = true;
    public int topEntranceXOffset = 0;

    public Vector2Int entranceGridPosition = new Vector2Int(5, 5);
    public Vector2Int coreGridPosition = new Vector2Int(9, 3);
    public bool hasCorePlaced = false;

    [Header("Dig Cost")]
    public int digManaCost = 1;

    [Header("Tile Look")]
    public Color soilColor = new Color(0.35f, 0.22f, 0.12f, 1f);
    public Color floorColor = new Color(0.18f, 0.18f, 0.2f, 1f);
    public Color entranceColor = new Color(0.15f, 0.45f, 1f, 1f);
    public Color coreTileColor = new Color(1f, 0.2f, 0.35f, 1f);

    [Header("Sorting")]
    public int sortingOrder = -40;

    private DungeonTile[,] tiles;
    private Sprite squareSprite;

    private readonly Vector2Int[] directions =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

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
        GenerateGrid();
    }

    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        ClearOldTiles();

        if (forceEntranceToTopCenter)
        {
            entranceGridPosition = GetTopCenterEntranceGridPosition();
        }

        entranceGridPosition = ClampGridPosition(entranceGridPosition);
        coreGridPosition = ClampGridPosition(coreGridPosition);

        hasCorePlaced = false;

        squareSprite = CreateSquareSprite();
        tiles = new DungeonTile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CreateTile(x, y);
            }
        }

        ForceEntranceTile();

        Debug.Log("Dungeon grid generated. Entrance: " + entranceGridPosition + " / Core is not placed yet.");
    }

    public Vector2Int GetTopCenterEntranceGridPosition()
    {
        int centerX = Mathf.Clamp((width / 2) + topEntranceXOffset, 0, width - 1);
        int topY = Mathf.Max(0, height - 1);

        return new Vector2Int(centerX, topY);
    }

    public float GetGridLeftEdgeWorldX()
    {
        return origin.x - tileSize * 0.5f;
    }

    public float GetGridRightEdgeWorldX()
    {
        return origin.x + (width - 1) * tileSize + tileSize * 0.5f;
    }

    public float GetGridTopEdgeWorldY()
    {
        return origin.y + (height - 1) * tileSize + tileSize * 0.5f;
    }

    public float GetGridBottomEdgeWorldY()
    {
        return origin.y - tileSize * 0.5f;
    }

    public float GetGridCenterWorldX()
    {
        return (GetGridLeftEdgeWorldX() + GetGridRightEdgeWorldX()) * 0.5f;
    }

    public float GetGridCenterWorldY()
    {
        return (GetGridTopEdgeWorldY() + GetGridBottomEdgeWorldY()) * 0.5f;
    }

    public Vector2 GetGridWorldSize()
    {
        return new Vector2(
            GetGridRightEdgeWorldX() - GetGridLeftEdgeWorldX(),
            GetGridTopEdgeWorldY() - GetGridBottomEdgeWorldY()
        );
    }

    public DungeonTile GetTileAtGridPosition(int x, int y)
    {
        if (tiles == null)
        {
            return null;
        }

        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return null;
        }

        return tiles[x, y];
    }

    public DungeonTile GetTileAtGridPosition(Vector2Int gridPosition)
    {
        return GetTileAtGridPosition(gridPosition.x, gridPosition.y);
    }

    public DungeonTile GetTileAtWorldPosition(Vector3 worldPosition)
    {
        if (!TryGetGridPositionFromWorldPosition(worldPosition, out int x, out int y))
        {
            return null;
        }

        return GetTileAtGridPosition(x, y);
    }

    public bool IsEntranceGridPosition(Vector2Int gridPosition)
    {
        return gridPosition == entranceGridPosition;
    }

    public bool IsCoreGridPosition(Vector2Int gridPosition)
    {
        return hasCorePlaced && gridPosition == coreGridPosition;
    }

    public bool IsEntranceOrCoreAtWorldPosition(Vector3 worldPosition)
    {
        if (!TryGetGridPositionFromWorldPosition(worldPosition, out int x, out int y))
        {
            return false;
        }

        Vector2Int gridPosition = new Vector2Int(x, y);

        if (IsEntranceGridPosition(gridPosition))
        {
            return true;
        }

        if (IsCoreGridPosition(gridPosition))
        {
            return true;
        }

        return false;
    }

    public bool IsFloorAtWorldPosition(Vector3 worldPosition)
    {
        DungeonTile tile = GetTileAtWorldPosition(worldPosition);

        if (tile == null)
        {
            return false;
        }

        return tile.IsFloor;
    }

    public bool IsTileOccupiedAtWorldPosition(Vector3 worldPosition)
    {
        if (!TryGetGridPositionFromWorldPosition(worldPosition, out int targetX, out int targetY))
        {
            return false;
        }

        PlaceableObject[] placeableObjects = FindObjectsByType<PlaceableObject>(FindObjectsSortMode.None);

        foreach (PlaceableObject placeableObject in placeableObjects)
        {
            if (placeableObject == null)
            {
                continue;
            }

            if (!placeableObject.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!placeableObject.countsAsOccupied)
            {
                continue;
            }

            if (!TryGetGridPositionFromWorldPosition(placeableObject.transform.position, out int objectX, out int objectY))
            {
                continue;
            }

            if (objectX == targetX && objectY == targetY)
            {
                return true;
            }
        }

        return false;
    }

    public bool TrySetCoreAtWorldPosition(Vector3 worldPosition)
    {
        if (!TryGetGridPositionFromWorldPosition(worldPosition, out int x, out int y))
        {
            return false;
        }

        return TrySetCoreAtGridPosition(new Vector2Int(x, y));
    }

    public bool TrySetCoreAtGridPosition(Vector2Int newCoreGridPosition)
    {
        newCoreGridPosition = ClampGridPosition(newCoreGridPosition);

        if (newCoreGridPosition == entranceGridPosition)
        {
            Debug.Log("Cannot set core on entrance.");
            return false;
        }

        DungeonTile newCoreTile = GetTileAtGridPosition(newCoreGridPosition);

        if (newCoreTile == null)
        {
            return false;
        }

        if (!newCoreTile.IsFloor)
        {
            Debug.Log("Core can be placed only on dug floor.");
            return false;
        }

        Vector3 newCoreWorldPosition = GetWorldPositionFromGridPosition(newCoreGridPosition);

        if (IsTileOccupiedAtWorldPosition(newCoreWorldPosition))
        {
            Debug.Log("Core cannot be placed on occupied tile.");
            return false;
        }

        if (hasCorePlaced)
        {
            Vector2Int oldCoreGridPosition = coreGridPosition;
            DungeonTile oldCoreTile = GetTileAtGridPosition(oldCoreGridPosition);

            if (oldCoreTile != null && oldCoreGridPosition != entranceGridPosition)
            {
                oldCoreTile.SetState(DungeonTile.TileState.Floor);
                oldCoreTile.canDig = false;
                oldCoreTile.ForceColor(floorColor);
                oldCoreTile.gameObject.name = "DungeonTile_" + oldCoreGridPosition.x + "_" + oldCoreGridPosition.y;
            }
        }

        coreGridPosition = newCoreGridPosition;
        hasCorePlaced = true;

        ForceCoreTile();
        ForceEntranceTile();

        Debug.Log("Core grid position changed to: " + coreGridPosition);

        return true;
    }

    public Vector3 SnapWorldPositionToTileCenter(Vector3 worldPosition)
    {
        if (!TryGetGridPositionFromWorldPosition(worldPosition, out int x, out int y))
        {
            return worldPosition;
        }

        return GetWorldPositionFromGridPosition(x, y);
    }

    public Vector3 GetWorldPositionFromGridPosition(int x, int y)
    {
        return new Vector3(
            origin.x + x * tileSize,
            origin.y + y * tileSize,
            0f
        );
    }

    public Vector3 GetWorldPositionFromGridPosition(Vector2Int gridPosition)
    {
        return GetWorldPositionFromGridPosition(gridPosition.x, gridPosition.y);
    }

    public Vector3 GetEntranceWorldPosition()
    {
        return GetWorldPositionFromGridPosition(entranceGridPosition);
    }

    public Vector3 GetCoreWorldPosition()
    {
        return GetWorldPositionFromGridPosition(coreGridPosition);
    }

    public bool TryGetGridPositionFromWorldPosition(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.RoundToInt((worldPosition.x - origin.x) / tileSize);
        y = Mathf.RoundToInt((worldPosition.y - origin.y) / tileSize);

        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return false;
        }

        return true;
    }

    public bool TryFindPathFromEntranceToCore(out List<Vector3> worldPath)
    {
        worldPath = new List<Vector3>();

        if (tiles == null)
        {
            Debug.LogWarning("DungeonGridManager: tiles are not generated.");
            return false;
        }

        if (!hasCorePlaced)
        {
            Debug.Log("No core placed. Set core before starting defense.");
            return false;
        }

        Vector2Int start = entranceGridPosition;
        Vector2Int goal = coreGridPosition;

        if (!IsWalkable(start))
        {
            Debug.LogWarning("Entrance tile is not walkable.");
            return false;
        }

        if (!IsWalkable(goal))
        {
            Debug.LogWarning("Core tile is not walkable.");
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
            Debug.Log("No path from entrance to core. Dig a connected tunnel first.");
            return false;
        }

        List<Vector2Int> gridPath = new List<Vector2Int>();
        Vector2Int pathPosition = goal;

        gridPath.Add(pathPosition);

        while (pathPosition != start)
        {
            pathPosition = cameFrom[pathPosition];
            gridPath.Add(pathPosition);
        }

        gridPath.Reverse();

        foreach (Vector2Int gridPosition in gridPath)
        {
            worldPath.Add(GetWorldPositionFromGridPosition(gridPosition));
        }

        Debug.Log("Path found. Length: " + worldPath.Count);

        return true;
    }

    private bool IsWalkable(Vector2Int gridPosition)
    {
        DungeonTile tile = GetTileAtGridPosition(gridPosition);

        if (tile == null)
        {
            return false;
        }

        return tile.IsFloor;
    }

    private void CreateTile(int x, int y)
    {
        GameObject tileObject = new GameObject("DungeonTile_" + x + "_" + y);
        tileObject.transform.SetParent(transform);

        Vector3 position = GetWorldPositionFromGridPosition(x, y);

        tileObject.transform.position = position;
        tileObject.transform.localScale = new Vector3(tileSize * 0.95f, tileSize * 0.95f, 1f);

        SpriteRenderer spriteRenderer = tileObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = squareSprite;
        spriteRenderer.color = soilColor;
        spriteRenderer.sortingOrder = sortingOrder;

        BoxCollider2D boxCollider = tileObject.AddComponent<BoxCollider2D>();
        boxCollider.size = Vector2.one;

        DungeonTile tile = tileObject.AddComponent<DungeonTile>();
        tile.soilColor = soilColor;
        tile.floorColor = floorColor;
        tile.digManaCost = digManaCost;
        tile.SetState(DungeonTile.TileState.Soil);

        tiles[x, y] = tile;
    }

    private void ForceEntranceTile()
    {
        DungeonTile tile = GetTileAtGridPosition(entranceGridPosition);

        if (tile == null)
        {
            return;
        }

        tile.SetState(DungeonTile.TileState.Floor);
        tile.canDig = false;
        tile.ForceColor(entranceColor);
        tile.gameObject.name = "DungeonTile_ENTRANCE";
    }

    private void ForceCoreTile()
    {
        if (!hasCorePlaced)
        {
            return;
        }

        DungeonTile tile = GetTileAtGridPosition(coreGridPosition);

        if (tile == null)
        {
            return;
        }

        tile.SetState(DungeonTile.TileState.Floor);
        tile.canDig = false;
        tile.ForceColor(coreTileColor);
        tile.gameObject.name = "DungeonTile_CORE";
    }

    private Vector2Int ClampGridPosition(Vector2Int gridPosition)
    {
        int clampedX = Mathf.Clamp(gridPosition.x, 0, width - 1);
        int clampedY = Mathf.Clamp(gridPosition.y, 0, height - 1);

        return new Vector2Int(clampedX, clampedY);
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.name = "DungeonTileSquareTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );
    }

    private void ClearOldTiles()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject childObject = transform.GetChild(i).gameObject;

            if (Application.isPlaying)
            {
                Destroy(childObject);
            }
            else
            {
                DestroyImmediate(childObject);
            }
        }
    }
}