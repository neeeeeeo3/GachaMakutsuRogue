using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class DungeonTile : MonoBehaviour
{
    public enum TileState
    {
        Soil,
        Floor
    }

    public enum SoilType
    {
        Normal,
        Rich,
        Hard
    }

    public static event System.Action<Vector3> OnAnyTileDug;
    public static event System.Action<Vector3, SoilType> OnAnyTileDugWithSoilType;
    public static event System.Action<Vector3, SoilType> OnAnyTileDigStartedWithSoilType;

    [Header("State")]
    public TileState currentState = TileState.Soil;
    public bool canDig = true;

    [Header("Legacy Dig Cost")]
    public int digManaCost = 1;

    [Header("Soil Type")]
    public SoilType soilType = SoilType.Normal;

    public Color soilColor = new Color(0.30f, 0.18f, 0.10f, 1f);
    public Color richSoilColor = new Color(0.72f, 0.52f, 0.18f, 1f);
    public Color hardSoilColor = new Color(0.06f, 0.10f, 0.18f, 1f);
    public Color floorColor = new Color(0.62f, 0.46f, 0.28f, 1f);

    public int normalDigCost = 1;
    public int richDigCost = 1;
    public int hardDigCost = 2;

    public int richSoilManaBonus = 1;

    [Header("Dig Feel")]
    public bool useDigDelay = true;
    public float normalDigDelay = 0.10f;
    public float richDigDelay = 0.10f;
    public float hardDigDelay = 0.34f;

    [Header("Dig Connection Rule")]
    public bool requireAdjacentFloorToDig = true;
    public bool allowDiagonalDigConnection = false;

    private SpriteRenderer spriteRenderer;
    private bool lookWasForced;
    private bool isBeingDug;

    public bool IsFloor
    {
        get
        {
            return currentState == TileState.Floor;
        }
    }

    public bool IsSoil
    {
        get
        {
            return currentState == TileState.Soil;
        }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        ApplyCurrentLook();
    }

    private void OnMouseDown()
    {
        TryDig();
    }

    public void TryDig()
    {
        if (IsPointerOverUI())
        {
            return;
        }

        if (!canDig)
        {
            return;
        }

        if (IsFloor)
        {
            return;
        }

        if (isBeingDug)
        {
            return;
        }

        if (RunManager.Instance != null && !RunManager.Instance.IsDungeonBuildPhase())
        {
            return;
        }

        if (RunManager.Instance != null && RunManager.Instance.isUpgradeSelectionActive)
        {
            return;
        }

        if (RemoveModeManager.Instance != null && RemoveModeManager.Instance.IsRemoveModeActive)
        {
            return;
        }

        if (CorePlacementManager.Instance != null && CorePlacementManager.Instance.IsCorePlacementModeActive)
        {
            return;
        }

        GachaManager gachaManager = FindFirstObjectByType<GachaManager>();

        if (gachaManager != null && gachaManager.HasPendingCapsule())
        {
            return;
        }

        if (!CanDigByConnectionRule())
        {
            Debug.Log("Cannot dig here. Dig next to an existing tunnel.");
            return;
        }

        int finalDigCost = GetCurrentDigCost();
        SoilType targetSoilType = soilType;

        if (RunManager.Instance != null)
        {
            if (!RunManager.Instance.SpendMana(finalDigCost))
            {
                return;
            }
        }
        else if (finalDigCost > 0)
        {
            Debug.LogWarning("RunManager not found. Cannot spend mana.");
            return;
        }

        StartCoroutine(DigWithDelayRoutine(targetSoilType));
    }

    private IEnumerator DigWithDelayRoutine(SoilType targetSoilType)
    {
        isBeingDug = true;

        OnAnyTileDigStartedWithSoilType?.Invoke(transform.position, targetSoilType);

        float delay = GetCurrentDigDelay(targetSoilType);

        if (useDigDelay && delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        ApplySoilDigReward();
        Dig();

        isBeingDug = false;
    }

    public void Dig()
    {
        SoilType dugSoilType = soilType;

        currentState = TileState.Floor;
        canDig = false;
        lookWasForced = false;

        ApplyCurrentLook();

        OnAnyTileDug?.Invoke(transform.position);
        OnAnyTileDugWithSoilType?.Invoke(transform.position, dugSoilType);

        Debug.Log("Tile dug: " + transform.position);
    }

    public void SetSoilType(SoilType newSoilType)
    {
        soilType = newSoilType;
        ApplySoilTypeLook();
    }

    public void SetTileState(TileState newState)
    {
        currentState = newState;

        if (currentState == TileState.Floor)
        {
            canDig = false;
        }
        else
        {
            canDig = true;
        }

        lookWasForced = false;
        ApplyCurrentLook();
    }

    public void SetState(TileState newState)
    {
        SetTileState(newState);
    }

    public void ForceFloor()
    {
        SetTileState(TileState.Floor);
    }

    public void ForceSoil()
    {
        SetTileState(TileState.Soil);
    }

    public void ForceColor(Color color)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        lookWasForced = true;
        spriteRenderer.color = color;
    }

    public void ApplyCurrentLook()
    {
        if (lookWasForced)
        {
            return;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        if (IsFloor)
        {
            spriteRenderer.color = floorColor;
            return;
        }

        ApplySoilTypeLook();
    }

    public void ApplySoilTypeLook()
    {
        if (IsFloor)
        {
            return;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        lookWasForced = false;

        if (soilType == SoilType.Rich)
        {
            spriteRenderer.color = richSoilColor;
        }
        else if (soilType == SoilType.Hard)
        {
            spriteRenderer.color = hardSoilColor;
        }
        else
        {
            spriteRenderer.color = soilColor;
        }
    }

    private int GetCurrentDigCost()
    {
        if (soilType == SoilType.Hard)
        {
            return hardDigCost;
        }

        if (soilType == SoilType.Rich)
        {
            return richDigCost;
        }

        return normalDigCost;
    }

    private float GetCurrentDigDelay(SoilType targetSoilType)
    {
        if (targetSoilType == SoilType.Hard)
        {
            return hardDigDelay;
        }

        if (targetSoilType == SoilType.Rich)
        {
            return richDigDelay;
        }

        return normalDigDelay;
    }

    private void ApplySoilDigReward()
    {
        if (soilType != SoilType.Rich)
        {
            return;
        }

        if (RunManager.Instance != null)
        {
            RunManager.Instance.AddMana(richSoilManaBonus);
        }

        Debug.Log("Rich soil dug. Mana +" + richSoilManaBonus);
    }

    private bool CanDigByConnectionRule()
    {
        if (!requireAdjacentFloorToDig)
        {
            return true;
        }

        if (IsFloor)
        {
            return false;
        }

        DungeonGridManager gridManager = FindFirstObjectByType<DungeonGridManager>();

        if (gridManager == null)
        {
            Debug.LogWarning("DungeonGridManager not found. Cannot check dig connection rule.");
            return false;
        }

        if (!gridManager.TryGetGridPositionFromWorldPosition(transform.position, out int x, out int y))
        {
            return false;
        }

        Vector2Int currentPosition = new Vector2Int(x, y);

        if (HasFloorNeighbor(gridManager, currentPosition, new Vector2Int(1, 0)))
        {
            return true;
        }

        if (HasFloorNeighbor(gridManager, currentPosition, new Vector2Int(-1, 0)))
        {
            return true;
        }

        if (HasFloorNeighbor(gridManager, currentPosition, new Vector2Int(0, 1)))
        {
            return true;
        }

        if (HasFloorNeighbor(gridManager, currentPosition, new Vector2Int(0, -1)))
        {
            return true;
        }

        if (allowDiagonalDigConnection)
        {
            if (HasFloorNeighbor(gridManager, currentPosition, new Vector2Int(1, 1)))
            {
                return true;
            }

            if (HasFloorNeighbor(gridManager, currentPosition, new Vector2Int(1, -1)))
            {
                return true;
            }

            if (HasFloorNeighbor(gridManager, currentPosition, new Vector2Int(-1, 1)))
            {
                return true;
            }

            if (HasFloorNeighbor(gridManager, currentPosition, new Vector2Int(-1, -1)))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasFloorNeighbor(DungeonGridManager gridManager, Vector2Int currentPosition, Vector2Int direction)
    {
        Vector2Int neighborPosition = currentPosition + direction;
        DungeonTile neighborTile = gridManager.GetTileAtGridPosition(neighborPosition);

        if (neighborTile == null)
        {
            return false;
        }

        return neighborTile.IsFloor;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject();
    }
}